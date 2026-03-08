# Organization UI Body Landing - Skolio.Frontend

## Boundary
- Sidebar zůstává jediná hlavní navigace.
- `Organization` body část není druhé menu ani druhý sidebar.
- V body části není nested stromová navigace.

## Entry points v Organization body
- Přehled
- Školy
- Přehled školních roků
- Ročníky
- Třídy
- Skupiny
- Předměty
- Teacher Assignments

## Proč `Ročníky` místo `Klasifikace`
- `Ročníky` jsou organizační struktura školy.
- `Klasifikace` (grading) patří do `Academics`.
- Organization neobsahuje grading/assessment významy.

## Přechody na sub-stránky
- Každý blok v landing části má lehké CTA `Spravovat`.
- CTA přechází na existující sub-routes.
- Bez duplikace sidebar menu v content/body.

## Scope guard
- Do Organization se nevrací grading/assessment významy.
- Zakázané oblasti zůstávají mimo scope: tests, quizzes, assessment, exams, online testing, question bank, automated grading, scoring, university model, credits, semesters, subject enrollment.
