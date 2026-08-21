namespace FrostWoodTech.API.DTOs.Admin;

public class CreateServiceRequest
{
    public string? Name { get; set; }

    /// <summary>Optional — generated from the name when omitted.</summary>
    public string? Slug { get; set; }

    /// <summary>Card text.</summary>
    public string? ShortDescription { get; set; }

    /// <summary>Markdown, service page body.</summary>
    public string? Description { get; set; }

    /// <summary>e.g. a Lucide icon key.</summary>
    public string? IconName { get; set; }

    /// <summary>Or an uploaded SVG/PNG — a Neon object key.</summary>
    public string? IconObjectKey { get; set; }

    public string? HeroImageId { get; set; }

    public bool IsPublished { get; set; }

    public bool ShowOnAgency { get; set; }

    public bool FeaturedOnAgency { get; set; }

    public int AgencySortOrder { get; set; }

    public bool ShowOnPersonal { get; set; }

    public bool FeaturedOnPersonal { get; set; }

    public int PersonalSortOrder { get; set; }
}
