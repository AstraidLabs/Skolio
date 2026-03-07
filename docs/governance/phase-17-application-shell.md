# Skolio - Phase 17 Application Shell

## Shell composition
- root shell: `AppShell`
- global full-width top bar: `AppNavbar`
- left navigation: `AppSidebar`
- page-level header: `AppPageHeader`
- content region: `page body`
- lightweight footer: `AppFooter` with `FooterLanguageSwitcher`
- structure order: navbar -> content row (sidebar + content region) -> footer in content flow

## Navbar behavior
- global, full-width and consistent across all roles
- contains `Skolio` wordmark, top-level context, notifications entry point and profile dropdown
- profile dropdown actions: `Profil`, `Odhlasit`
- profile context uses active role + school type/session context
- navbar shell (background + border + shadow) runs across viewport width, inner content is constrained by wrapper
- language switcher is not rendered in navbar

## Sidebar behavior
- primary navigation surface
- role-aware items derived from allowed routes
- school-type-aware priority section labels:
  - Kindergarten: operations prioritize groups and daily reports context
  - ElementarySchool: operations prioritize classes and subjects
  - SecondarySchool: operations include wider study context
- active item is visually explicit
- compact mode is intentionally not implemented

## Page header behavior
- unified page header in content region
- includes page title + subtitle
- optional page actions slot
- does not duplicate global navbar

## Footer behavior
- lightweight technical footer only
- shows `Skolio`, shell context and runtime mode
- hosts language switcher for shell-level locale change
- no marketing/footer module expansion

## Role-aware behavior summary
- PlatformAdministrator: overview + operations + administration sections
- SchoolAdministrator: overview + operations + administration (school scope)
- Teacher: overview + operations (teaching scope)
- Parent: overview + parent-relevant reads and communication
- Student: overview + student self scope and communication

## School-type-aware behavior summary
- Kindergarten: operational emphasis on group/daily-report context
- ElementarySchool: class/subject operational emphasis
- SecondarySchool: broader operations context without university model

## Out of scope
- tests, quizzes, assessment, exams, online testing
- question bank, automated grading, scoring
- evaluation/exam workflow engines
- university model, credits, semesters, subject enrollment
- analytics/reporting engines
- BFF, GraphQL, second frontend
- compact sidebar mode and compact shell toggle
