# Phase 34 — User Management Row Status Styling

## What row styles are used
- `Active` is baseline neutral row style without extra highlighting.
- `Deactivated` and `SoftDeleted` use a muted row background to lower visual weight while preserving readable text and actions.
- `Locked` and `Blocked` use subtle warning treatment (light rose background + left accent) to increase attention without aggressive error signaling.
- `Pending activation` keeps near-neutral row styling; status badge remains the primary state indicator.

## Soft deleted / deactivated differentiation
- Deactivated/soft-deleted rows are intentionally desaturated and visually quieter.
- Text, action buttons and badges remain readable; row is not collapsed and not disabled.
- This styling is distinct from lock/block treatment to prevent state confusion.

## Locked / blocked differentiation
- Locked/blocked rows use a gentle warning tone, not full-danger enterprise error fill.
- Accent is additive and lightweight, keeping full action readability.
- Lock/block action buttons and lifecycle badge remain visible and consistent with row tone.

## Why styling is conservative
- User Management is an operational table, not an alert dashboard.
- Overly strong styles increase noise and reduce scan efficiency.
- Conservative tones preserve focus on lifecycle text status, badges and explicit action labels.

## Why color is not the only carrier
- Lifecycle state remains visible as explicit text status in grid columns.
- Lifecycle state remains visible as status badge tone and label.
- Row background and accent only provide a secondary scan aid and do not replace textual state semantics.

## Scope and boundary guard
This phase must not introduce:
- New status framework.
- Dashboard alert engine.
- Marketing animations.
- Analytics engine.
- Reporting engine.
- New business module.
