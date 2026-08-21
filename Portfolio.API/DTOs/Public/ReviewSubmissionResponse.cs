namespace Portfolio.API.DTOs.Public;

/// <summary>
/// Deliberately thin — the review is unmoderated, so the submitter gets an acknowledgement, not
/// the row back.
/// </summary>
public sealed class ReviewSubmissionResponse
{
    public required Guid Id { get; init; }
}
