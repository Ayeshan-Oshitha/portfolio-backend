# Roadmap

Planned additions, **not** current state. Nothing here is implemented — don't build from this
file unless asked. `CLAUDE.md` and `.claude/rules/` describe what the code actually does.

Last reviewed: 2026-08-21.

## Known gaps

### Public contact endpoint

`.claude/rules/auth.md` mentions a public contact endpoint in its rate-limiting section, but no
route is specified anywhere and none exists. It would need its own rate limiting — the
`login_attempts` table and `ILoginRateLimiter` are the obvious thing to generalise, since they
already do per-IP windowing in Postgres.

### API documentation for the frontend teams

The `Worker.Extensions.OpenApi` package was referenced but never wired to anything, so it was
removed. Three frontends consume this API and currently have no machine-readable contract. If
that becomes a problem, add the package back along with the attributes — the value is in the
attributes, not the reference.

### Hard delete

Soft delete is everywhere; `.claude/rules/schema.md` calls for hard delete via a
super-admin-only endpoint, and only `project_images` hard-deletes today. A general one would
need to destroy the Cloudinary assets behind whatever it removes, after the row commits.

### Combo packs spanning several services

`pricing_plans.service_id = null` means combo pack. If a combo ever needs to name several
services, `.claude/rules/schema.md` suggests a `pricing_plan_services` join table with
`service_id` kept as the primary owner.

### Schema doc drift

`.claude/rules/schema.md` does not document `refresh_tokens` (which auth.md requires and the
code has) or `login_attempts` (added for login rate limiting). Worth folding in next time that
file is touched.

### Observability

`ILogger` is used in the exception middleware, the health check, the seeder and `MediaService`.
The service layer's failure paths are still silent, so a rejected write leaves no trace beyond
the response the caller got.
