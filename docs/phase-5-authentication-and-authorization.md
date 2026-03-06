# Skolio Phase 5 - Authentication and Authorization

## Technical decision summary
- Identity server is implemented in `Skolio.Identity.Api` with ASP.NET Identity + OpenIddict (Authorization Code + PKCE).
- Refresh tokens are explicitly disabled (`IssueRefreshTokens=false`) for lower complexity in SPA phase.
- Access token payload is minimal: `sub`, `email`, `role`, standard OIDC claims, and no business profile data.
- School context remains in business data (`SchoolRoleAssignment`, `ParentStudentLink`) and is not expanded into token claim sets.
- Protected APIs validate JWT via issuer, audience and JWKS through configured authority.

## Roles and policies
System roles:
- PlatformAdministrator
- SchoolAdministrator
- Teacher
- Parent
- Student

Conservative API policies:
- `platform-administration`
- `school-administration`
- `teacher-or-school-administration`
- `parent-student-teacher-read`

## Frontend flow
- React frontend redirects user to `/connect/authorize` with PKCE.
- Callback route `/auth/callback` exchanges code at `/connect/token`.
- Access token is persisted in `sessionStorage` only.
- Logout clears frontend session and redirects to `/connect/logout`.

## Local development seed
- Seeds minimal default OpenIddict SPA client `skolio-frontend`.
- Seeds core role set.
- Seeds one local development PlatformAdministrator user.

## Explicit Phase 6 deferrals
- No external identity providers.
- No MFA.
- No self-service registration.
- No password reset workflow.
- No cross-service orchestration/event bus.
- No additional frontend besides React and Razor host bridge.

## Scope guard
- Domain remains Kindergarten / ElementarySchool / SecondarySchool only.
- No university model, credits, semesters, subject enrollment.
- No testing/assessment/exam workflows or related engines.
