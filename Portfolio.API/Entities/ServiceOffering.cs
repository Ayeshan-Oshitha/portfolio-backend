using Portfolio.API.Entities.Common;

namespace Portfolio.API.Entities;

/// <summary>
/// The <c>services</c> table. Named <c>ServiceOffering</c> rather than <c>Service</c> so the type
/// does not collide with the <c>Portfolio.API.Services</c> namespace.
/// </summary>
public class ServiceOffering : SiteVisibleEntity
{
    public required string Slug { get; set; }

    public required string Name { get; set; }

    /// <summary>Card text.</summary>
    public required string ShortDescription { get; set; }

    /// <summary>Markdown, service page body.</summary>
    public required string Description { get; set; }

    /// <summary>e.g. a Lucide icon key.</summary>
    public string? IconName { get; set; }

    /// <summary>Or an uploaded SVG/PNG.</summary>
    public string? IconObjectKey { get; set; }

    public string? HeroImageId { get; set; }

    public bool IsPublished { get; set; }

    /// <summary>Stamped the first time the service goes live, never cleared.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    public ICollection<ServiceFeature> Features { get; set; } = [];

    public ICollection<PricingPlan> PricingPlans { get; set; } = [];
}
