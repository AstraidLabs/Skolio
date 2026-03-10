# Phase 33 — User Management School Context Switcher

## Why switcher exists only for PlatformAdministrator
- PlatformAdministrator can manage both global user scope and school-scoped user scope within Identity User Management.
- SchoolAdministrator is already constrained to assigned school scope and must not switch to different schools.
- The switcher is therefore rendered only for PlatformAdministrator and is not rendered (not even disabled) for other roles.

## How switcher works in User Management
- A page-level school context switcher is placed in User Management content area under header and above summary cards.
- Switcher offers `All schools` and concrete schools returned by Identity admin lookup endpoint.
- Selected value is passed as `schoolContextId` query parameter to User Management summary, list, detail and action endpoints.
- Selection is persisted only for this page via localStorage key `skolio.identity.userManagement.schoolContextId`.

## Impact on summary cards, grid and detail
- Summary cards are recalculated from backend for active context (`All schools` or selected school).
- Grid, filtering, sorting and paging remain server-side and run in selected context.
- User detail and role/lifecycle actions are executed in the same context from which admin opened detail.

## Why SchoolAdministrator does not see switcher
- SchoolAdministrator scope remains fixed by backend authorization and school assignments.
- Even if `schoolContextId` is sent manually, backend validates context against actor school scope and forbids unauthorized scope.

## Why this is page-level, not global tenant switcher
- Context affects only `Identity/User Management` endpoint calls.
- No shell-level menu, no global app context, no new tenant framework and no parallel multitenancy model are introduced.

## Out of scope (explicit)
- Global tenant switcher.
- New multitenancy framework or app-wide school context engine.
- New business module.
- Tests, quizzes, assessments, exams, automated grading, university model.
