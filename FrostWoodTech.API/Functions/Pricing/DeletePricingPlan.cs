using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Pricing;

public class DeletePricingPlan
{
    private readonly IPricingService _pricing;

    public DeletePricingPlan(IPricingService pricing)
    {
        _pricing = pricing;
    }

    /// <summary>Soft delete — the row stays in Postgres with is_deleted set.</summary>
    [Function("DeletePricingPlan")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "delete",
            Route = "admin/pricing-plans/{id:guid}")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _pricing.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
