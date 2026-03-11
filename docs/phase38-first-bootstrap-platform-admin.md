# Phase 38 — First bootstrap onboarding for PlatformAdministrator

## Technical decisions
1. Bootstrap flow is explicitly controlled by `BOOTSTRAP_PLATFORM_ADMIN_ENABLED=true` (or `Identity:Bootstrap:PlatformAdminEnabled=true`), and is closed by default.
2. Bootstrap availability is allowed only when no active `PlatformAdministrator` exists (`PlatformAdministrator` role + active lifecycle + confirmed email).
3. First admin creation is isolated under explicit endpoints in `Skolio.Identity.Api` (`/api/identity/bootstrap/*`) and does not reuse public signup or invite flow.
4. Bootstrap enforces MFA setup with ASP.NET Core Identity authenticator TOTP + recovery codes before activation/login states can progress.
5. Bootstrap lifecycle closes only after first successful login of the bootstrap admin; closure is audited and bootstrap endpoint stays blocked unless ops explicitly re-enables and no active admin exists.

## State model
- `BootstrapAvailable`
- `BootstrapAccountCreated`
- `PendingActivation`
- `PendingFirstLogin`
- `Active`
- `BootstrapClosed`

## First deployment flow
1. Deploy with `BOOTSTRAP_PLATFORM_ADMIN_ENABLED=true`.
2. Open React bootstrap page `/bootstrap/platform-admin`.
3. Create first platform admin (username, email, password, confirm password).
4. Complete required MFA setup and save recovery codes.
5. Confirm account via normal activation email delivered through `Skolio.EmailGateway.Api`.
6. Perform first successful login.
7. Disable bootstrap flag in runtime config/environment.

## Why no invite code
- Bootstrap is single-purpose first-deployment onboarding.
- Invite code is not needed because bootstrap has strict guard (flag + no active platform admin) and one-account lifecycle.
- This keeps production operations simpler and avoids another secret distribution step.

## Scope guard for this phase
This phase does **not** introduce:
- self-registration
- public signup
- invite code for bootstrap admin flow
- fixed default admin account/password in seed
- permanently open bootstrap endpoint
- new business module
- tests
- quizzes
- assessment
- exams
- automated grading
- university model

## Out of scope
- SMS MFA
- external identity providers
- custom MFA engine
- new frontend channel outside React
- business logic in WebHost
