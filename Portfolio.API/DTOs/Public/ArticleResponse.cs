namespace Portfolio.API.DTOs.Public;

/// <summary>
/// What the two public frontends see. The site's own visibility flags are already resolved into
/// <see cref="Featured"/> and <see cref="SortOrder"/> — the other site's flags, the draft state
/// and the audit metadata never cross this boundary.
/// </summary>
public sealed class ArticleResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Excerpt { get; init; }

    public string? Slug { get; init; }

    public required DateOnly PublishedDate { get; init; }

    public required string MediumUrl { get; init; }

    /// <summary>Cloudinary public_id — the frontend builds the delivery URL.</summary>
    public string? CoverImageId { get; init; }

    /// <summary>Featured on the requested site.</summary>
    public required bool Featured { get; init; }

    /// <summary>Sort order for the requested site.</summary>
    public required int SortOrder { get; init; }

    public required IReadOnlyList<TagResponse> Tags { get; init; }
}
