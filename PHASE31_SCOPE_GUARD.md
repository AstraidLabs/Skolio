# PHASE31 Scope Guard

This phase is limited to extending existing Identity User Management UI with iconography and tabbed user detail, plus explicit tab read endpoints.

## Explicitly allowed
- Extending existing User Management grid actions with icon + text pattern
- Extending existing user detail into tabbed layout (basic, roles, account state, security, school context, links)
- Improving visual hierarchy using existing primitive components/cards
- Confirm steps for sensitive lifecycle/role actions
- Explicit Identity user-management endpoints for tab read models
- i18n additions for new labels/messages in cs/en/sk/de/pl
- Brief governance documentation for grid → detail tabbed flow and scope model

## Explicitly forbidden
- New business module
- HR module
- CRM module
- Generic admin framework
- Generic role engine
- Heavy enterprise UI framework without strong reason
- Physical delete of user account from grid
- Any tests/quizzes/assessment/exams/automated grading features
- University model

## Service boundaries
- ASP.NET Core Identity + OpenIddict remain unchanged
- React remains the single frontend
- Razor remains host bridge only
- PostgreSQL remains primary database
- Redis remains supporting technology only
