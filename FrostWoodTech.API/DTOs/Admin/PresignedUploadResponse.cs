namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>
/// Everything the admin SPA needs to PUT the file itself straight to Neon Object Storage.
/// </summary>
public sealed class PresignedUploadResponse
{
    /// <summary>Presigned PUT URL. PUT the raw file bytes here with the matching content type.</summary>
    public required string UploadUrl { get; init; }

    /// <summary>The key the object will live at once uploaded — send this back with the metadata request.</summary>
    public required string ObjectKey { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
