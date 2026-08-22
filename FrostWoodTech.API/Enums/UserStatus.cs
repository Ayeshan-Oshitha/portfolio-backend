namespace FrostWoodTech.API.Enums;

/// <summary>Maps to the Postgres native enum <c>user_status</c>.</summary>
public enum UserStatus
{
    /// <summary>A password account that has not yet clicked its verification link.</summary>
    EmailVerificationRequired,
    Pending,
    Approved,
    Rejected,
    Disabled
}
