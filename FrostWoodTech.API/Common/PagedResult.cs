namespace FrostWoodTech.API.Common;

/// <summary>The shape every list endpoint returns.</summary>
public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int Total { get; init; }
}
