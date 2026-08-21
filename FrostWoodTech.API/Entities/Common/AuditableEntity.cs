namespace FrostWoodTech.API.Entities.Common;

/// <summary>
/// Every content table carries these. <see cref="CreatedAt"/> and <see cref="UpdatedAt"/> are
/// stamped by the DbContext, never by a service.
/// </summary>
public abstract class AuditableEntity
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
}
