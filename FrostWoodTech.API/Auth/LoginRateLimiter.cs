using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Data;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Auth;

/// <summary>
/// A fixed window counted in Postgres. Functions scale out, so an in-process counter would be
/// per-instance and would reset on every cold start — which is no limit at all.
/// </summary>
public sealed class LoginRateLimiter : ILoginRateLimiter
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Per-window ceilings, per action. Login: generous for a misremembered password, higher
    /// per-IP since offices share addresses. Reset: tight — a person needs one link, and a loose
    /// limit here is a way to flood somebody else's inbox.
    /// </summary>
    private static (int PerEmail, int PerIp) LimitsFor(AuthAttemptAction action) => action switch
    {
        AuthAttemptAction.PasswordReset => (3, 3),
        _ => (10, 30)
    };

    private readonly FrostWoodTechDbContext _db;

    public LoginRateLimiter(FrostWoodTechDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsBlockedAsync(
        string email,
        string? ipAddress,
        AuthAttemptAction action,
        CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow - Window;
        var (perEmail, perIp) = LimitsFor(action);

        var emailAttempts = await _db.LoginAttempts
            .CountAsync(a => a.Email == email && a.Action == action && a.AttemptedAt >= since, cancellationToken);

        if (emailAttempts >= perEmail)
        {
            return true;
        }

        if (ipAddress is null)
        {
            return false;
        }

        var ipAttempts = await _db.LoginAttempts
            .CountAsync(a => a.IpAddress == ipAddress && a.Action == action && a.AttemptedAt >= since, cancellationToken);

        return ipAttempts >= perIp;
    }

    public async Task RecordAttemptAsync(
        string email,
        string? ipAddress,
        AuthAttemptAction action,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        _db.LoginAttempts.Add(new LoginAttempt
        {
            Email = email,
            IpAddress = ipAddress,
            Action = action,
            AttemptedAt = now
        });

        // Swept here rather than on a timer — this is the only path that needs the table small.
        var cutoff = now - Window;
        await _db.LoginAttempts
            .Where(a => a.AttemptedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task ClearAsync(string email, AuthAttemptAction action, CancellationToken cancellationToken) =>
        _db.LoginAttempts
            .Where(a => a.Email == email && a.Action == action)
            .ExecuteDeleteAsync(cancellationToken);
}
