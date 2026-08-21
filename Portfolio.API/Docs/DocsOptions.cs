namespace Portfolio.API.Docs;

/// <summary>
/// Bound from the <c>Docs</c> configuration section (<c>Docs__Enabled</c>).
/// </summary>
public sealed class DocsOptions
{
    /// <summary>
    /// Off unless a deployment opts in. The spec is a map of the whole admin surface, and
    /// publishing it by default is not a decision an omitted setting should make.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Scalar's standalone bundle. Pinned to an exact version rather than <c>@latest</c> so the
    /// docs page cannot change under you when someone ships a new release.
    /// </summary>
    public string ScalarCdnUrl { get; set; } =
        "https://cdn.jsdelivr.net/npm/@scalar/api-reference@1.25.28/dist/browser/standalone.min.js";
}
