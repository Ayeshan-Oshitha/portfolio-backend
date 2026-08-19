---
paths:
  - "**/Program.cs"
  - "**/*.csproj"
  - "**/host.json"
  - "**/local.settings.json"
  - "**/local.settings.example.json"
  - ".github/workflows/**"
---

# Azure Functions hosting & startup

Isolated worker model. One `Program.cs` for all DI wiring.

## DbContext

Register with `AddDbContextPool` and Npgsql's `EnableRetryOnFailure`. Use Neon's **pooled**
connection string — Functions scale out and a direct endpoint will exhaust connections.

Keep `MaxPoolSize` small (5–10) per instance and set `Timeout` / `CommandTimeout` explicitly
rather than relying on defaults.

## Cold start

Neon serverless compute suspends when idle, so the first request after a pause takes roughly a
second. That is expected for a portfolio site — do not treat it as a bug or add a keep-alive
timer to work around it.

## Migrations

Run `dotnet ef database update` **from CI**, never automatically on function startup.
Concurrent instances starting at once will race each other.

## Auth level

Every HTTP trigger uses `AuthorizationLevel.Anonymous`. Auth is enforced in middleware — see
`.claude/rules/auth.md`. Function keys are not an auth system.

## Endpoint granularity

One function per endpoint, not a single router function. It keeps the portal's per-function
monitoring and cold-start metrics readable.

## Config

All settings come from app settings / Key Vault. `local.settings.json` stays out of git; keep a
`local.settings.example.json` with placeholder values checked in instead.
