using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Entities;

/// <summary>
/// One issued password link — setup or reset, told apart by <see cref="Purpose"/>. Only the hash
/// is stored, like <see cref="EmailVerificationToken"/>. Not an <c>AuditableEntity</c>: a used or
/// expired row must stay visible so a replayed link is recognised, not filtered out.
/// </summary>
public class PasswordToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>SHA-256 hex of the raw token — see <c>Auth/EmailVerificationTokenGenerator.cs</c>.</summary>
    public required string TokenHash { get; set; }

    /// <summary>Redemption treats both the same; this only carries the different lifetimes and shows up in logs.</summary>
    public PasswordTokenPurpose Purpose { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public bool IsActive(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;
}
