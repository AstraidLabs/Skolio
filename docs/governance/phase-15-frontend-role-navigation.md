# Skolio - Phase 15 Frontend Role Navigation Consolidation

## Role -> Navigation map

### PlatformAdministrator
- `/dashboard`
- `/organization`
- `/identity`
- `/administration`
- `/communication`
- `/academics`

### SchoolAdministrator
- `/dashboard`
- `/identity`
- `/communication`
- `/organization`
- `/academics`
- `/administration`

### Teacher
- `/dashboard`
- `/identity`
- `/communication`
- `/organization`
- `/academics`

### Parent
- `/dashboard`
- `/identity`
- `/organization`
- `/academics`
- `/communication`

### Student
- `/dashboard`
- `/identity`
- `/organization`
- `/academics`
- `/communication`

## Route-guard rules

- Frontend shows only role-relevant navigation entries.
- Frontend does not grant access; backend authorization remains mandatory.
- Unauthorized route requests must resolve to explicit forbidden/unauthorized UI state.
- Administration route is hidden for Teacher/Parent/Student by default navigation.

## School-type behavior in navigation/dashboard emphasis

### Kindergarten
- Emphasize groups, attendance, daily reports, parent operations.
- Student self-service remains conservative and limited.

### ElementarySchool
- Emphasize classes, subjects, attendance, grades, homework.

### SecondarySchool
- Emphasize broader subject/year context and field-of-study context.
- No university navigation concepts.

## Blind-path prohibition

- No visible dead links to unavailable role features.
- No role-specific menu entries for unsupported school type actions.
- No platform-admin controls in non-platform role UI.
