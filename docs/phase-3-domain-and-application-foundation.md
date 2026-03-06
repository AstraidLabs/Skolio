# Skolio - Phase 3 Domain and Application Foundation

## Domain entities introduced

### Organization service
- `School`, `SchoolType`, `SchoolYear`, `GradeLevel`, `ClassRoom`, `TeachingGroup`, `Subject`, `SecondaryFieldOfStudy`, `TeacherAssignment`.
- Guard rules enforce Kindergarten group-first behavior, SecondarySchool-only field-of-study support, and school-scoped organizational assignments.

### Academics service
- `TimetableEntry`, `LessonRecord`, `AttendanceRecord`, `ExcuseNote`, `GradeEntry`, `HomeworkAssignment`, `DailyReport`.
- Model supports class/group attendance and Kindergarten daily reporting without introducing exam or assessment workflows.

### Identity service
- `UserProfile`, `SchoolRoleAssignment`, `ParentStudentLink`, `UserType`.
- Business identity relationships are modeled without technical ASP.NET Identity or OpenIddict entities.

### Communication service
- `Announcement`, `Conversation`, `ConversationMessage`, `Notification`.
- Domain focuses on school-targeted communication, direct messaging, and application notifications.

### Administration service
- `AuditLogEntry`, `SystemSetting`, `FeatureToggle`, `SchoolYearLifecyclePolicy`, `HousekeepingPolicy`.
- School-year lifecycle policy is administrative policy data and does not duplicate canonical `SchoolYear`.

## Application contracts introduced

Each business service now contains:
- Service-local command contracts via MediatR request models for the initial business surface.
- Service-local DTO contracts for command responses.
- Service-local persistence boundary interfaces for write/read operations.
- Service-local Mapster registrations for domain-to-application contract mapping.

## Explicitly deferred to next phase

- EF Core entity configurations, DbSet registration, migrations, and seed data.
- Infrastructure repository implementations and persistence wiring.
- ASP.NET Identity and OpenIddict technical persistence models.
- API endpoints/controllers and authentication/token flows.

## Scope guard

Phase 3 intentionally does **not** introduce:
- Any assessment, exam, quiz, test, scoring, or automated grading model.
- Any university model, credits, semesters, or subject enrollment concepts.

The domain remains aligned only to:
- `Kindergarten`
- `ElementarySchool`
- `SecondarySchool`
