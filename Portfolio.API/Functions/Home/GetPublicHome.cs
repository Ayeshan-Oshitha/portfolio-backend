using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.DTOs.Public;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Home;

public class GetPublicHome
{
    /// <summary>
    /// A home page shows a handful of cards per slice, not a page of them. Capping here keeps
    /// the response small and bounds the work regardless of how much content exists.
    /// </summary>
    private const int SliceSize = 6;

    private const int FaqSliceSize = 20;

    private readonly IProjectService _projectService;
    private readonly IArticleService _articleService;
    private readonly IServiceCatalogService _serviceCatalogService;
    private readonly IPricingService _pricingService;
    private readonly IFaqService _faqService;
    private readonly IReviewService _reviewService;

    public GetPublicHome(
        IProjectService projectService,
        IArticleService articleService,
        IServiceCatalogService serviceCatalogService,
        IPricingService pricingService,
        IFaqService faqService,
        IReviewService reviewService)
    {
        _projectService = projectService;
        _articleService = articleService;
        _serviceCatalogService = serviceCatalogService;
        _pricingService = pricingService;
        _faqService = faqService;
        _reviewService = reviewService;
    }

    /// <summary>
    /// The single call each public home page makes, instead of one per section.
    /// </summary>
    [Function("GetPublicHome")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/home")] HttpRequest req,
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

        // Awaited one at a time on purpose: all five services share the same scoped DbContext,
        // which does not support concurrent operations. This is one round trip for the caller,
        // not one query.
        var projects = await _projectService.GetPublicProjectsAsync(
            site.Value,
            tagSlug: null,
            categorySlug: null,
            featured: true,
            page: 1,
            pageSize: SliceSize,
            cancellationToken);

        var articles = await _articleService.GetPublicArticlesAsync(
            site.Value,
            tagSlug: null,
            featured: true,
            page: 1,
            pageSize: SliceSize,
            cancellationToken);

        var services = await _serviceCatalogService.GetPublicServicesAsync(
            site.Value,
            featured: true,
            page: 1,
            pageSize: SliceSize,
            cancellationToken);

        var pricingPlans = await _pricingService.GetPublicComboPlansAsync(
            site.Value,
            featured: true,
            page: 1,
            pageSize: SliceSize,
            cancellationToken);

        var faqs = await _faqService.GetPublicFaqsAsync(site.Value, category: null, cancellationToken);

        var reviews = await _reviewService.GetFeaturedForHomeAsync(SliceSize, cancellationToken);

        var payload = new HomeResponse
        {
            FeaturedProjects = projects.Items,
            FeaturedArticles = articles.Items,
            FeaturedServices = services.Items,
            FeaturedPricingPlans = pricingPlans.Items,
            Faqs = [.. faqs.Take(FaqSliceSize)],
            FeaturedReviews = reviews
        };

        return HttpResponses.PublicJson(req, payload);
    }
}
