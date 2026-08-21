using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Full replacement — every field is written, so send the whole tag.</summary>
public sealed class UpdateTagRequest
{
    public string? Name { get; set; }

    public string? Slug { get; set; }

    public bool IsTechnology { get; set; }

    public TechCategory? TechnologyCategory { get; set; }

    public string? IconObjectKey { get; set; }

    public string? IconUrl { get; set; }

    public string? ColorHex { get; set; }

    public int SortOrder { get; set; }
}
