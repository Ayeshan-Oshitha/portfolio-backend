using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.DTOs.Admin;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Projects;

public class SetProjectPublished
{
    private readonly IProjectService _projectService;

    public SetProjectPublished(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// Flips the draft flag on its own so the admin SPA can take a project live without
    /// resubmitting the whole form. Going live the first time stamps published_at.
    /// </summary>
    [Function("SetProjectPublished")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/projects/{id:guid}/publish")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        SetPublishedRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<SetPublishedRequest>(
                req.Body,
                JsonDefaults.Options,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            return ProblemResults.BadRequest("validation_failed", ex.Message);
        }

        if (body is null)
        {
            return ProblemResults.BadRequest("validation_failed", "A request body is required.");
        }

        var result = await _projectService.SetPublishedAsync(id, body, cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
