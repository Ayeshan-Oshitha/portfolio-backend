namespace Portfolio.API.DTOs.Public;

/// <summary>
/// Cloudinary metadata only. The frontends build responsive URLs from the public_id with
/// transformations — the API never returns per-size URLs.
/// </summary>
public sealed class ProjectImageResponse
{
    public required Guid Id { get; init; }

    /// <summary>Cloudinary public_id.</summary>
    public required string CloudinaryId { get; init; }

    public required string Url { get; init; }

    public required string AltText { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    /// <summary>Exactly one per project — the card / hero image.</summary>
    public required bool IsPrimary { get; init; }

    public required int SortOrder { get; init; }
}
