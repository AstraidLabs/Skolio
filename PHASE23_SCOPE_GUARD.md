# PHASE23_SCOPE_GUARD

Faze 23 zavadi identity security self-service pouze v Identity boundary:
- change password
- forgot password / reset password
- change email
- MFA management (TOTP + recovery codes)
- security notifications pres Email Gateway

Faze 23 explicitne potvrzuje:
- `My Profile` je business profil
- `Security` je samostatna identity/security sekce
- self-service je self-only (uzivatel meni pouze vlastni security udaje)
- Identity pouziva `Skolio.EmailGateway.Api` pro security email delivery

Faze 23 explicitne NEVRACI:
- external identity providers
- SMS MFA
- obecny account center mimo Identity boundary
- role request workflow
- school assignment self-management
- parent-student link self-management
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
- credits
- semesters
- subject enrollment
