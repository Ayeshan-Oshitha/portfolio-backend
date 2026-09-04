namespace FrostWoodTech.API.Entities;

/// <summary>
/// One issued email-verification link. Only the hash is stored, same reasoning as
/// <see cref="RefreshToken"/> — a database leak must not hand out live verification links.
/// <para>
/// Deliberately not an <c>AuditableEntity</c>: a used or expired row must stay visible so a
/// replayed link is recognised and reported, not filtered out of the query.
/// </para>
/// </summary>
public class EmailVerificationToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>SHA-256 hex of the raw token — see <c>Auth/EmailVerificationTokenGenerator.cs</c>.</summary>
    public required string TokenHash { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public bool IsActive(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;
}
