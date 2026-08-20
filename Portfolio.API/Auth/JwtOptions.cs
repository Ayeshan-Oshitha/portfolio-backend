namespace Portfolio.API.Auth;

/// <summary>Bound from the <c>Jwt</c> configuration section (<c>Jwt__Signer</c> and friends).</summary>
public sealed class JwtOptions
{
    /// <summary>HMAC signing key. Lives in app settings / Key Vault, never in a committed file.</summary>
    public string Signer { get; set; } = string.Empty;

    public string Issuer { get; set; } = "portfolio-api";

    public string Audience { get; set; } = "portfolio-admin";

    /// <summary>No refresh token yet, so the access token has to last a working session.</summary>
    public int AccessTokenMinutes { get; set; } = 480;
}
