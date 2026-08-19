namespace Portfolio.API.Entities.Common;

/// <summary>
/// The site visibility block shared by projects, articles, services, pricing plans and FAQs.
/// A row is never duplicated per site — it carries a flag for each one.
/// </summary>
public abstract class SiteVisibleEntity : AuditableEntity
{
    public bool ShowOnAgency { get; set; }

    /// <summary>Only valid when <see cref="ShowOnAgency"/> is true.</summary>
    public bool FeaturedOnAgency { get; set; }

    public int AgencySortOrder { get; set; }

    public bool ShowOnPersonal { get; set; }

    /// <summary>Only valid when <see cref="ShowOnPersonal"/> is true.</summary>
    public bool FeaturedOnPersonal { get; set; }

    public int PersonalSortOrder { get; set; }
}
