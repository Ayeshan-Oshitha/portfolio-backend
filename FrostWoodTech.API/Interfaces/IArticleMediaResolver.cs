namespace FrostWoodTech.API.Interfaces;

/// <summary>
/// Rewrites <c>media://articles/...</c> tokens in article Markdown into real Neon Object Storage
/// URLs. This is the only place that knows the storage provider maps to a real URL — swapping
/// providers means changing <see cref="IMediaService.GetPublicUrl"/>, never the stored Markdown.
/// </summary>
public interface IArticleMediaResolver
{
    /// <summary>Returns the Markdown with every <c>media://...</c> reference resolved to a URL.</summary>
    string ResolveMediaReferences(string markdown);
}
