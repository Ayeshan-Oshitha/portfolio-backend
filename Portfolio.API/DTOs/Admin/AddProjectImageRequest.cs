namespace Portfolio.API.DTOs.Admin;

/// <summary>
/// Written after the client has uploaded straight to Cloudinary — the API stores the metadata
/// it hands back, never the bytes.
/// </summary>
public class AddProjectImageRequest
{
    /// <summary>Cloudinary public_id.</summary>
    public string? CloudinaryId { get; set; }

    public string? Url { get; set; }

    /// <summary>Required on every image.</summary>
    public string? AltText { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    /// <summary>Setting this clears the project's previous primary in the same save.</summary>
    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }
}
