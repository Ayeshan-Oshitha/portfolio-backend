namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>
/// Always the same shape, whatever the email address actually resolved to — see
/// <c>UserService.ResendVerificationAsync</c> for why the response must not vary.
/// </summary>
public sealed class ResendVerificationResponse
{
    public string Message { get; init; } = "If that account needs verification, we've sent a new email.";
}
