---
paths:
  - "**/Domain/**"
  - "**/Entities/**"
  - "**/Data/**"
  - "**/Infrastructure/**"
  - "**/Migrations/**"
  - "**/*DbContext.cs"
  - "**/*Configuration.cs"
---

# Database schema

snake_case in Postgres, PascalCase entities in C#. Every content table also gets
`created_at`, `updated_at`, `is_deleted`.

## Site visibility block

Applies to `projects`, `articles`, `services`, `pricing_plans`, `faqs`:

```
show_on_agency          bool
featured_on_agency      bool      -- home page card
agency_sort_order       int

show_on_personal        bool
featured_on_personal    bool
personal_sort_order     int
```

`featured_on_X` may only be true when `show_on_X` is true — enforce in the service layer.

## projects

Own dedicated page per project, addressed by slug.

```
id                  uuid pk
slug                text unique          -- from title, editable, stable once published
title               text
year                int
short_description   text                 -- card / list blurb
description         text                 -- markdown, long form
website_url         text null
problem             text null            -- markdown
solution            text null            -- markdown
what_we_delivered   text null            -- markdown
proof               text null            -- metrics/results, markdown, optional
client_name         text null
is_published        bool
published_at        timestamptz null
seo_title           text null
seo_description     text null
+ site visibility block
+ timestamps / soft delete
```

`project_images`

```
id            uuid pk
project_id    uuid fk
cloudinary_id text
url           text
alt_text      text          -- required
width, height int
is_primary    bool          -- exactly one per project
sort_order    int
```

Single primary enforced by a partial unique index:
`create unique index on project_images (project_id) where is_primary;`

`project_tags` — join `(project_id, tag_id)`, PK on both columns.

## articles

No dedicated page. All articles render on one list page; "Read Article" links out to Medium.

```
id                uuid pk
title             text
excerpt           text                 -- the "few lines"
published_date    date
medium_url        text                 -- external, required
cover_image_id    text null            -- Cloudinary public_id
slug              text unique          -- internal linking, optional
is_published      bool
+ site visibility block
+ timestamps / soft delete
```

`article_tags` — join `(article_id, tag_id)`.

## tags

One table for both project categories and technologies.

```
id                  uuid pk
name                text
slug                text unique
is_technology       bool
technology_category tech_category null   -- required when is_technology = true
icon_cloudinary_id  text null            -- required when is_technology = true
icon_url            text null
color_hex           text null            -- optional chip colour on the frontend
sort_order          int
```

`tech_category` enum:

```
frontend | backend | language | database | tool_or_platform |
cloud_devops | ai_ml_dl | agentic_ai | design | other
```

Validation: `is_technology = false` ⇒ `technology_category` and icon must be null.
Technology tags and category tags are distinguished by `is_technology`, not by a separate
column — group them at read time.

## services

```
id                  uuid pk
slug                text unique
name                text
short_description   text                 -- card text
description         text                 -- markdown, service page body
icon_name           text null            -- e.g. Lucide icon key
icon_cloudinary_id  text null            -- or an uploaded SVG/PNG
hero_image_id       text null
is_published        bool
published_at        timestamptz null
+ site visibility block   -- agency-only in practice, keep the shape uniform
+ timestamps / soft delete
```

`service_features` — `id, service_id fk, title, description null, icon_name null, sort_order`

## pricing_plans

Per-service tiers and general combo packs share **one table**. `service_id = null` means combo
pack. They render as the same card, need the same feature list, the same home-page flag, and
the same admin form — splitting them duplicates all of it.

```
id                uuid pk
service_id        uuid null fk services   -- NULL = general/combo package
name              text                    -- "Starter", "Growth", "Landing Page Combo"
tagline           text null
price_amount      numeric(12,2) null      -- NULL = "Custom / Contact us"
currency          char(3)                 -- 'LKR', 'USD'
price_type        price_type              -- fixed | starting_from | hourly | monthly | custom
delivery_days     int null
delivery_text     text null               -- for "2–3 weeks"
description       text
is_popular        bool                    -- the highlighted middle card
cta_label         text null
cta_url           text null
is_published      bool
sort_order        int
+ site visibility block
```

`pricing_plan_features` — `id, pricing_plan_id fk, text, is_included bool, sort_order`

`is_included = false` renders a greyed-out row in the comparison table.

Query `where service_id = @id` for a service page, `where service_id is null` for combo packs.
If a combo pack ever needs to span several services, add `pricing_plan_services
(pricing_plan_id, service_id)` and keep `service_id` as the primary owner.

## faqs

```
id, question, answer (markdown), category text null, sort_order,
is_published, + site visibility block, timestamps
```

`category` groups FAQs on the page ("Pricing", "Process", "Technical").

## users

```
id                uuid pk
email             citext unique
full_name         text
password_hash     text null        -- null for Google-only accounts
google_subject_id text null unique
avatar_url        text null
role              user_role         -- super_admin | admin
status            user_status       -- pending | approved | rejected | disabled
approved_by       uuid null fk users
approved_at       timestamptz null
rejection_reason  text null
last_login_at     timestamptz null
+ timestamps
```

Behaviour rules live in `.claude/rules/auth.md`.

## Seeding

Idempotent seeder ships the super admin (from app settings) and the technology tag set.
