namespace Portfolio.API.DTOs.Admin;

public class AddPricingPlanFeatureRequest
{
    public string? Text { get; set; }

    /// <summary>False renders a greyed-out row in the comparison table.</summary>
    public bool IsIncluded { get; set; }

    public int SortOrder { get; set; }
}
