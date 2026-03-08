# Skolio - Phase 19 Profile Boundary

## 1) Profile boundary
- `UserProfile` is business profile data only.
- Technical identity credentials remain in identity account layer.
- `SchoolRoleAssignment`, `ParentStudentLink`, school assignments and teacher assignments remain read-only in profile screens.
- Profile screens do not include role management actions.

## 2) Shared profile rules
- Shared self-edit fields for all roles:
  - `FirstName`
  - `LastName`
  - `PreferredDisplayName`
  - `PreferredLanguage`
  - `PhoneNumber`
- Shared read-only fields:
  - `UserId`
  - `Email` (login identifier)
  - `UserType`
  - account activation state
  - role assignments
  - school assignments
  - parent-student links
  - teacher assignments
- Out of scope for profile:
  - password reset, email change, username change
  - MFA and external login management
  - avatar upload and file storage

## 3) Self vs administrative edit
- `My Profile` endpoint and UI: self-edit in allowed scope only.
- `Administrative Profile Edit` endpoint and UI: admin edits only for authorized scope.
- Teacher/Parent/Student: self edit only.
- `SchoolAdministrator`: admin edit only for users in assigned schools.
- `PlatformAdministrator`: admin edit across platform scope.
- Role assignments remain outside profile edit.

## 4) Role-specific profile constraints
- `PlatformAdministrator` self edit:
  - first/last name, preferred display name, language, phone, position title.
- `SchoolAdministrator` self edit:
  - first/last name, preferred display name, language, phone, position title.
- `Teacher` self edit:
  - first/last name, preferred display name, language, phone, position title, public contact note.
- `Parent` self edit:
  - first/last name, preferred display name, language, phone, preferred contact note.
- `Student` self edit (conservative):
  - preferred display name, preferred language, phone.
  - first/last name is read-only in student-only profile mode.

## 5) School-type behavior
- `Kindergarten`:
  - student self profile remains conservative and secondary.
  - parent profile summary is primary.
- `ElementarySchool`:
  - simple student profile and conservative self-edit.
- `SecondarySchool`:
  - richer student summary than elementary, still without university concepts.

## 6) Backend changes (Skolio.Identity.Api)
- Added profile fields in `UserProfile` domain + persistence:
  - `preferred_display_name`
  - `preferred_language`
  - `phone_number`
  - `position_title`
  - `public_contact_note`
  - `preferred_contact_note`
- Added endpoint:
  - `GET /api/identity/user-profiles/me/summary`
- Updated endpoint boundaries:
  - `PUT /api/identity/user-profiles/me` enforces role-based self-edit field restrictions.
  - `PUT /api/identity/user-profiles/{id}` enforces admin scope and conservative school-admin payload.
- `Email`, `UserType`, role assignments, school assignments and links stay outside editable profile payloads.

## 7) Frontend profile screens
- `IdentityParityPage` now provides:
  - role-aware `My Profile` self edit form
  - read-only assignment/link sections
  - teacher assignment read-only section
  - parent linked-student read-only section
  - dedicated administrative profile edit panel for admin roles
- UI clearly marks read-only business boundaries.

## 8) Profile form standard
- All profile forms use:
  - clear sectioned layout
  - disabled read-only fields
  - explicit save action
  - loading/error/success states

## 9) Audit rules
- Audited actions:
  - `identity.user-profile.self-updated`
  - `identity.user-profile.admin-updated`
  - `identity.user-profile.activated`
  - `identity.user-profile.deactivated`
- Audit payload stores changed field names only, not sensitive raw values.

## 10) Seed and development data
- Seeded accounts have complete `UserProfile` defaults:
  - preferred display name
  - preferred language
  - phone
  - role-relevant position/contact notes where applicable
- Parent/student links remain seeded and profile summaries are usable immediately in local compose.

## 11) Profile exclusions
- No password reset workflow.
- No email/username change workflow.
- No MFA or external providers.
- No avatar upload.
- No role management in profile edit.
- No assessment/exam data in profile.

## 12) Scope guard
Phase 19 does not reintroduce:
- tests, quizzes, assessment, exams, online testing
- question bank, automated grading, scoring
- evaluation workflows, exam workflow, assessment engine
- university model, credits, semesters, subject enrollment
