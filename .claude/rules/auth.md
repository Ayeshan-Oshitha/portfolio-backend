---
paths:
  - "**/Auth/**"
  - "**/*Auth*.cs"
  - "**/*Jwt*.cs"
  - "**/*Token*.cs"
  - "**/*User*.cs"
  - "**/Middleware/**"
---

# Auth

Both public sites are fully anonymous. Only `/api/admin/*` (the CMS) is protected.

## Password login

Verify Argon2id hash → check `status = approved` → issue an access JWT (15 min) plus a refresh
token (rotating, stored **hashed** in the DB, 30 days).

## Google sign-in

React sends the Google ID token → API validates it against Google's JWKS and checks `aud`
matches the configured client ID → match the user by `google_subject_id`, falling back to the
verified email → issue the same JWT pair.

Google sign-in for an email with no user row creates a **`pending`** user. Never auto-approve.

## Tokens

Claims: `sub`, `email`, `role`, `jti`. Validate on every `/api/admin/*` call in a **Functions
middleware**, not per-function.

Function-level `AuthorizationLevel` is set to `Anonymous` everywhere; function keys are not an
auth system. Authorization is the middleware's job.

## User states

`status`: `pending | approved | rejected | disabled`.

Registration is open but powerless. A non-approved user is **rejected at token issue** with
`403` and code `account_pending` — do not issue a scopeless token. One place to get it wrong is
better than two.

## Super admin rules

- Exactly one `super_admin`, seeded on first deploy from app settings. Guard against creating a
  second one.
- Only `super_admin` may approve, reject, disable, or change roles.
- A `super_admin` cannot disable or demote themselves.

## Secrets

`Jwt__Signer`, `Google__ClientId`, Cloudinary keys, `ConnectionStrings__Default` live in app
settings / Key Vault. Never in a committed `local.settings.json`.

## Rate limiting

The login endpoint and (when it exists) the public contact endpoint need rate limiting —
per IP, and per email on login.
