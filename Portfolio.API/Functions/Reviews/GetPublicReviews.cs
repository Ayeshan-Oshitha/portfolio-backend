using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.Enums;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Reviews;

public class GetPublicReviews
{
    private readonly IReviewService _reviewService;

    public GetPublicReviews(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>The dedicated public reviews page.</summary>
    [Function("GetPublicReviews")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/reviews")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (!QueryParameters.TryReadEnum<ReviewSortOption>(req, "sort", out var sort))
        {
            return ProblemResults.BadRequest("validation_failed", "Unknown sort.");
        }

        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _reviewService.GetPublicReviewsAsync(
            sort ?? ReviewSortOption.Latest,
            page,
            pageSize,
            cancellationToken);

        return HttpResponses.PublicJson(req, result);
    }
}
