using Portfolio.API.Entities.Common;

namespace Portfolio.API.Entities;

/// <summary>
/// Articles have no dedicated page — they render on one list page and link out to Medium.
/// </summary>
public class Article : SiteVisibleEntity
{
    public required string Title { get; set; }

    public required string Excerpt { get; set; }

    public DateOnly PublishedDate { get; set; }

    /// <summary>External, required, absolute URL.</summary>
    public required string MediumUrl { get; set; }

    /// <summary>Neon object key.</summary>
    public string? CoverImageKey { get; set; }

    /// <summary>Optional, for internal linking.</summary>
    public string? Slug { get; set; }

    public bool IsPublished { get; set; }

    public ICollection<ArticleTag> ArticleTags { get; set; } = [];
}
