# Phase 37 — Admin-Created Invite Activation Onboarding

## What was added
- Admin create-user wizard always creates account in `PendingActivation` and dispatches invite e-mail via `Skolio.EmailGateway.Api`.
- Invite email contains invite link, activation code, and explicit 24-hour validity notice.
- Invite link opens React page `/security/invite-activation`.
- User must confirm invite token + activation code, then set password to complete activation.
- Account is not fully usable before invite confirmation and password setup completion.

## Invite flow
1. Admin creates user.
2. Identity API generates Identity email-confirmation token and random 6-digit activation code.
3. Token and code are stored as SHA-256 hashes with fixed expiration window `24h`.
4. Invite email is sent through EmailGateway template `AccountInvite`.
5. User opens invite page, context is validated.
6. User confirms activation code (rate-limited endpoint).
7. User sets password; backend confirms email using Identity token and resets password using Identity capability.
8. Lifecycle transitions to Active and onboarding completion timestamp is set.

## Lifecycle states
- `PendingActivation`
- `InviteSent`
- `InviteExpired`
- `InviteConfirmed`
- `Active`

## Resend policy
- Available only for non-active accounts and admin-scoped management endpoints.
- Resend is rate-limited by minimum resend window and creates new invite window.
- Old token/code become unusable because hashes are rotated.

## Out of scope (explicit guard)
- Self-registration
- Public signup
- Plain-text password by email
- Custom auth framework
- Generic onboarding engine
- New business module
- Tests/quizzes/assessment/exams/automated grading
- University model
