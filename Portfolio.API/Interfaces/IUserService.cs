using Portfolio.API.Common;
using Portfolio.API.DTOs.Admin;

namespace Portfolio.API.Interfaces;

/// <summary>
/// The <c>users</c> aggregate: registration, password login and the admin user list. Google
/// sign-in, the approval workflow and refresh tokens are not implemented yet.
/// </summary>
public interface IUserService
{
    /// <summary>Open registration. Creates an approved <c>admin</c> and logs them straight in.</summary>
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>The caller behind the current access token.</summary>
    Task<ServiceResult<AdminUserResponse>> GetMeAsync(CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken);

    Task<PagedResult<AdminUserResponse>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Soft delete.</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
