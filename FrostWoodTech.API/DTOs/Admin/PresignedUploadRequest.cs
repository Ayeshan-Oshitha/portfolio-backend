using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>
/// Asks for a presigned PUT URL that lets the admin SPA upload straight to Neon Object Storage.
/// The bytes never pass through the API.
/// </summary>
public class PresignedUploadRequest
{
    /// <summary>Required. Decides the folder — the client never names one itself.</summary>
    public MediaTarget? Target { get; set; }

    /// <summary>Required when <see cref="Target"/> is <c>projects</c> or <c>articles</c>; ignored otherwise.</summary>
    public string? Slug { get; set; }

    /// <summary>Optional. Supply it to overwrite one specific object instead of adding a new one.</summary>
    public string? ObjectKey { get; set; }
}
