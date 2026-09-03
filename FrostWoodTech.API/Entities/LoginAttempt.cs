using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Entities;

/// <summary>
/// One attempt on the sign-in surface — a failed login or a reset request, per
/// <see cref="Action"/> — kept just long enough to rate limit the next one. Lives in Postgres
/// since Functions scale out and an in-process counter would be per-instance. Not an
/// <c>AuditableEntity</c>: rows are swept once outside the window, nothing to soft delete.
/// </summary>
public class LoginAttempt
{
    public Guid Id { get; set; }

    /// <summary>Lowercased. Stored even when no such user exists — a miss is what guessing looks like.</summary>
    public required string Email { get; set; }

    /// <summary>Null when the caller's address could not be determined.</summary>
    public string? IpAddress { get; set; }

    public AuthAttemptAction Action { get; set; }

    public DateTimeOffset AttemptedAt { get; set; }
}
