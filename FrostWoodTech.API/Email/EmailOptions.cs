namespace FrostWoodTech.API.Email;

/// <summary>
/// Bound from the <c>Email</c> configuration section (<c>Email__FromAddress</c> and friends).
/// </summary>
public sealed class EmailOptions
{
    /// <summary>
    /// Which transport to use. <c>brevo</c> is the real path; <c>log</c> (the default, for local
    /// dev without a Brevo key) just writes the message to the log instead of sending it. A
    /// future provider reads the same section, so switching is a settings change plus one DI line.
    /// </summary>
    public string Provider { get; set; } = "log";

    /// <summary>
    /// The only sender address. Deliberately not on <see cref="EmailMessage"/> — a caller that
    /// could choose its own <c>From</c> could impersonate the deployment.
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "FrostWoodTech";

    /// <summary>
    /// Provider credential — the Brevo API key. Unused by the logging transport, and lives in app
    /// settings / Key Vault, never in a committed file.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the admin SPA, so a future password-reset mail can build a link the recipient
    /// can actually open. The API does not know its own front end otherwise.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(FromAddress);
}
