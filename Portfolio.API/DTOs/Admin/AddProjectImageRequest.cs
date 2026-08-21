namespace Portfolio.API.DTOs.Admin;

/// <summary>
/// Written after the client has uploaded straight to Neon Object Storage — the API stores the metadata
/// it hands back, never the bytes.
/// </summary>
public class AddProjectImageRequest
{
    /// <summary>Neon object key.</summary>
    public string? ObjectKey { get; set; }

    public string? Url { get; set; }

    /// <summary>Required on every image.</summary>
    public string? AltText { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    /// <summary>Setting this clears the project's previous primary in the same save.</summary>
    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }
}
