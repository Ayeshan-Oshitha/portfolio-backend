using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Pricing;

public class GetAdminPricingPlans
{
    private readonly IPricingService _pricing;

    public GetAdminPricingPlans(IPricingService pricing)
    {
        _pricing = pricing;
    }

    [Function("GetAdminPricingPlans")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/pricing-plans")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        // Site is optional here — the admin SPA lists drafts across both sites.
        if (!QueryParameters.TryReadSite(req, out var site))
        {
            return ProblemResults.BadRequest("validation_failed", "Unknown site.");
        }

        var serviceId = QueryParameters.ReadGuid(req, "serviceId");
        var comboOnly = QueryParameters.ReadBool(req, "comboOnly") ?? false;
        var isPublished = QueryParameters.ReadBool(req, "isPublished");
        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _pricing.GetAdminPlansAsync(
            site,
            serviceId,
            comboOnly,
            isPublished,
            search,
            page,
            pageSize,
            cancellationToken);

        return new OkObjectResult(result);
    }
}
