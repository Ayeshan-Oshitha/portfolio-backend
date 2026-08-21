namespace FrostWoodTech.API.Email;

/// <summary>
/// One outbound mail, in terms every provider understands. There is no <c>From</c> here — it
/// comes from <see cref="EmailOptions.FromAddress"/> so no caller can spoof the sender.
/// </summary>
public sealed class EmailMessage
{
    public string To { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Optional plain-text alternative. Null is fine — providers derive one from the HTML.
    /// </summary>
    public string? TextBody { get; set; }

    /// <summary>Optional address for replies, when it differs from the sender.</summary>
    public string? ReplyTo { get; set; }
}
