using Portfolio.API.DTOs.Public;

namespace Portfolio.API.DTOs.Admin;

/// <summary>Admin view: both sites' visibility, the draft flag and audit metadata.</summary>
public sealed class AdminServiceResponse
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    public required string ShortDescription { get; init; }

    public required string Description { get; init; }

    public string? IconName { get; init; }

    public string? IconObjectKey { get; init; }

    public string? HeroImageId { get; init; }

    public required bool IsPublished { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public required bool ShowOnAgency { get; init; }

    public required bool FeaturedOnAgency { get; init; }

    public required int AgencySortOrder { get; init; }

    public required bool ShowOnPersonal { get; init; }

    public required bool FeaturedOnPersonal { get; init; }

    public required int PersonalSortOrder { get; init; }

    /// <summary>Features carry no admin-only fields, so the public shape is reused as is.</summary>
    public required IReadOnlyList<ServiceFeatureResponse> Features { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
