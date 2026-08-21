namespace FrostWoodTech.API.Email;

/// <summary>
/// What the transport did with the message. <see cref="MessageId"/> is the provider's own id
/// where it returns one — the handle you need when someone asks why a mail never arrived.
/// </summary>
public sealed class EmailSendResult
{
    /// <summary>Provider-assigned id, or null for transports that do not issue one.</summary>
    public string? MessageId { get; set; }

    /// <summary>Which transport handled it, e.g. <c>log</c>.</summary>
    public string Provider { get; set; } = string.Empty;
}
