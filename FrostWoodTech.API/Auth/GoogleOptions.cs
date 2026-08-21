namespace FrostWoodTech.API.Auth;

/// <summary>Bound from the <c>Google</c> configuration section.</summary>
public sealed class GoogleOptions
{
    /// <summary>
    /// The OAuth client id the admin SPA signs in with. Every ID token must name it in
    /// <c>aud</c> — without that check a token minted for any other Google app would be
    /// accepted here.
    /// </summary>
    public string? ClientId { get; set; }
}
