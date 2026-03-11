# Phase 36 — User Management Admin Edit and Configuration

## Boundary
- User Management is a functional admin editor, not read-only detail.
- Access is limited to `PlatformAdministrator` and `SchoolAdministrator`.
- `Teacher`, `Parent`, `Student` have no access to this admin editor.

## Editable tabs and save model
- **Základní údaje**: editable first name, last name, display name, preferred language, phone, contact email; saved by `PUT /api/identity/user-management/users/{userId}/basic-profile`.
- **Role**: editable role set using Identity role manager; saved by `PUT /api/identity/user-management/users/{userId}/roles`.
- **Stav účtu**: explicit lifecycle actions with confirmations (`activate`, `deactivate`, `reactivate`, `block`, `unblock`, `resend-activation`).
- **Security**: explicit admin security actions (`disable-mfa`, `unlock-lockout`) and read-only status summary.
- **Školní kontext**: editable school assignment list via `PUT /api/identity/user-management/users/{userId}/school-context`.
- **Vazby**: editable parent links via `PUT /api/identity/user-management/users/{userId}/links/parent-students` for parent-role users.

## Admin scope governance
- `PlatformAdministrator` can edit globally including global role governance.
- `SchoolAdministrator` can edit only users inside their school scope and cannot modify global governance outside assigned schools.
- Scope is backend-enforced for every mutation endpoint.

## Audit
- Audit events are emitted for basic profile changes, role updates, lifecycle transitions, school context changes, link updates, invite resend, and security actions.
- Sensitive security values are not logged in raw form.

## Read-only areas
- Username and email remain in identity/security flow and are read-only in basic tab.
- Security tab does not expose sensitive tokens or recovery code values.

## Scope and boundary guard
This phase does not introduce:
- HR module
- CRM module
- generic admin configuration engine
- generic workflow/mutation engine
- new business module
- university model, credits, semesters, enrollment workflows
- tests, quizzes, assessment, exams, grading engines
