# Phase 26 — Identity Account Lifecycle, Security Self-Service and User Management

## Built-in ASP.NET Core Identity capabilities used directly
- Password reset tokens (`GeneratePasswordResetTokenAsync`, `ResetPasswordAsync`).
- Email confirmation and activation tokens (`GenerateEmailConfirmationTokenAsync`, `ConfirmEmailAsync`).
- Password change (`ChangePasswordAsync`).
- Email change flow (`GenerateChangeEmailTokenAsync`, `ChangeEmailAsync`).
- Lockout (`CheckPasswordSignInAsync(..., lockoutOnFailure: true)`, lockout options).
- MFA/TOTP + recovery codes (`ResetAuthenticatorKeyAsync`, authenticator token provider, recovery code generation).
- Role management (`UserManager` + `RoleManager`).

## Custom extension over Identity (Skolio lifecycle + governance)
- Account lifecycle status (`PendingActivation`, `Active`, `Deactivated`, `Locked`, `ReactivationPending`) persisted on identity user.
- Admin lifecycle operations (activate/deactivate/reactivate/block/unblock/resend activation) with role and school-scope validation.
- Lifecycle metadata: activation/deactivation/block reason and actor, last login/activity, inactivity warning timestamp.
- Admin user-management boundary split for `PlatformAdministrator` and `SchoolAdministrator`.
- Identity/security e-mail delivery through internal `Skolio.EmailGateway.Api` only.

## Flows
- Forgot password: anonymous request with non-enumerating response; reset token issued by Identity; completion through `ResetPasswordAsync`.
- Activation: token email confirmation; pending accounts remain blocked from login until activation.
- Reactivation: admin-driven transition from `Deactivated` to `Active`.
- Manual deactivation: admin action with mandatory reason; account remains retained.
- Lockout vs block: brute-force lockout is Identity built-in; admin block uses lifecycle metadata and explicit unblock action.
- MFA management: enable/disable (reauth required for disable), recovery code regeneration and audit notifications.
- Role management: controlled role-set updates via Identity managers with backend scope validation.

## User-management scope
- `PlatformAdministrator`: platform-wide read/manage of users and roles.
- `SchoolAdministrator`: school-scoped user visibility and management only.
- `Teacher`, `Parent`, `Student`: no user-management or role-management permissions.

## Email Gateway integration
- Every identity/security delivery (activation, reset, password changed, email change, MFA changed, block/unblock, deactivate/reactivate) is sent through `Skolio.EmailGateway.Api` client abstraction.
- `Skolio.Identity.Api` does not contain SMTP/MailKit integration.

## Explicit out-of-scope guard
This phase does **not** introduce:
- custom password reset engine
- custom MFA engine
- custom lockout engine parallel to Identity
- external identity providers
- SMS MFA
- self-registration
- generic account center outside Identity boundary
- HR or CRM modules
- university model, credits, semesters, subject enrollment
- tests/quizzes/assessment/exams/automated grading
