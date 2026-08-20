using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.DTOs.Admin;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Services;

public class UpdateServiceFeature
{
    private readonly IServiceCatalogService _serviceCatalog;

    public UpdateServiceFeature(IServiceCatalogService serviceCatalog)
    {
        _serviceCatalog = serviceCatalog;
    }

    /// <summary>A full replacement — the feature is looked up within its own service.</summary>
    [Function("UpdateServiceFeature")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "put",
            Route = "admin/services/{id:guid}/features/{featureId:guid}")] HttpRequest req,
        Guid id,
        Guid featureId,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        UpdateServiceFeatureRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<UpdateServiceFeatureRequest>(
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

        var result = await _serviceCatalog.UpdateFeatureAsync(id, featureId, body, cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
