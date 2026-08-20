using Portfolio.API.DTOs.Public;
using Portfolio.API.Enums;

namespace Portfolio.API.DTOs.Admin;

/// <summary>Admin view: both sites' visibility, the draft flag and audit metadata.</summary>
public sealed class AdminPricingPlanResponse
{
    public required Guid Id { get; init; }

    /// <summary>Null for a combo pack.</summary>
    public Guid? ServiceId { get; init; }

    public required string Name { get; init; }

    public string? Tagline { get; init; }

    /// <summary>Null means "Custom / Contact us".</summary>
    public decimal? PriceAmount { get; init; }

    public required string Currency { get; init; }

    public required PriceType PriceType { get; init; }

    public int? DeliveryDays { get; init; }

    public string? DeliveryText { get; init; }

    public required string Description { get; init; }

    public required bool IsPopular { get; init; }

    public string? CtaLabel { get; init; }

    public string? CtaUrl { get; init; }

    public required bool IsPublished { get; init; }

    /// <summary>The plan's own tier order within its service.</summary>
    public required int SortOrder { get; init; }

    public required bool ShowOnAgency { get; init; }

    public required bool FeaturedOnAgency { get; init; }

    public required int AgencySortOrder { get; init; }

    public required bool ShowOnPersonal { get; init; }

    public required bool FeaturedOnPersonal { get; init; }

    public required int PersonalSortOrder { get; init; }

    /// <summary>Features carry no admin-only fields, so the public shape is reused as is.</summary>
    public required IReadOnlyList<PricingPlanFeatureResponse> Features { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
