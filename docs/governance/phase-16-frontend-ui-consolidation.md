# Skolio - Phase 16 Frontend UI Consolidation

## Pøevzaté vizuální principy z inspirace
- èistý horní panel s jasným kontextem uživatele
- hero/welcome panel na dashboardu
- card-based rozvržení metrik a sekcí
- kompaktní summary widgets
- pøehledné sekundární list bloky
- konzistentní spacing, radius, shadow a hierarchy

## Zámìrnì nepøevzaté
- cizí doménové texty a cizí moduly
- BI/analytics/reporting model
- student analyze, retention analytics a podobné cizí koncepty
- healthcare/general SaaS obsah
- jakýkoliv university/assessment model

## Zavedené dashboard patterns
- role dashboard shell: hero + primary cards + secondary blocks + quick actions
- widget types: metric card, summary card, quick-action card, compact list card, notification card, contextual status card
- jednotný top header + page header pattern
- jednotné loading/empty/error state bloky

## School-type UI rozdíly
- Kindergarten: provozní, skupinové, daily-report orientované zvýraznìní
- ElementarySchool: tøídní a pøedmìtové zvýraznìní
- SecondarySchool: rozšíøení o roèníkový/oborový kontext bez university modelu

## Role-specific dashboard refinement
- PlatformAdministrator: platform governance summary + quick links na settings/toggles/audit/lifecycle
- SchoolAdministrator: school operations summary + quick actions pro školní entity
- Teacher: assignment-driven cards + pending pedagogické úkony + announcements
- Parent: child-linked operations + excuse/communication quick actions
- Student: self-only overview + timetable/attendance/grades/homework emphasis

## Mimo scope
- nový frontend framework
- metadata-driven dashboard builder
- analytics/reporting engine
- BFF/GraphQL/gateway orchestration layer
- nový business modul
