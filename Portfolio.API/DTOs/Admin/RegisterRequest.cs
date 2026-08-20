namespace Portfolio.API.DTOs.Admin;

/// <summary>Open registration. Everything is nullable — <c>UserService</c> does the validating.</summary>
public class RegisterRequest
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? ConfirmPassword { get; set; }
}
