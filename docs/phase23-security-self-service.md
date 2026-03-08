# Phase 23 - Identity Security Self-Service

## Co je security self-service
Security self-service je samostatna Identity sekce pro technicke bezpecnostni ukony nad vlastni identitou uzivatele.
Neni to business profil a neni to obecne account centrum.

## Oddeleni od business profilu
- `My Profile` zustava business profil (jmeno, preferovany jazyk, telefon, bezpecne profilove udaje)
- `Security` je pouze pro heslo, email login, MFA a recovery codes
- role assignments, school assignments, parent-student links a teacher assignments zustavaji mimo security self-service

## Password change
- endpoint: `POST /api/identity/security/change-password`
- vyzaduje `currentPassword`, `newPassword`, `confirmNewPassword`
- po uspesne zmene probehne update security stamp + refresh sign-in
- audit action: `identity.security.password.changed`
- odesila security notification pres Email Gateway

## Forgot password + reset password
- request endpoint: `POST /api/identity/security/forgot-password`
- completion endpoint: `POST /api/identity/security/reset-password`
- forgot-password vraci generickou odpoved bez user enumeration
- reset pouziva token z ASP.NET Identity, URL-safe transport a expiraci identity tokenu
- audit actions:
  - `identity.security.password.forgot-requested`
  - `identity.security.password.reset-completed`

## Change email
- request endpoint: `POST /api/identity/security/change-email/request`
- confirm endpoint: `POST /api/identity/security/change-email/confirm`
- request vyzaduje re-auth (`currentPassword`)
- finalizace zmeny probehne az po potvrzeni tokenu
- stara adresa dostane security notification o zmene
- audit actions:
  - `identity.security.email-change.requested`
  - `identity.security.email-change.confirmed`

## MFA management (TOTP + recovery codes)
- status endpoint: `GET /api/identity/security/mfa/status`
- setup start: `POST /api/identity/security/mfa/setup/start`
- setup confirm: `POST /api/identity/security/mfa/setup/confirm`
- disable: `POST /api/identity/security/mfa/disable`
- regenerate recovery codes: `POST /api/identity/security/mfa/recovery-codes/regenerate`
- flow je pouze TOTP authenticator app + recovery codes
- SMS MFA, external providers a hardware keys nejsou soucasti scope

## Re-auth boundary
Re-authentication je vyzadovana pro citlive akce:
- change email request
- disable MFA
- regenerate recovery codes

## Email Gateway napojeni
Identity neposila SMTP primo.
Identity vola `Skolio.EmailGateway.Api` pres `IIdentityEmailSender` adapter:
- password reset
- change email verification
- password changed notification
- MFA changed notification
- recovery-code regeneration notification

## Frontend obrazovky
- `/identity/security` - Security overview + change password + change email + MFA management
- `/security/forgot-password` - forgot password request
- `/security/reset-password` - reset password completion
- `/security/confirm-email-change` - confirm change email

`My Profile` a `Security` jsou oddelene route i UX.

## Rate limiting a abuse guard
Identity API ma pojmenovane limitery:
- `identity-security-forgot-password`
- `identity-security-reset-password`
- `identity-security-change-email`
- `identity-security-mfa-verify`

## Co zustava mimo scope
- external identity providers
- SMS MFA
- novy auth framework
- obecny account center mimo Identity boundary
- role/school/link self-management
