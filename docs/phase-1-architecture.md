# Skolio Phase 1 Architecture

## 1. Stručné technické rozhodnutí
- Backend je navržen jako service-based systém se šesti samostatně nasaditelnými API službami a jedním samostatným hostem `Skolio.WebHost`; každá business služba drží vlastní Clean Architecture vrstvy `Domain/Application/Infrastructure/Api`.
- Datová vrstva je explicitně uzamčena na jeden PostgreSQL server a samostatné databáze per service: `skolio_identity`, `skolio_organization`, `skolio_academics`, `skolio_communication`, `skolio_administration`; nejsou povoleny jiné databázové enginy.
- Frontend je jediný React + Vite + Tailwind + PostCSS projekt `Skolio.Frontend`; Razor Pages v `Skolio.WebHost` slouží pouze jako host bridge (shell, bootstrap konfigurace, SPA fallback, dev napojení na Vite).
- Redis je vyhrazen pro distribuovanou cache, dashboard cache, reference/config cache, krátkodobé notifikační a presence stavy, anti-abuse/rate limiting a volitelný SignalR backplane; Redis není primární datastore.
- Docker-first provoz je zaveden od počátku přes `docker/compose.yaml`, init SQL skript pro service databáze, per-service kontejnery, healthchecky, startup dependency chain a oddělené per-service connection stringy.

## 2. Rozdělení na služby a projekty
- `Skolio.WebHost`
- `Skolio.Identity.Domain`, `Skolio.Identity.Application`, `Skolio.Identity.Infrastructure`, `Skolio.Identity.Api`
- `Skolio.Organization.Domain`, `Skolio.Organization.Application`, `Skolio.Organization.Infrastructure`, `Skolio.Organization.Api`
- `Skolio.Academics.Domain`, `Skolio.Academics.Application`, `Skolio.Academics.Infrastructure`, `Skolio.Academics.Api`
- `Skolio.Communication.Domain`, `Skolio.Communication.Application`, `Skolio.Communication.Infrastructure`, `Skolio.Communication.Api`
- `Skolio.Administration.Domain`, `Skolio.Administration.Application`, `Skolio.Administration.Infrastructure`, `Skolio.Administration.Api`
- `Skolio.Frontend`

## 3. Project references
- `*.Application -> *.Domain`
- `*.Infrastructure -> *.Application + *.Domain`
- `*.Api -> *.Application + *.Infrastructure`
- `Skolio.WebHost` bez reference na business služby
- Žádné cross-service reference mezi Identity/Organization/Academics/Communication/Administration

## 4. Package dependencies (stack lock)
- `Microsoft.EntityFrameworkCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `OpenIddict.AspNetCore`
- `OpenIddict.EntityFrameworkCore`
- `MediatR`
- `Mapster`
- `Hangfire.AspNetCore`
- `Hangfire.PostgreSql`
- `Microsoft.Extensions.Caching.StackExchangeRedis`
- `StackExchange.Redis`

## 5. Hlavní doménové moduly
- Identity a uživatelé: účty, role, rodič-dítě/rodič-student vazby, autentizace/autorizační hranice.
- Organizace školy: školy, typy škol `Kindergarten/ElementarySchool/SecondarySchool`, školní roky, ročníky, třídy, skupiny, předměty, obory, přiřazení učitelů.
- Výuka a provoz: rozvrh, lesson záznamy, domácí úkoly, denní přehledy, provozní denní evidence.
- Hodnocení a docházka: známky, docházka, omluvenky, denní školní evidence dle typu školy.
- Komunikace: oznámení, zprávy, notifikace, realtime komunikační hranice.
- Administrace: audit log, systémová nastavení, feature toggles, školní roky lifecycle, housekeeping.

## 6. Typy škol a pravidla modelu
- `Kindergarten` je modelována primárně přes skupiny, denní evidenci, docházku, komunikaci s rodiči a denní reporty; není derivací modelu secondary školy.
- `ElementarySchool` pracuje s třídami, předměty, rozvrhem, docházkou, známkami, úkoly a rodičovskou komunikací.
- `SecondarySchool` rozšiřuje elementary model o obory, ročníky a širší studijní evidenci školní agendy.
- University model, kredity, semestry a enrollment workflow jsou mimo scope a nejsou součástí návrhu.

## 7. Docker struktura a compose služby
- Služby: `webhost`, `identity-api`, `organization-api`, `academics-api`, `communication-api`, `administration-api`, `frontend`, `postgresql`, `redis`.
- Persistované volumes: `postgresql_data`, `redis_data`.
- Init databází: `docker/postgresql/init/01-create-databases.sql`.
- Migration workflow: každá služba spravuje vlastní migrace proti své databázi, orchestrace běží před startem API v deployment pipeline.

## 8. Redis návrh
- Distribuovaná cache read-modelů dashboardů per role a typ školy.
- Cache referenčních číselníků a systémové konfigurace.
- Krátkodobé presence/notifikační stavy pro komunikační scénáře.
- Anti-abuse/rate-limiting storage.
- Volitelný SignalR backplane při horizontálním škálování `Skolio.Communication.Api`.

## 9. Razor host bridge model pro React
- `Skolio.WebHost` obsluhuje host page `Pages/AppHost.cshtml`, bootstrap JSON a SPA fallback routing.
- V developmentu host page načítá Vite klienta a React entrypoint.
- V produkci host page načte build výstup `Skolio.Frontend` publikovaný do `wwwroot/app`.
- Razor neobsahuje business obrazovky a neduplikuje React UI.
