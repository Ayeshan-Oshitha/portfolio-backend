namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Admin view: both sites' visibility, the draft flag and audit metadata.</summary>
public sealed class AdminFaqResponse
{
    public required Guid Id { get; init; }

    public required string Question { get; init; }

    public required string Answer { get; init; }

    public string? Category { get; init; }

    /// <summary>The entity's own fallback order, independent of either site.</summary>
    public required int SortOrder { get; init; }

    public required bool IsPublished { get; init; }

    public required bool ShowOnAgency { get; init; }

    public required bool FeaturedOnAgency { get; init; }

    public required int AgencySortOrder { get; init; }

    public required bool ShowOnPersonal { get; init; }

    public required bool FeaturedOnPersonal { get; init; }

    public required int PersonalSortOrder { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
