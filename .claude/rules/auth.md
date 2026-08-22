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

## Registration and email verification

`POST /admin/auth/register` creates a `email_verification_required` account and emails a
verification link (`{Email:BaseUrl}/verify-email?token=...`, 24h expiry, single-use, hashed in
the DB exactly like a refresh token — see `EmailVerificationToken`/`EmailVerificationTokenGenerator`).
No token is issued at registration.

`POST /admin/auth/verify-email` consumes the token and moves the account to `pending`. Errors:
`invalid_verification_token` (unknown token), `verification_token_already_used`,
`verification_token_expired` — none of these leak whether an email is registered, since the token
itself already proves inbox possession.

`POST /admin/auth/resend-verification` issues a fresh token, invalidating any prior unused one.
It **always returns the same generic response** regardless of whether the email exists, is
already verified, or is rate-limited (3 sends/hour/email) — this endpoint must never be usable to
enumerate accounts.

## Password login

Verify Argon2id hash → check `status = approved` → issue an access JWT (15 min) plus a refresh
token (rotating, stored **hashed** in the DB, 30 days).

## Google sign-in

React sends the Google ID token → API validates it against Google's JWKS and checks `aud`
matches the configured client ID → match the user by `google_subject_id`, falling back to the
verified email → issue the same JWT pair.

Google sign-in for an email with no user row creates a **`pending`** user directly — it skips
`email_verification_required` entirely, since Google has already verified the address. Never
auto-approve.

## Tokens

Claims: `sub`, `email`, `role`, `jti`. Validate on every `/api/admin/*` call in a **Functions
middleware**, not per-function.

Refresh tokens **rotate**: each refresh revokes the presented token and issues a new one. A
revoked token presented again is treated as theft — every live token for that user is revoked and
the call fails with `refresh_token_reused`.

A JWT cannot be recalled once issued, so revocation is what actually bounds access: disable,
reject, delete and change-password all revoke that user's refresh tokens, which caps their
remaining access at one access-token lifetime (15 min). Refresh also re-checks `status`, so a
disabled user cannot renew.

Function-level `AuthorizationLevel` is set to `Anonymous` everywhere; function keys are not an
auth system. Authorization is the middleware's job.

## User states

`status`: `email_verification_required | pending | approved | rejected | disabled`.

Registration is open but powerless. A non-approved user is **rejected at token issue** with
`403` (`NotApproved` in `UserService`) — do not issue a scopeless token. One place to get it
wrong is better than two. Codes: `email_verification_required`, `account_pending`,
`account_rejected`, `account_disabled`.

`ApproveAsync` only accepts a `pending` account (`user_not_pending` otherwise) — defense in depth
so an unverified account can't be approved by a stale admin tab or a direct DB edit.

## Super admin rules

- Exactly one `super_admin`, seeded on first deploy from app settings. Guard against creating a
  second one.
- Only `super_admin` may approve, reject, disable, or change roles.
- A `super_admin` cannot disable or demote themselves.

## Secrets

`Jwt__Signer`, `Google__ClientId`, Cloudinary keys, `ConnectionStrings__Default` live in app
settings / Key Vault. Never in a committed `local.settings.json`.

## Rate limiting

The login endpoint needs rate limiting per IP and per email —
`ILoginRateLimiter`/`LoginRateLimiter`, a Postgres-backed fixed window (in-memory would reset
per instance since Functions scale out).

`POST /api/public/reviews` — the one anonymous public write — uses the same fixed-window idea,
per IP only, but counts rows in the `reviews` table itself rather than a separate attempts
table (every submission is already persisted, unlike a failed login). See
`ReviewService.SubmitAsync`.
