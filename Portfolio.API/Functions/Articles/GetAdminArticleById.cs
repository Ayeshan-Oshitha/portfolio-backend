using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Articles;

public class GetAdminArticleById
{
    private readonly IArticleService _articleService;

    public GetAdminArticleById(IArticleService articleService)
    {
        _articleService = articleService;
    }

    [Function("GetAdminArticleById")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/articles/{id:guid}")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _articleService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
