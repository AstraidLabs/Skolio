# Organization Module — School Context Matrix

## Source of Truth

| Co | Source of truth |
|----|----------------|
| Identita školy, název, IZO, typ | `Organization` → `School` |
| Právnická osoba (PO) | `Organization` → `SchoolOperator` |
| Zřizovatel | `Organization` → `Founder` |
| Scope kontextu (povolené capability, role, sekce, flow) | `School Context Matrix` → `SchoolContextScopeMatrix` |
| Role jako systémový základ | `Identity` → `IdentityAuthSeeder` |

---

## Organization — co drží a co je odděleno

### Škola (`School`)
Hlavní doménová entita. Drží:
- název školy
- IZO školy
- typ školy (`SchoolType`: Kindergarten, ElementarySchool, SecondarySchool)
- druh školy (`SchoolKind`: General, Specialized)
- kontaktní údaje (email, telefon, web)
- hlavní adresa
- kapacity, datumy, vyučovací jazyk
- platforma status
- vazba na `SchoolOperator` (FK) a `Founder` (FK)

Škola **není** totéž co právnická osoba, která vykonává její činnost. Toto je záměrné oddělení.

### Právnická osoba (`SchoolOperator`)
Právnická osoba provozující školu:
- název PO
- IČO (`CompanyNumberIco`)
- RED_IZO (`RedIzo`) — rejstříkový identifikátor PO
- právní forma (`LegalForm`)
- sídlo (`RegisteredOfficeAddress`)
- email PO (`OperatorEmail`)
- datová schránka (`DataBox`)
- identifikátor resortu, ředitel/statutární zástupce

IZO (školy), RED_IZO a IČO jsou **záměrně oddělené** — různé rejstříkové identifikátory, různé subjekty.

### Zřizovatel (`Founder`)
Entita zřizující školu:
- název zřizovatele
- typ zřizovatele (`FounderType`: Municipality, Region, LegalEntity)
- kategorie (`FounderCategory`: Public, Private)
- IČO zřizovatele
- adresa
- email
- datová schránka (`FounderDataBox`)

Zřizovatel **není** totéž co PO provozující školu. Obec jako zřizovatel neznamená, že obec je PO školy.

---

## Školní struktura

| Entita | Kdo ji vlastní | Přiřazení |
|--------|----------------|-----------|
| `SchoolYear` | `School` | per school |
| `GradeLevel` | `School` | ElementarySchool + SecondarySchool |
| `ClassRoom` | `School` + `GradeLevel` | ElementarySchool + SecondarySchool |
| `TeachingGroup` | `School` | všechny typy; Kindergarten group-centric |
| `Subject` | `School` | ElementarySchool + SecondarySchool |
| `SecondaryFieldOfStudy` | `School` | SecondarySchool only |

---

## School Context Matrix — scope vrstva

`SchoolContextScopeMatrix` **není** paralelní school model. Je to scope vrstva nad existující školní strukturou.

### Co matice určuje
- které strukturální capability jsou povoleny (`SchoolContextScopeCapability`)
- které role jsou v daném kontextu school-scoped (`SchoolContextScopeAllowedRole`)
- které profile sections jsou dostupné (`SchoolContextScopeAllowedProfileSection`)
- které create-user flows jsou dostupné (`SchoolContextScopeAllowedCreateUserFlow`)
- které user management flows jsou dostupné (`SchoolContextScopeAllowedUserManagementFlow`)
- které organization sections jsou zobrazeny (`SchoolContextScopeAllowedOrganizationSection`)
- které academics sections jsou dostupné (`SchoolContextScopeAllowedAcademicsSection`)

### Co matice neurčuje
- identitu školy (název, IZO, typ)
- auth, MFA, lifecycle uživatele
- bootstrap
- invite tokeny, recovery tokeny
- password policy

### Capability kódy

| Code | Kindergarten | ElementarySchool | SecondarySchool |
|------|:---:|:---:|:---:|
| UsesClasses | ✗ | ✓ | ✓ |
| UsesGroups | ✓ | ✓ | ✓ |
| UsesSubjects | ✗ | ✓ | ✓ |
| UsesFieldOfStudy | ✗ | ✗ | ✓ |
| UsesDailyReports | ✓ | ✗ | ✗ |
| UsesAttendance | ✓ | ✓ | ✓ |
| UsesGrades | ✗ | ✓ | ✓ |
| UsesHomework | ✗ | ✓ | ✓ |

---

## N škol — podpora více škol

- `School` **není** singleton — platforma podporuje N škol
- Více škol může být stejného typu (více Kindergarten)
- Každý `SchoolType` má jeden výchozí `SchoolContextScopeMatrix` (3 záznamy celkem)
- Škola může mít volitelný `SchoolScopeOverride` pro lokální omezení capability
- Override může capability pouze **zakázat** (ne aktivovat), a to jen pokud je v defaultní matici povolena

### Dvouvrstvý model
```
SchoolType (Kindergarten | ElementarySchool | SecondarySchool)
    └─► SchoolContextScopeMatrix (default per type)
            └─► SchoolContextScopeCapability (8 rows)
            └─► SchoolContextScopeAllowedRole (school-scoped roles)
            └─► SchoolContextScopeAllowed* (sections, flows)
School
    └─► SchoolScopeOverride (volitelné, nullable bool per capability)
```

---

## Resolved School Context

Query `GetResolvedSchoolContextQuery(schoolId)` vrátí `ResolvedSchoolContextContract`:
1. Načte školu → SchoolType
2. Načte defaultní matrix pro daný SchoolType (se všemi Include)
3. Zkontroluje existenci `SchoolScopeOverride` pro schoolId
4. Aplikuje override (null = default z matice, false = zakázat)
5. Vrátí resolved DTO s bool flagy + listy povolených kódů

Frontend i backend musí řídit chování přes resolved kontext — ne přes hardkódovaný switch na SchoolType.

---

## Seed — co připravuje, co nevytváří

### Co seed připravuje
- 3 právnické osoby (`SchoolOperator`)
- 3 zřizovatele (`Founder`)
- 3 školy (Kindergarten, ElementarySchool, SecondarySchool)
- školní roky, ročníky, třídy, skupiny, předměty, obory
- `SchoolContextScopeMatrix` — 3 záznamy (jeden per SchoolType)
- `SchoolContextScopeCapability` — 24 záznamy (8 per matrix)
- `SchoolContextScopeAllowedRole`, `*ProfileSection`, `*CreateUserFlow`, `*UserManagementFlow`, `*OrganizationSection`, `*AcademicsSection`

### Co seed **nevytváří**
- žádné uživatelské účty
- žádné `PlatformAdministrator`
- žádné development login účty
- žádné invite tokeny
- žádné `SchoolScopeOverride` záznamy (overrides jsou volitelné per-school, zakládají se provozně)

### První PlatformAdministrator
Vzniká výhradně přes **bootstrap flow** — ne seedem.

### Další uživatelské účty
Vznikají výhradně přes **User Management / Create User Wizard** — ne seedem.

---

## Seed — spuštění a vypnutí

### Spuštění
Seed se spouští automaticky při startu aplikace, pokud není zakázán.

```json
// appsettings.json
{
  "Organization": {
    "Seed": {
      "Enabled": true
    }
  }
}
```

### Vypnutí
```json
{
  "Organization": {
    "Seed": {
      "Enabled": false
    }
  }
}
```

Nebo přes environment variable:
```
Organization__Seed__Enabled=false
```

### Seed je idempotentní
- Plně inicializovaná DB → seed se bezpečně ukončí bez změn
- Prázdná DB → seed vytvoří všechna povinná data
- Částečně inicializovaná DB → seed doplní chybějící data
- Nekonzistentní DB → seed failne s chybou

---

## Co se záměrně neduplikuje

- `School` drží název, IZO, typ — matice je **neopakuje**
- `SchoolContextScopeMatrix` drží scope logiku — škola ho **nenahrazuje**
- `Identity` drží role engine — matice jen určuje, které role jsou v kontextu povolené
- Zřizovatel a PO jsou oddělené entity — nejsou sloučeny do jedné položky

---

## Scope guard — co tato fáze nezavádí

- Paralelní school model vedle `Organization` ❌
- Generic config engine ❌
- Generic rules engine ❌
- Low-code konfigurátor ❌
- JSON blob jako source of truth ❌
- Uživatelské účty v seedu ❌
- Default PlatformAdministrator ❌
- University model (kredity, semestry, zkoušky, assessment) ❌
- Tests, quizzes, exams, automated grading ❌
- Druhý frontend ❌
- Business logika ve WebHostu ❌
