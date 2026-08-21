namespace FrostWoodTech.API.DTOs.Public;

/// <summary>
/// Neon Object Storage metadata only — image bytes never live in Postgres, and the API never
/// returns per-size URLs.
/// </summary>
public sealed class ProjectImageResponse
{
    public required Guid Id { get; init; }

    /// <summary>Neon object key.</summary>
    public required string ObjectKey { get; init; }

    public required string Url { get; init; }

    public required string AltText { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    /// <summary>Exactly one per project — the card / hero image.</summary>
    public required bool IsPrimary { get; init; }

    public required int SortOrder { get; init; }
}
