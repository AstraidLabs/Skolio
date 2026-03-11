# Skolio system seed model

## Always seeded baseline
- **Identity (Skolio.Identity.Infrastructure):** mandatory roles `PlatformAdministrator`, `SchoolAdministrator`, `Teacher`, `Parent`, `Student` and OpenIddict frontend client registration.
- **Organization (Skolio.Organization.Infrastructure):** mandatory school operators, founders, one school per supported type (`Kindergarten`, `ElementarySchool`, `SecondarySchool`), school year `2025/2026`, grade levels, classes, groups, subjects, and one secondary field of study.
- **Administration (Skolio.Administration.Infrastructure):** required system settings, required feature toggle defaults, required housekeeping policy, and required school-year lifecycle policies.
- **Academics (Skolio.Academics.Infrastructure):** no direct mutable data records are created; academics baseline depends on Organization structural seed.

## Mandatory lookup and structural seed boundary
- Seed is restricted to system, reference, and structure baseline needed for platform operation.
- Seed is idempotent and existence-aware: empty database initializes baseline, fully initialized database exits without changes.
- Partial state is repaired only by adding missing mandatory baseline rows with stable identifiers.
- Critical inconsistency fails startup (seed guard), instead of silently mutating existing stabilized data.

## Seed never creates
- Any login account.
- Any development account.
- Any default PlatformAdministrator account.
- Any bootstrap bypass, backdoor login, or public registration flow.
- Any tests/quizzes/assessment/exams/automated grading models.
- Any university model, credits, semesters, subject enrollment, import engine, or ERP provisioning module.

## Bootstrap and user management contract
- First `PlatformAdministrator` is created **only** via bootstrap flow.
- Additional users are created **only** via User Management / Create User Wizard.
- Seed only prepares roles and school structure so post-bootstrap user creation has valid school context.

## Execution and configuration
- Seed execution is bound to migration startup path of each backend service.
- Startup trigger is environment-aware:
  - Development runs migrations + seed by default.
  - Non-development requires `*:Seed:EnableLocalMode=true` to run startup migration/seed.
- Disable by service with `*:Seed:Enabled=false`.
- Works with container-first startup and EF Core migrations; no manual SQL seeding path is used.
