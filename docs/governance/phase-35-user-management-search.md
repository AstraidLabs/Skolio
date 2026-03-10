# Phase 35 — User Management Central Search

## How search works
- User Management now includes one central search input placed directly in the existing filter bar above the grid.
- Search is sent to backend as explicit `search` query parameter on existing `GET /api/identity/user-management/users` endpoint.
- Search is implemented as conservative server-side PostgreSQL query logic and remains part of the existing Identity User Management list flow.

## Fields covered by search
- Search matches case-insensitively across:
  - `FirstName`
  - `LastName`
  - `PreferredDisplayName` (display name)
  - `Email`
  - `UserName`
  - joined full name (`FirstName + LastName`)
  - `SchoolContextSummary` (school-bound display summary)
- Search does not include audit logs, security tokens, hidden security data, or unrelated internal security stores.

## School scope enforcement
- Scope is enforced in backend before search filtering.
- `PlatformAdministrator`:
  - Global scope when school context is `All schools`.
  - School-scoped search when specific school context is selected.
- `SchoolAdministrator`:
  - Search is always restricted to assigned school scope.
- Frontend never acts as authority for scope; backend scope filtering is authoritative.

## Combination with filters, sorting and paging
- `search` is combined with existing filters (`name`, `emailOrUsername`, lifecycle/status filters, school filters).
- Sorting and page size remain unchanged and continue to work with search results.
- Search submit and search clear reset to `pageNumber = 1` to keep paging coherent.
- Grid empty/loading/error states remain stable with search-specific messages.
- Summary cards remain available and do not introduce separate analytics/search subsystem.

## Why this is not a new search engine
- No new module, no external provider, no dedicated indexing subsystem.
- No search DSL, no autocomplete/suggestion pipeline, no fuzzy AI ranking.
- Search remains simple bounded filtering inside existing Identity admin users endpoint and PostgreSQL query execution.

## Out of scope
This phase does not introduce:
- New search engine.
- Elasticsearch-like infrastructure.
- Reporting module.
- Fuzzy AI search.
- Suggestion engine.
- Analytics engine.
- New business module.
