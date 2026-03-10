# PHASE33 Scope Guard

This phase is limited to adding a page-level school context switcher to Identity User Management.

## Explicitly allowed
- Add User Management school context switcher UI only for PlatformAdministrator.
- Extend Identity User Management endpoints with optional `schoolContextId` parameter.
- Add school lookup endpoint for switcher options.
- Re-scope summary cards, grid, detail and lifecycle/role actions to selected school context.
- Persist last selected User Management school context in page-local persistence.
- Add i18n keys related to school context switcher in cs/en/sk/de/pl.
- Add concise governance documentation for scope and boundary.

## Explicitly forbidden
- Global tenant switcher.
- New multitenancy framework or new tenant engine.
- App-wide school context engine without explicit approval.
- New business module.
- Tests, quizzes, assessments, exams, automated grading.
- University model.

## Service boundaries
- ASP.NET Core Identity + OpenIddict remain unchanged.
- React remains the single frontend.
- Razor remains host bridge only.
- PostgreSQL remains primary database.
- Redis remains supporting technology only.
