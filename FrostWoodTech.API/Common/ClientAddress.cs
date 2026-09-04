using Microsoft.AspNetCore.Http;

namespace FrostWoodTech.API.Common;

public static class ClientAddress
{
    /// <summary>
    /// The caller's address, for rate limiting. <c>RemoteIpAddress</c> is the load balancer
    /// behind Functions, so the real client comes from <c>X-Forwarded-For</c> — spoofable, which
    /// is why this only ever widens a limit, never authorises anything.
    /// </summary>
    public static string? Read(HttpRequest request)
    {
        var forwarded = request.Headers["X-Forwarded-For"].ToString();

        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',')[0].Trim();

            // Azure appends the source port ("10.0.0.1:52000"); IPv6 arrives bracketed.
            var lastColon = first.LastIndexOf(':');
            if (lastColon > 0 && !first.Contains("::", StringComparison.Ordinal) && first.Count(c => c == ':') == 1)
            {
                first = first[..lastColon];
            }

            if (first.Length > 0)
            {
                return first;
            }
        }

        return request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
