namespace Portfolio.API.DTOs.Admin;

public class CreateArticleRequest
{
    public string? Title { get; set; }

    public string? Excerpt { get; set; }

    /// <summary>Optional — generated from the title when omitted.</summary>
    public string? Slug { get; set; }

    public DateOnly PublishedDate { get; set; }

    /// <summary>Required, and must be an absolute URL.</summary>
    public string? MediumUrl { get; set; }

    public string? CoverImageKey { get; set; }

    public bool IsPublished { get; set; }

    public bool ShowOnAgency { get; set; }

    public bool FeaturedOnAgency { get; set; }

    public int AgencySortOrder { get; set; }

    public bool ShowOnPersonal { get; set; }

    public bool FeaturedOnPersonal { get; set; }

    public int PersonalSortOrder { get; set; }

    /// <summary>The full set of tags for the article — omitted or empty means none.</summary>
    public List<Guid>? TagIds { get; set; }
}
