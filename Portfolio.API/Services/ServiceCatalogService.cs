using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Portfolio.API.Common;
using Portfolio.API.Data;
using Portfolio.API.DTOs.Admin;
using Portfolio.API.DTOs.Public;
using Portfolio.API.Entities;
using Portfolio.API.Enums;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Services;

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly PortfolioDbContext _db;

    public ServiceCatalogService(PortfolioDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ServiceResponse>> GetPublicServicesAsync(
        Site site,
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // is_deleted is handled by the DbContext's global filter; is_published and the site flag
        // are applied here and are not optional.
        var query = ForSite(_db.Services.AsNoTracking().Where(s => s.IsPublished), site);

        if (featured is not null)
        {
            query = site == Site.Agency
                ? query.Where(s => s.FeaturedOnAgency == featured)
                : query.Where(s => s.FeaturedOnPersonal == featured);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await OrderForSite(query, site)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(PublicProjection(site))
            .ToListAsync(cancellationToken);

        return new PagedResult<ServiceResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<ServiceResponse>> GetPublicServiceBySlugAsync(
        Site site,
        string slug,
        CancellationToken cancellationToken)
    {
        var service = await ForSite(_db.Services.AsNoTracking().Where(s => s.IsPublished), site)
            .Where(s => s.Slug == slug)
            .Select(PublicProjection(site))
            .FirstOrDefaultAsync(cancellationToken);

        return service is null
            ? ServiceResult<ServiceResponse>.NotFound(
                "not_found",
                $"No published service with slug '{slug}' on this site.")
            : ServiceResult<ServiceResponse>.Success(service);
    }

    public async Task<PagedResult<AdminServiceResponse>> GetAdminServicesAsync(
        Site? site,
        bool? isPublished,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Services.AsNoTracking();

        if (site is not null)
        {
            query = ForSite(query, site.Value);
        }

        if (isPublished is not null)
        {
            query = query.Where(s => s.IsPublished == isPublished);
        }

        if (search is not null)
        {
            query = query.Where(s => EF.Functions.ILike(s.Name, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        // Drafts have no meaningful site order, so the admin list is alphabetical instead.
        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminServiceResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminServiceResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await _db.Services
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return service is null ? NotFound(id) : ServiceResult<AdminServiceResponse>.Success(service);
    }

    public async Task<ServiceResult<AdminServiceResponse>> CreateAsync(
        CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var name = Blank(request.Name);
        var shortDescription = Blank(request.ShortDescription);
        var description = Blank(request.Description);

        var validationError = Validate(name, shortDescription, description, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminServiceResponse>.Validation(validationError);
        }

        var slug = ResolveSlug(request.Slug, name!);
        if (await SlugExistsAsync(slug, excludingId: null, cancellationToken))
        {
            return SlugTaken(slug);
        }

        var service = new ServiceOffering
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = name!,
            ShortDescription = shortDescription!,
            Description = description!,
            IconName = Blank(request.IconName),
            IconCloudinaryId = Blank(request.IconCloudinaryId),
            HeroImageId = Blank(request.HeroImageId),
            ShowOnAgency = request.ShowOnAgency,
            FeaturedOnAgency = request.FeaturedOnAgency,
            AgencySortOrder = request.AgencySortOrder,
            ShowOnPersonal = request.ShowOnPersonal,
            FeaturedOnPersonal = request.FeaturedOnPersonal,
            PersonalSortOrder = request.PersonalSortOrder
        };

        ApplyPublished(service, request.IsPublished);

        _db.Services.Add(service);
        await _db.SaveChangesAsync(cancellationToken);

        // Re-read so the response carries the feature list the projection builds — empty here,
        // but the shape stays the same as every other admin read.
        return await GetByIdAsync(service.Id, cancellationToken);
    }

    public async Task<ServiceResult<AdminServiceResponse>> UpdateAsync(
        Guid id,
        UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (service is null)
        {
            return NotFound(id);
        }

        var name = Blank(request.Name);
        var shortDescription = Blank(request.ShortDescription);
        var description = Blank(request.Description);

        var validationError = Validate(name, shortDescription, description, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminServiceResponse>.Validation(validationError);
        }

        var slug = ResolveSlug(request.Slug, name!);
        if (await SlugExistsAsync(slug, excludingId: id, cancellationToken))
        {
            return SlugTaken(slug);
        }

        service.Slug = slug;
        service.Name = name!;
        service.ShortDescription = shortDescription!;
        service.Description = description!;
        service.IconName = Blank(request.IconName);
        service.IconCloudinaryId = Blank(request.IconCloudinaryId);
        service.HeroImageId = Blank(request.HeroImageId);
        service.ShowOnAgency = request.ShowOnAgency;
        service.FeaturedOnAgency = request.FeaturedOnAgency;
        service.AgencySortOrder = request.AgencySortOrder;
        service.ShowOnPersonal = request.ShowOnPersonal;
        service.FeaturedOnPersonal = request.FeaturedOnPersonal;
        service.PersonalSortOrder = request.PersonalSortOrder;

        ApplyPublished(service, request.IsPublished);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(service.Id, cancellationToken);
    }

    public async Task<ServiceResult<AdminServiceResponse>> SetPublishedAsync(
        Guid id,
        SetPublishedRequest request,
        CancellationToken cancellationToken)
    {
        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (service is null)
        {
            return NotFound(id);
        }

        ApplyPublished(service, request.IsPublished);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(service.Id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (service is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No service with id {id}.");
        }

        // Soft delete: the feature rows and pricing plans stay put so restoring the row keeps them.
        service.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ReorderAsync(ReorderRequest request, CancellationToken cancellationToken)
    {
        if (request.Site is null)
        {
            return ServiceResult<bool>.Validation("site is required — sort order is kept per site.");
        }

        var items = request.Items;
        if (items is null || items.Count == 0)
        {
            return ServiceResult<bool>.Validation("At least one item is required.");
        }

        var ids = items.Select(i => i.Id).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return ServiceResult<bool>.Validation("The same service id appears more than once.");
        }

        var services = await _db.Services
            .Where(s => ids.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var missing = ids.Where(id => !services.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No service with id {string.Join(", ", missing)}.");
        }

        foreach (var item in items)
        {
            var service = services[item.Id];

            if (request.Site == Site.Agency)
            {
                service.AgencySortOrder = item.SortOrder;
            }
            else
            {
                service.PersonalSortOrder = item.SortOrder;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<ServiceFeatureResponse>> AddFeatureAsync(
        Guid serviceId,
        AddServiceFeatureRequest request,
        CancellationToken cancellationToken)
    {
        var service = await LoadWithFeaturesAsync(serviceId, cancellationToken);
        if (service is null)
        {
            return FeatureServiceNotFound(serviceId);
        }

        var title = Blank(request.Title);
        if (title is null)
        {
            return ServiceResult<ServiceFeatureResponse>.Validation("Title is required.");
        }

        var feature = new ServiceFeature
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            Title = title,
            Description = Blank(request.Description),
            IconName = Blank(request.IconName),
            SortOrder = request.SortOrder
        };

        _db.ServiceFeatures.Add(feature);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<ServiceFeatureResponse>.Success(ToFeatureResponse(feature));
    }

    public async Task<ServiceResult<ServiceFeatureResponse>> UpdateFeatureAsync(
        Guid serviceId,
        Guid featureId,
        UpdateServiceFeatureRequest request,
        CancellationToken cancellationToken)
    {
        var service = await LoadWithFeaturesAsync(serviceId, cancellationToken);
        if (service is null)
        {
            return FeatureServiceNotFound(serviceId);
        }

        // Scoped to the parent, so a feature id borrowed from another service is a 404.
        var feature = service.Features.FirstOrDefault(f => f.Id == featureId);
        if (feature is null)
        {
            return ServiceResult<ServiceFeatureResponse>.NotFound(
                "not_found",
                $"No feature with id {featureId} on service {serviceId}.");
        }

        var title = Blank(request.Title);
        if (title is null)
        {
            return ServiceResult<ServiceFeatureResponse>.Validation("Title is required.");
        }

        feature.Title = title;
        feature.Description = Blank(request.Description);
        feature.IconName = Blank(request.IconName);
        feature.SortOrder = request.SortOrder;

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<ServiceFeatureResponse>.Success(ToFeatureResponse(feature));
    }

    public async Task<ServiceResult<bool>> DeleteFeatureAsync(
        Guid serviceId,
        Guid featureId,
        CancellationToken cancellationToken)
    {
        var service = await LoadWithFeaturesAsync(serviceId, cancellationToken);
        if (service is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No service with id {serviceId}.");
        }

        var feature = service.Features.FirstOrDefault(f => f.Id == featureId);
        if (feature is null)
        {
            return ServiceResult<bool>.NotFound(
                "not_found",
                $"No feature with id {featureId} on service {serviceId}.");
        }

        // Hard delete — the row carries no soft-delete flag.
        service.Features.Remove(feature);
        _db.ServiceFeatures.Remove(feature);

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ReorderFeaturesAsync(
        Guid serviceId,
        FeatureReorderRequest request,
        CancellationToken cancellationToken)
    {
        var service = await LoadWithFeaturesAsync(serviceId, cancellationToken);
        if (service is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No service with id {serviceId}.");
        }

        var items = request.Items;
        if (items is null || items.Count == 0)
        {
            return ServiceResult<bool>.Validation("At least one item is required.");
        }

        var ids = items.Select(i => i.Id).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return ServiceResult<bool>.Validation("The same feature id appears more than once.");
        }

        var features = service.Features.ToDictionary(f => f.Id);

        var missing = ids.Where(id => !features.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return ServiceResult<bool>.NotFound(
                "not_found",
                $"No feature with id {string.Join(", ", missing)} on service {serviceId}.");
        }

        foreach (var item in items)
        {
            features[item.Id].SortOrder = item.SortOrder;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private static IQueryable<ServiceOffering> ForSite(IQueryable<ServiceOffering> query, Site site) =>
        site == Site.Agency
            ? query.Where(s => s.ShowOnAgency)
            : query.Where(s => s.ShowOnPersonal);

    private static IOrderedQueryable<ServiceOffering> OrderForSite(IQueryable<ServiceOffering> query, Site site) =>
        site == Site.Agency
            ? query.OrderBy(s => s.AgencySortOrder).ThenByDescending(s => s.PublishedAt)
            : query.OrderBy(s => s.PersonalSortOrder).ThenByDescending(s => s.PublishedAt);

    /// <summary>Null when the service is valid, otherwise the message to hand back.</summary>
    private static string? Validate(
        string? name,
        string? shortDescription,
        string? description,
        CreateServiceRequest request)
    {
        if (name is null)
        {
            return "Name is required.";
        }

        if (shortDescription is null)
        {
            return "shortDescription is required.";
        }

        if (description is null)
        {
            return "Description is required.";
        }

        if (request.FeaturedOnAgency && !request.ShowOnAgency)
        {
            return "featuredOnAgency requires showOnAgency.";
        }

        if (request.FeaturedOnPersonal && !request.ShowOnPersonal)
        {
            return "featuredOnPersonal requires showOnPersonal.";
        }

        return null;
    }

    /// <summary>
    /// Going live stamps published_at the first time only; unpublishing never clears it, so a
    /// service that comes back keeps its original date.
    /// </summary>
    private static void ApplyPublished(ServiceOffering service, bool isPublished)
    {
        if (isPublished && service.PublishedAt is null)
        {
            service.PublishedAt = DateTimeOffset.UtcNow;
        }

        service.IsPublished = isPublished;
    }

    private Task<ServiceOffering?> LoadWithFeaturesAsync(Guid serviceId, CancellationToken cancellationToken) =>
        _db.Services
            .Include(s => s.Features)
            .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);

    private static string ResolveSlug(string? requestedSlug, string name) =>
        SlugGenerator.Generate(string.IsNullOrWhiteSpace(requestedSlug) ? name : requestedSlug);

    private Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken) =>
        _db.Services.AnyAsync(s => s.Slug == slug && (excludingId == null || s.Id != excludingId), cancellationToken);

    private static ServiceResult<AdminServiceResponse> NotFound(Guid id) =>
        ServiceResult<AdminServiceResponse>.NotFound("not_found", $"No service with id {id}.");

    private static ServiceResult<AdminServiceResponse> SlugTaken(string slug) =>
        ServiceResult<AdminServiceResponse>.Conflict("slug_taken", $"Slug '{slug}' is already in use.");

    private static ServiceResult<ServiceFeatureResponse> FeatureServiceNotFound(Guid serviceId) =>
        ServiceResult<ServiceFeatureResponse>.NotFound("not_found", $"No service with id {serviceId}.");

    /// <summary>Trimmed, or null when the caller sent nothing meaningful.</summary>
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ServiceFeatureResponse ToFeatureResponse(ServiceFeature feature) => new()
    {
        Id = feature.Id,
        Title = feature.Title,
        Description = feature.Description,
        IconName = feature.IconName,
        SortOrder = feature.SortOrder
    };

    /// <summary>
    /// Projected inside the query, features and all, so a list page is one round trip rather than
    /// one query per service. The site picks which visibility pair is exposed.
    /// </summary>
    private static Expression<Func<ServiceOffering, ServiceResponse>> PublicProjection(Site site)
    {
        if (site == Site.Agency)
        {
            return s => new ServiceResponse
            {
                Id = s.Id,
                Slug = s.Slug,
                Name = s.Name,
                ShortDescription = s.ShortDescription,
                Description = s.Description,
                IconName = s.IconName,
                IconCloudinaryId = s.IconCloudinaryId,
                HeroImageId = s.HeroImageId,
                PublishedAt = s.PublishedAt,
                Featured = s.FeaturedOnAgency,
                SortOrder = s.AgencySortOrder,
                Features = s.Features
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Title)
                    .Select(f => new ServiceFeatureResponse
                    {
                        Id = f.Id,
                        Title = f.Title,
                        Description = f.Description,
                        IconName = f.IconName,
                        SortOrder = f.SortOrder
                    })
                    .ToList()
            };
        }

        return s => new ServiceResponse
        {
            Id = s.Id,
            Slug = s.Slug,
            Name = s.Name,
            ShortDescription = s.ShortDescription,
            Description = s.Description,
            IconName = s.IconName,
            IconCloudinaryId = s.IconCloudinaryId,
            HeroImageId = s.HeroImageId,
            PublishedAt = s.PublishedAt,
            Featured = s.FeaturedOnPersonal,
            SortOrder = s.PersonalSortOrder,
            Features = s.Features
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Title)
                .Select(f => new ServiceFeatureResponse
                {
                    Id = f.Id,
                    Title = f.Title,
                    Description = f.Description,
                    IconName = f.IconName,
                    SortOrder = f.SortOrder
                })
                .ToList()
        };
    }

    private static readonly Expression<Func<ServiceOffering, AdminServiceResponse>> AdminProjection = s => new AdminServiceResponse
    {
        Id = s.Id,
        Slug = s.Slug,
        Name = s.Name,
        ShortDescription = s.ShortDescription,
        Description = s.Description,
        IconName = s.IconName,
        IconCloudinaryId = s.IconCloudinaryId,
        HeroImageId = s.HeroImageId,
        IsPublished = s.IsPublished,
        PublishedAt = s.PublishedAt,
        ShowOnAgency = s.ShowOnAgency,
        FeaturedOnAgency = s.FeaturedOnAgency,
        AgencySortOrder = s.AgencySortOrder,
        ShowOnPersonal = s.ShowOnPersonal,
        FeaturedOnPersonal = s.FeaturedOnPersonal,
        PersonalSortOrder = s.PersonalSortOrder,
        Features = s.Features
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Title)
            .Select(f => new ServiceFeatureResponse
            {
                Id = f.Id,
                Title = f.Title,
                Description = f.Description,
                IconName = f.IconName,
                SortOrder = f.SortOrder
            })
            .ToList(),
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };
}
