# Phase 31 — User Management Tabs, Iconography and Visual Hierarchy

## Technical decision
- The existing admin User Management grid remains the single list entry point.
- The existing user detail reached via `Edit` is extended to a tabbed layout; no parallel screen was introduced.
- Iconography is implemented as internal inline SVG wrappers in the existing React page to avoid introducing any heavy framework.
- Server-side filtering, sorting, paging and page size remain unchanged and continue to be enforced by existing Identity endpoints.

## Grid behavior
- Grid keeps server-side filtering by: name, email/username, role, account status, activation status, block/lock status, MFA status, school, school type, inactivity state.
- Grid keeps server-side sorting by: name, email, created at, last login, account status, school.
- Grid keeps page sizes: 10, 20, 50, 100.
- Lifecycle quick actions in grid now use icon + text pattern for: Activate, Deactivate, Block, Unblock, Resend activation, Edit.
- Status is displayed with a badge tone and remains explicitly labeled.

## User detail tabbed layout
`Edit` now opens user detail with top summary + tabs:
- `Základní údaje`
  - Name, surname, display name, email, username and core read model identity fields.
- `Role`
  - Current role set with update action and explicit confirmation before saving.
- `Stav účtu`
  - Lifecycle status, activation state, last login and last activity.
- `Security`
  - Email confirmed, MFA enabled, lockout summary and safe recovery code summary (never full codes).
- `Školní kontext`
  - School, school type, assigned school ids and scope hint.
- `Vazby`
  - Role-aware relationship summaries for parent/teacher/student links.

## API additions for explicit tab read models
Identity API was extended with explicit per-tab read endpoints:
- `GET /api/identity/user-management/users/{userId}/roles-detail`
- `GET /api/identity/user-management/users/{userId}/lifecycle-detail`
- `GET /api/identity/user-management/users/{userId}/security-detail`
- `GET /api/identity/user-management/users/{userId}/school-context-detail`
- `GET /api/identity/user-management/users/{userId}/links-summary`

All endpoints remain within Identity boundary and enforce the same scope checks (`PlatformAdministrator`, `SchoolAdministrator`).

## Scope model
- `PlatformAdministrator`
  - Can manage users globally and can manage `PlatformAdministrator` role assignment.
- `SchoolAdministrator`
  - Can manage users only in assigned school scope; cannot elevate to `PlatformAdministrator`.

## Why full editing is split into tabs
- Tabs reduce cognitive load versus a single long form.
- Tabs preserve explicit boundaries between lifecycle, security, role management and school context.
- Tabs keep the admin flow consistent: grid summary first, scoped detail second.

## Scope and boundary guard for this phase
This phase does **not** introduce:
- HR module
- CRM module
- Generic admin framework
- Heavy enterprise grid/tabs framework
- Tests, quizzes, assessments, exams, automated grading
- University model
