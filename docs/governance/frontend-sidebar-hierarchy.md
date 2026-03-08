# Skolio Frontend Sidebar Hierarchy

## Hierarchical Navigation
- Sidebar uses parent sections with child routes.
- `Organization` and `Academics` are expandable sections.
- Parent and child active states are resolved from current route.
- Direct entry to child route auto-opens matching parent section.

## Expand/Collapse Behavior
- Parent sections `Organization` and `Academics` support expand/collapse.
- Expand state is stable during route changes in current session.
- Active child always keeps parent visually active.
- No compact sidebar mode and no compact button exist.

## Role-Aware Behavior
- `PlatformAdministrator`: full Organization and Academics subtree.
- `SchoolAdministrator`: school-scoped Organization and Academics subtree.
- `Teacher`: only relevant Organization context plus Academics teaching flows.
- `Parent` and `Student`: no admin Organization tree; only allowed non-admin navigation and scoped Academics children.

## School-Type-Aware Behavior
- `Kindergarten`: operational priority on groups and daily-report oriented flows.
- `ElementarySchool`: priority on classes, subjects, attendance, grades, homework.
- `SecondarySchool`: includes fields of study and grade-level context.
- Kindergarten is not treated as reduced SecondarySchool.

## Out of Scope Guard
- No tests, quizzes, assessment, exams, online testing, question bank, automated grading, scoring, evaluation workflows, exam workflow.
- No university model, credits, semesters, subject enrollment.
- No analytics engine, reporting engine, BFF, GraphQL, second frontend.
