# Roadmap — not yet built

Human-facing notes. Claude does **not** load this file automatically and should not implement
from it unless asked directly.

## Gaps worth filling

1. **Contact / lead capture.** Both sites will have a contact form.
   `contact_submissions` (name, email, phone, service_id null, message, source_site, status,
   created_at) plus an email notification. Without it, enquiries have nowhere to go.

2. **Testimonials.** Agency sites live on these.
   `testimonials` (quote, author_name, author_role, company, avatar_id, project_id null, rating,
   site flags, sort_order).

3. **Site settings / singleton content.** Hero headline, about text, CV/resume PDF link, social
   links, contact email, meta defaults, "currently available for work" toggle — per site.
   A `site_settings` table keyed by `site` beats hardcoding copy in React.

4. **Preview tokens.** Draft/published is already in the schema. Add a `preview_token` if you
   want to share an unpublished project page with a client before it goes live.

5. **OG images** per project, article, and service. SEO title/description already exist on
   projects — extend to articles and services.

6. **Audit log.** `audit_log` (user_id, entity, entity_id, action, changes jsonb, created_at).
   With several approved admins this becomes the answer to "who deleted that project".

7. **Case-study extras on projects.** `role` (what you did), `team_size`, `duration`, and
   `results` (metric/value pairs). These are what make a project page persuasive rather than
   merely descriptive.

8. **Personal-portfolio-only content.** Work experience timeline, education, certifications,
   skills with proficiency. None of these fit into `projects`.

9. **Related projects.** Either a `project_relations` table or derive from shared tags.

10. **Rate limiting** on the public contact endpoint and the login endpoint.

## Repos (planned)

- `portfolio-cms-api` — this repo (Azure Functions)
- `agency-portfolio-web` — React
- `personal-portfolio-web` — React
- `portfolio-cms-admin` — React
