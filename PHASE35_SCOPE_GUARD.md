# PHASE35 Scope Guard

This phase is limited to conservative server-side user search in existing Identity User Management.

## Explicitly allowed
- Add one central search input in existing User Management filter bar.
- Extend existing admin user list endpoint with explicit `search` query parameter.
- Implement PostgreSQL-friendly case-insensitive search over basic user text fields.
- Keep scope enforcement for PlatformAdministrator/SchoolAdministrator with school context switcher.
- Keep pagination, sorting, summary cards and filters consistent when search is active.
- Add concise governance documentation for search behavior and boundaries.

## Explicitly forbidden
- New search engine.
- Elasticsearch-like module.
- New indexing subsystem outside PostgreSQL.
- Fuzzy AI search.
- Suggestion/autocomplete engine.
- Analytics engine.
- Reporting engine.
- New business module.

## Service boundaries
- ASP.NET Core Identity + OpenIddict remain unchanged.
- React remains the single frontend.
- Razor remains host bridge only.
- PostgreSQL remains primary database.
- Redis remains supporting technology only.
