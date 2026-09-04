using System.Security.Cryptography;
using System.Text;

namespace FrostWoodTech.API.Auth;

/// <summary>
/// Same shape as <see cref="RefreshTokenGenerator"/>: looked up by hash, so the hash must be
/// deterministic, and it's already 256 bits of CSPRNG entropy — no slow hash needed.
/// </summary>
public static class EmailVerificationTokenGenerator
{
    private const int TokenBytes = 32;

    /// <summary>The value put in the verification link. Never stored.</summary>
    public static string Create() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));

    /// <summary>The value stored in <c>email_verification_tokens.token_hash</c>.</summary>
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
