# portfolio-backend

One headless CMS API and one database, powering three frontends: the agency portfolio, the
personal portfolio, and the admin SPA.

.NET 10 Azure Functions (isolated worker) · PostgreSQL on Neon via EF Core + Npgsql ·
Cloudinary for media · custom JWT + Google Sign-In for admin auth.

See [`CLAUDE.md`](CLAUDE.md) for the architecture and the rules that govern changes; the detail
lives in [`.claude/rules/`](.claude/rules).

## Running it locally

Prerequisites: .NET 10 SDK, [Azure Functions Core Tools v4][func], a Neon database (or any
Postgres), a Cloudinary account. Docker is needed only for the tests.

```bash
cp Portfolio.API/local.settings.example.json Portfolio.API/local.settings.json
# then fill in the placeholders — connection string, Jwt__Signer, SuperAdmin__*, Cloudinary__*
```

`local.settings.json` is gitignored and must stay that way. Use Neon's **pooled** connection
string: Functions scale out, and a direct endpoint will exhaust connections.

Apply the schema, then start the host:

```bash
dotnet tool install --global dotnet-ef
ConnectionStrings__Default="<your-connection-string>" dotnet ef database update --project Portfolio.API

cd Portfolio.API && func start
```

The API comes up on `http://localhost:7060`. The super admin account is seeded from
`SuperAdmin__*` on first start.

Quick check:

```bash
curl http://localhost:7060/api/health
curl "http://localhost:7060/api/public/home?site=agency"
```

`?site=` is required on every public endpoint with site visibility — omitting it is a `400`
with code `site_required`, never "return everything".

## API reference

With `Docs__Enabled` set, the browsable reference is at `http://localhost:7060/api/docs` and the
spec it renders at `http://localhost:7060/api/openapi.yaml`. Both return `404` when the setting
is off, which is the default — the spec maps the whole admin surface, so a deployment opts in
rather than out.

The spec is hand-authored at `Portfolio.API/Docs/openapi.yaml` and embedded in the assembly.
Nothing generates it, so **a route change is a spec change**. Validate an edit with:

```bash
npx @redocly/cli lint Portfolio.API/Docs/openapi.yaml
```

## Tests

```bash
dotnet test
```

The tests run against **real Postgres** in a throwaway container (Testcontainers), because the
schema uses native enums, `citext` and a partial unique index that the EF in-memory provider
does not model. Docker must be running.

## Migrations

```bash
dotnet ef migrations add <Name> --project Portfolio.API
```

Migrations are applied **from CI**, never on function startup — concurrent instances would race
each other. CI also fails the build if an entity changed without a matching migration.

## Deployment

`.github/workflows/ci.yml` builds and tests every push and PR.
`.github/workflows/deploy.yml` applies migrations and publishes to Azure on a push to `main`.

Deployment needs these configured on the repository:

| Kind | Name | Notes |
|---|---|---|
| Secret | `NEON_MIGRATION_CONNECTION_STRING` | Neon's **direct** (non-pooled) string — migrations issue DDL |
| Secret | `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` | From the Function App's publish profile |
| Variable | `AZURE_FUNCTIONAPP_NAME` | The Function App's name |

App settings on the Function App mirror `local.settings.example.json` — `ConnectionStrings__Default`
(pooled), `Jwt__Signer`, `Google__ClientId`, `Cloudinary__*`, and `Cors__AllowedOrigins__0..n`
for the three frontend origins.

[func]: https://learn.microsoft.com/azure/azure-functions/functions-run-local
