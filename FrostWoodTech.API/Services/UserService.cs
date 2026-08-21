using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Services;

public class UserService : IUserService
{
    private const int MinimumPasswordLength = 8;

    private const string SuperAdminOnly = "Only the super admin can manage users.";

    private static readonly Expression<Func<User, AdminUserResponse>> AdminProjection = user =>
        new AdminUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            Status = user.Status,
            LastLoginAt = user.LastLoginAt,
            ApprovedAt = user.ApprovedAt,
            RejectionReason = user.RejectionReason,
            CreatedAt = user.CreatedAt
        };

    private static readonly Func<User, AdminUserResponse> ToResponse = AdminProjection.Compile();

    private readonly FrostWoodTechDbContext _db;
    private readonly IJwtTokenService _tokens;
    private readonly IGoogleTokenValidator _google;
    private readonly ILoginRateLimiter _rateLimiter;
    private readonly CurrentUser _currentUser;
    private readonly JwtOptions _jwtOptions;

    public UserService(
        FrostWoodTechDbContext db,
        IJwtTokenService tokens,
        IGoogleTokenValidator google,
        ILoginRateLimiter rateLimiter,
        CurrentUser currentUser,
        IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _tokens = tokens;
        _google = google;
        _rateLimiter = rateLimiter;
        _currentUser = currentUser;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<ServiceResult<AdminUserResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var firstName = Blank(request.FirstName);
        var lastName = Blank(request.LastName);
        var email = NormaliseEmail(request.Email);

        var validationError = ValidateRegistration(firstName, lastName, email, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminUserResponse>.Validation(validationError);
        }

        // Ignoring the soft-delete filter: a deleted account still owns its email address.
        var emailTaken = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailTaken)
        {
            return ServiceResult<AdminUserResponse>.Conflict(
                "email_taken", "That email address is already registered.");
        }

        var user = new User
        {
            Email = email!,
            FirstName = firstName!,
            LastName = lastName!,
            PasswordHash = PasswordHasher.Hash(request.Password!),
            // Registration is open but powerless: no token until the super admin approves.
            Role = UserRole.Admin,
            Status = UserStatus.Pending
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var email = NormaliseEmail(request.Email);
        var password = request.Password;

        if (email is null || string.IsNullOrEmpty(password))
        {
            return ServiceResult<AuthResponse>.Validation("Email and password are required.");
        }

        // Checked before the password so a blocked caller costs an index lookup rather than an
        // Argon2 hash — otherwise the throttle is itself the cheapest way to burn the CPU.
        if (await _rateLimiter.IsBlockedAsync(email, ipAddress, cancellationToken))
        {
            return ServiceResult<AuthResponse>.Unauthorized(
                "too_many_attempts",
                "Too many failed sign-in attempts. Wait a few minutes and try again.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            // Same work, same answer as a wrong password: the response must not reveal whether
            // the account exists.
            PasswordHasher.BurnVerifyTime(password);
            await _rateLimiter.RecordFailureAsync(email, ipAddress, cancellationToken);

            return InvalidCredentials();
        }

        if (!PasswordHasher.Verify(user.PasswordHash, password))
        {
            await _rateLimiter.RecordFailureAsync(email, ipAddress, cancellationToken);

            return InvalidCredentials();
        }

        // Checked after the password so an anonymous caller cannot probe account state.
        // A non-approved user is rejected here at token issue rather than handed a scopeless token.
        if (user.Status != UserStatus.Approved)
        {
            return NotApproved(user);
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;

        // A successful sign-in clears the slate, so earlier fumbled attempts do not count
        // towards a later lockout.
        await _rateLimiter.ClearAsync(email, cancellationToken);

        var (response, _) = await IssueTokensAsync(user, cancellationToken);

        return ServiceResult<AuthResponse>.Success(response);
    }

    public async Task<ServiceResult<AuthResponse>> GoogleSignInAsync(
        GoogleSignInRequest request,
        CancellationToken cancellationToken)
    {
        if (Blank(request.IdToken) is not { } idToken)
        {
            return ServiceResult<AuthResponse>.Validation("An idToken is required.");
        }

        var validated = await _google.ValidateAsync(idToken, cancellationToken);
        if (!validated.IsSuccess)
        {
            return ServiceResult<AuthResponse>.Failure(validated.Error!);
        }

        var identity = validated.Value!;

        // An unverified address is not proof of anything — matching on it would let anyone who
        // can create a Google account with someone else's email claim their CMS user.
        if (!identity.EmailVerified)
        {
            return ServiceResult<AuthResponse>.Unauthorized(
                "google_email_unverified", "This Google account's email address is not verified.");
        }

        // Match on the subject first: it is stable, whereas an address can be reassigned.
        // IgnoreQueryFilters so a soft-deleted account is found and refused rather than silently
        // re-created as a brand new pending user.
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.GoogleSubjectId == identity.Subject || u.Email == identity.Email,
                cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Email = identity.Email,
                FirstName = identity.FirstName ?? identity.Email,
                LastName = identity.LastName ?? string.Empty,
                GoogleSubjectId = identity.Subject,
                AvatarUrl = identity.AvatarUrl,
                // No password: this account can only ever arrive through Google.
                PasswordHash = null,
                Role = UserRole.Admin,
                Status = UserStatus.Pending
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            return NotApproved(user);
        }

        if (user.IsDeleted)
        {
            return ServiceResult<AuthResponse>.Forbidden(
                "account_disabled", "This account has been disabled. Contact the super admin.");
        }

        // First Google sign-in for an account that registered with a password: link the two.
        // The password still works — this adds a way in, it does not replace one.
        user.GoogleSubjectId ??= identity.Subject;
        user.AvatarUrl = identity.AvatarUrl ?? user.AvatarUrl;

        if (user.Status != UserStatus.Approved)
        {
            await _db.SaveChangesAsync(cancellationToken);

            return NotApproved(user);
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;

        var (response, _) = await IssueTokensAsync(user, cancellationToken);

        return ServiceResult<AuthResponse>.Success(response);
    }

    public async Task<ServiceResult<AuthResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (Blank(request.RefreshToken) is not { } presented)
        {
            return ServiceResult<AuthResponse>.Validation("A refresh token is required.");
        }

        var hash = RefreshTokenGenerator.Hash(presented);

        // IgnoreQueryFilters so a soft-deleted owner still loads — the User navigation would
        // otherwise come back null and the account checks below would never run.
        var stored = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null)
        {
            return InvalidRefreshToken();
        }

        if (stored.RevokedAt is not null)
        {
            // A revoked token coming back means it was replayed — most likely stolen, since the
            // legitimate client would be holding its replacement. Kill the whole family rather
            // than just this one.
            await RevokeAllForUserAsync(stored.UserId, cancellationToken);

            return ServiceResult<AuthResponse>.Unauthorized(
                "refresh_token_reused",
                "This refresh token was already used. All sessions have been signed out.");
        }

        if (stored.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return ServiceResult<AuthResponse>.Unauthorized(
                "refresh_token_expired", "This refresh token has expired. Sign in again.");
        }

        // The check that bounds a revoked user's access: they cannot renew, whatever their old
        // access token still says.
        if (stored.User.Status != UserStatus.Approved || stored.User.IsDeleted)
        {
            await RevokeAllForUserAsync(stored.UserId, cancellationToken);

            return NotApproved(stored.User);
        }

        var (issued, replacement) = await IssueTokensAsync(stored.User, cancellationToken);

        stored.RevokedAt = DateTimeOffset.UtcNow;
        stored.ReplacedByTokenId = replacement.Id;

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Success(issued);
    }

    public async Task<ServiceResult<bool>> LogoutAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        // No error for an unknown token: signing out is not a way to discover which tokens exist.
        if (Blank(request.RefreshToken) is { } presented)
        {
            var hash = RefreshTokenGenerator.Hash(presented);

            await _db.RefreshTokens
                .Where(t => t.TokenHash == hash && t.RevokedAt == null)
                .ExecuteUpdateAsync(
                    t => t.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow), cancellationToken);
        }

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<AdminUserResponse>> GetMeAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return ServiceResult<AdminUserResponse>.Unauthorized("unauthenticated", "No signed-in user.");
        }

        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return user is null
            ? ServiceResult<AdminUserResponse>.Unauthorized("unauthenticated", "This account no longer exists.")
            : ServiceResult<AdminUserResponse>.Success(user);
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return ServiceResult<bool>.Unauthorized("unauthenticated", "No signed-in user.");
        }

        var validationError = ValidatePassword(request.NewPassword, request.ConfirmNewPassword, "New password");
        if (validationError is not null)
        {
            return ServiceResult<bool>.Validation(validationError);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<bool>.Unauthorized("unauthenticated", "This account no longer exists.");
        }

        if (string.IsNullOrEmpty(request.CurrentPassword)
            || !PasswordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            return ServiceResult<bool>.Unauthorized("invalid_credentials", "The current password is incorrect.");
        }

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword!);
        await _db.SaveChangesAsync(cancellationToken);

        // Changing a password signs out everywhere else, including whoever prompted the change.
        await RevokeAllForUserAsync(userId, cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<PagedResult<AdminUserResponse>>> GetAllAsync(
        string? search,
        UserStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (RequireSuperAdmin<PagedResult<AdminUserResponse>>() is { } denied)
        {
            return denied;
        }

        var query = _db.Users.AsNoTracking();

        if (status is { } wanted)
        {
            query = query.Where(u => u.Status == wanted);
        }

        if (Blank(search) is { } term)
        {
            var pattern = $"%{term}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Email, pattern)
                || EF.Functions.ILike(u.FirstName, pattern)
                || EF.Functions.ILike(u.LastName, pattern));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return ServiceResult<PagedResult<AdminUserResponse>>.Success(new PagedResult<AdminUserResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        });
    }

    public async Task<ServiceResult<AdminUserResponse>> ApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        var loaded = await LoadManageableUserAsync(id, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return ServiceResult<AdminUserResponse>.Failure(loaded.Error!);
        }

        var user = loaded.Value!;

        user.Status = UserStatus.Approved;
        user.ApprovedBy = _currentUser.UserId;
        user.ApprovedAt = DateTimeOffset.UtcNow;
        user.RejectionReason = null;

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<AdminUserResponse>> RejectAsync(
        Guid id,
        RejectUserRequest request,
        CancellationToken cancellationToken)
    {
        if (Blank(request.Reason) is not { } reason)
        {
            return ServiceResult<AdminUserResponse>.Validation("A rejection reason is required.");
        }

        var loaded = await LoadManageableUserAsync(id, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return ServiceResult<AdminUserResponse>.Failure(loaded.Error!);
        }

        var user = loaded.Value!;

        user.Status = UserStatus.Rejected;
        user.RejectionReason = reason;
        user.ApprovedBy = null;
        user.ApprovedAt = null;

        await _db.SaveChangesAsync(cancellationToken);
        await RevokeAllForUserAsync(user.Id, cancellationToken);

        return ServiceResult<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<AdminUserResponse>> DisableAsync(Guid id, CancellationToken cancellationToken)
    {
        var loaded = await LoadManageableUserAsync(id, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return ServiceResult<AdminUserResponse>.Failure(loaded.Error!);
        }

        var user = loaded.Value!;

        user.Status = UserStatus.Disabled;

        await _db.SaveChangesAsync(cancellationToken);
        await RevokeAllForUserAsync(user.Id, cancellationToken);

        return ServiceResult<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var loaded = await LoadManageableUserAsync(id, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return ServiceResult<bool>.Failure(loaded.Error!);
        }

        loaded.Value!.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        await RevokeAllForUserAsync(loaded.Value.Id, cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    /// <summary>
    /// The guard every management method shares: super admin only, and the super admin's own row is
    /// off limits so the CMS cannot be locked out of itself.
    /// </summary>
    private async Task<ServiceResult<User>> LoadManageableUserAsync(Guid id, CancellationToken cancellationToken)
    {
        if (RequireSuperAdmin<User>() is { } denied)
        {
            return denied;
        }

        if (_currentUser.UserId == id)
        {
            return ServiceResult<User>.Conflict(
                "cannot_modify_self", "You cannot change your own account this way.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return ServiceResult<User>.NotFound("user_not_found", "No user with that id.");
        }

        if (user.Role == UserRole.SuperAdmin)
        {
            return ServiceResult<User>.Conflict(
                "cannot_modify_super_admin", "The super admin account cannot be changed here.");
        }

        return ServiceResult<User>.Success(user);
    }

    private ServiceResult<T>? RequireSuperAdmin<T>() =>
        _currentUser.IsSuperAdmin ? null : ServiceResult<T>.Forbidden("forbidden", SuperAdminOnly);

    /// <summary>
    /// Issues the access token and a fresh refresh token, storing only the refresh token's hash.
    /// Expired rows for the same user are swept here, which is why no timer function is needed.
    /// </summary>
    private async Task<(AuthResponse Response, RefreshToken Row)> IssueTokensAsync(
        User user,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var (accessToken, expiresAt) = _tokens.CreateAccessToken(user);

        var refreshToken = RefreshTokenGenerator.Create();
        var refreshExpiresAt = now.AddDays(_jwtOptions.RefreshTokenDays);

        var row = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = RefreshTokenGenerator.Hash(refreshToken),
            ExpiresAt = refreshExpiresAt,
            CreatedAt = now
        };

        _db.RefreshTokens.Add(row);

        await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.ExpiresAt < now)
            .ExecuteDeleteAsync(cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return (new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            User = ToResponse(user)
        }, row);
    }

    /// <summary>
    /// Kills every live session for a user. Called when the password changes and whenever the super
    /// admin takes access away — without it a revoked user keeps renewing for another 30 days.
    /// </summary>
    private Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(t => t.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow), cancellationToken);

    private static ServiceResult<AuthResponse> InvalidCredentials() =>
        ServiceResult<AuthResponse>.Unauthorized("invalid_credentials", "Incorrect email or password.");

    private static ServiceResult<AuthResponse> InvalidRefreshToken() =>
        ServiceResult<AuthResponse>.Unauthorized("invalid_refresh_token", "This refresh token is not valid.");

    /// <summary>Shared by login and refresh, so both surfaces report account state identically.</summary>
    private static ServiceResult<AuthResponse> NotApproved(User user) => user.Status switch
    {
        UserStatus.Pending => ServiceResult<AuthResponse>.Forbidden(
            "account_pending", "This account is waiting for super admin approval."),
        UserStatus.Rejected => ServiceResult<AuthResponse>.Forbidden(
            "account_rejected", user.RejectionReason ?? "This account was rejected."),
        _ => ServiceResult<AuthResponse>.Forbidden(
            "account_disabled", "This account has been disabled. Contact the super admin.")
    };

    private static string? ValidateRegistration(
        string? firstName,
        string? lastName,
        string? email,
        RegisterRequest request)
    {
        if (firstName is null)
        {
            return "First name is required.";
        }

        if (lastName is null)
        {
            return "Last name is required.";
        }

        if (email is null || !LooksLikeEmail(email))
        {
            return "A valid email address is required.";
        }

        return ValidatePassword(request.Password, request.ConfirmPassword, "Password");
    }

    private static string? ValidatePassword(string? password, string? confirmation, string label)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumPasswordLength)
        {
            return $"{label} must be at least {MinimumPasswordLength} characters.";
        }

        return password == confirmation ? null : $"{label} and its confirmation do not match.";
    }

    /// <summary>
    /// Deliberately loose: the column is <c>citext</c> and the real check is whether the person can
    /// receive mail there, which no regex settles.
    /// </summary>
    private static bool LooksLikeEmail(string email)
    {
        var at = email.IndexOf('@');

        return at > 0
            && at == email.LastIndexOf('@')
            && at < email.Length - 1
            && !email.Contains(' ');
    }

    private static string? NormaliseEmail(string? email) => Blank(email)?.ToLowerInvariant();

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
