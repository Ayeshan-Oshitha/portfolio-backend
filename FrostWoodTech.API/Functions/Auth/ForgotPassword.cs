using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Auth;

public class ForgotPassword
{
    private readonly IUserService _users;

    public ForgotPassword(IUserService users)
    {
        _users = users;
    }

    /// <summary>
    /// Anonymous by design. No <c>!result.IsSuccess</c> branch below — the service never fails
    /// this call, since a failure would itself reveal something. See <c>UserService.ForgotPasswordAsync</c>.
    /// </summary>
    [Function("ForgotPassword")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/auth/forgot-password")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        ForgotPasswordRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ForgotPasswordRequest>(
                req.Body, JsonDefaults.Options, cancellationToken);
        }
        catch (JsonException ex)
        {
            return ProblemResults.BadRequest("validation_failed", ex.Message);
        }

        if (body is null)
            return ProblemResults.BadRequest("validation_failed", "A request body is required.");

        var result = await _users.ForgotPasswordAsync(body, ClientAddress.Read(req), cancellationToken);

        return new OkObjectResult(result.Value);
    }
}
