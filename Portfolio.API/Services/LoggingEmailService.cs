using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Portfolio.API.Common;
using Portfolio.API.Email;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Services;

/// <summary>
/// The default transport: it validates the message and writes it to the log instead of sending
/// it. Local dev and CI get a working <see cref="IEmailService"/> with no account, no key and no
/// network call, and a real provider inherits the same validation contract because the checks
/// live before the send rather than inside it.
/// </summary>
public class LoggingEmailService : IEmailService
{
    /// <summary>Enough of the body to recognise the mail in a log, not enough to fill it.</summary>
    private const int BodyPreviewLength = 200;

    private readonly EmailOptions _options;
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(IOptions<EmailOptions> options, ILogger<LoggingEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<ServiceResult<EmailSendResult>> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var validation = Validate(message);
        if (validation is not null)
        {
            return Task.FromResult(validation);
        }

        _logger.LogInformation(
            "Email not sent (provider 'log'). To: {To}, Subject: {Subject}, Body: {BodyPreview}",
            message.To,
            message.Subject,
            Preview(message.TextBody ?? message.HtmlBody));

        return Task.FromResult(ServiceResult<EmailSendResult>.Success(new EmailSendResult
        {
            Provider = "log"
        }));
    }

    /// <summary>
    /// Returns the failure to report, or null when the message is sendable. A misconfigured
    /// deployment is a bad request the operator can act on, not a 500.
    /// </summary>
    private ServiceResult<EmailSendResult>? Validate(EmailMessage message)
    {
        if (!_options.IsConfigured)
        {
            return ServiceResult<EmailSendResult>.Validation(
                "Email is not configured. Set Email__FromAddress.");
        }

        if (string.IsNullOrWhiteSpace(message.To) || !message.To.Contains('@', StringComparison.Ordinal))
        {
            return ServiceResult<EmailSendResult>.Validation("A valid recipient address is required.");
        }

        if (string.IsNullOrWhiteSpace(message.Subject))
        {
            return ServiceResult<EmailSendResult>.Validation("subject is required.");
        }

        return string.IsNullOrWhiteSpace(message.HtmlBody)
            ? ServiceResult<EmailSendResult>.Validation("htmlBody is required.")
            : null;
    }

    private static string Preview(string body) =>
        body.Length <= BodyPreviewLength ? body : body[..BodyPreviewLength] + "…";
}
