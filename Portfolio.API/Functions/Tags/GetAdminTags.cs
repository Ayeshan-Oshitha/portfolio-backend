using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.Enums;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Tags;

public class GetAdminTags
{
    private readonly ITagService _tagService;

    public GetAdminTags(ITagService tagService)
    {
        _tagService = tagService;
    }

    [Function("GetAdminTags")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/tags")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var isTechnology = QueryParameters.ReadBool(req, "isTechnology");

        if (!QueryParameters.TryReadEnum<TechCategory>(req, "category", out var category))
        {
            return ProblemResults.BadRequest("validation_failed", "Unknown category.");
        }

        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _tagService.GetAdminTagsAsync(
            isTechnology,
            category,
            search,
            page,
            pageSize,
            cancellationToken);

        return new OkObjectResult(result);
    }
}
