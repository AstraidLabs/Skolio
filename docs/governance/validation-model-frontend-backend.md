# Validation Model Frontend/Backend

## Boundary
- Backend is the only source of truth for validation.
- Frontend performs only immediate UX validation.
- Frontend validation never replaces backend validation.
- Every request is validated again on backend even when frontend already validated form inputs.

## Frontend Validation Scope
- Required fields.
- Min/max length.
- Basic formats (email, phone, simple numeric/date ranges).
- Simple cross-field checks with immediate UX value.
- Submit disable when obvious invalid state exists.

## Backend Validation Scope
- Required/min/max/format validation.
- Domain invariants.
- School context, role/scope and ownership validation.
- Lifecycle and existence checks.
- Security-sensitive validation and authorization-related checks.

## Backend Source of Truth Placement
- Domain: invariants and domain rules.
- Application: command/query and use-case validation.
- API: receives request and returns standardized validation payload.
- Infrastructure: never authoritative validation source.

## Standard Validation Error Response
All service APIs return `ValidationProblemDetails` for validation failures.

- HTTP status: `400`
- `title`: `Validation failed.`
- `errors`: dictionary `fieldKey -> [messages]`
- Optional form-level key: `$form`

Example shape:

```json
{
  "title": "Validation failed.",
  "status": 400,
  "errors": {
    "email": ["Invalid email format."],
    "$form": ["Unable to save changes."]
  }
}
```

## Frontend Mapping Model
- Field-level errors are mapped from `errors[field]`.
- Form-level errors are mapped from `errors["$form"]` (and empty/global keys).
- Frontend keeps one shared mapping helper for backend validation payload.
- Frontend shows inline field errors and a form-level error area.

## Parity Rules
- Same required fields are required on frontend and backend.
- Same basic max length and format checks are mirrored on frontend.
- Complex business rules remain backend-only.
- Backend validation messages are mapped consistently into feature forms.

## Applied Services
- `Skolio.Identity.Api`
- `Skolio.Organization.Api`
- `Skolio.Academics.Api`
- `Skolio.Communication.Api`
- `Skolio.Administration.Api`
- `Skolio.Frontend`

## Forms Covered in This Phase
- My Profile
- Security (Change Password, Change Email)
- Parent self-service excuses
- Existing API-bound forms now consume standardized backend validation payload

## Out of Scope
This phase does not introduce:
- Generic platform-wide validation framework.
- Shared cross-service business validation library.
- Generic mega form engine.
- New business modules.
- Tests/quizzes/assessment/exams/automated grading.
- University model.
