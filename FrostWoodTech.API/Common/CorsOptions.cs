namespace FrostWoodTech.API.Common;

/// <summary>
/// Bound from the <c>Cors</c> configuration section. Allowed origins live in source control
/// rather than a hand-set portal field, since three SPAs consume this API.
/// </summary>
public sealed class CorsOptions
{
    /// <summary>
    /// Comma-separated exact origins ("https://agency.example.com,https://personal.example.com"),
    /// no trailing slash. Empty means CORS is off and no origin is echoed — never a wildcard,
    /// because admin calls send an Authorization header and credentials are not valid with <c>*</c>.
    /// </summary>
    public string AllowedOrigins { get; set; } = "";

    /// <summary>Parsed, trimmed origins from <see cref="AllowedOrigins"/>.</summary>
    public string[] Origins => AllowedOrigins
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
