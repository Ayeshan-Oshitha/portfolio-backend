namespace FrostWoodTech.API.DTOs.Public;

/// <summary>
/// What the two public frontends see. The site's own visibility flags are already resolved into
/// <see cref="Featured"/> and <see cref="SortOrder"/> — the other site's flags, the draft state
/// and the audit metadata never cross this boundary.
/// </summary>
public sealed class ProjectResponse
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public required int Year { get; init; }

    /// <summary>Card / list blurb.</summary>
    public required string ShortDescription { get; init; }

    /// <summary>Markdown — sanitised on render, not on write.</summary>
    public required string Description { get; init; }

    public string? WebsiteUrl { get; init; }

    public string? Problem { get; init; }

    public string? Solution { get; init; }

    public string? WhatWeDelivered { get; init; }

    public string? Proof { get; init; }

    public string? ClientName { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public string? SeoTitle { get; init; }

    public string? SeoDescription { get; init; }

    /// <summary>Featured on the requested site.</summary>
    public required bool Featured { get; init; }

    /// <summary>Sort order for the requested site.</summary>
    public required int SortOrder { get; init; }

    public required IReadOnlyList<TagResponse> Tags { get; init; }

    public required IReadOnlyList<ProjectImageResponse> Images { get; init; }
}
