namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>
/// Always the same shape and the same text, whatever the email address actually resolved to —
/// see <c>UserService.ForgotPasswordAsync</c> for why the response must not vary.
/// </summary>
public sealed class ForgotPasswordResponse
{
    public string Message { get; init; } = "If that account exists, we've sent a password reset email.";
}
