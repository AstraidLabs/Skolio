# Phase 26 - MFA Login Challenge Flow

## Rozsah
- Implementace rozšiøuje stávající login flow v `Skolio.Identity.Api` a stávající login UI ve `Skolio.Frontend`.
- Nevzniká nový auth framework, nový identity provider ani paralelní login backend.

## Login + MFA rozdìlení
1. Primary authentication:
- uživatel zadá username/e-mail a heslo do `/account/login`
- pøi neplatných credentials je vrácen login error stav
- pøi platných credentials bez MFA je uživatel ihned pøihlášen

2. Secondary MFA challenge:
- pøi platných credentials s aktivním MFA se vytvoøí èasovì omezený challenge
- frontend pøejde do MFA kroku na stejné login stránce
- uživatel potvrdí TOTP kód nebo recovery code pøes `/account/login/mfa/verify`
- až po úspìchu probìhne finální sign-in a návrat do OIDC `returnUrl`

## Challenge model
- challenge je uložen server-side v `IMemoryCache`
- challenge je svázaný s konkrétním login pokusem (`challengeId`)
- challenge expiruje po 5 minutách
- po úspìchu, expiraci nebo pøekroèení poètu pokusù je zneplatnìn
- challenge není znovupoužitelný

## Recovery code login
- MFA verify endpoint podporuje `useRecoveryCode=true`
- validace recovery code bìží pøes ASP.NET Identity (`RedeemTwoFactorRecoveryCodeAsync`)
- použitý recovery code je po úspìchu zneplatnìn Identity frameworkem

## OIDC/OpenIddict integrace
- token issuance pøes OpenIddict se nemìní
- bez dokonèení MFA nedojde k finálnímu pøihlášení do `IdentityConstants.ApplicationScheme`
- OIDC/JWT tokeny vznikají až po úplném dokonèení login flow

## Abuse protection a audit
- rate limiting je doplnìn pro primary login a MFA challenge verify
- audit pokrývá: invalid credentials, MFA required, MFA success, MFA fail, recovery success/fail, challenge expiration
- plné MFA/recovery kódy se nelogují

## Co zùstává mimo scope
- SMS MFA
- external identity providers
- hardware token modul
- trusted device / remember device
- nový auth framework
- nový identity provider
