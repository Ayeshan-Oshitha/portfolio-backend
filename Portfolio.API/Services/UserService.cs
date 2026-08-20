using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Portfolio.API.Auth;
using Portfolio.API.Common;
using Portfolio.API.Data;
using Portfolio.API.DTOs.Admin;
using Portfolio.API.Entities;
using Portfolio.API.Enums;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Services;

public class UserService : IUserService
{
    private const int MinimumPasswordLength = 8;

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
            CreatedAt = user.CreatedAt
        };

    private readonly PortfolioDbContext _db;
    private readonly IJwtTokenService _tokens;
    private readonly CurrentUser _currentUser;

    public UserService(PortfolioDbContext db, IJwtTokenService tokens, CurrentUser currentUser)
    {
        _db = db;
        _tokens = tokens;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var firstName = Blank(request.FirstName);
        var lastName = Blank(request.LastName);
        var email = NormaliseEmail(request.Email);

        var validationError = ValidateRegistration(firstName, lastName, email, request);
        if (validationError is not null)
        {
            return ServiceResult<AuthResponse>.Validation(validationError);
        }

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (emailTaken)
        {
            return ServiceResult<AuthResponse>.Conflict("email_taken", "That email address is already registered.");
        }

        var user = new User
        {
            Email = email!,
            FirstName = firstName!,
            LastName = lastName!,
            PasswordHash = PasswordHasher.Hash(request.Password!),
            // No approval workflow yet: whoever registers can use the CMS immediately.
            Role = UserRole.Admin,
            Status = UserStatus.Approved,
            LastLoginAt = DateTimeOffset.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Success(IssueToken(user));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormaliseEmail(request.Email);
        var password = request.Password;

        if (email is null || string.IsNullOrEmpty(password))
        {
            return ServiceResult<AuthResponse>.Validation("Email and password are required.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            // Same work, same answer as a wrong password: the response must not reveal whether
            // the account exists.
            PasswordHasher.BurnVerifyTime(password);

            return InvalidCredentials();
        }

        if (!PasswordHasher.Verify(user.PasswordHash, password))
        {
            return InvalidCredentials();
        }

        if (user.Status != UserStatus.Approved)
        {
            return ServiceResult<AuthResponse>.Forbidden(
                "account_disabled",
                "This account cannot sign in. Contact an administrator.");
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Success(IssueToken(user));
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

        return ServiceResult<bool>.Success(true);
    }

    public async Task<PagedResult<AdminUserResponse>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Users.AsNoTracking();

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

        return new PagedResult<AdminUserResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == id)
        {
            return ServiceResult<bool>.Conflict("cannot_delete_self", "You cannot delete your own account.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return ServiceResult<bool>.NotFound("user_not_found", "No user with that id.");
        }

        user.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private AuthResponse IssueToken(User user)
    {
        var (token, expiresAt) = _tokens.CreateAccessToken(user);

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = new AdminUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                Status = user.Status,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            }
        };
    }

    private static ServiceResult<AuthResponse> InvalidCredentials() =>
        ServiceResult<AuthResponse>.Unauthorized("invalid_credentials", "Incorrect email or password.");

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
