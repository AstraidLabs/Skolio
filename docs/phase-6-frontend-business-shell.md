# Skolio Phase 6 - Frontend business shell

## Implemented frontend feature areas
- identity
- organization
- academics
- communication
- administration
- shared (auth + http)

## Implemented dashboards
- PlatformAdministrator dashboard cards and navigation
- SchoolAdministrator dashboard cards and navigation
- Teacher dashboard cards and navigation
- Parent dashboard cards and navigation
- Student dashboard cards and navigation

## Added query endpoints
- Identity: my profile, list role assignments, list parent-student links
- Organization: school list/detail, school year list, class list, group list, subject list, teacher assignments list
- Academics: timetable list, lesson list, attendance list, grade list, homework list, daily reports list
- Communication: announcement list/detail, conversation list/detail/messages, notifications list
- Administration: settings list/update, feature toggle list/update, audit log list, lifecycle/housekeeping policy list/update

## School-type UX differences
- Kindergarten highlights daily reports, groups, attendance and parent communication context.
- ElementarySchool highlights classes, subjects, timetable, attendance, grades and homework.
- SecondarySchool highlights classes, subjects and wider agenda with fields of study context in dashboard summaries.

## Deferred to Phase 7
- deeper workflow polishing and richer filtering
- more advanced dashboard aggregation
- broader realtime refresh patterns outside communication scope

## Scope guard
- No assessment, tests, quizzes, exams, scoring, or evaluation engines.
- No university model, credits, semesters, or subject enrollment.
- React stays primary frontend and Razor remains host bridge only.
