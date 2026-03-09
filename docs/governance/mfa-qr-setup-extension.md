# MFA QR Setup Extension (Identity Security)

## Rozsah úpravy
- Rozšíøení je provedeno pouze v existujícím flow `Skolio.Identity` + `Skolio.Frontend` na existující Security stránce.
- MFA setup novì vrací backend payload pro QR setup (`otpauth://` URI + issuer + account label) a frontend renderuje QR kód v sekci Dvoufázové ovìøení.
- Potvrzení MFA zùstává pøes verifikaèní TOTP kód a recovery codes se zobrazují až po úspìšném potvrzení setupu.

## Backend autorita
- MFA secret generuje výhradnì backend (`IdentitySecurityController.StartMfaSetup`).
- Frontend secret negeneruje ani neupravuje, pouze zobrazuje QR kód z backend `authenticatorUri`.
- Audit zùstává na akcích: start MFA setup, potvrzení setupu, disable MFA, regenerate recovery codes.
- Citlivé hodnoty se nelogují v plné podobì.

## Frontend QR render
- QR render je doplnìn do existující karty `Dvoufázové ovìøení`, bez nové route a bez nové stránky.
- Použit je lehký balíèek `qrcode` pro klientské vykreslení PNG data URL.
- UI obsahuje: scan QR krok, fallback setup key, verifikaèní kód, potvrzení aktivace.
- UX zachovává stávající card patterny stránky Security.

## Recovery codes návaznost
- Recovery codes se stále generují po úspìšném potvrzení MFA setupu.
- Regenerace recovery codes zùstává dostupná pøes existující endpoint s re-auth pravidlem.

## Scope And Boundary Guard
Tato fáze výslovnì nezavádí:
- SMS MFA
- external identity providers
- hardware token modul
- nový auth framework
- nový business modul
- nový frontend framework
- novou samostatnou security stránku
- tests, quizzes, assessment, exams, automated grading
- university model
- druhý frontend
