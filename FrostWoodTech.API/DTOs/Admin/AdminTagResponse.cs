using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Admin view: adds the object key and audit metadata.</summary>
public sealed class AdminTagResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public required bool IsTechnology { get; init; }

    public TechCategory? TechnologyCategory { get; init; }

    public string? IconObjectKey { get; init; }

    public string? IconUrl { get; init; }

    public string? ColorHex { get; init; }

    public required int SortOrder { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
