using Portfolio.API.Enums;

namespace Portfolio.API.DTOs.Public;

/// <summary>
/// What the two public frontends see. No audit fields, no Cloudinary ids — those are admin-only.
/// </summary>
public sealed class TagResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public required bool IsTechnology { get; init; }

    public TechCategory? TechnologyCategory { get; init; }

    public string? IconUrl { get; init; }

    public string? ColorHex { get; init; }

    public required int SortOrder { get; init; }
}
