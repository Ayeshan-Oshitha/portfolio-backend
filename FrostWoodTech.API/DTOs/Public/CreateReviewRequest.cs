namespace FrostWoodTech.API.DTOs.Public;

/// <summary>What an anonymous visitor submits. Lands unpublished — nothing here can make a
/// review appear on either site without an admin publishing it.</summary>
public class CreateReviewRequest
{
    public string? Name { get; set; }

    public string? Country { get; set; }

    /// <summary>ISO 3166-1 alpha-2, e.g. <c>US</c>.</summary>
    public string? CountryCode { get; set; }

    public string? Position { get; set; }

    public int Rating { get; set; }

    public string? ReviewText { get; set; }
}
