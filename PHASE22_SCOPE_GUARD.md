# PHASE22_SCOPE_GUARD

Faze 22 zavadi samostatnou technickou sluzbu Skolio.EmailGateway.Api pouze pro internal identity/security email delivery.

Faze 22 explicitne potvrzuje:
- Email Gateway je technicka sluzba, ne business modul
- Identity vola Email Gateway pres interni HTTP kontrakt
- Email Gateway pouziva MailKit + interni SMTP relay boundary
- Email Gateway ma uzky template whitelist pro identity/security use cases

Faze 22 explicitne NEVRACI:
- tests
- quizzes
- assessment
- exams
- online testing
- question bank
- automated grading
- scoring
- evaluation workflows
- exam workflow
- assessment engine
- university model
- business communication
- announcements
- parent/teacher conversations
- generic notification hub
- event bus
- SMS gateway
- push notification platform
