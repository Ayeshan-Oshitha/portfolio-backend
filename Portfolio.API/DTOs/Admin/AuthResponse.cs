namespace Portfolio.API.DTOs.Admin;

/// <summary>
/// A short-lived access token plus the rotating refresh token that renews it. The refresh token
/// is returned in the body rather than an httpOnly cookie because the admin SPA is a different
/// origin from the Functions app.
/// </summary>
public sealed class AuthResponse
{
    public required string AccessToken { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Raw value — it is never stored, only its hash is.</summary>
    public required string RefreshToken { get; init; }

    public required DateTimeOffset RefreshTokenExpiresAt { get; init; }

    public required AdminUserResponse User { get; init; }
}
