using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.DTOs.Admin;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Auth;

public class Refresh
{
    private readonly IUserService _users;

    public Refresh(IUserService users)
    {
        _users = users;
    }

    /// <summary>
    /// Anonymous by design — this has to work precisely because the access token has expired. See
    /// the allow-list in <c>JwtAuthenticationMiddleware</c>.
    /// </summary>
    [Function("Refresh")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/auth/refresh")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        RefreshTokenRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<RefreshTokenRequest>(
                req.Body, JsonDefaults.Options, cancellationToken);
        }
        catch (JsonException ex)
        {
            return ProblemResults.BadRequest("validation_failed", ex.Message);
        }

        if (body is null)
            return ProblemResults.BadRequest("validation_failed", "A request body is required.");

        var result = await _users.RefreshAsync(body, cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
