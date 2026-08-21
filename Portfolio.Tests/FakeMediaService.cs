using Portfolio.API.Common;
using Portfolio.API.DTOs.Admin;
using Portfolio.API.Interfaces;

namespace Portfolio.Tests;

/// <summary>
/// Keeps Neon Object Storage out of the test run. The rules the project tests care about — one
/// primary image, required alt text — are decided before anything is deleted, so recording the
/// calls is enough.
/// </summary>
public sealed class FakeMediaService : IMediaService
{
    public List<string> Deleted { get; } = [];

    public ServiceResult<PresignedUploadResponse> CreatePresignedUpload(PresignedUploadRequest request) =>
        throw new NotSupportedException("Presigning is not exercised by these tests.");

    public Task<bool> DeleteFileAsync(string objectKey, CancellationToken cancellationToken)
    {
        Deleted.Add(objectKey);

        return Task.FromResult(true);
    }
}
