namespace FrostWoodTech.API.Enums;

/// <summary>
/// Which sign-in-surface action a <c>login_attempts</c> row counts towards — counted separately
/// so a failed login can't consume a reset budget or vice versa. Native enum <c>auth_attempt_action</c>.
/// </summary>
public enum AuthAttemptAction
{
    /// <summary>A failed password sign-in.</summary>
    Login,

    /// <summary>A forgotten-password request, successful or not — every one counts.</summary>
    PasswordReset
}
