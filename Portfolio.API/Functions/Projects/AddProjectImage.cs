using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.DTOs.Admin;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Projects;

public class AddProjectImage
{
    private readonly IProjectService _projectService;

    public AddProjectImage(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// Records what Cloudinary handed back after a direct upload — the bytes never pass through
    /// the API.
    /// </summary>
    [Function("AddProjectImage")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/projects/{id:guid}/images")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        AddProjectImageRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<AddProjectImageRequest>(
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

        var result = await _projectService.AddImageAsync(id, body, cancellationToken);
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
