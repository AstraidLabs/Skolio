import type { createHttpClient } from '../shared/http/httpClient';

export type UserProfile = { id: string; firstName: string; lastName: string; userType: string; email: string; isActive: boolean };
export type RoleAssignment = { id: string; userProfileId: string; schoolId: string; roleCode: string };
export type ParentStudentLink = { id: string; parentUserProfileId: string; studentUserProfileId: string; relationship: string };

export function createIdentityApi(http: ReturnType<typeof createHttpClient>) {
  return {
    myProfile: () => http<UserProfile>('identity', '/api/identity/user-profiles/me'),
    updateMyProfile: (payload: Omit<UserProfile, 'id' | 'isActive'>) => http<UserProfile>('identity', '/api/identity/user-profiles/me', { method: 'PUT', body: JSON.stringify(payload) }),
    userProfiles: (query?: { search?: string; userType?: string; isActive?: boolean }) => {
      const params = new URLSearchParams();
      if (query?.search) params.set('search', query.search);
      if (query?.userType) params.set('userType', query.userType);
      if (typeof query?.isActive === 'boolean') params.set('isActive', String(query.isActive));
      return http<UserProfile[]>('identity', `/api/identity/user-profiles${params.size > 0 ? `?${params.toString()}` : ''}`);
    },
    userProfile: (id: string) => http<UserProfile>('identity', `/api/identity/user-profiles/${id}`),
    updateUserProfile: (id: string, payload: Omit<UserProfile, 'id' | 'isActive'>) => http<UserProfile>('identity', `/api/identity/user-profiles/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    setUserProfileActivation: (id: string, isActive: boolean) => http<UserProfile>('identity', `/api/identity/user-profiles/${id}/activation`, { method: 'PUT', body: JSON.stringify({ isActive }) }),
    roleAssignments: (query?: { schoolId?: string; roleCode?: string }) => {
      const params = new URLSearchParams();
      if (query?.schoolId) params.set('schoolId', query.schoolId);
      if (query?.roleCode) params.set('roleCode', query.roleCode);
      return http<RoleAssignment[]>('identity', `/api/identity/school-roles${params.size > 0 ? `?${params.toString()}` : ''}`);
    },
    assignRole: (payload: { userProfileId: string; schoolId: string; roleCode: string }) => http<RoleAssignment>('identity', '/api/identity/school-roles', { method: 'POST', body: JSON.stringify(payload) }),
    deleteRoleAssignment: (id: string) => http<void>('identity', `/api/identity/school-roles/${id}`, { method: 'DELETE' }),
    parentStudentLinks: (query?: { parentUserProfileId?: string; studentUserProfileId?: string }) => {
      const params = new URLSearchParams();
      if (query?.parentUserProfileId) params.set('parentUserProfileId', query.parentUserProfileId);
      if (query?.studentUserProfileId) params.set('studentUserProfileId', query.studentUserProfileId);
      return http<ParentStudentLink[]>('identity', `/api/identity/parent-student-links${params.size > 0 ? `?${params.toString()}` : ''}`);
    },
    createParentStudentLink: (payload: { parentUserProfileId: string; studentUserProfileId: string; relationship: string }) => http<ParentStudentLink>('identity', '/api/identity/parent-student-links', { method: 'POST', body: JSON.stringify(payload) }),
    overrideParentStudentLink: (id: string, payload: { relationship: string; overrideReason: string }) => http<ParentStudentLink>('identity', `/api/identity/parent-student-links/${id}/override`, { method: 'PUT', body: JSON.stringify(payload) }),
    deleteParentStudentLink: (id: string) => http<void>('identity', `/api/identity/parent-student-links/${id}`, { method: 'DELETE' })
  };
}