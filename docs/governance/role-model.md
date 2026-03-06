# Skolio — Role Model (Phase 8)

## PlatformAdministrator
- Moduly: plný přístup do všech API, zejména Administration a cross-school governance.
- Write: feature toggles, lifecycle policies, housekeeping policies, audit investigations.
- Dashboard: provozní přehledy všech škol a služeb.
- Guard: policy-based authorization s nejvyšším oprávněním.

## SchoolAdministrator
- Moduly: Organization, Academics, Communication, Identity business flows v rámci své školy.
- Write: school-level entity management, role assignment (school context), provozní nastavení.
- Dashboard: školní provozní stav, read-heavy seznamy s filtrováním.
- Guard: policy-based authorization omezené školním kontextem.

## Teacher
- Moduly: Academics + Communication + vlastní profil.
- Write: attendance, grades, homework, lessons/daily reports dle typu školy.
- Dashboard: třídy/skupiny, nevyřízené záznamy, unread komunikace.
- Guard: policy-based authorization pouze pro teacher scénáře.

## Parent
- Moduly: Communication + read modely dítěte + vlastní profil.
- Write: participace v konverzacích v povoleném kontextu.
- Dashboard: notifikace, přehled vazeb parent-student.
- Guard: parent přístup vychází z business vazby, ne z token payload dump.

## Student
- Moduly: vlastní akademické read surfaces + Communication + vlastní profil.
- Write: omezené na komunikační scénáře dle policy.
- Dashboard: osobní přehled záznamů, unread komunikace.
- Guard: policy-based authorization s minimálním oprávněním.
