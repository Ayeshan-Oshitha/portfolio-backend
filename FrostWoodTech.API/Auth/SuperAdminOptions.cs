namespace FrostWoodTech.API.Auth;

/// <summary>
/// Bound from the <c>SuperAdmin</c> section. Identity only, never credentials — no password
/// lives in configuration; the seeder emails a single-use setup link instead.
/// </summary>
public sealed class SuperAdminOptions
{
    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = "Super";

    public string LastName { get; set; } = "Admin";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Email);
}
