# Skolio Phase 21 - Self-Service Boundary

## Scope
Phase 21 implements conservative self-service only in approved boundaries:
- `My Profile` self-edit for own business profile fields.
- Own data read access only.
- Parent self-service excuses via own linked students only.
- Preferred language as lightweight profile preference.

## Included Self-Service
- `GET /api/identity/user-profiles/me`
- `PUT /api/identity/user-profiles/me`
- `GET /api/identity/user-profiles/me/summary`
- `GET /api/academics/attendance/my/excuse-requests` (Parent only)
- `POST /api/academics/attendance/my/excuse-requests` (Parent only)
- `PUT /api/academics/attendance/my/excuse-requests/{id}` (Parent only, lifecycle window enforced)
- `DELETE /api/academics/attendance/my/excuse-requests/{id}` (Parent only, lifecycle window enforced)

## Not Included
Self-service does not include:
- Self-registration
- Password reset workflow
- Email change workflow
- Username change workflow
- MFA management
- External identity providers
- Role request workflow
- Role assignment changes
- Parent-student link changes
- School assignment changes
- Account management center

## Role Limits
- `PlatformAdministrator`: own profile self-edit and own context read.
- `SchoolAdministrator`: own profile self-edit and school context read.
- `Teacher`: own profile self-edit and teacher assignment summary read-only.
- `Parent`: own profile self-edit + own excuse lifecycle operations in linked-student scope.
- `Student`: conservative own profile self-edit (`PreferredDisplayName`, `PreferredLanguage`, `PhoneNumber` if present) and own context read-only.

## Audit
Audited actions in existing audit model:
- `identity.user-profile.self-updated`
- `academics.excuse-note.changed` for create/update/cancel self-service parent operations.

## Boundary Guard
Out of scope and explicitly excluded:
- tests, quizzes, assessment, exams, online testing
- question bank, automated grading, scoring
- evaluation workflows, exam workflow, assessment engine
- university model, credits, semesters, subject enrollment
