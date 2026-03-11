# PHASE 38 SCOPE GUARD — FIRST BOOTSTRAP PLATFORM ADMIN

Allowed:
- Explicit bootstrap endpoints in `Skolio.Identity.Api` for first `PlatformAdministrator` onboarding.
- Bootstrap availability guard based on active platform admin existence and explicit enable flag.
- Mandatory MFA setup (TOTP + recovery codes) before bootstrap can progress.
- Standard Identity email activation using `Skolio.EmailGateway.Api`.
- Bootstrap closure only after first successful login.
- React bootstrap setup page with i18n texts.

Forbidden:
- Self-registration.
- Public signup.
- Invite code in bootstrap admin flow.
- Fixed default platform admin account with hardcoded password.
- Persistently open bootstrap endpoint.
- New business module.
- Tests/quizzes/assessment/exams/automated grading/university model.
