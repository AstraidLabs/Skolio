# SchoolAdministrator Profile extension in Identity/Profile flow

## Scope decision
- This phase extends the existing `Skolio.Identity` profile flow and existing `Skolio.Frontend` profile screen.
- No new standalone profile page, no parallel route, and no new business module were introduced.

## Backend extension
- `UserProfile` in `Skolio.Identity.Domain` was extended with:
  - `AdministrativeWorkDesignation`
  - `AdministrativeOrganizationSummary`
- Existing Identity profile contracts and update commands were extended with the same fields.
- Existing `UserProfilesController` self/admin update flow was extended for:
  - role-aware edit boundaries for SchoolAdministrator/Profile
  - backend validation for added fields
  - existing school-position dropdown validation against school-context lookup
- New EF Core migration added:
  - `20260310110000_AddSchoolAdministratorProfileSections`
  - adds `administrative_work_designation` and `administrative_organization_summary` to `user_profiles`

## Frontend extension
- Existing `IdentityParityPage` profile screen was extended with SchoolAdministrator-scoped tabs inside the same route:
  - Základní údaje
  - Adresa a kontakt
  - Pracovní zařazení
  - Školní kontext
  - Spravované školy
  - Administrativní přehled
- UI remains role-aware and school-administrator-scoped.
- School position remains dropdown-only (`school-position-options` API), no free-text position input was added.

## Ownership boundaries kept
- Authoritative school entities, school years, classes, groups, subjects, and assignments remain in Organization service.
- Feature toggles, settings and audit management remain in Administration service.
- Identity profile keeps only lightweight school-admin-scoped summary/read-model fields.

## Role-based access boundaries
- SchoolAdministrator: self profile edit only in allowed profile fields.
- PlatformAdministrator: broader administrative profile editing in existing admin profile flow.
- Teacher/Parent/Student: do not receive SchoolAdministrator profile editing scope.
- School assignments and roles are not self-editable via profile.

## Explicit non-goals for this phase
- No new school-administrator module outside identity/profile flow.
- No new parallel profile page.
- No HR module, payroll logic, ERP admin cockpit.
- No tests/quizzes/assessment/exams/automated grading/university model.
