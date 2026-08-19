using Portfolio.API.Common;
using Portfolio.API.DTOs.Admin;
using Portfolio.API.DTOs.Public;
using Portfolio.API.Enums;

namespace Portfolio.API.Interfaces;

public interface IProjectService
{
    /// <summary>
    /// Public read: always scoped to one site and to published, non-deleted rows. There is no
    /// overload that lets a caller skip those filters.
    /// </summary>
    Task<PagedResult<ProjectResponse>> GetPublicProjectsAsync(
        Site site,
        string? tagSlug,
        string? categorySlug,
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProjectResponse>> GetPublicProjectBySlugAsync(
        Site site,
        string slug,
        CancellationToken cancellationToken);

    Task<PagedResult<AdminProjectResponse>> GetAdminProjectsAsync(
        Site? site,
        bool? isPublished,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminProjectResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<AdminProjectResponse>> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminProjectResponse>> UpdateAsync(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken);

    /// <summary>Flips the draft flag on its own, stamping published_at the first time it goes live.</summary>
    Task<ServiceResult<AdminProjectResponse>> SetPublishedAsync(
        Guid id,
        SetPublishedRequest request,
        CancellationToken cancellationToken);

    /// <summary>Soft delete.</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Bulk sort_order update for one site.</summary>
    Task<ServiceResult<bool>> ReorderAsync(ReorderRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<ProjectImageResponse>> AddImageAsync(
        Guid projectId,
        AddProjectImageRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProjectImageResponse>> UpdateImageAsync(
        Guid projectId,
        Guid imageId,
        UpdateProjectImageRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Hard delete — image rows carry no soft-delete flag. The Cloudinary asset is left alone.
    /// </summary>
    Task<ServiceResult<bool>> DeleteImageAsync(Guid projectId, Guid imageId, CancellationToken cancellationToken);

    /// <summary>Bulk sort_order update for one project's gallery.</summary>
    Task<ServiceResult<bool>> ReorderImagesAsync(
        Guid projectId,
        ImageReorderRequest request,
        CancellationToken cancellationToken);
}
