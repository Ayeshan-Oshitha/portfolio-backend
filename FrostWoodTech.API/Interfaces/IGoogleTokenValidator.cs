using FrostWoodTech.API.Common;

namespace FrostWoodTech.API.Interfaces;

/// <summary>The verified contents of a Google ID token.</summary>
/// <param name="Subject">Google's stable user id — the <c>sub</c> claim.</param>
public sealed record GoogleIdentity(
    string Subject,
    string Email,
    bool EmailVerified,
    string? FirstName,
    string? LastName,
    string? AvatarUrl);

public interface IGoogleTokenValidator
{
    /// <summary>
    /// Validates the token's signature against Google's published keys and checks its issuer and
    /// audience. Returns a failure rather than throwing so the caller stays a thin service method.
    /// </summary>
    Task<ServiceResult<GoogleIdentity>> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
