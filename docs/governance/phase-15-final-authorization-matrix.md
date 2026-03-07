# Skolio - Phase 15 Final Authorization Matrix

## 1) Role responsibility baseline

### PlatformAdministrator
- Primary responsibility: global platform governance.
- Read: all platform and school domains.
- Create/Update/Delete/Deactivate: schools, global settings, global toggles, delegated governance records.
- Override: allowed only in explicit support-correction endpoints with audit.
- Never: primary daily teacher/school operator workflows.

### SchoolAdministrator
- Primary responsibility: one or more assigned schools.
- Read: full assigned school operational domains.
- Create/Update/Delete/Deactivate: school structure, school-year operations, assigned-school user governance.
- Override: limited school operational corrections with audit.
- Never: global platform settings/governance outside delegated scope.

### Teacher
- Primary responsibility: daily teaching operations in teacher assignment scope.
- Read: assignment-bound students/classes/groups/subjects and relevant communication.
- Create/Update: lesson records, attendance, grades, homework, daily reports (within lifecycle).
- Delete/Deactivate: only where existing lifecycle/process allows.
- Never: platform/school governance and cross-school administration.

### Parent
- Primary responsibility: child/student read + excuse workflow + parent communication.
- Read: only through ParentStudentLink.
- Create/Update/Delete: excuse in allowed lifecycle windows only.
- Override: none.
- Never: teacher/admin workflows, role governance, school structure governance.

### Student
- Primary responsibility: own self-service read scope.
- Read: own profile, own timetable/attendance/grades/homework/daily reports (where applicable), own communication.
- Create/Update/Delete: only explicitly allowed self interactions (communication flow), no pedagogical data writes.
- Override: none.
- Never: admin/teacher/parent governance actions, other-student data access.

---

## 2) Final CRUD/override matrix
Legend: `R` read, `C` create, `U` update, `D` delete/deactivate/archive, `O` override, `-` no access.

| Domain Item | PlatformAdministrator | SchoolAdministrator | Teacher | Parent | Student |
|---|---|---|---|---|---|
| Schools | RCUD | R (assigned) + U (assigned metadata scope) | R (assigned scope only where exposed) | R (linked school context) | R (own school context) |
| School years | RCUD | RCUD (assigned school) | R (teaching context) | R (linked child context) | R (own context) |
| Grade levels | RCUD | RCUD (assigned school) | R (assignment relevance) | R (linked child relevance) | R (own relevance) |
| Class rooms | RCUDO | RCUD (assigned school) | R (assignment bound) | R (linked child relevance) | R (own relevance) |
| Teaching groups | RCUDO | RCUD (assigned school) | R (assignment bound) | R (linked child relevance) | R (own relevance) |
| Subjects | RCUDO | RCUD (assigned school) | R (assignment bound) | R (linked child relevance) | R (own relevance) |
| Secondary fields of study | RCUD | RCUD (secondary assigned school) | R (teaching relevance) | R (linked child relevance) | R (own relevance) |
| Teacher assignments | RCUDO | RCUD (assigned school) | R (own assignments) | - | - |
| User profiles | RCUD | RCUD (assigned school scope) | R/U self | R/U self | R/U self |
| Role assignments | RCUD | RCUD (assigned school allowed roles) | R own | R own | R own |
| Parent-student links | RCUDO | RCUD (assigned school scope) | R (school support read if allowed) | R own links | R own links |
| Timetable | R | RCUD (assigned school) | RCU (assignment scope) | R (link scope) | R own |
| Lesson records | RO | RCUDO (assigned school support) | RCU (assignment scope) | R (link scope filtered) | R own filtered |
| Attendance | RO | RCUDO (assigned school support) | RCU (assignment scope) | R + excuse flow | R own |
| Excuses | RO | RCUDO (assigned school support) | R/U support by process | RCUD in parent lifecycle window | R status only |
| Grades | RO | RCUDO (assigned school support) | RCU (assignment scope) | R (link scope) | R own |
| Homework | RO | RCUDO (assigned school support) | RCU (assignment scope) | R (link scope) | R own |
| Daily reports | RO | RCUDO (assigned school support) | RCU (assignment scope) | R (link scope) | R own (school-type aware) |
| Announcements | RCUDO (platform+school governance) | RCUD (school governance) | RCU (allowed pedagogical comm scope) | R | R |
| Conversations | R (admin visibility) + support actions | R/U moderation in school scope | R/C/U in allowed conversation scope | R/C/U in allowed conversation scope | R/C/U in allowed conversation scope |
| Messages | R + support | R + support | C/R in allowed conversation scope | C/R in allowed conversation scope | C/R in allowed conversation scope |
| Notifications | R + C/U admin flow | R + delegated ops | R own + limited workflow | R own | R own |
| Audit log | R global | R school-scoped | R own-impact if exposed | - (except own-operational hints if exposed) | - (except own-operational hints if exposed) |
| System settings | RCUD | R delegated school-safe subset | - | - | - |
| Feature toggles | RCUD | R/U delegated school toggles only | - | - | - |
| School-year lifecycle policies | RCUD | R/U delegated assigned-school policies | R hints | R summaries | R summaries |
| Housekeeping policies | RCUD | R delegated subset | - | - | - |

---

## 3) Cross-role hard boundaries

- PlatformAdministrator > SchoolAdministrator:
  - only PlatformAdministrator manages global system settings/global toggles/platform-wide audit/governance override.
- SchoolAdministrator > Teacher:
  - Teacher cannot manage school structure, school user governance, role governance.
- Teacher > Parent:
  - Parent cannot write pedagogical records (attendance/grades/homework/lesson records/daily reports).
- Parent > Student:
  - Parent can operate excuse lifecycle in allowed window via ParentStudentLink; Student is read-only for excuse status.
- Student self-only:
  - Student can only access own identity and own academic/communication scope.

Override model:
- Exists only in explicit admin support-correction endpoints.
- Always audit-bound.
- Never grants unrestricted superuser behavior in daily workflows.

---

## 4) Data visibility matrix

- PlatformAdministrator: global visibility with policy/audit boundary.
- SchoolAdministrator: assigned-school visibility only.
- Teacher: assignment-bound visibility (teacher assignments + timetable/subject/audience relation).
- Parent: visibility only through ParentStudentLink.
- Student: visibility only for own identity and own records.

Privacy hardening:
- Backend is source of truth for visibility enforcement.
- Frontend navigation is secondary UX filter, not security boundary.
- No role gets data only because an endpoint exists; endpoint guard is explicit.

---

## 5) School-type-aware authorization behavior

### Kindergarten
- Teacher: group/attendance/daily-report/parent communication centric.
- Parent: strong daily reports and operational communication relevance.
- Student: conservative minimal self-service; parent model remains primary.

### ElementarySchool
- Teacher: class+subject+attendance+grades+homework+lesson records.
- Parent: linked class/subject academic read.
- Student: simple self overview for own timetable/attendance/grades/homework.

### SecondarySchool
- Adds field-of-study and broader year context.
- Role boundaries remain identical; only contextual data projection expands.
- No university model concepts.

---

## 6) Backend enforcement checklist (final pass)

- School context enforcement: `school_id` scoped checks in every service boundary.
- Teacher assignment enforcement: teacher writes/reads restricted to assignment/timetable context.
- Parent boundary enforcement: linked student checks via `linked_student_id` or parent link scope.
- Student boundary enforcement: actor self checks + student-context scoped read filters.
- Override enforcement: explicit override endpoints + mandatory audit payload.
- No frontend-only authorization assumptions.

---

## 7) Final forbidden scope guard

Still forbidden in Phase 15:
- tests
- quizzes
- assessment
- exams
- online testing
- question bank
- automated grading
- scoring
- evaluation workflows
- exam workflow
- assessment engine
- university model
- credits
- semesters
- subject enrollment
