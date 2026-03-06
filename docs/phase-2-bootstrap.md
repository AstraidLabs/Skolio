# Skolio Phase 2 - Technical Bootstrap

## Implementováno
- Dokončen composition root pro všech pět business služeb v Application, Infrastructure a Api vrstvách.
- Přidána registrace MediatR a Mapster scanningu v každé Application vrstvě bez use case handlerů.
- Přidány Infrastructure bootstrapy pro PostgreSQL (Npgsql), Redis a service-specific DbContext shell bez entit.
- Připraveny Api hosty se základním technical root endpointem, routingem, health endpointem a development CORS.
- Communication služba obsahuje SignalR hub skeleton a mapování hub endpointu.
- Administration služba obsahuje Hangfire registraci, server bootstrap a dashboard endpoint.
- Identity služba obsahuje konfigurační sekce pro Identity, OpenIddict, JWT a JWKS s technickými placeholders.
- Skolio.WebHost obsahuje Razor host bridge, bootstrap endpoint pro frontend konfiguraci, SPA fallback a dev/prod shell režim.
- Skolio.Frontend obsahuje React app shell, router shell, typed bootstrap config načítaný z WebHost.
- Doplněn central package management, sjednocení assembly/root namespace nastavení a aktualizovaný Docker Compose bootstrap.

## Není implementováno
- Business doménové entity.
- Use case handlery, business orchestrace a business DTO.
- Business controllery a business endpointy.
- EF Core model konfigurace s entitami ani migrace.
- Identity/OpenIddict flow, token issuance, login/logout.
- Business notifikační workflow ani background job implementace.

## Dál platné boundaries
- Backend zůstává service-based s Clean Architecture po službách.
- Žádné cross-service project references mezi business službami.
- React je jediný frontend, Razor je pouze host bridge.
- Databáze je pouze PostgreSQL (Npgsql), jedna databáze per service.
- Redis je pouze distribuovaná cache/distributed support vrstva, nikoliv primární databáze.
- Podporované segmenty vzdělávání jsou Kindergarten, ElementarySchool a SecondarySchool.

## Odloženo do Fáze 3
- Návrh a implementace doménových modelů jednotlivých služeb.
- První vertikální use case flows přes MediatR handlery.
- Mapster mapování mezi doménou a aplikačními kontrakty.
- EF Core entity model, fluent konfigurace a migrace.
- Business API endpointy pro jednotlivé bounded contexts.
