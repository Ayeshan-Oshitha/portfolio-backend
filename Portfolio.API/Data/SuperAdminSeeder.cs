using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Portfolio.API.Auth;
using Portfolio.API.Entities;
using Portfolio.API.Enums;

namespace Portfolio.API.Data;

/// <summary>
/// Creates the single <c>super_admin</c> from app settings on startup.
/// <para>
/// Deliberately not an EF <c>HasData</c> seed: a migration is committed to git, so the email and
/// password hash would end up in source control, and a fixed seed row would never pick up a
/// rotated <c>SuperAdmin__Password</c>. This runs after the schema exists instead, and is safe to
/// run on every cold start.
/// </para>
/// </summary>
public static class SuperAdminSeeder
{
    /// <summary>
    /// Any fixed number. Functions scale out, so two instances can boot at once — the lock makes
    /// the second one wait rather than insert a duplicate.
    /// </summary>
    private const long AdvisoryLockKey = 4_820_117_003L;

    public static async Task EnsureSeededAsync(
        PortfolioDbContext db,
        SuperAdminOptions options,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!options.IsConfigured)
        {
            logger.LogWarning(
                "SuperAdmin__Email / SuperAdmin__Password are not set — skipping super admin seeding.");
            return;
        }

        var email = options.Email.Trim().ToLowerInvariant();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Held until the transaction ends, so the lookup and the insert below are one critical section.
        await db.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})", [AdvisoryLockKey], cancellationToken);

        // Ignoring the soft-delete filter: a deleted super admin is revived, never duplicated.
        var existing = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Role == UserRole.SuperAdmin, cancellationToken);

        if (existing is null)
        {
            db.Users.Add(new User
            {
                Email = email,
                FirstName = options.FirstName,
                LastName = options.LastName,
                PasswordHash = PasswordHasher.Hash(options.Password),
                Role = UserRole.SuperAdmin,
                Status = UserStatus.Approved
            });

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Seeded the super admin account {Email}.", email);
            return;
        }

        if (!string.Equals(existing.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            // auth.md: exactly one super admin. Promoting a second one from config would be a
            // silent privilege grant, so say so and change nothing.
            logger.LogWarning(
                "A super admin already exists as {ExistingEmail} but SuperAdmin__Email is {ConfiguredEmail}. Leaving the existing account alone.",
                existing.Email,
                email);

            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        var changed = false;

        if (!PasswordHasher.Verify(existing.PasswordHash, options.Password))
        {
            existing.PasswordHash = PasswordHasher.Hash(options.Password);
            changed = true;
        }

        // The super admin must never be locked out of their own CMS.
        if (existing.Status != UserStatus.Approved || existing.IsDeleted)
        {
            existing.Status = UserStatus.Approved;
            existing.IsDeleted = false;
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Restored the super admin account {Email} from app settings.", email);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
