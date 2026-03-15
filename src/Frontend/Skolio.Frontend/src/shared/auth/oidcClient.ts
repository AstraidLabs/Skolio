import { UserManager, WebStorageStateStore, type User } from 'oidc-client-ts';
import type { SkolioBootstrapConfig } from '../../bootstrap';
import { extractRolesFromClaims, parseJwt, type SchoolType, type SessionState } from './session';

export function createUserManager(config: SkolioBootstrapConfig): UserManager {
  return new UserManager({
    authority: config.identityAuthority,
    client_id: config.oidcClientId,
    redirect_uri: config.oidcRedirectUri,
    post_logout_redirect_uri: config.oidcPostLogoutRedirectUri,
    scope: config.oidcScope,
    response_type: 'code',
    userStore: new WebStorageStateStore({ store: window.sessionStorage }),
    automaticSilentRenew: true,
    loadUserInfo: false,
    filterProtocolClaims: false,
  });
}

export function buildSessionFromUser(user: User): SessionState {
  const claims = parseJwt(user.access_token);
  return {
    accessToken: user.access_token,
    refreshToken: user.refresh_token ?? null,
    expiresAtUtc: (user.expires_at ?? 0) * 1000,
    subject: typeof claims.sub === 'string' ? claims.sub : 'unknown',
    roles: extractRolesFromClaims(claims),
    schoolType: (claims['school_type'] as SchoolType) ?? 'ElementarySchool',
    schoolIds: Array.isArray(claims['school_id'])
      ? (claims['school_id'] as string[])
      : claims['school_id']
        ? [claims['school_id'] as string]
        : [],
    linkedStudentIds: Array.isArray(claims['linked_student_id'])
      ? (claims['linked_student_id'] as string[])
      : claims['linked_student_id']
        ? [claims['linked_student_id'] as string]
        : [],
  };
}
