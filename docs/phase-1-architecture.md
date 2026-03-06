# Fáze 1 – Service-based architektonický skeleton

## 1) Solution a projektová struktura
- `SchoolPlatform.slnx` sdružuje WebHost, 5 business služeb (každá `Domain/Application/Infrastructure/Api`) a samostatný frontend projekt.
- Backend je service-based: každá API služba je samostatně deployovatelná jednotka s vlastním Clean Architecture řezem.
- `SchoolPlatform.WebHost` je pouze host bridge pro React a technické endpointy, bez doménové business logiky.

## 2) Rozdělení na služby a projekty
- `SchoolPlatform.Identity.Api`: účty, role, parent-child vazby, token issuance přes OpenIddict.
- `SchoolPlatform.Organization.Api`: školy, typy škol, školní roky, ročníky, třídy, skupiny, předměty, obory, teacher assignments.
- `SchoolPlatform.Academics.Api`: rozvrh, lesson records, attendance, omluvenky, známky, domácí úkoly, daily reports.
- `SchoolPlatform.Communication.Api`: oznámení, zprávy, notifikace, SignalR huby.
- `SchoolPlatform.Administration.Api`: audit log, systémové nastavení, feature toggles, správa školních roků, housekeeping.

## 3) Package dependencies (Phase 1 skeleton)
- Common backend baseline: `MediatR`, `Mapster`, `FluentValidation` v Application.
- Infrastructure: `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.Extensions.Caching.StackExchangeRedis`.
- Api: `AspNetCore.Authentication.JwtBearer`, `AspNetCore.Identity.EntityFrameworkCore`, `OpenIddict.AspNetCore`, `Hangfire.AspNetCore`, `StackExchange.Redis`, `AspNetCore.SignalR` (jen Communication API).

## 4) Základní architektonické rozhodnutí
- **Datová izolace**: jeden sdílený PostgreSQL server + **samostatná databáze per service** (konzervativní a čitelné provozní oddělení).
- Každá služba vlastní migrace, vlastní connection string, vlastní schema governance.
- Žádný generic repository/service/controller layer; EF Core je primární data access mechanismus v Infrastructure.

## 5) Docker struktura
- Každý runtime projekt má vlastní Dockerfile v kořeni projektu.
- `docker/compose.yaml` definuje infrastrukturu (PostgreSQL, Redis), backend služby, WebHost a frontend runtime.
- Healthchecky jsou definované pro PostgreSQL, Redis a všechna API i host služby.

## 6) Compose služby
- `postgresql`, `redis`, `identity-api`, `organization-api`, `academics-api`, `communication-api`, `administration-api`, `webhost`, `frontend`.
- `depends_on` používá health conditions, aby backend startoval až po dostupné infrastruktuře.
- Persistent volume: `postgresql-data`.

## 7) Hlavní doménové moduly
- Identity a uživatelé.
- Organizace školy.
- Výuka a provoz.
- Hodnocení.
- Docházka.
- Komunikace.
- Administrace.
- Školní typy: Kindergarten, ElementarySchool, SecondarySchool (bez university modelu).

## 8) Redis návrh
- Distribuovaná cache dashboard dat podle role/type školy.
- Cache referenčních číselníků a systémových konfigurací.
- Krátkodobé presence/notifikační stavy v Communication službě.
- Rate limiting klíče pro anti-abuse na veřejných endpointách.
- SignalR backplane pro scale-out režim Communication API.

## 9) Razor host bridge model pro React
- Razor host page (`/Pages/AppHost.cshtml`) renderuje pouze shell + bootstrap config.
- Development: WebHost proxy na Vite dev server.
- Production: WebHost servíruje build artefakty Reactu (`wwwroot/app`) + SPA fallback route.
- Razor nepoužívá duplicitní business obrazovky.
