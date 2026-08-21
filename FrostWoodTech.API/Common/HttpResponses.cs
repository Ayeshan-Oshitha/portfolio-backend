using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace FrostWoodTech.API.Common;

/// <summary>
/// Caching rules from the API surface doc: public GETs are cacheable with an ETag, admin
/// responses are never stored.
/// </summary>
public static class HttpResponses
{
    private const int PublicMaxAgeSeconds = 300;

    /// <summary>
    /// Serialises <paramref name="payload"/>, tags it with an ETag and returns <c>304</c> when the
    /// caller already has that version.
    /// </summary>
    public static IActionResult PublicJson(HttpRequest request, object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonDefaults.Options);
        var etag = ComputeETag(json);

        var response = request.HttpContext.Response;
        response.Headers.CacheControl = $"public, max-age={PublicMaxAgeSeconds}";
        response.Headers.ETag = etag;

        if (request.Headers.IfNoneMatch.Contains(etag))
        {
            return new StatusCodeResult(StatusCodes.Status304NotModified);
        }

        return new ContentResult
        {
            Content = json,
            ContentType = "application/json",
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>Admin payloads must not be cached anywhere — they contain drafts.</summary>
    public static void MarkNoStore(HttpRequest request)
    {
        request.HttpContext.Response.Headers[HeaderNames.CacheControl] = "no-store";
    }

    private static string ComputeETag(string json)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json));

        return $"\"{Convert.ToHexString(hash)[..32].ToLowerInvariant()}\"";
    }
}
