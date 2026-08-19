using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Projects;

public class GetAdminProjectById
{
    private readonly IProjectService _projectService;

    public GetAdminProjectById(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [Function("GetAdminProjectById")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/projects/{id:guid}")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _projectService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
