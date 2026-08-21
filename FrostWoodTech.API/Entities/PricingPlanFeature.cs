namespace FrostWoodTech.API.Entities;

public class PricingPlanFeature
{
    public Guid Id { get; set; }

    public Guid PricingPlanId { get; set; }

    public PricingPlan PricingPlan { get; set; } = null!;

    public required string Text { get; set; }

    /// <summary>False renders a greyed-out row in the comparison table.</summary>
    public bool IsIncluded { get; set; }

    public int SortOrder { get; set; }
}
