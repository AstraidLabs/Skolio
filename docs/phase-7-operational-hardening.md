# Skolio Phase 7 - Operational hardening

## Service operational model
- Backend remains service-based (Identity, Organization, Academics, Communication, Administration), each service keeps its own Clean Architecture layers.
- React remains the single business frontend, while `Skolio.WebHost` remains a host bridge for bootstrap config and static asset fallback.
- PostgreSQL remains the only primary data store with isolated databases per service (`skolio_identity`, `skolio_organization`, `skolio_academics`, `skolio_communication`, `skolio_administration`).
- Redis remains auxiliary for distributed cache and anti-abuse support, never as source of truth.

## Health and readiness model
- Every API now exposes `/health/live` and `/health/ready`.
- Readiness includes EF Core DbContext checks against PostgreSQL and Redis connectivity checks where Redis is configured.
- Administration readiness adds Hangfire readiness probing via monitoring API availability.
- WebHost exposes `/health/live` and `/health/ready` for bridge-level runtime status.

## Migration workflow
- Migrations are applied automatically only in Development environment per service startup.
- Production startup does not run implicit destructive migration flow.
- Migration execution remains service-local (`Apply*MigrationsAsync` per service) with no cross-service SQL coupling.
- Runtime migrations are logged through standard startup logs.

## Logging and correlation model
- APIs and WebHost now emit startup and shutdown logs with service/environment tags.
- Request pipeline adds `X-Correlation-Id` propagation and logging scope enrichment (`Service`, `Environment`, `CorrelationId`).
- Global exception handling returns Problem Details with `correlationId` extension.
- Sensitive data (tokens/passwords) is not logged by hardening changes.

## Rate limiting and Redis model
- `Skolio.Identity.Api` has stricter rate limit partition for `/connect/*` auth endpoints and broader default partition for remaining traffic.
- `Skolio.Communication.Api` applies fixed-window rate limiting on controllers to protect message/notification-heavy paths.
- Redis key ownership remains per-service via configured `InstanceName` prefixes.
- Redis connections are hardened with conservative reconnect settings (`AbortOnConnectFail=false`, retries, timeout).

## Housekeeping boundary
- Administration keeps Hangfire PostgreSQL storage and recurring housekeeping job (`administration-housekeeping-boundary`, every 6 hours).
- Hangfire dashboard is protected for authenticated `PlatformAdministrator` only.
- Housekeeping remains technical/operational (no business workflow engine expansion).

## Deferred scope after Phase 7
- No analytics/reporting engine.
- No event bus or event-driven redesign.
- No file storage/attachments.
- No external integrations.
- No Kubernetes-only runtime redesign.

## Scope guard confirmation
- No assessment/testing/exam workflows were introduced.
- No university model, credits, semesters, or subject enrollment were introduced.
- Supported school model remains: Kindergarten, ElementarySchool, SecondarySchool.
