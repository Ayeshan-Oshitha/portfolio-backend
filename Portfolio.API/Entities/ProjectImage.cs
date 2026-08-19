namespace Portfolio.API.Entities;

/// <summary>Cloudinary metadata only — image bytes never live in Postgres.</summary>
public class ProjectImage
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public required string CloudinaryId { get; set; }

    public required string Url { get; set; }

    /// <summary>Required on every image.</summary>
    public required string AltText { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    /// <summary>Exactly one per project — enforced by a partial unique index.</summary>
    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }
}
