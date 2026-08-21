using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.DTOs.Admin;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Media;

public class CreatePresignedUpload
{
    private readonly IMediaService _mediaService;

    public CreatePresignedUpload(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    /// <summary>
    /// Step one of the upload flow: hands the admin SPA a presigned PUT URL so it can upload the
    /// file straight to Neon Object Storage. Admin-only by virtue of the route.
    /// </summary>
    [Function("CreatePresignedUpload")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/media/presigned-upload")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        PresignedUploadRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<PresignedUploadRequest>(
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

        var result = _mediaService.CreatePresignedUpload(body);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
