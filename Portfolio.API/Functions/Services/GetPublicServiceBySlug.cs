using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Services;

public class GetPublicServiceBySlug
{
    private readonly IServiceCatalogService _serviceCatalog;

    public GetPublicServiceBySlug(IServiceCatalogService serviceCatalog)
    {
        _serviceCatalog = serviceCatalog;
    }

    [Function("GetPublicServiceBySlug")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/services/{slug}")] HttpRequest req,
        string slug,
        CancellationToken cancellationToken)
    {
        if (!QueryParameters.TryReadSite(req, out var site))
        {
            return ProblemResults.BadRequest("validation_failed", "Unknown site.");
        }

        if (site is null)
        {
            return ProblemResults.SiteRequired();
        }

        var result = await _serviceCatalog.GetPublicServiceBySlugAsync(site.Value, slug, cancellationToken);

        return result.IsSuccess
            ? HttpResponses.PublicJson(req, result.Value!)
            : ProblemResults.FromError(result.Error!);
    }
}
