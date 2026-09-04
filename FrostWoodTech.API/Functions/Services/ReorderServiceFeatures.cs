using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Services;

public class ReorderServiceFeatures
{
    private readonly IServiceCatalogService _serviceCatalog;

    public ReorderServiceFeatures(IServiceCatalogService serviceCatalog)
    {
        _serviceCatalog = serviceCatalog;
    }

    /// <summary>Bulk sort_order update for one feature list. No site here — a service has a single feature order.</summary>
    [Function("ReorderServiceFeatures")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "cms/admin/services/{id:guid}/features/reorder")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        FeatureReorderRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<FeatureReorderRequest>(
                req.Body,
                JsonDefaults.Options,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            return ProblemResults.BadRequest("validation_failed", ex.Message);
        }

        if (body is null)
        {
            return ProblemResults.BadRequest("validation_failed", "A request body is required.");
        }

        var result = await _serviceCatalog.ReorderFeaturesAsync(id, body, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
