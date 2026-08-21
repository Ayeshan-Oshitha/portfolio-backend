using Portfolio.API.Entities.Common;

namespace Portfolio.API.Entities;

/// <summary>
/// A visitor-submitted testimonial. Unpublished until an admin reviews it — never shown anywhere
/// on either public site by default.
/// </summary>
public class Review : AuditableEntity
{
    public required string Name { get; set; }

    public required string Country { get; set; }

    /// <summary>ISO 3166-1 alpha-2, e.g. <c>US</c> — lets the frontend render a flag with no
    /// server-side lookup.</summary>
    public required string CountryCode { get; set; }

    public string? Position { get; set; }

    /// <summary>1-5, enforced by a check constraint.</summary>
    public int Rating { get; set; }

    public required string ReviewText { get; set; }

    public bool IsPublished { get; set; }

    /// <summary>Shown in the home page slice.</summary>
    public bool IsFeatured { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Admin-only, for spam moderation and the submission rate limit. Never returned on
    /// the public surface.</summary>
    public string? SubmitterIp { get; set; }
}
