using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Tags;

public class DeleteTag
{
    private readonly ITagService _tagService;

    public DeleteTag(ITagService tagService)
    {
        _tagService = tagService;
    }

    /// <summary>
    /// Soft delete. Refused with <c>tag_in_use</c> while any project or article still carries
    /// the tag — hard delete stays a super-admin-only concern.
    /// </summary>
    [Function("DeleteTag")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "cms/admin/tags/{id:guid}")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _tagService.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
