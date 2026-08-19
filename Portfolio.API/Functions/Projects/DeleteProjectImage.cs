using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Projects;

public class DeleteProjectImage
{
    private readonly IProjectService _projectService;

    public DeleteProjectImage(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// Hard delete — image rows carry no soft-delete flag. The Cloudinary asset survives;
    /// destroying it belongs to the media slice.
    /// </summary>
    [Function("DeleteProjectImage")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "delete",
            Route = "admin/projects/{id:guid}/images/{imageId:guid}")] HttpRequest req,
        Guid id,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _projectService.DeleteImageAsync(id, imageId, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
