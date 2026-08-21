using System.Text.RegularExpressions;

using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Services;

/// <inheritdoc cref="IArticleMediaResolver"/>
public partial class ArticleMediaResolver : IArticleMediaResolver
{
    private readonly IMediaService _mediaService;

    public ArticleMediaResolver(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    public string ResolveMediaReferences(string markdown) =>
        string.IsNullOrEmpty(markdown)
            ? markdown
            : MediaReferencePattern().Replace(markdown, m => _mediaService.GetPublicUrl(m.Groups[1].Value));

    // The object key is exactly what follows "media://" — the reference and the storage key
    // share the same "articles/images/..." shape by convention, so no lookup is needed.
    [GeneratedRegex(@"media://([\w\-./]+)")]
    private static partial Regex MediaReferencePattern();
}
