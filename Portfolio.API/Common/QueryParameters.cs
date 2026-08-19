using Microsoft.AspNetCore.Http;

using Portfolio.API.Enums;

namespace Portfolio.API.Common;

/// <summary>Query string parsing shared by every list endpoint.</summary>
public static class QueryParameters
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>Reads <c>page</c> / <c>pageSize</c>, clamping both to sane bounds.</summary>
    public static (int Page, int PageSize) ReadPaging(HttpRequest request)
    {
        var page = ReadInt(request, "page") ?? 1;
        var pageSize = ReadInt(request, "pageSize") ?? DefaultPageSize;

        return (Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize));
    }

    /// <summary>Null when the parameter is absent or not a bool.</summary>
    public static bool? ReadBool(HttpRequest request, string name) =>
        bool.TryParse(request.Query[name], out var value) ? value : null;

    public static int? ReadInt(HttpRequest request, string name) =>
        int.TryParse(request.Query[name], out var value) ? value : null;

    /// <summary>Trimmed value, or null when absent or blank.</summary>
    public static string? ReadString(HttpRequest request, string name)
    {
        var value = request.Query[name].ToString();

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Parses an enum from its snake_case wire name (<c>tool_or_platform</c>).
    /// <c>found</c> is false only when the parameter is present but not a valid member.
    /// </summary>
    public static bool TryReadEnum<TEnum>(HttpRequest request, string name, out TEnum? value)
        where TEnum : struct, Enum
    {
        value = null;

        var raw = ReadString(request, name);
        if (raw is null)
        {
            return true;
        }

        if (Enum.TryParse<TEnum>(raw.Replace("_", string.Empty), ignoreCase: true, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reads the mandatory <c>?site=</c>. <c>false</c> means the value was present but not a known
    /// site; a null <paramref name="site"/> alongside <c>true</c> means it was absent altogether,
    /// which the caller answers with <see cref="ProblemResults.SiteRequired"/>.
    /// </summary>
    public static bool TryReadSite(HttpRequest request, out Site? site) =>
        TryReadEnum(request, "site", out site);
}
