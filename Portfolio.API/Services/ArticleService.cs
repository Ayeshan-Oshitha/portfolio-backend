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

public class ArticleService : IArticleService
{
    private readonly PortfolioDbContext _db;

    public ArticleService(PortfolioDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ArticleResponse>> GetPublicArticlesAsync(
        Site site,
        string? tagSlug,
        bool? featured,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // is_deleted is handled by the DbContext's global filter; is_published and the site flag
        // are applied here and are not optional.
        var query = ForSite(_db.Articles.AsNoTracking().Where(a => a.IsPublished), site);

        if (tagSlug is not null)
        {
            query = query.Where(a => a.ArticleTags.Any(at => at.Tag.Slug == tagSlug && !at.Tag.IsDeleted));
        }

        if (featured is not null)
        {
            query = site == Site.Agency
                ? query.Where(a => a.FeaturedOnAgency == featured)
                : query.Where(a => a.FeaturedOnPersonal == featured);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await OrderForSite(query, site)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(PublicProjection(site))
            .ToListAsync(cancellationToken);

        return new PagedResult<ArticleResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<ArticleResponse>> GetPublicArticleBySlugAsync(
        Site site,
        string slug,
        CancellationToken cancellationToken)
    {
        var article = await ForSite(_db.Articles.AsNoTracking().Where(a => a.IsPublished), site)
            .Where(a => a.Slug == slug)
            .Select(PublicProjection(site))
            .FirstOrDefaultAsync(cancellationToken);

        return article is null
            ? ServiceResult<ArticleResponse>.NotFound(
                "not_found",
                $"No published article with slug '{slug}' on this site.")
            : ServiceResult<ArticleResponse>.Success(article);
    }

    public async Task<PagedResult<AdminArticleResponse>> GetAdminArticlesAsync(
        Site? site,
        bool? isPublished,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Articles.AsNoTracking();

        if (site is not null)
        {
            query = ForSite(query, site.Value);
        }

        if (isPublished is not null)
        {
            query = query.Where(a => a.IsPublished == isPublished);
        }

        if (search is not null)
        {
            query = query.Where(a => EF.Functions.ILike(a.Title, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        // Drafts have no meaningful site order, so the admin list is newest-first instead.
        var items = await query
            .OrderByDescending(a => a.PublishedDate)
            .ThenBy(a => a.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AdminProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminArticleResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<ServiceResult<AdminArticleResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var article = await _db.Articles
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(AdminProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return article is null ? NotFound(id) : ServiceResult<AdminArticleResponse>.Success(article);
    }

    public async Task<ServiceResult<AdminArticleResponse>> CreateAsync(
        CreateArticleRequest request,
        CancellationToken cancellationToken)
    {
        var title = Blank(request.Title);
        var excerpt = Blank(request.Excerpt);
        var mediumUrl = Blank(request.MediumUrl);

        var validationError = Validate(title, excerpt, mediumUrl, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminArticleResponse>.Validation(validationError);
        }

        var tagIds = Distinct(request.TagIds);

        var unknownTags = await UnknownTagIdsAsync(tagIds, cancellationToken);
        if (unknownTags is not null)
        {
            return ServiceResult<AdminArticleResponse>.Validation(unknownTags);
        }

        var slug = ResolveSlug(request.Slug, title!);
        if (await SlugExistsAsync(slug, excludingId: null, cancellationToken))
        {
            return SlugTaken(slug);
        }

        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = title!,
            Excerpt = excerpt!,
            Slug = slug,
            PublishedDate = request.PublishedDate,
            MediumUrl = mediumUrl!,
            CoverImageId = Blank(request.CoverImageId),
            IsPublished = request.IsPublished,
            ShowOnAgency = request.ShowOnAgency,
            FeaturedOnAgency = request.FeaturedOnAgency,
            AgencySortOrder = request.AgencySortOrder,
            ShowOnPersonal = request.ShowOnPersonal,
            FeaturedOnPersonal = request.FeaturedOnPersonal,
            PersonalSortOrder = request.PersonalSortOrder,
            ArticleTags = [.. tagIds.Select(tagId => new ArticleTag { TagId = tagId })]
        };

        _db.Articles.Add(article);
        await _db.SaveChangesAsync(cancellationToken);

        // Re-read rather than project the in-memory entity: the links were added by tag id, so
        // their Tag navigations are not loaded yet.
        return await GetByIdAsync(article.Id, cancellationToken);
    }

    public async Task<ServiceResult<AdminArticleResponse>> UpdateAsync(
        Guid id,
        UpdateArticleRequest request,
        CancellationToken cancellationToken)
    {
        var article = await _db.Articles
            .Include(a => a.ArticleTags)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (article is null)
        {
            return NotFound(id);
        }

        var title = Blank(request.Title);
        var excerpt = Blank(request.Excerpt);
        var mediumUrl = Blank(request.MediumUrl);

        var validationError = Validate(title, excerpt, mediumUrl, request);
        if (validationError is not null)
        {
            return ServiceResult<AdminArticleResponse>.Validation(validationError);
        }

        var tagIds = Distinct(request.TagIds);

        var unknownTags = await UnknownTagIdsAsync(tagIds, cancellationToken);
        if (unknownTags is not null)
        {
            return ServiceResult<AdminArticleResponse>.Validation(unknownTags);
        }

        var slug = ResolveSlug(request.Slug, title!);
        if (await SlugExistsAsync(slug, excludingId: id, cancellationToken))
        {
            return SlugTaken(slug);
        }

        article.Title = title!;
        article.Excerpt = excerpt!;
        article.Slug = slug;
        article.PublishedDate = request.PublishedDate;
        article.MediumUrl = mediumUrl!;
        article.CoverImageId = Blank(request.CoverImageId);
        article.IsPublished = request.IsPublished;
        article.ShowOnAgency = request.ShowOnAgency;
        article.FeaturedOnAgency = request.FeaturedOnAgency;
        article.AgencySortOrder = request.AgencySortOrder;
        article.ShowOnPersonal = request.ShowOnPersonal;
        article.FeaturedOnPersonal = request.FeaturedOnPersonal;
        article.PersonalSortOrder = request.PersonalSortOrder;

        SyncTags(article, tagIds);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(article.Id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (article is null)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No article with id {id}.");
        }

        // Soft delete: the tag links stay put so restoring the row keeps its tags.
        article.IsDeleted = true;
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
            return ServiceResult<bool>.Validation("The same article id appears more than once.");
        }

        var articles = await _db.Articles
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var missing = ids.Where(id => !articles.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return ServiceResult<bool>.NotFound("not_found", $"No article with id {string.Join(", ", missing)}.");
        }

        foreach (var item in items)
        {
            var article = articles[item.Id];

            if (request.Site == Site.Agency)
            {
                article.AgencySortOrder = item.SortOrder;
            }
            else
            {
                article.PersonalSortOrder = item.SortOrder;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private static IQueryable<Article> ForSite(IQueryable<Article> query, Site site) =>
        site == Site.Agency
            ? query.Where(a => a.ShowOnAgency)
            : query.Where(a => a.ShowOnPersonal);

    private static IOrderedQueryable<Article> OrderForSite(IQueryable<Article> query, Site site) =>
        site == Site.Agency
            ? query.OrderBy(a => a.AgencySortOrder).ThenByDescending(a => a.PublishedDate)
            : query.OrderBy(a => a.PersonalSortOrder).ThenByDescending(a => a.PublishedDate);

    /// <summary>Null when the article is valid, otherwise the message to hand back.</summary>
    private static string? Validate(string? title, string? excerpt, string? mediumUrl, CreateArticleRequest request)
    {
        if (title is null)
        {
            return "Title is required.";
        }

        if (excerpt is null)
        {
            return "Excerpt is required.";
        }

        if (mediumUrl is null)
        {
            return "mediumUrl is required.";
        }

        if (!Uri.TryCreate(mediumUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return "mediumUrl must be an absolute http(s) URL.";
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

    /// <summary>Null when every id resolves to a live tag, otherwise the message to hand back.</summary>
    private async Task<string?> UnknownTagIdsAsync(IReadOnlyList<Guid> tagIds, CancellationToken cancellationToken)
    {
        if (tagIds.Count == 0)
        {
            return null;
        }

        var found = await _db.Tags
            .Where(t => tagIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var missing = tagIds.Except(found).ToList();

        return missing.Count == 0 ? null : $"Unknown tag id(s): {string.Join(", ", missing)}.";
    }

    /// <summary>Adds and removes only what changed, so untouched links keep their row.</summary>
    private void SyncTags(Article article, IReadOnlyList<Guid> tagIds)
    {
        foreach (var link in article.ArticleTags.Where(at => !tagIds.Contains(at.TagId)).ToList())
        {
            article.ArticleTags.Remove(link);
            _db.Remove(link);
        }

        foreach (var tagId in tagIds.Where(id => article.ArticleTags.All(at => at.TagId != id)))
        {
            article.ArticleTags.Add(new ArticleTag { ArticleId = article.Id, TagId = tagId });
        }
    }

    private static IReadOnlyList<Guid> Distinct(List<Guid>? tagIds) =>
        tagIds is null ? [] : [.. tagIds.Distinct()];

    private static string ResolveSlug(string? requestedSlug, string title) =>
        SlugGenerator.Generate(string.IsNullOrWhiteSpace(requestedSlug) ? title : requestedSlug);

    private Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken) =>
        _db.Articles.AnyAsync(a => a.Slug == slug && (excludingId == null || a.Id != excludingId), cancellationToken);

    private static ServiceResult<AdminArticleResponse> NotFound(Guid id) =>
        ServiceResult<AdminArticleResponse>.NotFound("not_found", $"No article with id {id}.");

    private static ServiceResult<AdminArticleResponse> SlugTaken(string slug) =>
        ServiceResult<AdminArticleResponse>.Conflict("slug_taken", $"Slug '{slug}' is already in use.");

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Projected inside the query, tags and all, so a list page is one round trip rather than one
    /// query per article. The site picks which visibility pair is exposed.
    /// </summary>
    private static Expression<Func<Article, ArticleResponse>> PublicProjection(Site site)
    {
        if (site == Site.Agency)
        {
            return a => new ArticleResponse
            {
                Id = a.Id,
                Title = a.Title,
                Excerpt = a.Excerpt,
                Slug = a.Slug,
                PublishedDate = a.PublishedDate,
                MediumUrl = a.MediumUrl,
                CoverImageId = a.CoverImageId,
                Featured = a.FeaturedOnAgency,
                SortOrder = a.AgencySortOrder,
                Tags = a.ArticleTags
                    .Where(at => !at.Tag.IsDeleted)
                    .OrderBy(at => at.Tag.SortOrder)
                    .ThenBy(at => at.Tag.Name)
                    .Select(at => new TagResponse
                    {
                        Id = at.Tag.Id,
                        Name = at.Tag.Name,
                        Slug = at.Tag.Slug,
                        IsTechnology = at.Tag.IsTechnology,
                        TechnologyCategory = at.Tag.TechnologyCategory,
                        IconUrl = at.Tag.IconUrl,
                        ColorHex = at.Tag.ColorHex,
                        SortOrder = at.Tag.SortOrder
                    })
                    .ToList()
            };
        }

        return a => new ArticleResponse
        {
            Id = a.Id,
            Title = a.Title,
            Excerpt = a.Excerpt,
            Slug = a.Slug,
            PublishedDate = a.PublishedDate,
            MediumUrl = a.MediumUrl,
            CoverImageId = a.CoverImageId,
            Featured = a.FeaturedOnPersonal,
            SortOrder = a.PersonalSortOrder,
            Tags = a.ArticleTags
                .Where(at => !at.Tag.IsDeleted)
                .OrderBy(at => at.Tag.SortOrder)
                .ThenBy(at => at.Tag.Name)
                .Select(at => new TagResponse
                {
                    Id = at.Tag.Id,
                    Name = at.Tag.Name,
                    Slug = at.Tag.Slug,
                    IsTechnology = at.Tag.IsTechnology,
                    TechnologyCategory = at.Tag.TechnologyCategory,
                    IconUrl = at.Tag.IconUrl,
                    ColorHex = at.Tag.ColorHex,
                    SortOrder = at.Tag.SortOrder
                })
                .ToList()
        };
    }

    private static readonly Expression<Func<Article, AdminArticleResponse>> AdminProjection = a => new AdminArticleResponse
    {
        Id = a.Id,
        Title = a.Title,
        Excerpt = a.Excerpt,
        Slug = a.Slug,
        PublishedDate = a.PublishedDate,
        MediumUrl = a.MediumUrl,
        CoverImageId = a.CoverImageId,
        IsPublished = a.IsPublished,
        ShowOnAgency = a.ShowOnAgency,
        FeaturedOnAgency = a.FeaturedOnAgency,
        AgencySortOrder = a.AgencySortOrder,
        ShowOnPersonal = a.ShowOnPersonal,
        FeaturedOnPersonal = a.FeaturedOnPersonal,
        PersonalSortOrder = a.PersonalSortOrder,
        Tags = a.ArticleTags
            .Where(at => !at.Tag.IsDeleted)
            .OrderBy(at => at.Tag.SortOrder)
            .ThenBy(at => at.Tag.Name)
            .Select(at => new AdminTagResponse
            {
                Id = at.Tag.Id,
                Name = at.Tag.Name,
                Slug = at.Tag.Slug,
                IsTechnology = at.Tag.IsTechnology,
                TechnologyCategory = at.Tag.TechnologyCategory,
                IconCloudinaryId = at.Tag.IconCloudinaryId,
                IconUrl = at.Tag.IconUrl,
                ColorHex = at.Tag.ColorHex,
                SortOrder = at.Tag.SortOrder,
                CreatedAt = at.Tag.CreatedAt,
                UpdatedAt = at.Tag.UpdatedAt
            })
            .ToList(),
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };
}
