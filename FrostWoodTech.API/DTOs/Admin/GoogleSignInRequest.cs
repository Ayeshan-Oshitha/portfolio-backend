namespace FrostWoodTech.API.DTOs.Admin;

public sealed class GoogleSignInRequest
{
    /// <summary>
    /// The ID token the admin SPA received from Google Sign-In. The API verifies it against
    /// Google's keys — it is never trusted on the client's word.
    /// </summary>
    public string? IdToken { get; set; }
}
