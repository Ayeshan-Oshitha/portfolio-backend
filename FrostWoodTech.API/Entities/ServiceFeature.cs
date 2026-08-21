namespace FrostWoodTech.API.Entities;

public class ServiceFeature
{
    public Guid Id { get; set; }

    public Guid ServiceId { get; set; }

    public ServiceOffering Service { get; set; } = null!;

    public required string Title { get; set; }

    public string? Description { get; set; }

    public string? IconName { get; set; }

    public int SortOrder { get; set; }
}
