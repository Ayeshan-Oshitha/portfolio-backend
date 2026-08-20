namespace Portfolio.API.DTOs.Public;

/// <summary>
/// What the public frontends see. The site's own visibility flags are already resolved into
/// <see cref="Featured"/> and <see cref="SortOrder"/> — the other site's flags, the draft state
/// and the audit metadata never cross this boundary.
/// </summary>
public sealed class ServiceResponse
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    /// <summary>Card text.</summary>
    public required string ShortDescription { get; init; }

    /// <summary>Markdown — sanitised on render, not on write.</summary>
    public required string Description { get; init; }

    /// <summary>e.g. a Lucide icon key.</summary>
    public string? IconName { get; init; }

    /// <summary>Or an uploaded SVG/PNG — a Cloudinary public_id.</summary>
    public string? IconCloudinaryId { get; init; }

    public string? HeroImageId { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    /// <summary>Featured on the requested site.</summary>
    public required bool Featured { get; init; }

    /// <summary>Sort order for the requested site.</summary>
    public required int SortOrder { get; init; }

    public required IReadOnlyList<ServiceFeatureResponse> Features { get; init; }
}
