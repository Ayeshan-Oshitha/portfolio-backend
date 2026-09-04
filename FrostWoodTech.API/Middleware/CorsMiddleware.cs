using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

using FrostWoodTech.API.Common;

namespace FrostWoodTech.API.Middleware;

/// <summary>
/// Registered outermost so every response carries the headers — including the 500 written by
/// <see cref="ExceptionHandlingMiddleware"/> and the 401 written by
/// <see cref="JwtAuthenticationMiddleware"/>. Without them the browser blocks the response and
/// the admin SPA sees an opaque network error instead of the problem+json it needs to read.
///
/// Preflight <c>OPTIONS</c> requests are answered by the Functions host itself (local:
/// <c>Host:CORS</c> in <c>local.settings.json</c>; Azure: the Function App's CORS settings) —
/// no function trigger declares <c>options</c>, so a preflight never reaches this middleware.
/// This only adds the headers a real request still needs.
/// </summary>
public sealed class CorsMiddleware : IFunctionsWorkerMiddleware
{
    private readonly CorsOptions _options;

    public CorsMiddleware(IOptions<CorsOptions> options)
    {
        _options = options.Value;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is null)
        {
            await next(context);
            return;
        }

        var origin = httpContext.Request.Headers.Origin.ToString();

        // Only echo a configured origin — no wildcard, no reflecting whatever the caller sent.
        if (!string.IsNullOrEmpty(origin) && _options.Origins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            var headers = httpContext.Response.Headers;
            headers[HeaderNames.AccessControlAllowOrigin] = origin;
            headers[HeaderNames.AccessControlAllowCredentials] = "true";
            headers[HeaderNames.AccessControlExposeHeaders] = "ETag";

            // Varies by origin, so a shared cache must not serve one site's response to another.
            // Public GETs are cacheable, which makes this load-bearing.
            headers.Append(HeaderNames.Vary, HeaderNames.Origin);
        }

        await next(context);
    }
}
