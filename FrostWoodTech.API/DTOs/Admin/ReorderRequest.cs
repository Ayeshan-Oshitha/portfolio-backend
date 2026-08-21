using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>
/// Bulk sort_order update for one site. Shared by every site-visible entity — sort order is
/// per site, so the caller has to say which one it is reordering.
/// </summary>
public sealed class ReorderRequest
{
    public Site? Site { get; set; }

    public List<ReorderItem>? Items { get; set; }
}

public sealed class ReorderItem
{
    public Guid Id { get; set; }

    public int SortOrder { get; set; }
}
