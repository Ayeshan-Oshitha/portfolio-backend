namespace FrostWoodTech.API.Entities;

/// <summary>
/// One issued refresh token. Only the hash is stored — a database leak must not hand out live
/// sessions.
/// <para>
/// Deliberately not an <c>AuditableEntity</c>: soft delete is the wrong shape for a token. A
/// revoked row has to stay visible to the reuse check, not be filtered out of every query.
/// </para>
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>SHA-256 hex of the raw token — see <c>Auth/RefreshTokenGenerator.cs</c>.</summary>
    public required string TokenHash { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>The token that replaced this one on rotation. Null until it is used or revoked.</summary>
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
