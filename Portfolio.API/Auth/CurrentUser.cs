using Portfolio.API.Enums;

namespace Portfolio.API.Auth;

/// <summary>
/// How the authenticated identity reaches the service layer. The isolated worker has no
/// <c>HttpContext.User</c> flowing into a Function, so the middleware fills this scoped holder
/// instead and the services read it.
/// </summary>
public sealed class CurrentUser
{
    public Guid? UserId { get; set; }

    public string? Email { get; set; }

    public UserRole? Role { get; set; }

    public bool IsAuthenticated => UserId is not null;

    /// <summary>Only the super admin may approve, reject, disable or delete users — see auth.md.</summary>
    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;
}
