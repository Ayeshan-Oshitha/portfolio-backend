using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Email;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Data;

/// <summary>
/// Creates the single <c>super_admin</c> from app settings on startup with <b>no password</b>,
/// and emails a single-use setup link — so no password ever lives in app settings, and an
/// existing account's password is never touched. Not an EF <c>HasData</c> seed: that would put
/// the email in a committed migration and can't send mail.
/// </summary>
public static class SuperAdminSeeder
{
    /// <summary>How long the emailed setup link stays usable — the same 24h as a verification link.</summary>
    private static readonly TimeSpan SetupTokenLifetime = TimeSpan.FromHours(24);

    /// <summary>Any fixed number — serialises two cold starts that boot at once.</summary>
    private const long AdvisoryLockKey = 4_820_117_003L;

    public static async Task EnsureSeededAsync(
        FrostWoodTechDbContext db,
        SuperAdminOptions options,
        IEmailService email,
        EmailOptions emailOptions,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!options.IsConfigured)
        {
            logger.LogWarning("SuperAdmin__Email is not set — skipping super admin seeding.");
            return;
        }

        var address = options.Email.Trim().ToLowerInvariant();
        string rawToken;

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            // Held until the transaction ends, making the lookups and insert below atomic.
            await db.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock({0})", [AdvisoryLockKey], cancellationToken);

            // IgnoreQueryFilters: a deleted super admin still counts as existing.
            var existingSuperAdmin = await db.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Role == UserRole.SuperAdmin, cancellationToken);

            if (existingSuperAdmin is not null)
            {
                if (!string.Equals(existingSuperAdmin.Email, address, StringComparison.OrdinalIgnoreCase))
                {
                    // auth.md: exactly one super admin — never promote a second from config.
                    logger.LogWarning(
                        "A super admin already exists as {ExistingEmail} but SuperAdmin__Email is {ConfiguredEmail}. Leaving the existing account alone.",
                        existingSuperAdmin.Email,
                        address);
                }

                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            // Seed-if-missing only — an address already owned by someone else is a job for a
            // human, not a seeder that would have to overwrite a real account's role.
            var addressTaken = await db.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email == address, cancellationToken);

            if (addressTaken)
            {
                logger.LogWarning(
                    "SuperAdmin__Email {Email} already belongs to a non-super-admin account. Not seeding, and not modifying that account.",
                    address);

                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            var now = DateTimeOffset.UtcNow;
            rawToken = EmailVerificationTokenGenerator.Create();

            var user = new User
            {
                Email = address,
                FirstName = options.FirstName,
                LastName = options.LastName,
                // Unreachable by password login until the emailed link is redeemed — LoginAsync
                // rejects a null hash outright.
                PasswordHash = null,
                Role = UserRole.SuperAdmin,
                Status = UserStatus.Approved,
                // Trusted config, not user input, so nothing to verify — and leaving this unset
                // would leave Status at EmailVerificationRequired, blocking login even post-setup.
                EmailVerifiedAt = now
            };

            db.Users.Add(user);
            db.PasswordTokens.Add(new PasswordToken
            {
                User = user,
                TokenHash = EmailVerificationTokenGenerator.Hash(rawToken),
                Purpose = PasswordTokenPurpose.Setup,
                ExpiresAt = now.Add(SetupTokenLifetime),
                CreatedAt = now
            });

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // The advisory lock covers the common race; the unique indexes on email and on
                // role='super_admin' catch anything else. Either way the account now exists, so
                // don't send a link for a token that just rolled back.
                logger.LogWarning(
                    ex, "Super admin seeding lost a race — the account already exists. No mail sent.");

                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            // Warning, not Information: should happen exactly once per deployment.
            logger.LogWarning(
                "Seeded the super admin account {Email} with no password. Sending a setup link.", address);
        }

        await SendSetupLinkAsync(
            email, emailOptions, address, options.FirstName, rawToken, logger, cancellationToken);
    }

    /// <summary>Called after the commit, so a slow mail provider can't hold the transaction open.</summary>
    private static async Task SendSetupLinkAsync(
        IEmailService email,
        EmailOptions emailOptions,
        string address,
        string firstName,
        string rawToken,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var link = $"{emailOptions.BaseUrl.TrimEnd('/')}/set-password?token={rawToken}";

        var sent = await email.SendAsync(new EmailMessage
        {
            To = address,
            Subject = "Set your FrostWoodTech super admin password",
            HtmlBody =
                $"<p>Hi {firstName},</p>" +
                "<p>Your FrostWoodTech super admin account has been created. Choose a password to finish setting it up:</p>" +
                $"<p><a href=\"{link}\">{link}</a></p>" +
                "<p>This link expires in 24 hours and can only be used once. Until then the account cannot be signed in to.</p>",
            TextBody =
                $"Hi {firstName},\n\n" +
                "Your FrostWoodTech super admin account has been created. Choose a password to finish setting it up:\n" +
                $"{link}\n\n" +
                "This link expires in 24 hours and can only be used once. Until then the account cannot be signed in to."
        }, cancellationToken);

        if (!sent.IsSuccess)
        {
            // Error, not Warning: unlike verification mail there's no self-service resend here.
            logger.LogError(
                "The super admin setup link for {Email} could not be sent: {Code}. Issue a fresh one before the 24 hours are up.",
                address,
                sent.Error!.Code);
        }
    }
}
