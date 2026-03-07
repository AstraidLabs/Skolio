# PHASE 14 SCOPE GUARD

Fáze 14 uzamyká roli `Student` jako konzervativní self-service roli pouze pro vlastní data.

Student je v této fázi omezen na:
- vlastní školní kontext
- vlastní studijní a provozní pøehled
- povolené komunikaèní kanály ve vlastním scope

Student není:
- PlatformAdministrator
- SchoolAdministrator
- Teacher
- Parent

Student nesmí:
- mìnit attendance, grades, homework, lesson records ani daily reports
- mìnit role assignments a parent-student links
- mìnit school/platform settings, feature toggles, lifecycle nebo housekeeping policy
- provádìt admin override zásahy
- èíst cizí studentská nebo rodièovská data

Fáze 14 explicitnì NEVRACÍ následující scope:
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
