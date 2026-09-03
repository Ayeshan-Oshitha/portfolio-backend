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
    /// Scalar renders the spec client-side; this is just the shell. The URL is absolute because
    /// <c>host.json</c> sets no <c>routePrefix</c> — keep the two in sync.
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
    /// Anonymous, gated on <c>Docs__Enabled</c> like the spec — a browser tab can't carry a
    /// bearer token, so that flag is what keeps this off production.
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
