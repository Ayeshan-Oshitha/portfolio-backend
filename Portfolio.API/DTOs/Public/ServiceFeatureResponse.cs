namespace Portfolio.API.DTOs.Public;

/// <summary>
/// One bullet on a service page. Features carry no admin-only fields, so the admin surface
/// reuses this shape as is.
/// </summary>
public sealed class ServiceFeatureResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    /// <summary>e.g. a Lucide icon key.</summary>
    public string? IconName { get; init; }

    public required int SortOrder { get; init; }
}
