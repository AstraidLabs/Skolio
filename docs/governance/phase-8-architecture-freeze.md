# Skolio — Phase 8 Architecture Freeze

## Platná architektonická rozhodnutí
- Backend zůstává service-based: `Identity`, `Organization`, `Academics`, `Communication`, `Administration`.
- Každá business služba drží vlastní Clean Architecture vrstvy: `Domain`, `Application`, `Infrastructure`, `Api`.
- `Skolio.WebHost` je pouze host bridge pro frontend a zdravotní endpointy; neobsahuje business orchestrace.
- Frontend je pouze `React` (`src/Frontend/Skolio.Frontend`), Razor Pages se nepoužívají pro business obrazovky.
- Datový model a use cases zůstávají konzistentní pouze pro `Kindergarten`, `ElementarySchool`, `SecondarySchool`.

## Dependency guardrails
- `Domain` nesmí záviset na `Infrastructure`, `Api`, `Frontend`.
- `Application` nesmí záviset na `Api`, `Frontend`.
- `Infrastructure` může záviset na `Application` a `Domain`.
- `Api` může záviset na `Application` a `Infrastructure`.
- Cross-service project references mezi business službami jsou zakázané.
- Shared business library napříč službami je zakázaná.

## Runtime guardrails
- PostgreSQL (Npgsql) je jediná primární databáze; jedna databáze per service.
- Redis je pouze pomocná technologie (cache/presence/rate limiting), není source of truth.
- SignalR je realtime transport, není source of truth.
- JWT/OIDC model zůstává na ASP.NET Identity + OpenIddict + JWKS.

## Governance freeze po Fázi 8
- Bez redesignu architektury, bez nových business modulů mimo schválené boundaries.
- Bez BFF, GraphQL, event bus, generic repository/service/controller vrstev.
- Bez univerzitního modelu (kredity, semestry, enrollment, exam periods).
