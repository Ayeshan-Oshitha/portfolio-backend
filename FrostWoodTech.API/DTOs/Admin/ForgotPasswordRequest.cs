namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Nullable — <c>UserService</c> does the validating, same as the other auth DTOs.</summary>
public class ForgotPasswordRequest
{
    public string? Email { get; set; }
}
