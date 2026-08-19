using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.DTOs.Admin;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Projects;

public class CreateProject
{
    private readonly IProjectService _projectService;

    public CreateProject(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [Function("CreateProject")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/projects")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        CreateProjectRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<CreateProjectRequest>(
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

        var result = await _projectService.CreateAsync(body, cancellationToken);
        if (!result.IsSuccess)
        {
            return ProblemResults.FromError(result.Error!);
        }

        return new ObjectResult(result.Value)
        {
            StatusCode = StatusCodes.Status201Created
        };
    }
}
