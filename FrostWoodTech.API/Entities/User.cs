using FrostWoodTech.API.Entities.Common;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Entities;

/// <summary>Admin accounts. Behaviour rules live in <c>.claude/rules/auth.md</c>.</summary>
public class User : AuditableEntity
{
    /// <summary>Stored as <c>citext</c> so lookups are case-insensitive.</summary>
    public required string Email { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    /// <summary>Null for Google-only accounts.</summary>
    public string? PasswordHash { get; set; }

    public string? GoogleSubjectId { get; set; }

    public string? AvatarUrl { get; set; }

    public UserRole Role { get; set; }

    public UserStatus Status { get; set; }

    public Guid? ApprovedBy { get; set; }

    public User? ApprovedByUser { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }

    public string? RejectionReason { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// Set when the verification link is clicked. Kept independent of <see cref="Status"/> so the
    /// fact survives a later rejection or disable.
    /// </summary>
    public DateTimeOffset? EmailVerifiedAt { get; set; }
}
