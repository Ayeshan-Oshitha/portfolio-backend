namespace Portfolio.API.DTOs.Public;

/// <summary>
/// One round trip instead of six. Every slice is already scoped to the requested site, to
/// published non-deleted rows, and — apart from the FAQs — to that site's featured flag.
/// </summary>
public sealed class HomeResponse
{
    public required IReadOnlyList<ProjectResponse> FeaturedProjects { get; init; }

    public required IReadOnlyList<ArticleResponse> FeaturedArticles { get; init; }

    public required IReadOnlyList<ServiceResponse> FeaturedServices { get; init; }

    /// <summary>Featured combo packs — the plans with no owning service.</summary>
    public required IReadOnlyList<PricingPlanResponse> FeaturedPricingPlans { get; init; }

    /// <summary>Not filtered by featured: the home page shows the FAQ list as-is.</summary>
    public required IReadOnlyList<FaqResponse> Faqs { get; init; }

    /// <summary>Featured, published reviews. Not site-scoped — reviews are shared across both
    /// public sites.</summary>
    public required IReadOnlyList<ReviewResponse> FeaturedReviews { get; init; }
}
