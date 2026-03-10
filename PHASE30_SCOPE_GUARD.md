# PHASE30 Scope Guard

This phase is limited to administrative User Management UI and explicit Identity user-management API support.

## Explicitly allowed
- Identity-boundary admin user grid (filtering, sorting, paging, page size)
- Identity-boundary user detail/edit view reached from `Edit`
- Lifecycle quick actions: activate, deactivate, block, unblock, resend activation
- Role management in user detail using ASP.NET Core Identity `UserManager` and `RoleManager`
- Explicit Identity API endpoints for role assign/remove/update role set
- Backend validation and scope enforcement for PlatformAdministrator vs SchoolAdministrator
- i18n coverage for new user-management labels/messages in cs/en/sk/de/pl
- Brief phase documentation for user-management behavior and scope model

## Explicitly forbidden
- New business module
- HR module
- CRM module
- Generic admin framework
- Generic custom role engine
- Heavy enterprise grid framework without strong reason
- Standard physical delete action in user grid
- Second frontend
- Business logic moved to WebHost
- Tests/quizzes/assessment/exams/automated grading/university model

## Service boundaries
- ASP.NET Core Identity + OpenIddict remain unchanged as identity base
- React remains the only frontend
- Razor remains host bridge only
- PostgreSQL remains primary database
- Redis remains helper technology only
