using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Services;

public class DeleteServiceFeature
{
    private readonly IServiceCatalogService _serviceCatalog;

    public DeleteServiceFeature(IServiceCatalogService serviceCatalog)
    {
        _serviceCatalog = serviceCatalog;
    }

    /// <summary>Hard delete — feature rows carry no soft-delete flag.</summary>
    [Function("DeleteServiceFeature")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "delete",
            Route = "admin/services/{id:guid}/features/{featureId:guid}")] HttpRequest req,
        Guid id,
        Guid featureId,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _serviceCatalog.DeleteFeatureAsync(id, featureId, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
