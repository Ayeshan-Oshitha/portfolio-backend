using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Services;

public class FaqService : IFaqService
{
    private readonly FrostWoodTechDbContext _db;

    public FaqService(FrostWoodTechDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FaqResponse>> GetPublicFaqsAsync(
        Site site,
        string? category,
        CancellationToken cancellationToken)
    {
        // is_deleted is handled by the DbContext's global filter; is_published and the site flag
        // are applied here and are not optional.
        var query = ForSite(_db.Faqs.AsNoTracking().Where(f => f.IsPublished), site);

        if (category is not null)
        {
            query = query.Where(f => f.Category == category);
        }

        // Not paged: an FAQ page renders the whole list, grouped by category client-side.
        return await OrderForSite(query, site)
            .Select(PublicProjection(site))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<AdminFaqResponse>> GetAdminFaqsAsync(
        Site? site,
        bool? isPublished,
        string? category,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Faqs.AsNoTracking();

        if (site is not null)
        {
            query = ForSite(query, site.Value);
        }

        if (isPublished is not null)
        {
            query = query.Where(f => f.IsPublished == isPublished);
        }

        if (category is not null)
        {
            query = query.Where(f => f.Category == category);
        }

        if (search is not null)
        {
            query = query.Where(f =>
                EF.Functions.ILike(f.Question, $"%{search}%")
                || EF.Functions.ILike(f.Answer, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        // Drafts have no meaningful site order, so the admin list groups by category instead.
        var items = await query
            .OrderBy(f => f.Category)
            .ThenBy(f => f.SortOrder)
            .ThenBy(f => f.Question)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminFaqResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminFaqResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var faq = await _db.Faqs
            .AsNoTracking()
            .Where(f => f.Id == id)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return faq is null ? NotFound(id) : ServiceResult<AdminFaqResponse>.Success(faq);
    }

    public async Task<ServiceResult<AdminFaqResponse>> CreateAsync(
        CreateFaqRequest request,
        CancellationToken cancellationToken)
    {
        var question = Blank(request.Question);
        var answer = Blank(request.Answer);

        var validationError = Validate(question, answer, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminFaqResponse>.Validation(validationError);
        }

        var faq = new Faq
        {
            Id = Guid.NewGuid(),
            Question = question!,
            Answer = answer!,
            Category = Blank(request.Category),
            SortOrder = request.SortOrder,
            IsPublished = request.IsPublished,
            ShowOnAgency = request.ShowOnAgency,
            FeaturedOnAgency = request.FeaturedOnAgency,
            AgencySortOrder = request.AgencySortOrder,
            ShowOnPersonal = request.ShowOnPersonal,
            FeaturedOnPersonal = request.FeaturedOnPersonal,
            PersonalSortOrder = request.PersonalSortOrder
        };

        _db.Faqs.Add(faq);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminFaqResponse>.Success(ToAdminResponse(faq));
    }

    public async Task<ServiceResult<AdminFaqResponse>> UpdateAsync(
        Guid id,
        UpdateFaqRequest request,
        CancellationToken cancellationToken)
    {
        var faq = await _db.Faqs.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (faq is null)
        {
            return NotFound(id);
        }

        var question = Blank(request.Question);
        var answer = Blank(request.Answer);

        var validationError = Validate(question, answer, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminFaqResponse>.Validation(validationError);
        }

        faq.Question = question!;
        faq.Answer = answer!;
        faq.Category = Blank(request.Category);
        faq.SortOrder = request.SortOrder;
        faq.IsPublished = request.IsPublished;
        faq.ShowOnAgency = request.ShowOnAgency;
        faq.FeaturedOnAgency = request.FeaturedOnAgency;
        faq.AgencySortOrder = request.AgencySortOrder;
        faq.ShowOnPersonal = request.ShowOnPersonal;
        faq.FeaturedOnPersonal = request.FeaturedOnPersonal;
        faq.PersonalSortOrder = request.PersonalSortOrder;

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminFaqResponse>.Success(ToAdminResponse(faq));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var faq = await _db.Faqs.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (faq is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No FAQ with id {id}.");
        }

        faq.IsDeleted = true;
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
            return ServiceResult<bool>.Validation("The same FAQ id appears more than once.");
        }

        var faqs = await _db.Faqs
            .Where(f => ids.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, cancellationToken);

        var missing = ids.Where(id => !faqs.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No FAQ with id {string.Join(", ", missing)}.");
        }

        foreach (var item in items)
        {
            var faq = faqs[item.Id];

            if (request.Site == Site.Agency)
            {
                faq.AgencySortOrder = item.SortOrder;
            }
            else
            {
                faq.PersonalSortOrder = item.SortOrder;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private static IQueryable<Faq> ForSite(IQueryable<Faq> query, Site site) =>
        site == Site.Agency
            ? query.Where(f => f.ShowOnAgency)
            : query.Where(f => f.ShowOnPersonal);

    /// <summary>
    /// Category first so the page can render its groups in a stable order, then that site's
    /// sort order, then the entity's own fallback order.
    /// </summary>
    private static IOrderedQueryable<Faq> OrderForSite(IQueryable<Faq> query, Site site) =>
        site == Site.Agency
            ? query.OrderBy(f => f.Category).ThenBy(f => f.AgencySortOrder).ThenBy(f => f.SortOrder)
            : query.OrderBy(f => f.Category).ThenBy(f => f.PersonalSortOrder).ThenBy(f => f.SortOrder);

    /// <summary>Null when the FAQ is valid, otherwise the message to hand back.</summary>
    private static string? Validate(string? question, string? answer, CreateFaqRequest request)
    {
        if (question is null)
        {
            return "Question is required.";
        }

        if (answer is null)
        {
            return "Answer is required.";
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

    private static ServiceResult<AdminFaqResponse> NotFound(Guid id) =>
        ServiceResult<AdminFaqResponse>.NotFound("not_found", $"No FAQ with id {id}.");

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Projected inside the query so the SQL stays narrow.</summary>
    private static Expression<Func<Faq, FaqResponse>> PublicProjection(Site site)
    {
        if (site == Site.Agency)
        {
            return f => new FaqResponse
            {
                Id = f.Id,
                Question = f.Question,
                Answer = f.Answer,
                Category = f.Category,
                Featured = f.FeaturedOnAgency,
                SortOrder = f.AgencySortOrder
            };
        }

        return f => new FaqResponse
        {
            Id = f.Id,
            Question = f.Question,
            Answer = f.Answer,
            Category = f.Category,
            Featured = f.FeaturedOnPersonal,
            SortOrder = f.PersonalSortOrder
        };
    }

    private static readonly Expression<Func<Faq, AdminFaqResponse>> AdminProjection = f => new AdminFaqResponse
    {
        Id = f.Id,
        Question = f.Question,
        Answer = f.Answer,
        Category = f.Category,
        SortOrder = f.SortOrder,
        IsPublished = f.IsPublished,
        ShowOnAgency = f.ShowOnAgency,
        FeaturedOnAgency = f.FeaturedOnAgency,
        AgencySortOrder = f.AgencySortOrder,
        ShowOnPersonal = f.ShowOnPersonal,
        FeaturedOnPersonal = f.FeaturedOnPersonal,
        PersonalSortOrder = f.PersonalSortOrder,
        CreatedAt = f.CreatedAt,
        UpdatedAt = f.UpdatedAt
    };

    /// <summary>The same shape for an entity already in memory after a write.</summary>
    private static readonly Func<Faq, AdminFaqResponse> ToAdminResponse = AdminProjection.Compile();
}
