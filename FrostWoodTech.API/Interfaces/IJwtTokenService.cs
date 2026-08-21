using Microsoft.IdentityModel.Tokens;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Interfaces;

/// <summary>Issues and describes how to validate the admin access token.</summary>
public interface IJwtTokenService
{
    /// <summary>Claims are <c>sub</c>, <c>email</c>, <c>role</c> and <c>jti</c>.</summary>
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user);

    /// <summary>The middleware validates with exactly the parameters the issuer signed against.</summary>
    TokenValidationParameters CreateValidationParameters();
}
