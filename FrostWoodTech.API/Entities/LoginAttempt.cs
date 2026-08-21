namespace FrostWoodTech.API.Entities;

/// <summary>
/// One failed sign-in attempt, kept just long enough to rate limit the next one.
/// <para>
/// It lives in Postgres rather than in memory because Functions scale out — an in-process
/// counter would reset on every cold start and would be per-instance, which is no limit at all.
/// </para>
/// <para>
/// Deliberately not an <c>AuditableEntity</c>: there is nothing to soft delete here. Rows are
/// swept once they fall outside the window.
/// </para>
/// </summary>
public class LoginAttempt
{
    public Guid Id { get; set; }

    /// <summary>
    /// The address that was tried, lowercased. Stored even when no such user exists — the point
    /// is to slow down guessing, and a miss is exactly what guessing looks like.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>Null when the caller's address could not be determined.</summary>
    public string? IpAddress { get; set; }

    public DateTimeOffset AttemptedAt { get; set; }
}
