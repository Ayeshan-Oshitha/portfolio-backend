namespace Portfolio.API.Auth;

/// <summary>
/// Bound from the <c>SuperAdmin</c> configuration section (<c>SuperAdmin__Email</c> and friends).
/// These are the credentials the seeder writes on startup — the app settings are the source of
/// truth, so changing <see cref="Password"/> and redeploying rotates the password.
/// </summary>
public sealed class SuperAdminOptions
{
    public string Email { get; set; } = string.Empty;

    /// <summary>Never in a committed file. App settings / Key Vault only.</summary>
    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = "Super";

    public string LastName { get; set; } = "Admin";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
}
