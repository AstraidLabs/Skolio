# Phase 27 - PlatformAdministrator Profile Extension

## Decision
PlatformAdministrator Profile is implemented strictly as an extension of the existing Identity/Profile flow in `Skolio.Identity.Api`, `Skolio.Identity.Infrastructure`, and `Skolio.Frontend` without creating a new route, module, or parallel profile screen.

## Backend extension
Extended existing `user_profiles` profile model with platform-administrator-scoped summary fields:
- `platform_role_context_summary`
- `managed_platform_areas_summary`
- `administrative_boundary_summary`

Updated in:
- Domain entity: `UserProfile`
- Application contract: `UserProfileContract`
- Application command: `UpsertUserProfileCommand`
- API profile flow: `UserProfilesController`
- EF mapping: `UserProfileConfiguration`
- Seeder defaults: `IdentityAuthSeeder`

## Migration
Added EF Core additive migration:
- `20260310123000_AddPlatformAdministratorProfileSections`

Migration is non-destructive and preserves existing data.

## Frontend extension
Extended existing profile page (`IdentityParityPage`) with PlatformAdministrator-scoped tabs (inside current identity profile route):
- Basic details
- Address and contact
- Platform role and context
- Managed platform areas
- Administrative overview

UI is role-aware and platform-administrator-scoped.
Managed areas and administrative overview sections are informational and rendered as read-only summaries.

## Ownership boundaries preserved
The following data ownership remains unchanged:
- Schools: `Organization`
- Feature toggles: `Administration`
- System settings: `Administration`
- Audit logs: `Administration`
- Housekeeping policies: `Administration`
- School-year lifecycle policies: `Administration`

Identity profile stores only profile-scoped summaries for platform administrators.

## Role-based access boundaries
- `PlatformAdministrator`: self profile view/edit in allowed profile scope, including platform summary fields.
- `SchoolAdministrator`, `Teacher`, `Parent`, `Student`: no standard editable PlatformAdministrator self-profile context.
- Profile flow does not allow role assignment editing or security-boundary management.

## Scope guard
This phase does not introduce:
- New platform-admin business module outside identity/profile flow
- New standalone profile page
- ERP admin cockpit
- HR module
- Payroll logic
- Exams/tests/quizzes/assessment/automated grading/university model
