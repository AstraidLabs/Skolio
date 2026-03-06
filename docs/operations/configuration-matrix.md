# Skolio — Configuration Matrix (Release Candidate)

## Povinné runtime proměnné per service

### Identity API
- `Identity__Database__ConnectionString`
- `Identity__Redis__ConnectionString`
- `Identity__Redis__InstanceName`
- `Identity__Service__PublicBaseUrl`
- `Identity__OpenIddict__Issuer`
- `Identity__Jwt__AccessTokenLifetime`
- `Identity__Jwks__DiscoveryPath`
- `Identity__Jwks__KeySetId`
- `Identity__Signing__UseDevelopmentCertificate` (jen Development)
- `Identity__Signing__KeyId`

### Organization API
- `Organization__Database__ConnectionString`
- `Organization__Redis__ConnectionString`
- `Organization__Redis__InstanceName`
- `Organization__Service__PublicBaseUrl`
- `Organization__Auth__Authority`
- `Organization__Auth__Audience`

### Academics API
- `Academics__Database__ConnectionString`
- `Academics__Redis__ConnectionString`
- `Academics__Redis__InstanceName`
- `Academics__Service__PublicBaseUrl`
- `Academics__Auth__Authority`
- `Academics__Auth__Audience`

### Communication API
- `Communication__Database__ConnectionString`
- `Communication__Redis__ConnectionString`
- `Communication__Redis__InstanceName`
- `Communication__Service__PublicBaseUrl`
- `Communication__Auth__Authority`
- `Communication__Auth__Audience`

### Administration API
- `Administration__Database__ConnectionString`
- `Administration__Redis__ConnectionString`
- `Administration__Redis__InstanceName`
- `Administration__Service__PublicBaseUrl`
- `Administration__Auth__Authority`
- `Administration__Auth__Audience`
- `Administration__Hangfire__StorageConnectionString`
- `Administration__Hangfire__SchemaName`

### WebHost
- `ServiceEndpoints__IdentityApi`
- `ServiceEndpoints__OrganizationApi`
- `ServiceEndpoints__AcademicsApi`
- `ServiceEndpoints__CommunicationApi`
- `ServiceEndpoints__AdministrationApi`
- `ServiceEndpoints__OidcClientId`
- `ServiceEndpoints__OidcRedirectUri`
- `ServiceEndpoints__OidcPostLogoutRedirectUri`
- `ServiceEndpoints__OidcScope`

## Development-only vs production-required
- Development-only: `*__Signing__UseDevelopmentCertificate=true`, lokální connection strings, lokální redirect URIs.
- Production-required: produkční signing key management, produkční DB/Redis endpoints, veřejně správné `PublicBaseUrl` a OIDC issuer.
- Zakázané: ukládání produkčních secretů přímo do repozitáře.
