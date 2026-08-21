namespace Portfolio.API.Common;

/// <summary>
/// Bound from the <c>Cors</c> configuration section. Three browser SPAs consume this API, so the
/// allowed origins belong in source control rather than in a hand-set portal field nobody can
/// review.
/// </summary>
public sealed class CorsOptions
{
    /// <summary>
    /// Exact origins ("https://agency.example.com"), no trailing slash. Empty means CORS is off
    /// and no origin is echoed — never a wildcard, because admin calls send an Authorization
    /// header and credentials are not valid with <c>*</c>.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>How long a browser may cache the preflight result.</summary>
    public int PreflightMaxAgeSeconds { get; set; } = 3600;
}
