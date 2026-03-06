# Skolio Phase 4 - backend vertical slices

## Implemented vertical slices

### Identity
- Upsert `UserProfile`
- Assign `SchoolRoleAssignment`
- Create `ParentStudentLink`

### Organization
- Create school
- Create school year
- Create class room
- Create teaching group
- Create subject
- Assign teacher

### Academics
- Create timetable entry
- Record lesson
- Record attendance
- Submit excuse note
- Record grade entry
- Assign homework
- Record daily report

### Communication
- Publish announcement
- Create conversation
- Add conversation message
- Create notification

### Administration
- Change system setting
- Change feature toggle
- Write audit log entry
- Manage school year lifecycle policy
- Manage housekeeping policy

## Persisted entities per service
- Identity: `UserProfile`, `SchoolRoleAssignment`, `ParentStudentLink`
- Organization: `School`, `SchoolYear`, `GradeLevel`, `ClassRoom`, `TeachingGroup`, `Subject`, `TeacherAssignment`, `SecondaryFieldOfStudy`
- Academics: `TimetableEntry`, `LessonRecord`, `AttendanceRecord`, `ExcuseNote`, `GradeEntry`, `HomeworkAssignment`, `DailyReport`
- Communication: `Announcement`, `Conversation`, `ConversationMessage`, `Notification`
- Administration: `SystemSetting`, `FeatureToggle`, `AuditLogEntry`, `SchoolYearLifecyclePolicy`, `HousekeepingPolicy`

## API endpoints added
- `POST /api/identity/user-profiles`
- `POST /api/identity/school-role-assignments`
- `POST /api/identity/parent-student-links`
- `POST /api/organization/schools`
- `POST /api/organization/school-years`
- `POST /api/organization/class-rooms`
- `POST /api/organization/teaching-groups`
- `POST /api/organization/subjects`
- `POST /api/organization/teacher-assignments`
- `POST /api/academics/timetable`
- `POST /api/academics/lessons`
- `POST /api/academics/attendance/records`
- `POST /api/academics/attendance/excuse-notes`
- `POST /api/academics/grades`
- `POST /api/academics/homework`
- `POST /api/academics/daily-reports`
- `POST /api/communication/announcements`
- `POST /api/communication/conversations`
- `POST /api/communication/conversations/messages`
- `POST /api/communication/notifications`
- `POST /api/administration/system-settings`
- `POST /api/administration/feature-toggles`
- `POST /api/administration/audit-logs`
- `POST /api/administration/school-year-lifecycle-policies`
- `POST /api/administration/housekeeping-policies`

## Migration workflow
- Every API now applies service-local EF migrations in `Development` only at startup.
- Docker Compose now runs APIs with `ASPNETCORE_ENVIRONMENT=Development`, enabling local auto-migrate workflow.
- Production release workflow remains explicit and external to runtime.

## Deferred to Phase 5
- Full OpenIddict/OIDC auth flow
- Token issuance and login/logout use cases
- Cross-service orchestration beyond local service boundaries

## Scope guard
- No university model, credits, semesters, subject enrollment, tests, quizzes, exams, assessment, automated grading, scoring, or exam workflows were added.
- Supported education model stays limited to Kindergarten, ElementarySchool, SecondarySchool.
- Kindergarten remains group-first and is not modeled as a reduced SecondarySchool.
