# Skolio Development Seed Accounts

## Seed policy
- Seed runs only when:
  - service startup executes migration pipeline and
  - `Environment=Development` or explicit local mode flag is enabled:
    - `Identity:Seed:EnableLocalMode=true`
    - `Organization:Seed:EnableLocalMode=true`
- Seed is idempotent:
  - roles, accounts, profiles, assignments and links are checked before create.
- Seed is development/local compose only.
- Seed does not run in production by default.
- Seed creates only minimal role-usable data.

## Seeded roles
- PlatformAdministrator
- SchoolAdministrator
- Teacher
- Parent
- Student

## Seeded schools
- `Skolio Kindergarten Brno` (`Kindergarten`)
- `Skolio Elementary Prague` (`ElementarySchool`)
- `Skolio Secondary Ostrava` (`SecondarySchool`)

## Seeded minimal organization data
- For each school:
  - active school year `2025/2026`
- Kindergarten:
  - teaching group `Berusky`
  - teacher assignment scope `DailyOperations`
- ElementarySchool:
  - grade level `1. rocnik`
  - class room `1A`
  - teaching group `Skupina 1A`
  - subject `MAT / Matematika`
  - subject teacher assignment
- SecondarySchool:
  - grade level `1. rocnik`
  - class room `S1A`
  - teaching group `Skupina S1A`
  - subject `INF / Informatika`
  - field of study `IT / Informacni technologie`
  - subject teacher assignment

## Seeded accounts
Development password for all login accounts:
- `SkolioDev!2026`

Accounts:
- `platform.admin@skolio.local` (`PlatformAdministrator`)
- `kindergarten.admin@skolio.local` (`SchoolAdministrator`, Kindergarten)
- `elementary.admin@skolio.local` (`SchoolAdministrator`, ElementarySchool)
- `secondary.admin@skolio.local` (`SchoolAdministrator`, SecondarySchool)
- `kindergarten.teacher@skolio.local` (`Teacher`, Kindergarten)
- `elementary.teacher@skolio.local` (`Teacher`, ElementarySchool)
- `secondary.teacher@skolio.local` (`Teacher`, SecondarySchool)
- `kindergarten.parent@skolio.local` (`Parent`, Kindergarten)
- `elementary.parent@skolio.local` (`Parent`, ElementarySchool)
- `secondary.parent@skolio.local` (`Parent`, SecondarySchool)
- `elementary.student@skolio.local` (`Student`, ElementarySchool)
- `secondary.student@skolio.local` (`Student`, SecondarySchool)

## Parent-student links
- `kindergarten.parent@skolio.local` -> kindergarten child profile (non-login child profile)
- `elementary.parent@skolio.local` -> `elementary.student@skolio.local`
- `secondary.parent@skolio.local` -> `secondary.student@skolio.local`

## Kindergarten student self-service note
- Dedicated Kindergarten student login account is intentionally not seeded.
- Kindergarten child data is seeded as business profile for parent-child flow coverage.
- This keeps seed aligned with conservative kindergarten model.

## Startup order and logging
- Startup order is migration -> seed.
- Seed logs:
  - seed start/end
  - created/existing roles
  - created/existing accounts
  - created/refreshed profiles
  - created/existing role assignments
  - created/existing parent-student links
  - created/refreshed schools and organization entities
- Passwords are never logged.

## Scope guard
Seed does not introduce and must not reintroduce:
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
