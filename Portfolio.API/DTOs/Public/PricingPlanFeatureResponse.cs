namespace Portfolio.API.DTOs.Public;

/// <summary>
/// One row in a plan's comparison table. Features carry no admin-only fields, so the admin
/// surface reuses this shape as is.
/// </summary>
public sealed class PricingPlanFeatureResponse
{
    public required Guid Id { get; init; }

    public required string Text { get; init; }

    /// <summary>False renders a greyed-out row.</summary>
    public required bool IsIncluded { get; init; }

    public required int SortOrder { get; init; }
}
