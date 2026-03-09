# Phase 26 - Parent Profile extension inside Identity/Profile flow

## Architectural decision
- Parent Profile is implemented as an extension of existing Identity/Profile flow.
- No new parent module and no standalone profile page/route were introduced.

## Added parent profile data
The existing `UserProfile` model is extended with parent-scoped profile fields:
- `parentRelationshipSummary`
- `deliveryContactName`
- `deliveryContactPhone`
- `preferredContactChannel`
- `communicationPreferencesSummary`

Existing profile fields (addresses, contact email, preferred language, preferred contact note) remain part of parent profile usage.

## Backend scope
Changes are limited to existing Identity layers:
- `Skolio.Identity.Api`
- `Skolio.Identity.Application`
- `Skolio.Identity.Domain`
- `Skolio.Identity.Infrastructure`

No ownership shift from Organization/Academics was introduced.

## Migration
- Added additive migration `20260309193000_AddParentProfileSections` for new parent profile columns.
- Migration is non-destructive and keeps existing data.

## Parent-scoped UI extension
- Existing frontend identity profile page is extended with parent tabs (inside the same screen):
  - Basic data
  - Address and contact
  - Delivery details
  - Linked students
  - Relationships and school context
  - Communication preferences
- Linked students and parent-student relationships remain read-only in self-profile.

## Role boundaries
- `Parent`: self-profile edit in allowed scope.
- `SchoolAdministrator`: school-scoped administrative edit in existing admin profile edit flow.
- `PlatformAdministrator`: broader administrative edit.
- `Teacher`/`Student`: no parent administrative self-management path introduced.

## Service ownership boundaries
- Parent-student links remain authoritative in Identity.
- Attendance/grades/homework/daily reports remain in Academics.
- School structure remains in Organization.

## Explicit out-of-scope
This phase does not introduce:
- new parent module outside identity/profile flow
- new parallel profile page
- CRM module
- family ERP module
- tests/quizzes/assessment/exams/automated grading
- university model
