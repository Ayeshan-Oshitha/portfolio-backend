using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

public interface ITagService
{
    /// <summary>Public read. Tags carry no site visibility, so there is no site filter.</summary>
    Task<IReadOnlyList<TagResponse>> GetPublicTagsAsync(
        bool? isTechnology,
        TechCategory? category,
        CancellationToken cancellationToken);

    Task<PagedResult<AdminTagResponse>> GetAdminTagsAsync(
        bool? isTechnology,
        TechCategory? category,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminTagResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<AdminTagResponse>> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<AdminTagResponse>> UpdateAsync(Guid id, UpdateTagRequest request, CancellationToken cancellationToken);

    /// <summary>Soft delete. Refused while the tag is still attached to a project or article.</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
