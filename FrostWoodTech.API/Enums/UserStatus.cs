namespace FrostWoodTech.API.Enums;

/// <summary>Maps to the Postgres native enum <c>user_status</c>.</summary>
public enum UserStatus
{
    Pending,
    Approved,
    Rejected,
    Disabled
}
