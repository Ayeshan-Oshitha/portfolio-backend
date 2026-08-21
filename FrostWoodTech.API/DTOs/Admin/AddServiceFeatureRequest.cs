namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>One bullet on a service page.</summary>
public class AddServiceFeatureRequest
{
    public string? Title { get; set; }

    public string? Description { get; set; }

    /// <summary>e.g. a Lucide icon key.</summary>
    public string? IconName { get; set; }

    public int SortOrder { get; set; }
}
