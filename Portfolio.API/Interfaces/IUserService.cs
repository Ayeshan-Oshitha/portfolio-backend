using Portfolio.API.Common;
using Portfolio.API.DTOs.Admin;
using Portfolio.API.Enums;

namespace Portfolio.API.Interfaces;

/// <summary>
/// The <c>users</c> aggregate: registration, password login and the super admin's approval
/// workflow. Google sign-in and refresh tokens are not implemented yet.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Open registration, but powerless: the new account is <c>pending</c> and gets no token until
    /// the super admin approves it.
    /// </summary>
    Task<ServiceResult<AdminUserResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Rotates a refresh token for a fresh pair. Presenting an already-revoked token is treated as
    /// theft and kills every live token for that user.
    /// </summary>
    Task<ServiceResult<AuthResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Revokes one refresh token. Succeeds even for an unknown token, so it cannot be used to probe
    /// which tokens exist.
    /// </summary>
    Task<ServiceResult<bool>> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    /// <summary>The caller behind the current access token.</summary>
    Task<ServiceResult<AdminUserResponse>> GetMeAsync(CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken);

    /// <summary>Super admin only.</summary>
    Task<ServiceResult<PagedResult<AdminUserResponse>>> GetAllAsync(
        string? search,
        UserStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Super admin only. Lets a <c>pending</c> or <c>rejected</c> account sign in.</summary>
    Task<ServiceResult<AdminUserResponse>> ApproveAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Super admin only. Records why, so the applicant can be told.</summary>
    Task<ServiceResult<AdminUserResponse>> RejectAsync(
        Guid id,
        RejectUserRequest request,
        CancellationToken cancellationToken);

    /// <summary>Super admin only. Revokes access from a previously approved account.</summary>
    Task<ServiceResult<AdminUserResponse>> DisableAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Super admin only. Soft delete.</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
