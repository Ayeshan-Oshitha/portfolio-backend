---
paths:
  - "**/Functions/**"
  - "**/Api/**"
  - "**/*Function.cs"
  - "**/*Functions.cs"
  - "**/Dtos/**"
  - "**/DTOs/**"
---

# API surface

One function per endpoint — not one router function. It keeps the Azure portal's monitoring
readable.

## Public (anonymous, cached)

```
GET /api/public/projects?site=agency|personal&tag=&category=&featured=&page=&pageSize=
GET /api/public/projects/{slug}
GET /api/public/articles?site=&tag=&featured=
GET /api/public/services?site=&featured=
GET /api/public/services/{slug}
GET /api/public/pricing/combos?site=&featured=            # plans with no owning service
GET /api/public/pricing/services/{serviceId}?site=       # that service's tiers
GET /api/public/faqs?site=&category=
GET /api/public/tags?isTechnology=&category=
GET /api/public/home?site=agency|personal
```

`?site=` is **required** wherever site visibility applies. A missing `site` is a `400` with code
`site_required` — never "return everything".

Pricing is two routes, not one with an optional `serviceId`. Absence of a query parameter must
never silently change the `where` clause — combo packs (`service_id is null`) and a service's
tiers (`service_id = @id`) are different questions, so they get different URLs. The admin list
keeps both as explicit filters: `?serviceId=` and `?comboOnly=true`, defaulting to everything.

Public endpoints return only rows where `is_published = true`, `is_deleted = false`, and the
matching `show_on_{site}` flag is true. Order by that site's `sort_order`, then by
`published_at`/`year` descending.

`/api/public/home` is one round trip instead of six. Return only the featured slices for that
site: featured projects, featured articles, featured services, featured pricing plans, FAQs.

## Admin (JWT required)

```
POST   /api/admin/auth/login            # email + password
POST   /api/admin/auth/google           # Google ID token exchange
POST   /api/admin/auth/register
POST   /api/admin/auth/refresh

GET    /api/admin/users                 # super_admin only
POST   /api/admin/users/{id}/approve    # super_admin only
POST   /api/admin/users/{id}/reject     # super_admin only

CRUD   /api/admin/projects              # + /{id}/images, /{id}/images/reorder
CRUD   /api/admin/articles
CRUD   /api/admin/services              # + /{id}/features
CRUD   /api/admin/pricing-plans
CRUD   /api/admin/faqs
CRUD   /api/admin/tags

POST   /api/admin/media/signature       # Cloudinary signed upload params
POST   /api/admin/{entity}/reorder      # bulk sort_order update
```

Admin endpoints return drafts and metadata. Use admin-specific DTOs — never reuse the public
response DTOs, or admin-only fields will eventually leak to the public sites.

## Response conventions

- Lists: `{ items, page, pageSize, total }`. Default `pageSize` 20, cap at 100.
- Errors: RFC 7807 `application/problem+json` with a stable machine-readable `code`
  (`site_required`, `account_pending`, `slug_taken`, `validation_failed`, …).
- Public GETs: `Cache-Control: public, max-age=300` plus an `ETag`. Handle
  `If-None-Match` and return `304`.
- Admin responses: `Cache-Control: no-store`.
