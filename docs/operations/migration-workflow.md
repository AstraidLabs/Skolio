# Skolio — Migration Workflow Policy

## Závazné rozhodnutí
Migration policy:
- Build nikdy nespouští databázové migrace.
- Development/local compose: každá business služba aplikuje své migrace automaticky při startupu.
- Production-like/production: migrace se aplikují řízeně mimo běžící API proces, per service.
- Každá služba migruje pouze svou databázi.
- WebHost nikdy nemigruje business databáze.

## Scope služeb a databází
- `Skolio.Identity.Api` migruje pouze `skolio_identity` přes `IdentityDbContext`.
- `Skolio.Organization.Api` migruje pouze `skolio_organization` přes `OrganizationDbContext`.
- `Skolio.Academics.Api` migruje pouze `skolio_academics` přes `AcademicsDbContext`.
- `Skolio.Communication.Api` migruje pouze `skolio_communication` přes `CommunicationDbContext`.
- `Skolio.Administration.Api` migruje pouze `skolio_administration` přes `AdministrationDbContext`.

## Development / local compose
- V `ASPNETCORE_ENVIRONMENT=Development` každá business API služba při startupu volá vlastní `Database.MigrateAsync()`.
- Startup migration je explicitně service-local, se samostatným loggingem pro start/úspěch/selhání.
- Pokud migrace selže, služba failne startup čitelnou výjimkou.
- WebHost migrace nespouští.

## Production-like / production
- API služby běží s `ASPNETCORE_ENVIRONMENT=Production`, proto startup migrace uvnitř API procesu nejsou aktivní.
- Migrace se spouští samostatným krokem per service přes migration bundle kontejner (`Dockerfile.migrator`).
- V compose variantě `docker/compose.production-like.yaml` má každá služba vlastní migrator kontejner:
  - `identity-migrator`
  - `organization-migrator`
  - `academics-migrator`
  - `communication-migrator`
  - `administration-migrator`
- Odpovídající API služba startuje až po `service_completed_successfully` svého migratoru.

## Generování migration bundle per service
Migration bundle se generuje při buildu migrator image přes `dotnet ef migrations bundle`:

- Identity:
  - project: `src/Services/Identity/Skolio.Identity.Infrastructure/Skolio.Identity.Infrastructure.csproj`
  - startup project: `src/Services/Identity/Skolio.Identity.Api/Skolio.Identity.Api.csproj`
  - context: `IdentityDbContext`
- Organization:
  - project: `src/Services/Organization/Skolio.Organization.Infrastructure/Skolio.Organization.Infrastructure.csproj`
  - startup project: `src/Services/Organization/Skolio.Organization.Api/Skolio.Organization.Api.csproj`
  - context: `OrganizationDbContext`
- Academics:
  - project: `src/Services/Academics/Skolio.Academics.Infrastructure/Skolio.Academics.Infrastructure.csproj`
  - startup project: `src/Services/Academics/Skolio.Academics.Api/Skolio.Academics.Api.csproj`
  - context: `AcademicsDbContext`
- Communication:
  - project: `src/Services/Communication/Skolio.Communication.Infrastructure/Skolio.Communication.Infrastructure.csproj`
  - startup project: `src/Services/Communication/Skolio.Communication.Api/Skolio.Communication.Api.csproj`
  - context: `CommunicationDbContext`
- Administration:
  - project: `src/Services/Administration/Skolio.Administration.Infrastructure/Skolio.Administration.Infrastructure.csproj`
  - startup project: `src/Services/Administration/Skolio.Administration.Api/Skolio.Administration.Api.csproj`
  - context: `AdministrationDbContext`

Každý bundle je izolovaný na jeden DbContext a jednu service databázi.

## Spuštění migrací
### Development
- `docker compose -f docker/compose.yaml up --build`
- Migrace proběhnou při startupu business API služeb automaticky.

### Production-like
1. Spusť production-like profil:
   - `docker compose -f docker/compose.yaml -f docker/compose.production-like.yaml --profile production-like up --build`
2. Nejprve doběhnou migrator kontejnery per service.
3. Až potom startují API služby.

## Connection string konfigurace
- V production-like compose je connection string předán přímo migrator commandu přes `--connection`.
- Connection string je service-specific a cílí pouze na odpovídající databázi.
- API služby používají své standardní `...__Database__ConnectionString` proměnné.

## Proč migrace neběží při buildu
- Build artefakt musí být deterministický a bez side-effectu na runtime databáze.
- Migrace jsou provozní krok závislý na cílovém prostředí a connection stringu.
- Oddělení buildu a migrace drží čisté service boundaries a auditovatelný rollout.
