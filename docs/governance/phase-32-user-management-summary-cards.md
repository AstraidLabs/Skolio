# Phase 32 — User Management Summary Cards

## Technical decision
- Summary cards are added directly above existing User Management filters and grid in the Identity frontend page.
- A dedicated Identity endpoint `GET /api/identity/user-management/summary` provides lightweight scope-aware counts.
- Summary cards are operational overview only; they are not analytics, reporting, or BI.
- Card click behavior applies existing grid filters (conservative variant) without navigation to any new screen.

## Metrics
The summary row exposes these backend-driven counts:
- Total users
- Active users
- Locked users
- Deactivated users
- Pending activation users
- Users with MFA

## Scope enforcement
- `PlatformAdministrator` receives global platform counts.
- `SchoolAdministrator` receives counts strictly for users inside assigned school scope.
- Scope is enforced in backend query construction before counting; frontend does not hide global data post-factum.

## Click-to-filter behavior
- Total users card resets status-related filters.
- Active card applies `accountStatus=Active` + `blockStatus=clear`.
- Locked card applies `blockStatus=locked`.
- Deactivated card applies `accountStatus=Deactivated`.
- Pending activation card applies `activationStatus=pending`.
- MFA card applies `mfaStatus=enabled`.

## Error and resiliency behavior
- Summary loading/error is isolated from the grid.
- If summary endpoint fails, User Management grid stays fully usable.
- Summary values are always backend-driven and never hardcoded.

## Boundary statement
This phase explicitly does **not** introduce:
- analytics engine
- reporting engine
- BI dashboard
- charts or graph libraries
- new business module
- tests/quizzes/assessment/exams/automated grading
- university model
