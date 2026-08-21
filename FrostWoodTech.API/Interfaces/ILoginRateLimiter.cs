namespace FrostWoodTech.API.Interfaces;

/// <summary>
/// Rate limiting for the sign-in surface — per email and per IP, as the auth rules require.
/// </summary>
public interface ILoginRateLimiter
{
    /// <summary>
    /// True when this email or address has failed too many times inside the window and should be
    /// refused without the password even being checked.
    /// </summary>
    Task<bool> IsBlockedAsync(string email, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>Records a failure. Only failures count — a successful sign-in is not suspicious.</summary>
    Task RecordFailureAsync(string email, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>Clears an email's failures after it signs in successfully.</summary>
    Task ClearAsync(string email, CancellationToken cancellationToken);
}
