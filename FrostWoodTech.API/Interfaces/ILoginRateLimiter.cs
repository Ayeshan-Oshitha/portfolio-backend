using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

/// <summary>Rate limiting for the sign-in surface — per email and per IP, per <see cref="AuthAttemptAction"/>.</summary>
public interface ILoginRateLimiter
{
    /// <summary>True when this email or address should be refused before any real work is done.</summary>
    Task<bool> IsBlockedAsync(
        string email,
        string? ipAddress,
        AuthAttemptAction action,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records one attempt. Login records failures only; reset records every request — there's no
    /// such thing as a failed one from the caller's side.
    /// </summary>
    Task RecordAttemptAsync(
        string email,
        string? ipAddress,
        AuthAttemptAction action,
        CancellationToken cancellationToken);

    /// <summary>Clears an email's attempts for one action, e.g. after a successful sign-in.</summary>
    Task ClearAsync(string email, AuthAttemptAction action, CancellationToken cancellationToken);
}
