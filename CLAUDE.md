# CLAUDE.md — FrostWoodTech CMS Backend

## What this is

One headless CMS API + one database, powering **three** frontends:

| Consumer | Content it reads |
|---|---|
| FrostWoodTech agency site (React) | Agency projects, articles, services, pricing, FAQs |
| FrostWoodTech personal site (React) | Personal projects, articles, FAQs |
| Admin SPA (React) | Everything, incl. drafts + user approvals |

No duplicated rows per site — every visible entity carries per-site visibility flags and the
frontend asks for the site it wants. A project can appear on both sites at once.

## Stack

.NET 10 **Azure Functions** (not ASP.NET Web API) · **PostgreSQL on Neon** via EF Core + Npgsql ·
**Cloudinary** for media · custom **JWT** + **Google Sign-In** for admin auth.

## Layering

```
Azure Functions layer  →  Service layer  →  EF Core  →  Postgres
```

There is **no separate repository layer**. The service layer holds business logic *and* data
access. This is deliberate — the project must stay readable for a junior developer. Do not
propose adding repositories, CQRS, or a mediator.

## Two API surfaces — keep them physically separate

- **`/api/public/*`** — anonymous, read-only, cache-friendly. Returns only published,
  non-deleted rows.
- **`/api/cms/admin/*`** — JWT required, full CRUD, returns drafts and metadata.

Separate folders, separate response DTOs. A public DTO must never carry admin fields
(internal notes, audit info, unpublished relations).

## Non-negotiables

- **Never store image bytes in Postgres.** Store the Cloudinary `public_id` + dimensions + alt text.
- **Never let the client control `is_published` filtering.** Public endpoints filter server-side.
- **`?site=` is required** on every public endpoint with site visibility. Missing site ⇒ `400`,
  never "return everything". This is the guard that stops personal content leaking onto the
  agency site.
- All timestamps `timestamptz`, stored UTC.
- Every content table gets `created_at`, `updated_at`, `is_deleted` (soft delete).
- `alt_text` is required on every image — enforce in validation.
- Secrets live in app settings / Key Vault, never in committed `local.settings.json`.

## Conventions

- snake_case tables/columns in Postgres, PascalCase entities in C#.
- Slugs: generated from title, lowercase, hyphenated, uniqueness-checked, and **stable once
  published** (changing one breaks live links).
- Markdown stored raw; sanitize on render in React, not on write.
- Soft delete everywhere; hard delete only via a super-admin-only endpoint.
- Enums as C# enums mapped to Postgres native enums or text — **never int**, so migrations
  stay readable.
- Lists return `{ items, page, pageSize, total }`.
- Errors return RFC 7807 `application/problem+json` with a stable `code` string.

## Where the detail lives

These load automatically when you touch the matching files — don't go read them preemptively:

| Rule | Covers |
|---|---|
| `.claude/rules/schema.md` | Every table, column, index, enum |
| `.claude/rules/api-surface.md` | Endpoint list, query params, caching |
| `.claude/rules/services-layer.md` | Service layer shape, validation rules |
| `.claude/rules/auth.md` | JWT + Google flow, user states, approval rules |
| `.claude/rules/media.md` | Cloudinary signed-upload flow, folder convention |
| `.claude/rules/functions-hosting.md` | DI, Neon pooling, migrations, cold start |

`docs/roadmap.md` is for humans — planned additions, not current state. Don't implement from it
unless asked.

## Glossary

| Term | Meaning |
|---|---|
| Site | `agency` or `personal` — the two public frontends |
| Featured | Appears on that site's home page |
| Combo pack | A `pricing_plan` with `service_id = null` |
| Technology tag | `tags.is_technology = true`; has an icon and a category |
| Category tag | `tags.is_technology = false`; e.g. Frontend, Backend, Agentic AI |
