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

        var request = httpContext.Request;
        var origin = request.Headers.Origin.ToString();

        // Only ever echo an origin we were configured with — no wildcard, no reflection of
        // whatever the caller sent.
        var allowed = !string.IsNullOrEmpty(origin)
            && _options.Origins.Contains(origin, StringComparer.OrdinalIgnoreCase);

        if (allowed)
        {
            var headers = httpContext.Response.Headers;
            headers[HeaderNames.AccessControlAllowOrigin] = origin;
            headers[HeaderNames.AccessControlAllowCredentials] = "true";

            // The response varies by origin, so a shared cache must not serve one site's
            // response to another. Public GETs are cacheable, which makes this load-bearing.
            headers.Append(HeaderNames.Vary, HeaderNames.Origin);
        }

        if (HttpMethods.IsOptions(request.Method))
        {
            if (allowed)
            {
                var headers = httpContext.Response.Headers;
                headers[HeaderNames.AccessControlAllowMethods] = "GET, POST, PUT, DELETE, OPTIONS";
                headers[HeaderNames.AccessControlAllowHeaders] = "Authorization, Content-Type, If-None-Match";
                headers[HeaderNames.AccessControlExposeHeaders] = "ETag";
                headers[HeaderNames.AccessControlMaxAge] = _options.PreflightMaxAgeSeconds.ToString();
            }

            // Preflight is answered here and never reaches a function — there is no OPTIONS
            // trigger to route it to.
            httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        await next(context);
    }
}
