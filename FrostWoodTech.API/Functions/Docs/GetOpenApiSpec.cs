using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

using FrostWoodTech.API.Docs;

namespace FrostWoodTech.API.Functions.Docs;

public class GetOpenApiSpec
{
    private readonly DocsOptions _options;

    public GetOpenApiSpec(IOptions<DocsOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Anonymous, gated on <c>Docs__Enabled</c>; a disabled deployment answers <c>404</c> rather
    /// than <c>403</c> so it doesn't confirm the endpoint exists.
    /// </summary>
    [Function("GetOpenApiSpec")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "openapi.yaml")] HttpRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);

        if (!_options.Enabled)
        {
            return new NotFoundResult();
        }

        // No-store, not cached: an edited spec should show up on the next request.
        req.HttpContext.Response.Headers.CacheControl = "no-store";

        return new ContentResult
        {
            Content = OpenApiDocument.Yaml,
            ContentType = OpenApiDocument.ContentType,
            StatusCode = StatusCodes.Status200OK
        };
    }
}
