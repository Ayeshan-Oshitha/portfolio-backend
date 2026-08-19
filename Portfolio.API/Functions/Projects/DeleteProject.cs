using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Projects;

public class DeleteProject
{
    private readonly IProjectService _projectService;

    public DeleteProject(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>Soft delete — hard delete stays a super-admin-only concern.</summary>
    [Function("DeleteProject")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "admin/projects/{id:guid}")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _projectService.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
