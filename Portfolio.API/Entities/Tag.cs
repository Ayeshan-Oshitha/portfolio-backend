using Portfolio.API.Entities.Common;
using Portfolio.API.Enums;

namespace Portfolio.API.Entities;

/// <summary>
/// One table for both project categories and technologies. The two are told apart by
/// <see cref="IsTechnology"/>, not by a separate column — group them at read time.
/// </summary>
public class Tag : AuditableEntity
{
    public required string Name { get; set; }

    public required string Slug { get; set; }

    public bool IsTechnology { get; set; }

    /// <summary>Required when <see cref="IsTechnology"/> is true, otherwise must be null.</summary>
    public TechCategory? TechnologyCategory { get; set; }

    /// <summary>Required when <see cref="IsTechnology"/> is true, otherwise must be null.</summary>
    public string? IconCloudinaryId { get; set; }

    public string? IconUrl { get; set; }

    /// <summary>Optional chip colour on the frontend, <c>#rrggbb</c>.</summary>
    public string? ColorHex { get; set; }

    public int SortOrder { get; set; }

    public ICollection<ProjectTag> ProjectTags { get; set; } = [];

    public ICollection<ArticleTag> ArticleTags { get; set; } = [];
}
