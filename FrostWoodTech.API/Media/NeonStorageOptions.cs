namespace FrostWoodTech.API.Media;

/// <summary>
/// Bound from the <c>NeonS3</c> configuration section (<c>NeonS3__Endpoint</c> and friends).
/// </summary>
public sealed class NeonStorageOptions
{
    /// <summary>Neon's S3-compatible endpoint, e.g. <c>https://&lt;project&gt;.neon.tech/storage</c>.</summary>
    public string Endpoint { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    /// <summary>Signs every request. Lives in app settings / Key Vault, never in a committed file.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Region Neon expects for SigV4 request signing (their S3-compatible endpoint requires it).</summary>
    public string Region { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    /// <summary>Root of the folder convention, e.g. <c>frostwoodtech/projects/{slug}/</c>.</summary>
    public string BaseFolder { get; set; } = "frostwoodtech";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint)
        && !string.IsNullOrWhiteSpace(AccessKey)
        && !string.IsNullOrWhiteSpace(SecretKey)
        && !string.IsNullOrWhiteSpace(Region)
        && !string.IsNullOrWhiteSpace(BucketName);
}
