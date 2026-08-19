using Portfolio.API.Entities.Common;

namespace Portfolio.API.Entities;

public class Faq : SiteVisibleEntity
{
    public required string Question { get; set; }

    /// <summary>Markdown.</summary>
    public required string Answer { get; set; }

    /// <summary>Groups FAQs on the page — "Pricing", "Process", "Technical".</summary>
    public string? Category { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }
}
