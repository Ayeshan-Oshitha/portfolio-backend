namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>
/// Redeems an emailed password-setup link. Everything is nullable — <c>UserService</c> does the
/// validating, same as <see cref="RegisterRequest"/>.
/// </summary>
public class SetPasswordRequest
{
    public string? Token { get; set; }

    public string? Password { get; set; }

    public string? ConfirmPassword { get; set; }
}
