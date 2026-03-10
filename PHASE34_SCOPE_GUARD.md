# PHASE34 Scope Guard

This phase is limited to conservative visual row status styling in the existing Identity User Management grid.

## Explicitly allowed
- Add lightweight row styling in User Management list for lifecycle readability.
- Differentiate deactivated/soft-deleted rows with muted, readable styling.
- Differentiate locked/blocked rows with subtle warning accent styling.
- Keep active rows as baseline neutral styling.
- Keep pending activation as near-baseline style with status badge as primary indicator.
- Add concise governance documentation for this visual-only phase.

## Explicitly forbidden
- New status framework.
- Dashboard alert engine.
- Marketing animations.
- Analytics engine.
- Reporting engine.
- New business module.
- Backend lifecycle logic changes.

## Service boundaries
- ASP.NET Core Identity + OpenIddict remain unchanged.
- React remains the single frontend.
- Razor remains host bridge only.
- PostgreSQL remains primary database.
- Redis remains supporting technology only.
