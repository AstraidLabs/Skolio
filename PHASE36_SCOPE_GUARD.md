# PHASE 36 – Create User Wizard

## Fáze

Phase 36: Create User Flow Wizard v User Management

---

## Co bylo implementováno

### 1. Backend – nové endpointy v `IdentityUserManagementController`

**`GET /api/identity/user-management/create-wizard/student-candidates?schoolId=...`**
- Vrací studenty dostupné pro propojení při vytváření rodiče (Parent role)
- Scope: SchoolAdministrator vidí jen žáky svých škol; PlatformAdministrator vidí všechny (nebo filtrované podle schoolId)
- Maximálně 200 výsledků, řazeno podle jména

**`POST /api/identity/user-management/create-wizard`**
- Atomický endpoint pro celý wizard flow
- Pořadí operací:
  1. Validace requestu (email, username, role, school scope)
  2. Uniqueness check pro email a username
  3. Vytvoření `SkolioIdentityUser` přes `UserManager.CreateAsync` (bez hesla)
  4. Vytvoření `UserProfile` s rolově-specifickým `UserType`
  5. Vytvoření `SchoolRoleAssignment` (pokud schoolId je relevantní pro danou roli)
  6. Vytvoření `ParentStudentLink` (pokud role=Parent)
  7. Přiřazení Identity role přes `UserManager.AddToRoleAsync`
  8. Zpracování activation policy (SendActivationEmail nebo CreateActive)
  9. Audit log každé klíčové operace
- Scope enforcement: SchoolAdministrator může zakládat uživatele pouze ve svém school scope
- PlatformAdministrator role mohou vytvářet pouze PlatformAdministrators

### 2. Frontend – `CreateUserWizard.tsx`

Nová komponenta s 5-krokovým wizard flow.

**Krok 1 – Základní účet**
- Email, Username, FirstName, LastName, DisplayName (nepovinné), PreferredLanguage
- Validace: formát emailu, povinná pole

**Krok 2 – Role a scope**
- Výběr role: SchoolAdministrator, Teacher, Parent, Student (+ PlatformAdministrator pouze pro PlatformAdministrators)
- School scope:
  - PlatformAdministrator: dropdown ze škol (přes Organization API)
  - SchoolAdministrator: zafixováno na jejich školu (session.schoolIds)
- Role-specific info text pro každou roli

**Krok 3 – Profilová data**
- Rolově-relevantní pole:
  - Teacher/SchoolAdministrator: PositionTitle, SchoolContextSummary
  - Student: SchoolPlacement (třída)
  - Parent: ParentRelationshipSummary
- Společná pole: PhoneNumber, ContactEmail

**Krok 4 – Vazby podle role**
- Pro roli Parent: výběr propojeného žáka (ze student candidates) + typ vztahu
- Pro ostatní role: informace, že vazby nejsou vyžadovány
- Krok je automaticky přeskočen pro role bez povinných vazeb

**Krok 5 – Aktivace a dokončení**
- Shrnutí vytvořeného účtu (email, jméno, role)
- Activation policy:
  - `SendActivationEmail` (výchozí, doporučeno): aktivační e-mail přes identity confirmation flow
  - `CreateActive` (jen PlatformAdministrator): účet okamžitě aktivní + password setup email

### 3. Frontend – `IdentityParityPage.tsx`

- Přidáno tlačítko „Vytvořit uživatele" (CTA) v hlavičce User Management sekce
- Wizard se zobrazuje místo gridu (ne modální overlay), po dokončení se grid obnoví
- Success notification po úspěšném vytvoření
- organizationApi předán jako prop pro school lookup

### 4. Frontend – `router.tsx`

- `organizationApi={apis.organization}` předáno do `IdentityParityPage` při user-management mode

### 5. i18n

Přidány překlady pro všech 5 jazyků:
- čeština (cs)
- angličtina (en)
- slovenština (sk)
- němčina (de)
- polština (pl)

Klíče pokrývají: wizard title, step labels, field labels, validace, success/error messages, activation texts.

---

## Kdo může wizard používat

| Role | Přístup k wizardu |
|---|---|
| PlatformAdministrator | Plný přístup, může vytvářet uživatele globálně nebo v libovolném school scope, může přiřadit roli PlatformAdministrator |
| SchoolAdministrator | Omezený přístup, může vytvářet uživatele pouze ve svém school scope, nemůže přiřadit PlatformAdministrator roli |
| Teacher | Nemá přístup |
| Parent | Nemá přístup |
| Student | Nemá přístup |

Enforcement je na backendu – frontend pouze skrývá tlačítko pro neoprávněné role, backend vždy validuje scope.

---

## Activation flow

### SendActivationEmail (výchozí)
1. Admin vytvoří účet v wizardu
2. Backend: `UserManager.CreateAsync` bez hesla → `AccountLifecycleStatus = PendingActivation`
3. Backend: `GenerateEmailConfirmationTokenAsync` → `SendAccountConfirmationAsync`
4. Uživatel obdrží e-mail s potvrzovacím linkem (`/security/confirm-activation`)
5. Po kliknutí: email potvrzen + `AccountLifecycleStatus = Active`
6. Uživatel použije „Zapomenuté heslo" pro nastavení hesla

### CreateActive (pouze PlatformAdministrator)
1. Admin vytvoří účet s `ActivationPolicy = CreateActive`
2. Backend: `UserManager.CreateAsync` → `EmailConfirmed = true`, `AccountLifecycleStatus = Active`, `ActivatedAtUtc = now`
3. Backend: `GeneratePasswordResetTokenAsync` → `SendPasswordResetAsync`
4. Uživatel obdrží e-mail s linkem pro nastavení hesla (`/security/reset-password`)

---

## Vazba na User Management grid

- Wizard se otevře po kliknutí na „Vytvořit uživatele" v hlavičce gridu
- Po úspěšném vytvoření:
  1. Wizard se zavře
  2. Grid se znovu načte filtrovaný na email nového uživatele
  3. Zobrazí se success notification
- Tlačítko „Přejít na detail uživatele" v success screenu otevře detail nově vytvořeného uživatele

---

## School-type aware chování

- Kindergarten, ElementarySchool, SecondarySchool ovlivňují pole dostupná ve Wizard kroku 2 (school picker zobrazuje typ školy)
- Krok 3 (profilová data) zobrazuje jen rolově-relevantní pole
- Krok 4 (vazby) je dynamický podle role

---

## Audit trail

Auditované akce:
- `identity.user-management.create-wizard.user-created`
- `identity.user-management.create-wizard.profile-created`
- `identity.user-management.create-wizard.school-assignment-created`
- `identity.user-management.create-wizard.parent-link-created`
- `identity.user-management.create-wizard.role-assigned`
- `identity.user-management.create-wizard.activation-email-sent`
- `identity.user-management.create-wizard.created-active`
- `identity.user-management.create-wizard.completed`

---

## Proč nejde o self-registration

- Wizard je dostupný pouze přes `/administration/user-management` route
- Route je chráněna rolemi `PlatformAdministrator` nebo `SchoolAdministrator`
- Backend endpoint `POST /api/identity/user-management/create-wizard` vyžaduje `[Authorize(Roles = "PlatformAdministrator,SchoolAdministrator")]`
- Neexistuje žádný veřejný endpoint ani anonymní přístup
- Admin explicitně definuje roli, school scope a activation policy

---

## Co zůstává mimo scope této fáze

Tato fáze NESMÍ a NEZAVÁDÍ:

- Self-registration nebo public signup
- Generic workflow engine nebo DSL pro onboarding
- HR modul ani medical ERP modul
- Vlastní token engine mimo ASP.NET Core Identity
- Vlastní password management mimo Identity
- University/vysokoškolský model (kredity, semestry, zkoušky)
- Assessment, quizzy, testing, automated grading
- Nový business modul mimo Identity boundary
- Druhý frontend nebo nový Razor flow
- Business logika v WebHost projektu
- Přepsání ASP.NET Core Identity capabilities

---

## Technická rozhodnutí

**Atomický backend endpoint**: Celý wizard je odeslán v jednom `POST /create-wizard` requestu. Důvod: jednodušší error handling, konzistentní stav (žádný partial create), jeden transakční audit.

**Žádná draft persistence**: Wizard state je v React state (in-memory). Důvod: wizard je krátký, admin ho dokončí v jednom sezení, persistence by přidala zbytečnou komplexitu.

**School lookup přes Organization API**: School jména jsou v Organization service, ne v Identity. Frontend volá `organizationApi.schools()` pro PlatformAdministrator. Důvod: zachování service boundary bez cross-service backendu.

**SchoolAdministrator fixní scope**: SchoolAdministrator má school scope z `session.schoolIds`. Backend vždy validuje, že SchoolAdministrator nemůže použít cizí schoolId. Důvod: security by design, ne jen UX.

**Krok 4 automaticky přeskočen**: Pro role bez povinných vazeb (Teacher, Student, SchoolAdministrator) je krok 4 přeskočen v navigaci. Důvod: minimalizace kroků a zbytečných klikání.
