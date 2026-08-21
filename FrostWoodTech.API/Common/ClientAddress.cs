using Microsoft.AspNetCore.Http;

namespace FrostWoodTech.API.Common;

public static class ClientAddress
{
    /// <summary>
    /// The caller's address, for rate limiting.
    /// <para>
    /// Behind the Functions front end <c>RemoteIpAddress</c> is the load balancer, so the real
    /// client is the first entry of <c>X-Forwarded-For</c>. That header is caller-supplied and
    /// therefore spoofable — which is exactly why it only ever widens a limit here, never
    /// authorises anything.
    /// </para>
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
