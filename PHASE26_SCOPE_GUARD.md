# PHASE 26 SCOPE GUARD

Allowed:
- Extend existing Identity login flow with MFA second step.
- Keep ASP.NET Identity + OpenIddict + OIDC/OAuth2/JWT/JWKS stack unchanged.
- Add server-side short-lived MFA challenge state tied to login attempt.
- Add TOTP and recovery-code verification in existing identity boundary.
- Update existing frontend login page to support MFA challenge step.

Not allowed:
- New auth framework.
- New identity provider.
- SMS MFA.
- External identity providers.
- Hardware token module.
- Trusted device / remember-device engine.
- New business module.
- New standalone frontend/auth app.
- Tests/quizzes/assessment/exams/automated grading.
- University model.
