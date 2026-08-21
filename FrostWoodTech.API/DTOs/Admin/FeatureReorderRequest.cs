namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>
/// Bulk sort_order update for one service's features. Unlike <see cref="ReorderRequest"/> there
/// is no site — a feature list has a single order.
/// </summary>
public sealed class FeatureReorderRequest
{
    public List<ReorderItem>? Items { get; set; }
}
