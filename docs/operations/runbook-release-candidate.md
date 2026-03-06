# Skolio — Release-Ready Runbook (Local + Production-like)

## 1) Lokální spuštění (development compose)
1. Spusť služby: `docker compose -f docker/compose.yaml up --build`.
2. Ověř health endpointy:
   - WebHost: `http://localhost:8080/health/ready`
   - Identity: `http://localhost:8081/health/ready`
   - Organization: `http://localhost:8082/health/ready`
   - Academics: `http://localhost:8083/health/ready`
   - Communication: `http://localhost:8084/health/ready`
   - Administration: `http://localhost:8085/health/ready`
3. Ověř OIDC discovery: `http://localhost:8081/.well-known/openid-configuration`.

## 2) Production-like lokální režim
1. Zachovej stejnou compose topologii, ale přepni proměnné na production-like hodnoty (`ASPNETCORE_ENVIRONMENT`, connection strings, authority/issuer, signing key).
2. Vypni development signing certifikát (`Identity__Signing__UseDevelopmentCertificate=false`).
3. Udrž stejné service boundaries, bez přidání nových runtime komponent.

## 3) Databáze a seed disciplína
- PostgreSQL instance je jednotná, databáze jsou oddělené per service.
- Seed je pouze minimální a provozně účelový; nepoužívá masivní demo data.
- Redis slouží jen jako pomocná runtime technologie.

## 4) Operační kontrolní checklist před release
- Health endpointy všech runtime služeb vrací `ready`.
- OIDC issuer/JWKS odpovídají nasazenému prostředí.
- Admin-only endpointy jsou dostupné pouze rolím `PlatformAdministrator`/`SchoolAdministrator` podle policy.
- School-year lifecycle omezení a feature toggle enforcement jsou aktivní.
- React frontend běží jako jediná business UI vrstva přes WebHost bridge.
