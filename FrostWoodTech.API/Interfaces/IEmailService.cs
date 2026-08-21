using FrostWoodTech.API.Common;
using FrostWoodTech.API.Email;

namespace FrostWoodTech.API.Interfaces;

/// <summary>
/// The one seam every outbound mail goes through — password reset tokens, account rejection
/// notices, anything later. Callers depend on this, never on a provider SDK, so swapping the
/// transport for Resend or SES touches one DI line and nothing else.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends one message. Returns a failed <see cref="ServiceResult{T}"/> rather than throwing
    /// when the provider is unreachable or misconfigured, so the caller decides whether a mail
    /// that did not go out should fail the request it belongs to.
    ///
    /// Templating, retries and batching are deliberately absent — a caller builds its own subject
    /// and body and calls this once.
    /// </summary>
    Task<ServiceResult<EmailSendResult>> SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
