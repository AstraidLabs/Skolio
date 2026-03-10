# Phase 30 — Identity User Management UI

## Boundary and scope
- User Management remains inside `Skolio.Identity` boundary and is strictly admin-only (`PlatformAdministrator`, `SchoolAdministrator`).
- This phase is account lifecycle and role governance only; it is not HR, CRM, employee directory, or generic personnel management.
- `PlatformAdministrator` is global scope; `SchoolAdministrator` remains school-scoped and backend-enforced.

## User Management grid
- Main admin grid is the operational entrypoint for user lifecycle management.
- Grid columns: Name, Email/Username, Role, School, Account Status, MFA, Last Login, Actions.
- Grid supports backend-enforced filtering, server-side sorting, paging, and fixed page sizes (`10, 20, 50, 100`).
- Grid remains overview + quick actions only; deep editing is intentionally moved to row action `Edit`.

## Grid lifecycle actions
Per-row actions are visible only when they are relevant for current account state:
- Activate
- Deactivate
- Block
- Unblock
- Resend activation
- Edit

All lifecycle actions are executed via explicit Identity endpoints and are audit logged in Identity API.

## User detail / edit form
The `Edit` action opens a dedicated admin detail form for one user as the primary place for complex updates. The form is separated into practical sections:
- Basic info
- Account/lifecycle state
- School context
- Role management

This keeps the grid readable and prevents heavy inline editing.

## Role management
Role management is centered in the user detail form and uses ASP.NET Core Identity `UserManager` + `RoleManager`.

Supported operations:
- list role set
- update role set
- assign role (explicit endpoint)
- remove role (explicit endpoint)

Role changes are backend validated with domain guards:
- `Parent` requires `ParentStudentLink`
- `Teacher` requires school teaching context (`SchoolRoleAssignment` with `Teacher`)
- `SchoolAdministrator` requires school scope assignment
- `Student` requires student profile context
- `PlatformAdministrator` requires governance context and can only be managed by PlatformAdministrator

## Filtering, sorting, paging
Filtering supports:
- name
- email/username
- role
- account status
- activation status
- block/lock status
- MFA status
- school
- school type
- inactivity state

Sorting supports:
- name
- email
- created at
- last login
- account status
- school

Paging supports:
- current page, total count, total pages, page size, prev/next, empty state
- validated page sizes: `10, 20, 50, 100`

## Deletion policy
Standard physical delete is intentionally not exposed in grid actions. This phase follows lifecycle model:
- activation
- deactivation
- block
- unblock

## Scope differences
- `PlatformAdministrator`: global listing, filtering, and role/lifecycle management.
- `SchoolAdministrator`: school-scoped listing and actions only; enforced in backend Identity API.
