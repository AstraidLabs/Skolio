# Phase 24: Organization School Formal Identity Extension

## Decision
School formal identity is modeled as three explicit entities inside `Skolio.Organization`:
- `School` (school institution identity and operations)
- `SchoolOperator` (legal entity operating the school)
- `Founder` (founder authority)

No new service module, no parallel frontend module, and no ERP/HR/payroll scope was introduced.

## Why entities are separated
`School` represents school-level operational identity data and references both legal owner contexts via foreign keys.
`SchoolOperator` keeps legal entity registration minimum for operator-level context.
`Founder` keeps controlled founder type/category and founder identity minimum.
This avoids flattening all legal context into one oversized school record and removes free-text founder/operator drift.

## Data added
### School
- `Name`, `SchoolType`, `SchoolKind`
- `SchoolIzo`, `SchoolEmail`, `SchoolPhone`, `SchoolWebsite`
- `MainAddress` (street, city, postal code, country)
- `EducationLocationsSummary`
- `RegistryEntryDate`, `EducationStartDate`
- `MaxStudentCapacity`
- `TeachingLanguage`
- `SchoolOperatorId`, `FounderId`
- `PlatformStatus`

### SchoolOperator
- `LegalEntityName`
- `LegalForm`
- `CompanyNumberIco`
- `RegisteredOfficeAddress`
- `ResortIdentifier`
- `DirectorSummary`
- `StatutoryBodySummary`

### Founder
- `FounderType` (controlled enum)
- `FounderCategory` (controlled enum)
- `FounderName`
- `FounderLegalForm`
- `FounderIco`
- `FounderAddress`
- `FounderEmail`

## Controlled founder type model
Implemented controlled founder type values:
- `State`
- `Region`
- `Municipality`
- `AssociationOfMunicipalities`
- `Church`
- `PrivateLegalEntity`
- `NaturalPerson`

## Backend extension
Extended in boundary:
- `Skolio.Organization.Domain`: new entities/enums/value object + `School` extension
- `Skolio.Organization.Application`: new contracts and school command payload extension
- `Skolio.Organization.Infrastructure`: EF configuration, DbContext extension, seed update, migration extension bootstrap
- `Skolio.Organization.Api`: schools endpoint contract expansion, role-aware update flow, validation

No business ownership transfer from Academics/Administration was done.

## Frontend extension
Extended existing `Skolio.Frontend` Organization flow:
- no new route or module
- schools view now includes school identity card, operator card and founder card inside existing schools flow
- role-aware editing (`PlatformAdministrator`, `SchoolAdministrator`) and read-only display for others
- i18n keys added for new school/operator/founder labels and controlled enum labels

## Role access boundaries
- `PlatformAdministrator`: create/update `School`, `SchoolOperator`, `Founder`
- `SchoolAdministrator`: read and update within scoped school context
- `Teacher`, `Parent`, `Student`: read-only in allowed scope, no administrative edit path

## Migration
Added migration:
- `20260309192000_Phase24SchoolOperatorFounderSeparation`

It adds school identity fields and new tables `school_operators`, `founders`, with explicit relations from `schools`.
The migration is additive and non-destructive.

## Seed and development compatibility
Development seeding now ensures:
- seeded schools have formal identity fields populated
- each seeded school has linked `SchoolOperator` and `Founder`
- conservative defaults for controlled enums and operational summary values

Migration bootstrap for existing EnsureCreated schemas was added to avoid destructive transition and keep existing development data operable.

## Out of scope (kept outside this phase)
- ERP module
- generic registry/master-data engine
- HR/payroll
- commercial register implementation
- university model
- credits/semesters/enrollment
- tests/quizzes/assessment/exams/automated grading
- second frontend
