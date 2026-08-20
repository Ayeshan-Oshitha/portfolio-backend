namespace Portfolio.API.DTOs.Admin;

/// <summary>Access token only for now — there is no refresh token yet.</summary>
public sealed class AuthResponse
{
    public required string AccessToken { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required AdminUserResponse User { get; init; }
}
