namespace Portfolio.API.Auth;

/// <summary>Bound from the <c>Jwt</c> configuration section (<c>Jwt__Signer</c> and friends).</summary>
public sealed class JwtOptions
{
    /// <summary>HMAC signing key. Lives in app settings / Key Vault, never in a committed file.</summary>
    public string Signer { get; set; } = string.Empty;

    public string Issuer { get; set; } = "portfolio-api";

    public string Audience { get; set; } = "portfolio-admin";

    /// <summary>
    /// Short on purpose: this is the window in which a disabled or deleted user can still act,
    /// because a JWT cannot be recalled once issued. The SPA renews silently via the refresh token.
    /// </summary>
    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 30;
}
