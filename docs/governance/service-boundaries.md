# Skolio — Service Boundaries (Phase 8)

## Identity (`Skolio.Identity.*`)
- Scope: technická identita, business profil uživatele, role assignment, parent-student vazby.
- Vstupy/výstupy: OIDC/OAuth2, JWT issuance, profile read/edit, role/link management.
- Mimo scope: veřejná registrace, MFA, externí identity providery, password recovery.

## Organization (`Skolio.Organization.*`)
- Scope: školy, školní roky, třídy/skupiny, předměty, teacher assignments.
- Vstupy/výstupy: organizační katalog pro provoz školy podle typu školy.
- Mimo scope: univerzitní semestrální struktura nebo enrollment workflow.

## Academics (`Skolio.Academics.*`)
- Scope: rozvrh, lessons, attendance, grades, homework, daily reports.
- Vstupy/výstupy: provozní akademické záznamy list/detail + konzervativní filtry.
- Mimo scope: testy/quizy/exams/assessment workflow.

## Communication (`Skolio.Communication.*`)
- Scope: announcements, conversations, notifications, unread/presence náznaky.
- Vstupy/výstupy: persisted komunikace v PostgreSQL + krátkodobý realtime stav přes Redis/SignalR.
- Mimo scope: chat platform overengineering, event-driven redesign.

## Administration (`Skolio.Administration.*`)
- Scope: audit log, feature toggles, school year lifecycle policy, housekeeping policy, provozní přehledy.
- Vstupy/výstupy: admin-only governance a operativní řízení.
- Mimo scope: ERP modul, licensing engine, workflow engine.

## WebHost (`Skolio.WebHost`)
- Scope: host bridge pro React frontend, reverse integration surface, health endpoints.
- Mimo scope: API gateway orchestrace, business logika, duplikované business obrazovky.
