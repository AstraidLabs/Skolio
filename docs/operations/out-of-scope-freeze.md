# Skolio — Explicit Out-of-Scope Freeze after Phase 8

## Funkční mimo scope
- testy, quizy, assessment, exams, online testing, question bank, automated grading, scoring
- evaluation workflows, exam workflow, assessment engine
- university model, credits, semesters, subject enrollment

## Technické mimo scope
- API gateway orchestrace ve WebHost
- druhý frontend (včetně Razor business obrazovek)
- event bus, event-driven redesign, GraphQL, BFF vrstva
- generic repository/service/controller frameworky
- shared business library napříč službami
- analytics engine, reporting engine, licensing engine
- external integrations, mobile app, file storage, attachments

## Datová a provozní omezení
- PostgreSQL-only přes Npgsql, jedna databáze per service
- Redis pouze auxiliary runtime/cache, nikdy primární datastore
- SignalR pouze realtime transport, nikdy source of truth
