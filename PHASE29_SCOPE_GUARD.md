# PHASE29 Scope Guard

This phase is limited to login page UI redesign and minimal remember-me wiring.

## Explicitly allowed
- Redesign existing login card UI in `Skolio.Frontend`
- Move language switcher under login form
- Add `Remember me` checkbox and wire it to existing login flow
- Add lightweight login icons and subtle motion
- Extend login i18n texts (`cs/en/sk/de/pl`)

## Explicitly forbidden
- New auth framework
- New business module
- Marketing landing-page redesign
- Second frontend
- Rewriting full auth backend
- Tests/quizzes/assessment/exams/automated grading/university model

## Service boundaries
- React remains single frontend
- Razor remains host bridge only
- Existing ASP.NET Identity + OpenIddict auth stack remains
- No business logic moved into WebHost
