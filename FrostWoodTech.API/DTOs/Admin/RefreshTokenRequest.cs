namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Body for both <c>admin/auth/refresh</c> and <c>admin/auth/logout</c>.</summary>
public sealed class RefreshTokenRequest
{
    public string? RefreshToken { get; set; }
}
