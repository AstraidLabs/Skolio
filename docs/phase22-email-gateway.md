# Phase 22 - Skolio.EmailGateway.Api

## Proc nova sluzba vznikla
Skolio.EmailGateway.Api je samostatna technicka delivery vrstva pro identity/security email scenare.

## Presny ucel
Sluzba smi dorucovat pouze identity/security emaily:
- PasswordReset
- ChangeEmailVerification
- SecurityNotification
- MfaChanged
- AccountConfirmation

## Co sluzba nesmi delat
- business announcements
- parent/teacher communication
- generic notification hub
- event bus
- SMS/push delivery

## Napojeni na Identity
Skolio.Identity.Api pouziva application port IIdentityEmailSender.
Infrastructure adapter vola Email Gateway pres interni HTTP endpointy a X-Internal-Service-Key.
Identity stale ridi security flow; Email Gateway dela pouze delivery.

## Transport
- EmailGateway API: ASP.NET Core Web API
- Sender: MailKit
- Relay: interni SMTP relay boundary

## Endpointy
- POST /internal/email-gateway/password-reset
- POST /internal/email-gateway/change-email-verification
- POST /internal/email-gateway/security-notification
- POST /internal/email-gateway/mfa-changed
- POST /internal/email-gateway/account-confirmation

## Compose a konfigurace
Compose obsahuje email-gateway-api service.
Identity ma nakonfigurovany BaseUrl, InternalApiKey a timeout pro volani gateway.
Email Gateway ma SMTP konfiguraci, from identity a allowed template typy.

## Health a observability
- /health/live
- /health/ready (SMTP relay check jako degraded pri nedostupnosti)
- structured logging se service/correlation scope

## Mimo scope
Sluzba neresi business messaging, analytics/reporting, event bus, BFF ani GraphQL.
