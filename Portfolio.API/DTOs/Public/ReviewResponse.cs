namespace Portfolio.API.DTOs.Public;

/// <summary>What the two public frontends see. The draft state, featured flag, sort order, and
/// submitter IP never cross this boundary.</summary>
public sealed class ReviewResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Country { get; init; }

    /// <summary>ISO 3166-1 alpha-2 — the frontend derives the flag emoji from this.</summary>
    public required string CountryCode { get; init; }

    public string? Position { get; init; }

    public required int Rating { get; init; }

    public required string ReviewText { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
