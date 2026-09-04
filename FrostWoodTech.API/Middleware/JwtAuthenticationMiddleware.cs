using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Middleware;

/// <summary>
/// Authorisation for <c>/api/cms/admin/*</c> lives here rather than per function, so a new admin
/// endpoint is protected the moment it is added. Function keys are not an auth system — every
/// trigger stays <c>AuthorizationLevel.Anonymous</c>.
/// </summary>
public sealed class JwtAuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private const string AdminPrefix = "/cms/admin/";

    /// <summary>
    /// An explicit allow-list, not a deny-list: forgetting to add a route here fails closed.
    /// </summary>
    private static readonly string[] AnonymousAdminPaths =
    [
        "/cms/admin/auth/register",
        "/cms/admin/auth/login",
        // Exchanging a Google token is how a caller gets its first access token.
        "/cms/admin/auth/google",
        // Both must work with a dead access token — that is the whole point of them.
        "/cms/admin/auth/refresh",
        "/cms/admin/auth/logout",
        // A caller has no token yet at either step of the verification flow.
        "/cms/admin/auth/verify-email",
        "/cms/admin/auth/resend-verification",
        // Neither has a token yet either, by definition.
        "/cms/admin/auth/forgot-password",
        "/cms/admin/auth/set-password"
    ];

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is null)
        {
            await next(context);
            return;
        }

        var path = Normalise(httpContext.Request.Path.Value);

        if (!path.StartsWith(AdminPrefix, StringComparison.Ordinal)
            || AnonymousAdminPaths.Contains(path, StringComparer.Ordinal))
        {
            await next(context);
            return;
        }

        var token = ReadBearerToken(httpContext.Request);
        if (token is null)
        {
            await Unauthorized(httpContext, "unauthenticated", "An Authorization: Bearer token is required.");
            return;
        }

        var tokenService = context.InstanceServices.GetRequiredService<IJwtTokenService>();

        var validation = await new JsonWebTokenHandler()
            .ValidateTokenAsync(token, tokenService.CreateValidationParameters());

        if (!validation.IsValid
            || !Guid.TryParse(ReadClaim(validation.Claims, "sub"), out var userId))
        {
            await Unauthorized(httpContext, "invalid_token", "The access token is invalid or has expired.");
            return;
        }

        var currentUser = context.InstanceServices.GetRequiredService<CurrentUser>();
        currentUser.UserId = userId;
        currentUser.Email = ReadClaim(validation.Claims, "email");
        currentUser.Role = Enum.TryParse<UserRole>(ReadClaim(validation.Claims, "role"), out var role) ? role : null;

        await next(context);
    }

    /// <summary>Routes are declared without the <c>api/</c> prefix, so compare without it too.</summary>
    private static string Normalise(string? path)
    {
        path = (path ?? string.Empty).TrimEnd('/').ToLowerInvariant();

        return path.StartsWith("/api/", StringComparison.Ordinal) ? path[4..] : path;
    }

    private static string? ReadBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();

        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim() is { Length: > 0 } value ? value : null
            : null;
    }

    private static string? ReadClaim(IDictionary<string, object> claims, string name) =>
        claims.TryGetValue(name, out var value) ? value?.ToString() : null;

    private static Task Unauthorized(HttpContext httpContext, string code, string detail) =>
        ProblemResults.WriteAsync(httpContext.Response, StatusCodes.Status401Unauthorized, "Unauthorized", code, detail);
}
