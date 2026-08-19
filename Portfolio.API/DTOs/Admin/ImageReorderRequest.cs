namespace Portfolio.API.DTOs.Admin;

/// <summary>
/// Bulk sort_order update for one project's images. Unlike <see cref="ReorderRequest"/> there is
/// no site — an image gallery has a single order.
/// </summary>
public sealed class ImageReorderRequest
{
    public List<ReorderItem>? Items { get; set; }
}
