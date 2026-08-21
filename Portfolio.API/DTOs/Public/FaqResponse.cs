namespace Portfolio.API.DTOs.Public;

/// <summary>
/// What the two public frontends see. The site's own visibility flags are already resolved into
/// <see cref="Featured"/> and <see cref="SortOrder"/> — the other site's flags, the draft state
/// and the audit metadata never cross this boundary.
/// </summary>
public sealed class FaqResponse
{
    public required Guid Id { get; init; }

    public required string Question { get; init; }

    /// <summary>Markdown — sanitised on render in React, not on write.</summary>
    public required string Answer { get; init; }

    /// <summary>Groups FAQs on the page — "Pricing", "Process", "Technical".</summary>
    public string? Category { get; init; }

    /// <summary>Featured on the requested site.</summary>
    public required bool Featured { get; init; }

    /// <summary>Sort order for the requested site.</summary>
    public required int SortOrder { get; init; }
}
