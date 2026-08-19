---
paths:
  - "**/*Cloudinary*.cs"
  - "**/*Media*.cs"
  - "**/*Image*.cs"
  - "**/*Upload*.cs"
---

# Media (Cloudinary)

Image bytes never touch the API. The client uploads straight to Cloudinary using a signature
the API generates.

```
Admin SPA → POST /api/admin/media/signature { folder, publicId? }
          ← { signature, timestamp, apiKey, cloudName, folder }
Admin SPA → POST direct to Cloudinary
          ← { public_id, secure_url, width, height }
Admin SPA → POST /api/admin/projects/{id}/images { publicId, width, height, altText }
```

The signature endpoint requires a valid admin JWT — an unsigned-upload preset would let anyone
fill the account.

## What the DB stores

`cloudinary_id` (public_id), `url`, `width`, `height`, `alt_text`. Never per-size URLs — the
frontends build responsive variants from the `public_id` with transformations like
`f_auto,q_auto,w_800`.

`alt_text` is required. Reject the request if it's missing or blank.

## Folder convention

```
portfolio/projects/{slug}/
portfolio/services/
portfolio/tags/
portfolio/articles/
```

## Deletes

Soft-deleting a row does not delete the Cloudinary asset. Hard delete (super-admin only) should
also destroy the asset by `public_id`, and must tolerate the asset already being gone.
