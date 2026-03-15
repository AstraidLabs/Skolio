export type UserRole = 'PlatformAdministrator' | 'SchoolAdministrator' | 'Teacher' | 'Parent' | 'Student';
export type SchoolType = 'Kindergarten' | 'ElementarySchool' | 'SecondarySchool';

export type SessionState = {
  accessToken: string;
  refreshToken: string | null;
  expiresAtUtc: number;
  subject: string;
  roles: string[];
  schoolType: SchoolType;
  schoolIds: string[];
  linkedStudentIds: string[];
};

const sessionStorageKey = 'skolio.auth.session';
const pkceVerifierStorageKey = 'skolio.auth.pkce';

export function loadSession(): SessionState | null {
  const raw = sessionStorage.getItem(sessionStorageKey);
  if (!raw) return null;
  return JSON.parse(raw) as SessionState;
}

export function persistSession(session: SessionState) {
  sessionStorage.setItem(sessionStorageKey, JSON.stringify(session));
}

export function clearSession() {
  sessionStorage.removeItem(sessionStorageKey);
  sessionStorage.removeItem(pkceVerifierStorageKey);
}

export function persistPkce(verifier: string, state: string) {
  sessionStorage.setItem(pkceVerifierStorageKey, JSON.stringify({ verifier, state }));
}

export function loadPkce(): { verifier: string; state: string } | null {
  const payload = sessionStorage.getItem(pkceVerifierStorageKey);
  return payload ? (JSON.parse(payload) as { verifier: string; state: string }) : null;
}

export function clearPkce() {
  sessionStorage.removeItem(pkceVerifierStorageKey);
}

export function normalizeRoles(roleClaim: string | string[] | undefined): string[] {
  if (!roleClaim) return [];
  return Array.isArray(roleClaim) ? roleClaim : [roleClaim];
}

export function extractRolesFromClaims(claims: Record<string, string | string[]>): string[] {
  const roleKeys = [
    'role',
    'roles',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'
  ];

  const roleSet = new Set<string>();
  for (const key of roleKeys) {
    const normalized = normalizeRoles(claims[key]);
    for (const role of normalized) {
      if (role && role.trim().length > 0) {
        roleSet.add(role.trim());
      }
    }
  }

  return [...roleSet];
}

export function parseJwt(token: string): Record<string, string | string[]> {
  const [, payload] = token.split('.');
  const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
  return JSON.parse(json) as Record<string, string | string[]>;
}
