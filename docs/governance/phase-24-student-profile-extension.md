# Phase 24 - Student Profile extension inside Identity/Profile flow

## Architectural decision
- Student Profile is implemented as an extension of existing `Identity` profile flow (`/api/identity/user-profiles` + existing frontend Identity profile screen).
- No new profile module, no new standalone profile page, no new frontend route.

## Added profile data
The existing `UserProfile` model was extended with student-scoped sections:
- Basic data: gender, date of birth, national id number, birth place.
- Address and contact: permanent address, correspondence address, contact email, phone.
- Legal guardians: legal guardian 1, legal guardian 2.
- School placement: school placement summary.
- Health and safety: insurance provider, pediatrician, health/safety notes.
- Support measures: support measures summary.

## Backend scope
Changes are limited to existing Identity boundary:
- `Skolio.Identity.Api`
- `Skolio.Identity.Application`
- `Skolio.Identity.Domain`
- `Skolio.Identity.Infrastructure`

No ownership transfer from Academics/Organization was introduced.

## Migration
A new EF Core migration was added for `UserProfile` student extension columns in Identity infrastructure.
The migration is additive and non-destructive.

## Role-based access boundary
- `PlatformAdministrator`: full view/edit in administrative profile edit path.
- `SchoolAdministrator`: broad profile edit in existing admin boundary (without bypassing platform-only behavior).
- `Teacher`: self-service edit for permitted profile fields and school-position scoped fields.
- `Parent`: mostly read-oriented profile access, edit constrained to existing self-service scope.
- `Student`: read-oriented access; self edits remain restricted and cannot bypass admin constraints.

Sensitive fields remain inside Identity profile boundary and are not exposed as cross-student aggregate views.

## Explicit out-of-scope guard
This phase does **not** introduce:
- new student module outside Identity/Profile flow
- new parallel profile page
- assessment/exams/tests/quizzes
- automated grading
- university model, credits, semesters, subject enrollment
- medical ERP module
- HR module
