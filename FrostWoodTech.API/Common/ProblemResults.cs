using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FrostWoodTech.API.Common;

/// <summary>
/// RFC 7807 responses. Every error carries a stable <c>code</c> the frontends can switch on —
/// they must never have to match on the human-readable detail string.
/// </summary>
public static class ProblemResults
{
    public const string ContentType = "application/problem+json";

    public static IActionResult BadRequest(string code, string detail) =>
        Create(StatusCodes.Status400BadRequest, "Bad Request", code, detail);

    public static IActionResult Unauthorized(string code, string detail) =>
        Create(StatusCodes.Status401Unauthorized, "Unauthorized", code, detail);

    public static IActionResult Forbidden(string code, string detail) =>
        Create(StatusCodes.Status403Forbidden, "Forbidden", code, detail);

    public static IActionResult NotFound(string code, string detail) =>
        Create(StatusCodes.Status404NotFound, "Not Found", code, detail);

    public static IActionResult Conflict(string code, string detail) =>
        Create(StatusCodes.Status409Conflict, "Conflict", code, detail);

    /// <summary>
    /// The guard that stops personal content leaking onto the agency site: a public endpoint with
    /// site visibility never falls back to "return everything".
    /// </summary>
    public static IActionResult SiteRequired() =>
        BadRequest("site_required", "?site=agency or ?site=personal is required.");

    /// <summary>Maps a service failure onto the matching status code.</summary>
    public static IActionResult FromError(ServiceError error) => error.Kind switch
    {
        ServiceErrorKind.NotFound => NotFound(error.Code, error.Message),
        ServiceErrorKind.Conflict => Conflict(error.Code, error.Message),
        ServiceErrorKind.Unauthorized => Unauthorized(error.Code, error.Message),
        ServiceErrorKind.Forbidden => Forbidden(error.Code, error.Message),
        _ => BadRequest(error.Code, error.Message)
    };

    /// <summary>
    /// The middleware runs before the Functions pipeline can turn an <see cref="IActionResult"/>
    /// into a response, so it writes the same document straight onto the stream.
    /// </summary>
    public static Task WriteAsync(HttpResponse response, int status, string title, string code, string detail)
    {
        response.StatusCode = status;
        response.ContentType = ContentType;

        return response.WriteAsync(JsonSerializer.Serialize(Build(status, title, code, detail), JsonDefaults.Options));
    }

    private static IActionResult Create(int status, string title, string code, string detail) =>
        new ObjectResult(Build(status, title, code, detail))
        {
            StatusCode = status,
            ContentTypes = { ContentType }
        };

    private static ProblemDetails Build(int status, string title, string code, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };

        problem.Extensions["code"] = code;

        return problem;
    }
}
