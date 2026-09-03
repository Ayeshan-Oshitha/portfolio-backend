using System.Security.Cryptography;
using System.Text;

namespace FrostWoodTech.API.Auth;

/// <summary>
/// SHA-256, not the Argon2id in <see cref="PasswordHasher"/>: the token is looked up
/// <em>by</em> its hash, so it must be deterministic, and it's already 256 bits of CSPRNG
/// entropy with no dictionary to slow down.
/// </summary>
public static class RefreshTokenGenerator
{
    private const int TokenBytes = 32;

    /// <summary>The value handed to the client. Never stored.</summary>
    public static string Create() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));

    /// <summary>The value stored in <c>refresh_tokens.token_hash</c>.</summary>
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    /// <summary>Base64 without the padding and URL-unsafe characters, so it travels in JSON cleanly.</summary>
    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
