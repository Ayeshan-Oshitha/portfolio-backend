using FrostWoodTech.API.Entities.Common;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Entities;

/// <summary>
/// Per-service tiers and general combo packs share this one table.
/// <see cref="ServiceId"/> = null means combo pack.
/// </summary>
public class PricingPlan : SiteVisibleEntity
{
    /// <summary>Null means a general / combo package.</summary>
    public Guid? ServiceId { get; set; }

    public ServiceOffering? Service { get; set; }

    public required string Name { get; set; }

    public string? Tagline { get; set; }

    /// <summary>Null means "Custom / Contact us" — never defaulted to 0.</summary>
    public decimal? PriceAmount { get; set; }

    /// <summary>ISO 4217, e.g. 'LKR', 'USD'.</summary>
    public required string Currency { get; set; }

    public PriceType PriceType { get; set; }

    public int? DeliveryDays { get; set; }

    /// <summary>Free text for ranges such as "2–3 weeks".</summary>
    public string? DeliveryText { get; set; }

    public required string Description { get; set; }

    /// <summary>The highlighted middle card.</summary>
    public bool IsPopular { get; set; }

    public string? CtaLabel { get; set; }

    public string? CtaUrl { get; set; }

    public bool IsPublished { get; set; }

    public int SortOrder { get; set; }

    public ICollection<PricingPlanFeature> Features { get; set; } = [];
}
