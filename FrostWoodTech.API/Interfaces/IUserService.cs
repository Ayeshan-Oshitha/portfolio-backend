using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

/// <summary>
/// The <c>users</c> aggregate: registration, password and Google sign-in, refresh token
/// rotation, and the super admin's approval workflow.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Open registration, but powerless: the new account starts in
    /// <c>email_verification_required</c> and gets no token until it verifies its address and the
    /// super admin approves it.
    /// </summary>
    Task<ServiceResult<AdminUserResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Moves a password account from <c>email_verification_required</c> to <c>pending</c>. Never
    /// issues a token — approval still comes from the super admin.
    /// </summary>
    Task<ServiceResult<AdminUserResponse>> VerifyEmailAsync(
        VerifyEmailRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Issues a fresh verification link for an account still waiting to verify. Always reports the
    /// same generic success regardless of whether the address exists, so it cannot be used to probe
    /// which emails are registered.
    /// </summary>
    Task<ServiceResult<ResendVerificationResponse>> ResendVerificationAsync(
        ResendVerificationRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Issues a password reset link. Always the same generic success, whatever the address
    /// resolves to, so this cannot be used to probe which addresses have accounts. Invalidates
    /// any outstanding unused reset link first, so only the newest one works.
    /// </summary>
    /// <param name="ipAddress">
    /// The caller's address, for rate limiting. Null when it cannot be determined — the per-email
    /// limit still applies.
    /// </param>
    Task<ServiceResult<ForgotPasswordResponse>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Redeems an emailed password link — setup or reset, consumed identically. Single-use:
    /// invalidates any other outstanding link for the user and revokes every refresh token.
    /// Never issues a token itself.
    /// </summary>
    Task<ServiceResult<AdminUserResponse>> SetPasswordAsync(
        SetPasswordRequest request,
        CancellationToken cancellationToken);

    /// <param name="ipAddress">
    /// The caller's address, for rate limiting. Null when it cannot be determined — the per-email
    /// limit still applies.
    /// </param>
    Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Exchanges a verified Google ID token for the same JWT pair as password login. An email with
    /// no user row gets a <c>pending</c> account — Google having verified the address says nothing
    /// about whether the super admin wants them in the CMS.
    /// </summary>
    Task<ServiceResult<AuthResponse>> GoogleSignInAsync(
        GoogleSignInRequest request,
        CancellationToken cancellationToken);

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
