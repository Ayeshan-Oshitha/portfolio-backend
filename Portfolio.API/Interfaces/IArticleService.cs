using Portfolio.API.Common;
using Portfolio.API.DTOs.Admin;
using Portfolio.API.DTOs.Public;
using Portfolio.API.Enums;

namespace Portfolio.API.Interfaces;

public interface IArticleService
{
    /// <summary>
    /// Public read: always scoped to one site and to published, non-deleted rows. There is no
    /// overload that lets a caller skip those filters.
    /// </summary>
    Task<PagedResult<ArticleResponse>> GetPublicArticlesAsync(
        Site site,
        string? tagSlug,
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<ArticleResponse>> GetPublicArticleBySlugAsync(
        Site site,
        string slug,
        CancellationToken cancellationToken);

    Task<PagedResult<AdminArticleResponse>> GetAdminArticlesAsync(
        Site? site,
        bool? isPublished,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminArticleResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<AdminArticleResponse>> CreateAsync(
        CreateArticleRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminArticleResponse>> UpdateAsync(
        Guid id,
        UpdateArticleRequest request,
        CancellationToken cancellationToken);

    /// <summary>Soft delete.</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Bulk sort_order update for one site.</summary>
    Task<ServiceResult<bool>> ReorderAsync(ReorderRequest request, CancellationToken cancellationToken);
}
