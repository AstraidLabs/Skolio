export type UserRole = 'PlatformAdministrator' | 'SchoolAdministrator' | 'Teacher' | 'Parent' | 'Student';
export type SchoolType = 'Kindergarten' | 'ElementarySchool' | 'SecondarySchool';

export type SessionState = {
  accessToken: string;
  expiresAtUtc: number;
  subject: string;
  roles: string[];
  schoolType: SchoolType;
  schoolIds: string[];
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

export function parseJwt(token: string): Record<string, string | string[]> {
  const [, payload] = token.split('.');
  const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
  return JSON.parse(json) as Record<string, string | string[]>;
}
