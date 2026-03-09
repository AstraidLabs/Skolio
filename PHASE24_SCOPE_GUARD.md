# Phase 24 Scope Guard

This phase is strictly limited to targeted Organization boundary extension for formal school identity.

Allowed in this phase:
- extend `Skolio.Organization` domain, application, infrastructure, API and existing frontend Organization flow
- separate school identity model into `School`, `SchoolOperator`, `Founder`
- add conservative registration and operational school identity fields
- add EF Core migration for this model extension

Explicitly forbidden in this phase:
- new ERP module
- generic registry engine
- commercial register implementation
- HR module
- payroll logic
- university model
- credits
- semesters
- subject enrollment
- tests
- quizzes
- assessment
- exams
- automated grading
- second frontend
