using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Never carries <c>password_hash</c> — this is the only shape a user is returned in.</summary>
public sealed class AdminUserResponse
{
    public required Guid Id { get; init; }

    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required UserRole Role { get; init; }

    public required UserStatus Status { get; init; }

    public DateTimeOffset? LastLoginAt { get; init; }

    public DateTimeOffset? ApprovedAt { get; init; }

    public DateTimeOffset? EmailVerifiedAt { get; init; }

    /// <summary>Set only on a rejected account, so the admin SPA can show why.</summary>
    public string? RejectionReason { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
