# PHASE27 Scope Guard

This phase is limited to extending the existing Identity/Profile flow for PlatformAdministrator profiles.

## Explicitly allowed
- Extend existing `Skolio.Identity` profile data model
- Extend existing API contracts and profile endpoints
- Extend existing `Skolio.Frontend` profile UI inside current route
- Add additive EF Core migration for profile columns
- Keep role-aware validation in frontend and backend

## Explicitly forbidden
- New platform-admin module outside identity/profile flow
- New parallel profile page/route
- ERP admin cockpit
- HR module
- Payroll logic
- Moving Organization or Administration data ownership into Identity
- Tests/quizzes/assessment/exams/automated grading/university model

## Service boundaries
- React remains single frontend
- Razor remains host bridge only
- PostgreSQL remains single database
- No business logic added to WebHost
