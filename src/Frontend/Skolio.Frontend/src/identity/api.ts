import type { createHttpClient } from '../shared/http/httpClient';

export type UserProfile = { id: string; firstName: string; lastName: string; userType: string; email: string };
export type RoleAssignment = { id: string; userProfileId: string; schoolId: string; roleCode: string };
export type ParentStudentLink = { id: string; parentUserProfileId: string; studentUserProfileId: string; relationship: string };

export function createIdentityApi(http: ReturnType<typeof createHttpClient>) {
  return {
    myProfile: () => http<UserProfile>('identity', '/api/identity/user-profiles/me'),
    updateMyProfile: (payload: Omit<UserProfile, 'id'>) => http<UserProfile>('identity', '/api/identity/user-profiles/me', { method: 'PUT', body: JSON.stringify(payload) }),
    roleAssignments: () => http<RoleAssignment[]>('identity', '/api/identity/school-roles'),
    parentStudentLinks: () => http<ParentStudentLink[]>('identity', '/api/identity/parent-student-links')
  };
}
