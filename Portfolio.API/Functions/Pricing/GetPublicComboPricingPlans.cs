using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Pricing;

/// <summary>
/// The general pricing page: plans that belong to no single service. Combo packs get their own
/// route rather than being "the pricing list with serviceId left off" — an absent parameter
/// silently changing the query is invisible in the URL.
/// </summary>
public class GetPublicComboPricingPlans
{
    private readonly IPricingService _pricing;

    public GetPublicComboPricingPlans(IPricingService pricing)
    {
        _pricing = pricing;
    }

    [Function("GetPublicComboPricingPlans")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/pricing/combos")] HttpRequest req,
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

        var featured = QueryParameters.ReadBool(req, "featured");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _pricing.GetPublicComboPlansAsync(
            site.Value,
            featured,
            page,
            pageSize,
            cancellationToken);

        return HttpResponses.PublicJson(req, result);
    }
}
