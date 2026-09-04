using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Email;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

/// <summary>Forgot-password. Uses the <b>real</b> <see cref="LoginRateLimiter"/> — a fake would prove nothing about the Postgres-backed, per-action limit.</summary>
[Collection(nameof(PostgresCollection))]
public class PasswordResetTests
{
    private readonly PostgresFixture _fixture;

    public PasswordResetTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task A_request_for_an_approved_account_issues_one_reset_link()
    {
        var email = await NewApprovedUserAsync();

        var emails = await ForgotAsync(email);

        Assert.Equal(1, emails.SentCount);
        Assert.Contains("/set-password?token=", emails.LastMessage!.HtmlBody);
        // Distinct from the bootstrap mail, which says "Set your ... super admin password".
        Assert.Contains("Reset your FrostWoodTech password", emails.LastMessage.Subject);

        await using var db = _fixture.CreateContext();
        var token = await db.PasswordTokens.SingleAsync(t => t.User.Email == email);

        Assert.Equal(PasswordTokenPurpose.Reset, token.Purpose);
        Assert.Null(token.UsedAt);
        // One hour, not the setup link's twenty-four.
        Assert.InRange(token.ExpiresAt - token.CreatedAt, TimeSpan.FromMinutes(59), TimeSpan.FromMinutes(61));
    }

    [Fact]
    public async Task An_unknown_address_gets_the_same_response_and_no_mail()
    {
        var known = await ForgotAsync(await NewApprovedUserAsync());
        var unknown = await ForgotAsync($"nobody-{Guid.NewGuid():N}@example.com");

        Assert.True(known.Result!.IsSuccess);
        Assert.True(unknown.Result!.IsSuccess);
        // Byte-identical bodies: this is the property that stops the endpoint being an oracle.
        Assert.Equal(known.Result.Value!.Message, unknown.Result.Value!.Message);
        Assert.Equal(0, unknown.SentCount);
    }

    [Fact]
    public async Task A_second_request_invalidates_the_first_link()
    {
        var email = await NewApprovedUserAsync();

        await ForgotAsync(email);
        var firstRawToken = await RawResetTokenForAsync(email);

        await ForgotAsync(email);

        await using (var db = _fixture.CreateContext())
        {
            var stale = await NewService(db, out _)
                .SetPasswordAsync(NewSetPassword(firstRawToken), CancellationToken.None);

            Assert.False(stale.IsSuccess);
            Assert.Equal("setup_token_already_used", stale.Error!.Code);
        }

        // The newest link still works.
        var secondRawToken = await RawResetTokenForAsync(email);

        await using var fresh = _fixture.CreateContext();
        var redeemed = await NewService(fresh, out _)
            .SetPasswordAsync(NewSetPassword(secondRawToken), CancellationToken.None);

        Assert.True(redeemed.IsSuccess);
    }

    [Fact]
    public async Task Redeeming_a_reset_link_changes_the_password_and_revokes_every_session()
    {
        var email = await NewApprovedUserAsync();

        // Sign in first, so there is a live refresh token for the reset to kill.
        await using (var db = _fixture.CreateContext())
        {
            var signIn = await NewService(db, out _).LoginAsync(
                new LoginRequest { Email = email, Password = OriginalPassword }, "127.0.0.1", CancellationToken.None);
            Assert.True(signIn.IsSuccess);
        }

        await ForgotAsync(email);
        var rawToken = await RawResetTokenForAsync(email);

        await using (var db = _fixture.CreateContext())
        {
            Assert.True((await NewService(db, out _)
                .SetPasswordAsync(NewSetPassword(rawToken), CancellationToken.None)).IsSuccess);
        }

        await using var after = _fixture.CreateContext();

        var live = await after.RefreshTokens.CountAsync(t => t.User.Email == email && t.RevokedAt == null);
        Assert.Equal(0, live);

        var service = NewService(after, out _);

        var withOld = await service.LoginAsync(
            new LoginRequest { Email = email, Password = OriginalPassword }, "127.0.0.1", CancellationToken.None);
        Assert.False(withOld.IsSuccess);

        var withNew = await service.LoginAsync(
            new LoginRequest { Email = email, Password = NewPassword }, "127.0.0.1", CancellationToken.None);
        Assert.True(withNew.IsSuccess);
    }

    [Fact]
    public async Task An_unverified_account_gets_the_generic_response_and_no_reset_link()
    {
        // It has its own resend-verification flow; a reset link would be a way around verifying.
        var email = await NewUserAsync(UserStatus.EmailVerificationRequired);

        var emails = await ForgotAsync(email);

        Assert.True(emails.Result!.IsSuccess);
        Assert.Equal(0, emails.SentCount);
    }

    [Fact]
    public async Task A_disabled_account_gets_the_generic_response_and_no_reset_link()
    {
        var email = await NewUserAsync(UserStatus.Disabled);

        var emails = await ForgotAsync(email);

        Assert.True(emails.Result!.IsSuccess);
        Assert.Equal(0, emails.SentCount);
    }

    [Fact]
    public async Task A_google_only_account_can_still_request_a_link_and_add_a_password()
    {
        var email = await NewUserAsync(UserStatus.Approved, passwordHash: null, googleSubjectId: $"g-{Guid.NewGuid():N}");

        var emails = await ForgotAsync(email);
        Assert.Equal(1, emails.SentCount);

        var rawToken = await RawResetTokenForAsync(email);

        await using (var db = _fixture.CreateContext())
        {
            Assert.True((await NewService(db, out _)
                .SetPasswordAsync(NewSetPassword(rawToken), CancellationToken.None)).IsSuccess);
        }

        await using var after = _fixture.CreateContext();
        var signIn = await NewService(after, out _).LoginAsync(
            new LoginRequest { Email = email, Password = NewPassword }, "127.0.0.1", CancellationToken.None);

        Assert.True(signIn.IsSuccess);
    }

    [Fact]
    public async Task The_fourth_request_for_one_address_inside_the_window_is_dropped()
    {
        var email = await NewApprovedUserAsync();

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(1, (await ForgotAsync(email, ip: $"10.0.0.{i}")).SentCount);
        }

        // Still a 200 with the same body — a throttled caller must not be able to tell either.
        var fourth = await ForgotAsync(email, ip: "10.0.0.9");

        Assert.True(fourth.Result!.IsSuccess);
        Assert.Equal(0, fourth.SentCount);
    }

    [Fact]
    public async Task The_fourth_request_from_one_ip_inside_the_window_is_dropped()
    {
        const string ip = "203.0.113.7";

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(1, (await ForgotAsync(await NewApprovedUserAsync(), ip)).SentCount);
        }

        var fourth = await ForgotAsync(await NewApprovedUserAsync(), ip);

        Assert.True(fourth.Result!.IsSuccess);
        Assert.Equal(0, fourth.SentCount);
    }

    [Fact]
    public async Task Reset_requests_and_failed_logins_are_counted_separately()
    {
        var email = await NewApprovedUserAsync();
        const string ip = "198.51.100.4";

        // Exhaust the reset budget.
        for (var i = 0; i < 3; i++)
        {
            await ForgotAsync(email, ip);
        }
        Assert.Equal(0, (await ForgotAsync(email, ip)).SentCount);

        // Login has its own, untouched budget.
        await using var db = _fixture.CreateContext();
        var signIn = await NewService(db, out _).LoginAsync(
            new LoginRequest { Email = email, Password = OriginalPassword }, ip, CancellationToken.None);

        Assert.True(signIn.IsSuccess);
    }

    private const string OriginalPassword = "original horse battery staple";
    private const string NewPassword = "brand new battery staple";

    private static SetPasswordRequest NewSetPassword(string token) => new()
    {
        Token = token,
        Password = NewPassword,
        ConfirmPassword = NewPassword
    };

    private Task<string> NewApprovedUserAsync() => NewUserAsync(UserStatus.Approved);

    private async Task<string> NewUserAsync(
        UserStatus status,
        string? passwordHash = "",
        string? googleSubjectId = null)
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";

        await using var db = _fixture.CreateContext();

        db.Users.Add(new User
        {
            Email = email,
            FirstName = "Ada",
            LastName = "Lovelace",
            // "" is the sentinel for "give it the standard test password"; null means Google-only.
            PasswordHash = passwordHash == string.Empty ? PasswordHasher.Hash(OriginalPassword) : passwordHash,
            GoogleSubjectId = googleSubjectId,
            Role = UserRole.Admin,
            Status = status,
            EmailVerifiedAt = status == UserStatus.EmailVerificationRequired ? null : DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        return email;
    }

    private async Task<ForgotOutcome> ForgotAsync(string email, string? ip = "192.0.2.1")
    {
        await using var db = _fixture.CreateContext();
        var service = NewService(db, out var emails);

        var result = await service.ForgotPasswordAsync(
            new ForgotPasswordRequest { Email = email }, ip, CancellationToken.None);

        return new ForgotOutcome(emails.SentCount, emails.LastMessage, result);
    }

    private sealed record ForgotOutcome(
        int SentCount,
        EmailMessage? LastMessage,
        ServiceResult<ForgotPasswordResponse>? Result);

    /// <summary>Mints a token the same way the service does and overwrites the stored hash to match.</summary>
    private async Task<string> RawResetTokenForAsync(string email)
    {
        await using var db = _fixture.CreateContext();

        var token = await db.PasswordTokens
            .Include(t => t.User)
            .Where(t => t.User.Email == email && t.Purpose == PasswordTokenPurpose.Reset && t.UsedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .FirstAsync();

        var rawToken = EmailVerificationTokenGenerator.Create();
        token.TokenHash = EmailVerificationTokenGenerator.Hash(rawToken);
        await db.SaveChangesAsync();

        return rawToken;
    }

    private static UserService NewService(FrostWoodTechDbContext db, out FakeEmailService emails)
    {
        emails = new FakeEmailService();

        return new UserService(
            db,
            new FakeJwtTokenService(),
            new FakeGoogleTokenValidator(),
            // The real one, on the real table — see the class summary.
            new LoginRateLimiter(db),
            new CurrentUser { UserId = Guid.NewGuid(), Role = UserRole.Admin },
            Options.Create(new JwtOptions()),
            emails,
            Options.Create(new EmailOptions { BaseUrl = "https://admin.frostwoodtech.test" }),
            NullLogger<UserService>.Instance);
    }

    private sealed class FakeEmailService : IEmailService
    {
        public int SentCount { get; private set; }

        public EmailMessage? LastMessage { get; private set; }

        public Task<ServiceResult<EmailSendResult>> SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            SentCount++;
            LastMessage = message;

            return Task.FromResult(ServiceResult<EmailSendResult>.Success(new EmailSendResult { Provider = "fake" }));
        }
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user) =>
            ("fake-token", DateTimeOffset.UtcNow.AddMinutes(15));

        public TokenValidationParameters CreateValidationParameters() => new();
    }

    private sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
    {
        public Task<ServiceResult<GoogleIdentity>> ValidateAsync(string idToken, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Not exercised by these tests.");
    }
}
