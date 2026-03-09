# PHASE28 Scope Guard

This phase is limited to idle-timeout extension of existing auth/session behavior.

## Explicitly allowed
- Add 30-minute inactivity timeout in existing frontend auth/session flow
- Add warning UI before idle logout in existing shell
- Extend existing logout flow with explicit idle reason propagation
- Add i18n keys for idle warning and idle logout info
- Add audit distinction between idle-timeout logout and manual logout

## Explicitly forbidden
- New auth framework
- New generic session engine
- New business module
- Second frontend
- Rewriting entire login/auth architecture
- Tests/quizzes/assessment/exams/automated grading/university model

## Service boundaries
- ASP.NET Identity + OpenIddict + OAuth2/OIDC + JWT + JWKS stay unchanged
- React remains the only frontend
- Razor remains host bridge only
- No business logic moved into WebHost
