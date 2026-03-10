# PHASE32 Scope Guard

This phase is limited to lightweight summary cards above existing Identity User Management grid.

## Explicitly allowed
- Add summary cards row above User Management filters and grid
- Add explicit Identity summary endpoint for operational user counts
- Enforce PlatformAdministrator vs SchoolAdministrator scope on backend summary data
- Add click-to-filter mapping from summary cards to existing grid filters
- Add i18n labels/messages for summary cards in cs/en/sk/de/pl
- Add concise governance documentation for summary behavior and boundaries

## Explicitly forbidden
- Analytics engine
- Reporting engine
- BI dashboard
- Charts or graph libraries
- Generic dashboard framework
- New business module
- Tests, quizzes, assessments, exams, automated grading
- University model

## Service boundaries
- ASP.NET Core Identity + OpenIddict remain unchanged
- React remains the single frontend
- Razor remains host bridge only
- PostgreSQL remains primary database
- Redis remains supporting technology only
