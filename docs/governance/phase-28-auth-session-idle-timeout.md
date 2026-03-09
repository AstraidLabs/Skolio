# Phase 28: Auth Session Idle Timeout (30 minutes)

## Decision
Idle timeout is implemented as an extension of the existing Identity + Frontend session flow.
The user is automatically logged out after 30 minutes of inactivity, with a warning 2 minutes before timeout.

## Idle timeout definition
- Timeout window: 30 minutes of inactivity.
- It is idle timeout, not absolute session lifetime.
- Relevant activity resets idle timer.
- Automatic background traffic does not keep session alive by itself.

## Activity model
The frontend idle tracker treats the following as activity:
- Click
- Key down
- Scroll
- Pointer down / touch start
- Route navigation in SPA shell

Cross-tab behavior is coordinated through browser storage so activity/logout state is shared in the same browser context.

## Warning before logout
- Warning is shown in the last 2 minutes before timeout.
- User can continue the session (timer reset) or log out immediately.
- Warning is localized via existing i18n model.

## Auto logout behavior
- On timeout, frontend performs normal logout flow (`/connect/logout`) with `logoutReason=idle`.
- Local auth session is cleared before redirect.
- Login page shows localized info that logout happened due to inactivity.

## Backend/session coordination
- Existing logout endpoint remains the authority for session end.
- Logout reason is accepted as optional query parameter and audited.
- Audit differentiates `idle-timeout` logout from `user-initiated` logout.
- No new auth framework or parallel session engine was introduced.

## Remember me and trusted device
- Remember me remains persistence convenience only and does not bypass idle timeout.
- Trusted device remains MFA convenience for future sign-ins and does not keep active session alive.

## Out of scope
- No new auth framework
- No new generic session engine
- No second frontend
- No business module changes
- No tests/quizzes/assessment/exams/automated grading/university model
