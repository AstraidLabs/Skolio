# PHASE 12 SCOPE GUARD

Fáze 12 uzamyká roli `Teacher` jako pedagogickou a provozní roli v rámci:
- teacher assignments
- school-boundary scope
- school year lifecycle omezení

Teacher není:
- PlatformAdministrator
- SchoolAdministrator
- Parent
- Student

Teacher nesmí:
- měnit platform/system settings
- měnit feature toggles
- spravovat global role assignments
- dělat platformové override zásahy

Fáze 12 explicitně NEVRACÍ následující scope:
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
