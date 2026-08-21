using System.Globalization;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

using FrostWoodTech.API.Docs;

namespace FrostWoodTech.API.Functions.Docs;

public class GetDocs
{
    /// <summary>
    /// Scalar renders the spec client-side, so the page itself is this shell and nothing more.
    /// The spec URL is absolute because <c>host.json</c> sets no <c>routePrefix</c> — the default
    /// <c>api/</c> applies. Change one and you must change the other.
    /// </summary>
    private const string Template = """
        <!doctype html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>FrostWoodTech CMS API</title>
        </head>
        <body>
        <script id="api-reference" data-url="/api/openapi.yaml"></script>
        <script src="{0}"></script>
        </body>
        </html>
        """;

    private readonly DocsOptions _options;

    public GetDocs(IOptions<DocsOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Browsable API reference for the three frontends. Anonymous, and gated on the same
    /// <c>Docs__Enabled</c> switch as the spec — a browser tab never carries a bearer token, so
    /// the setting is what keeps this off a production deployment.
    /// </summary>
    [Function("GetDocs")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "docs")] HttpRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);

        if (!_options.Enabled)
        {
            return new NotFoundResult();
        }

        req.HttpContext.Response.Headers.CacheControl = "no-store";

        return new ContentResult
        {
            Content = string.Format(CultureInfo.InvariantCulture, Template, _options.ScalarCdnUrl),
            ContentType = "text/html; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }
}
