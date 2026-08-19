using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Articles;

public class DeleteArticle
{
    private readonly IArticleService _articleService;

    public DeleteArticle(IArticleService articleService)
    {
        _articleService = articleService;
    }

    /// <summary>Soft delete — hard delete stays a super-admin-only concern.</summary>
    [Function("DeleteArticle")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "admin/articles/{id:guid}")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _articleService.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
