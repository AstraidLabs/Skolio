# Skolio Phase 8 — Release Readiness Freeze

## Final architecture and boundary freeze
- Backend remains service-based with Clean Architecture layers per business service (`Domain`, `Application`, `Infrastructure`, `Api`).
- `Skolio.WebHost` remains only a frontend host bridge and does not contain business orchestration.
- Cross-service project references between business services remain prohibited.
- React remains the single business frontend; Razor Pages remain host bridge only.

## Final role model
- `PlatformAdministrator`: administration read/write and operational policy control.
- `SchoolAdministrator`: school-level organization, identity role assignment, and operational management.
- `Teacher`: class/group operations, attendance, grades, homework, communication.
- `Parent`: profile and family-bound read surfaces, communication participation.
- `Student`: own profile and school communication read surfaces.

## School-type model freeze
- `Kindergarten`: group-centric daily operations with daily reports, attendance, and parent communication.
- `ElementarySchool`: class/subject/timetable operations with attendance, grades, and homework.
- `SecondarySchool`: elementary capabilities plus study branch and grade-year structure.
- No university semantics (`semesters`, `credits`, `subject enrollment`, `exam periods`) are introduced.

## Phase 8 implementation decisions
- Read surfaces were completed with list/detail endpoints and conservative query filters for organization, identity, communication, and administration APIs.
- Pagination model was unified on simple `pageNumber` and `pageSize` parameters for admin-heavy and read-heavy lists.
- Lightweight search was added where operationally needed (`schools`, `subjects`) with PostgreSQL `ILIKE`.
- Audit log listing now supports filtered paging and explicit detail endpoint for admin investigations.
- Persistence tuning was limited to explicit indexes for high-frequency read paths and filters.

## Operational model
- PostgreSQL remains the source of truth with one database per service:
  - `skolio_identity`
  - `skolio_organization`
  - `skolio_academics`
  - `skolio_communication`
  - `skolio_administration`
- Redis remains auxiliary for cache and short-lived runtime state, not a primary datastore.
- SignalR remains realtime transport and not source of truth.

## Configuration and release discipline
- Environment configuration naming and URL wiring from previous phases remain unchanged and locked.
- Development seed scope remains minimal and operationally targeted.
- Developer API usability remains OpenAPI-based without introducing custom explorer frameworks.

## Explicit out-of-scope freeze
- No tests/quizzes/assessment/exam workflow.
- No analytics/reporting engines.
- No external integrations, event bus, GraphQL, BFF, file storage, or mobile app.
- No second frontend and no Razor business screens.
