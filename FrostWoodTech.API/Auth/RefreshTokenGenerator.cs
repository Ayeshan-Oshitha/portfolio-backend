using System.Security.Cryptography;
using System.Text;

namespace FrostWoodTech.API.Auth;

/// <summary>
/// Raw refresh tokens and the hash stored against them.
/// <para>
/// SHA-256 here rather than the Argon2id in <see cref="PasswordHasher"/>, and that difference is
/// deliberate. A refresh token is looked up <em>by</em> its hash, so the hash has to be
/// deterministic and unsalted; and unlike a password this is 256 bits of entropy from a CSPRNG,
/// so there is no dictionary to slow an attacker down against.
/// </para>
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
