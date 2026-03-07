# Skolio - Phase 15 Role Governance Checkpoint

## Final role governance

### PlatformAdministrator
- Global governance: schools, platform settings, global toggles, global audit reading, support overrides.
- Not primary daily pedagogical operator.

### SchoolAdministrator
- Assigned-school governance: school structure, school year operations, assigned-school user/role governance, school operational oversight.
- Not global platform governance.

### Teacher
- Assignment-bound pedagogical operations: lessons, attendance, grades, homework, daily reports.
- Not school/global governance.

### Parent
- ParentStudentLink-bound read and excuse lifecycle operations.
- Not pedagogical writer, not governance role.

### Student
- Own-data self-service read model with limited interaction.
- Not pedagogical writer, not governance role.

## Override governance

- Platform override: only explicit support-correction endpoints with audit.
- School operational correction: only explicit school admin support paths with audit where implemented.
- No generic superuser workflow.

## Data visibility governance

- PlatformAdministrator: global visibility.
- SchoolAdministrator: school-scoped visibility.
- Teacher: assignment-scoped visibility.
- Parent: link-scoped visibility.
- Student: self-scoped visibility.

## Governance lock

- No new role introduction.
- No permission engine replacement.
- No ABAC introduction.
- No module expansion beyond approved service boundaries.
