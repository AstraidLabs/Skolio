# Organization School Context Switcher - Skolio.Frontend

## Umisteni
- Prepinac skolniho kontextu je page-level v `Organization` content casti.
- Je umisteny nad organizacnim prehledem, pod globalnim shellem.
- Sidebar zustava jedina hlavni navigace.

## Kdo prepinac vidi
- `PlatformAdministrator`: ano, pokud ma vice skolnich kontextu.
- `SchoolAdministrator`: ano, pokud ma prirazeni k vice skolam.
- Jediny kontext: zobrazi se read-only aktivni kontext bez prepinani.
- `Teacher`, `Parent`, `Student`: prepinac neni standardne zobrazovan.

## Chovani prepnuti
- Po prepnuti se zmeni aktivni skola pro Organization page.
- Prenactou se organizacni prehledy, summary bloky a navazujici entry points.
- Aktivni kontext je vzdy viditelny (nazev skoly + typ skoly).
- Prepinac je zamerne page-level, nejde o globalni tenant-switching engine.

## Scope guard
- Nezavadi se globalni tenant-switching engine ani novy multitenancy modul.
- Nevraci se tests, quizzes, assessment, exams, online testing, question bank, automated grading, scoring.
- Nevraci se university model, credits, semesters, subject enrollment.
- Nezavadi se analytics engine, reporting engine, BFF, GraphQL ani druhy frontend.
