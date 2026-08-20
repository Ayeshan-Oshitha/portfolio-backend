using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.DTOs.Admin;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Auth;

public class Register
{
    private readonly IUserService _users;

    public Register(IUserService users)
    {
        _users = users;
    }

    /// <summary>Anonymous by design — see the allow-list in <c>JwtAuthenticationMiddleware</c>.</summary>
    [Function("Register")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/auth/register")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        RegisterRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<RegisterRequest>(
                req.Body, JsonDefaults.Options, cancellationToken);
        }
        catch (JsonException ex)
        {
            return ProblemResults.BadRequest("validation_failed", ex.Message);
        }

        if (body is null)
            return ProblemResults.BadRequest("validation_failed", "A request body is required.");

        var result = await _users.RegisterAsync(body, cancellationToken);
        if (!result.IsSuccess)
            return ProblemResults.FromError(result.Error!);

        return new ObjectResult(result.Value) { StatusCode = StatusCodes.Status201Created };
    }
}
