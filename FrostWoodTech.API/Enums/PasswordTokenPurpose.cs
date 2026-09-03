namespace FrostWoodTech.API.Enums;

/// <summary>
/// Why a <c>password_tokens</c> row was issued. One table with this discriminator rather than
/// two near-identical ones — only the lifetime and email wording differ. Native enum
/// <c>password_token_purpose</c>.
/// </summary>
public enum PasswordTokenPurpose
{
    /// <summary>First password for an account that has never had one — the seeded super admin.</summary>
    Setup,

    /// <summary>Replacement password for an account that has forgotten theirs.</summary>
    Reset
}
