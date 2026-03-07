# Skolio - Phase 18 Frontend Parity

## 1) Frontend parity principle
- Frontend implements only existing backend capabilities.
- Business screens are backed only by real API responses.
- No fake CRUD, no placeholder business actions, no fabricated KPIs.
- Role-aware and school-type-aware visibility is enforced in shell navigation and feature pages.
- Every backend-backed screen uses loading, empty and error states.

## 2) Backend capability inventory

### Skolio.Identity.Api
- Read:
  - `GET /api/identity/user-profiles/me`
  - `GET /api/identity/user-profiles/student-context`
  - `GET /api/identity/user-profiles/linked-students`
  - `GET /api/identity/user-profiles`
  - `GET /api/identity/user-profiles/{id}`
  - `GET /api/identity/school-roles/me`
  - `GET /api/identity/school-roles/student-me`
  - `GET /api/identity/school-roles`
  - `GET /api/identity/parent-student-links/me`
  - `GET /api/identity/parent-student-links`
- Create:
  - `POST /api/identity/school-roles`
  - `POST /api/identity/parent-student-links`
- Update:
  - `PUT /api/identity/user-profiles/me`
  - `PUT /api/identity/user-profiles/{id}`
  - `PUT /api/identity/user-profiles/{id}/activation`
  - `PUT /api/identity/parent-student-links/{id}/override`
- Delete:
  - `DELETE /api/identity/school-roles/{id}`
  - `DELETE /api/identity/parent-student-links/{id}`
- Not implemented in frontend (backend not in approved scope):
  - self-service registration, password recovery, MFA, external IdP management.

### Skolio.Organization.Api
- Read:
  - schools, school detail
  - school years
  - grade levels
  - class rooms
  - teaching groups
  - subjects
  - secondary fields of study
  - teacher assignments
  - student organization context
- Create:
  - school
  - initial school year
  - school year
  - grade level
  - class room
  - teaching group
  - subject
  - secondary field of study
  - teacher assignment
- Update:
  - school
  - school status activation/deactivation
  - school administrator assignment
  - school year
  - grade level
  - secondary field of study
  - class room override
  - teaching group override
  - subject override
  - teacher assignment override/reassign
- Delete/close:
  - explicit delete endpoints are not exposed for organization entities; lifecycle/override model is used.
- School-type scope:
  - Secondary fields only for `SecondarySchool`.
  - Group-oriented emphasis for `Kindergarten`.

### Skolio.Academics.Api
- Read:
  - timetable
  - lesson records
  - attendance records
  - excuse notes
  - grades
  - homework
  - daily reports
- Create:
  - timetable entry
  - lesson record
  - attendance record
  - excuse note
  - grade entry
  - homework
  - daily report
- Update:
  - lesson override
  - attendance override
  - excuse update and override
  - grade override
  - homework override
  - daily report override
- Delete:
  - excuse cancel (`DELETE /attendance/excuse-notes/{id}`)
- Explicitly out of scope:
  - tests, quizzes, assessment, exams, scoring, automated grading, question bank.

### Skolio.Communication.Api
- Read:
  - announcements list/detail
  - conversations list/detail
  - conversation messages
  - notifications list
- Create:
  - school announcement
  - platform announcement
  - conversation message send
- Update:
  - announcement override
  - announcement activation/deactivation
- Delete:
  - explicit delete endpoint for announcements/messages is not exposed.

### Skolio.Administration.Api
- Read:
  - system settings
  - feature toggles
  - audit log
  - school-year lifecycle policies
  - housekeeping policies
  - operational summary
  - parent context
  - teacher context
  - student context
- Update:
  - system setting
  - feature toggle
  - school-year policy
  - housekeeping policy
- Delete:
  - explicit delete endpoints are not exposed for administration entities.
- Out of scope:
  - licensing engine, ERP extensions, analytics/reporting engines.

## 3) Frontend parity implementation
- Identity parity screen:
  - own profile read/update
  - admin user/roles/links lists
  - role assignment create/delete
  - parent-student link create/override/delete
  - parent and student read-only scoped sections.
- Organization parity screen:
  - schools list + activation
  - school-year, grade-level, class-room, group, subject, secondary-field and teacher-assignment flows
  - student read-only organization context.
- Academics parity screen:
  - timetable, lessons, attendance, excuses, grades, homework, daily reports lists
  - create flows where backend supports create
  - parent excuse flow
  - platform override audit read summary.
- Communication parity screen:
  - announcement publish/activation
  - conversations and messages flow
  - notifications panel.
- Administration parity screen:
  - settings, toggles, lifecycle, housekeeping, audit and summary
  - role-scoped teacher/parent/student context screens.

## 4) Role parity mapping
- `PlatformAdministrator`:
  - full admin read/write flows across Identity, Organization, Academics override audit, Communication, Administration.
- `SchoolAdministrator`:
  - school-scoped management in Organization and Administration write paths where backend allows.
- `Teacher`:
  - pedagogy create/read/update in Academics scope
  - communication and teacher administration context read.
- `Parent`:
  - parent-student identity reads
  - student-linked academics read and excuse create/cancel
  - communication reads/messages in allowed scope
  - parent administration context read.
- `Student`:
  - self-only read views in Identity/Organization/Academics/Communication
  - student administration context read.

## 5) School-type parity mapping
- `Kindergarten`:
  - UI priorities: groups, attendance, daily reports, operational communication.
- `ElementarySchool`:
  - UI priorities: classes, subjects, attendance, grades, homework.
- `SecondarySchool`:
  - UI priorities: classes, subjects, grade-level context and secondary fields of study.

## 6) Missing backend support rule outcomes
- No new business capabilities were introduced in backend for Phase 18.
- Frontend avoided fake flows where backend endpoints do not exist.
- No analytics/reporting/BFF/GraphQL additions were introduced.

## 7) Scope guard
- Still excluded:
  - tests, quizzes, assessment, exams, online testing
  - question bank, automated grading, scoring
  - evaluation workflows, exam workflow, assessment engine
  - university model, credits, semesters, subject enrollment
  - analytics engine, reporting engine
  - BFF, GraphQL, second frontend.
