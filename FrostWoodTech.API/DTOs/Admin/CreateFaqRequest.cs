namespace FrostWoodTech.API.DTOs.Admin;

public class CreateFaqRequest
{
    public string? Question { get; set; }

    /// <summary>Markdown, stored raw.</summary>
    public string? Answer { get; set; }

    /// <summary>Optional grouping heading — "Pricing", "Process", "Technical".</summary>
    public string? Category { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }

    public bool ShowOnAgency { get; set; }

    public bool FeaturedOnAgency { get; set; }

    public int AgencySortOrder { get; set; }

    public bool ShowOnPersonal { get; set; }

    public bool FeaturedOnPersonal { get; set; }

    public int PersonalSortOrder { get; set; }
}
