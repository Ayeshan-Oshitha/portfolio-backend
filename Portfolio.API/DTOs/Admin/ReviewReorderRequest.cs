namespace Portfolio.API.DTOs.Admin;

/// <summary>Bulk sort_order update. Reviews have one shared order, not a per-site one, so unlike
/// <see cref="ReorderRequest"/> there is no <c>Site</c> to specify.</summary>
public sealed class ReviewReorderRequest
{
    public List<ReorderItem>? Items { get; set; }
}
