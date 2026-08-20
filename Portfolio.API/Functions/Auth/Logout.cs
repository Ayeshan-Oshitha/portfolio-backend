using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.DTOs.Admin;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Auth;

public class Logout
{
    private readonly IUserService _users;

    public Logout(IUserService users)
    {
        _users = users;
    }

    /// <summary>
    /// Anonymous by design: signing out has to work even once the access token is dead, and the
    /// refresh token in the body is itself the proof of ownership.
    /// </summary>
    [Function("Logout")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/auth/logout")] HttpRequest req,
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

        var result = await _users.LogoutAsync(body, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
