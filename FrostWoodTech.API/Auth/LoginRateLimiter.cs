using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Data;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Auth;

/// <summary>
/// A fixed window counted in Postgres. Functions scale out, so an in-process counter would be
/// per-instance and would reset on every cold start — which is no limit at all.
/// </summary>
public sealed class LoginRateLimiter : ILoginRateLimiter
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    /// <summary>Generous enough for someone genuinely misremembering their password.</summary>
    private const int MaxFailuresPerEmail = 10;

    /// <summary>
    /// Higher than the per-email limit because a whole office can share one address, but low
    /// enough to blunt someone spraying one password across many accounts.
    /// </summary>
    private const int MaxFailuresPerIp = 30;

    private readonly FrostWoodTechDbContext _db;

    public LoginRateLimiter(FrostWoodTechDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsBlockedAsync(string email, string? ipAddress, CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow - Window;

        var emailFailures = await _db.LoginAttempts
            .CountAsync(a => a.Email == email && a.AttemptedAt >= since, cancellationToken);

        if (emailFailures >= MaxFailuresPerEmail)
        {
            return true;
        }

        if (ipAddress is null)
        {
            return false;
        }

        var ipFailures = await _db.LoginAttempts
            .CountAsync(a => a.IpAddress == ipAddress && a.AttemptedAt >= since, cancellationToken);

        return ipFailures >= MaxFailuresPerIp;
    }

    public async Task RecordFailureAsync(string email, string? ipAddress, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        _db.LoginAttempts.Add(new LoginAttempt
        {
            Email = email,
            IpAddress = ipAddress,
            AttemptedAt = now
        });

        // Swept here rather than on a timer function: the table only matters at sign-in, so this
        // is the one code path that needs it kept small.
        var cutoff = now - Window;
        await _db.LoginAttempts
            .Where(a => a.AttemptedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task ClearAsync(string email, CancellationToken cancellationToken) =>
        _db.LoginAttempts
            .Where(a => a.Email == email)
            .ExecuteDeleteAsync(cancellationToken);
}
