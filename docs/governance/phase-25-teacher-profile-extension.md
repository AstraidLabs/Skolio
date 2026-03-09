# Phase 25 - Teacher Profile extension inside Identity/Profile flow

## Architectural decision
- Teacher Profile is implemented as a targeted extension of existing Identity/Profile flow (`/api/identity/user-profiles` + existing frontend identity profile page).
- No new standalone profile route, no new teacher module, no service split changes.

## Added profile data (Identity ownership)
- `positionTitle` remains school-position dropdown code validated against school context lookup.
- Added teacher-scoped profile fields in `UserProfile`:
  - `teacherRoleLabel`
  - `qualificationSummary`
  - `schoolContextSummary`
- Existing contact/address fields are reused for Teacher Profile address/contact tab.

## Backend extension scope
- `Skolio.Identity.Api`
- `Skolio.Identity.Application`
- `Skolio.Identity.Domain`
- `Skolio.Identity.Infrastructure`

No ownership move from Organization/Academics to Identity was introduced.

## Migration
- Added additive migration:
  - `20260309170000_AddTeacherProfileSections`
- New nullable columns:
  - `teacher_role_label`
  - `qualification_summary`
  - `school_context_summary`

## Frontend extension
- Existing `IdentityParityPage` is extended with teacher-scoped tabs:
  - Basic data
  - Address and contact
  - Employment placement
  - School context
  - Teaching assignments
- No new frontend route.
- Employment position uses dropdown (`SchoolPositionField`) for teacher/school-admin context, no textbox fallback for those profiles.

## Role boundaries
- `PlatformAdministrator`: broad admin edit including teacher profile fields in existing admin edit section.
- `SchoolAdministrator`: school-scoped administrative edit; school-position constrained by target profile school context.
- `Teacher`: self profile view/edit in permitted profile fields; teaching assignments remain read-only in profile.
- `Parent` and `Student`: no teacher-profile administrative editing path.

## Service ownership boundary
- Organization remains authoritative for teacher assignments.
- Academics remains authoritative for timetable, lesson records, attendance, grades, homework, daily reports.
- Identity profile shows teacher-scoped read model and context summaries only.

## Explicit out-of-scope guard
This phase does not introduce:
- new teacher module outside Identity/Profile flow
- new parallel profile page
- HR module
- payroll logic
- tests/quizzes/assessment/exams/automated grading
- university model
