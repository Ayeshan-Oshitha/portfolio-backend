using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Auth;

public class VerifyEmail
{
    private readonly IUserService _users;

    public VerifyEmail(IUserService users)
    {
        _users = users;
    }

    /// <summary>
    /// Anonymous by design — see the allow-list in <c>JwtAuthenticationMiddleware</c>. Called by
    /// the admin SPA's <c>/verify-email</c> page with the token from the emailed link.
    /// </summary>
    [Function("VerifyEmail")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/auth/verify-email")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        VerifyEmailRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<VerifyEmailRequest>(
                req.Body, JsonDefaults.Options, cancellationToken);
        }
        catch (JsonException ex)
        {
            return ProblemResults.BadRequest("validation_failed", ex.Message);
        }

        if (body is null)
            return ProblemResults.BadRequest("validation_failed", "A request body is required.");

        var result = await _users.VerifyEmailAsync(body, cancellationToken);
        if (!result.IsSuccess)
            return ProblemResults.FromError(result.Error!);

        return new OkObjectResult(result.Value);
    }
}
