import type { createHttpClient } from '../shared/http/httpClient';

export type UserProfile = {
  id: string;
  firstName: string;
  lastName: string;
  userType: string;
  email: string;
  isActive: boolean;
  preferredDisplayName?: string | null;
  preferredLanguage?: string | null;
  phoneNumber?: string | null;
  positionTitle?: string | null;
  publicContactNote?: string | null;
  preferredContactNote?: string | null;
};
export type RoleAssignment = { id: string; userProfileId: string; schoolId: string; roleCode: string };
export type ParentStudentLink = { id: string; parentUserProfileId: string; studentUserProfileId: string; relationship: string };
export type StudentIdentityContext = { profile: UserProfile; roleAssignments: RoleAssignment[] };
export type MyProfileSummary = {
  profile: UserProfile;
  roleAssignments: RoleAssignment[];
  parentStudentLinks: ParentStudentLink[];
  schoolIds: string[];
  isPlatformAdministrator: boolean;
  isSchoolAdministrator: boolean;
  isTeacher: boolean;
  isParent: boolean;
  isStudent: boolean;
};

export type SelfProfileUpdatePayload = {
  firstName: string;
  lastName: string;
  preferredDisplayName?: string | null;
  preferredLanguage?: string | null;
  phoneNumber?: string | null;
  positionTitle?: string | null;
  publicContactNote?: string | null;
  preferredContactNote?: string | null;
};

export type AdminProfileUpdatePayload = SelfProfileUpdatePayload;

export function createIdentityApi(http: ReturnType<typeof createHttpClient>) {
  return {
    myProfile: () => http<UserProfile>('identity', '/api/identity/user-profiles/me'),
    myProfileSummary: () => http<MyProfileSummary>('identity', '/api/identity/user-profiles/me/summary'),
    studentContext: () => http<StudentIdentityContext>('identity', '/api/identity/user-profiles/student-context'),
    updateMyProfile: (payload: SelfProfileUpdatePayload) => http<UserProfile>('identity', '/api/identity/user-profiles/me', { method: 'PUT', body: JSON.stringify(payload) }),
    linkedStudents: () => http<UserProfile[]>('identity', '/api/identity/user-profiles/linked-students'),
    userProfiles: (query?: { search?: string; userType?: string; isActive?: boolean }) => {
      const params = new URLSearchParams();
      if (query?.search) params.set('search', query.search);
      if (query?.userType) params.set('userType', query.userType);
      if (typeof query?.isActive === 'boolean') params.set('isActive', String(query.isActive));
      return http<UserProfile[]>('identity', `/api/identity/user-profiles${params.size > 0 ? `?${params.toString()}` : ''}`);
    },
    userProfile: (id: string) => http<UserProfile>('identity', `/api/identity/user-profiles/${id}`),
    updateUserProfile: (id: string, payload: AdminProfileUpdatePayload) => http<UserProfile>('identity', `/api/identity/user-profiles/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    setUserProfileActivation: (id: string, isActive: boolean) => http<UserProfile>('identity', `/api/identity/user-profiles/${id}/activation`, { method: 'PUT', body: JSON.stringify({ isActive }) }),
    myRoleAssignments: (schoolId?: string) => http<RoleAssignment[]>('identity', `/api/identity/school-roles/me${schoolId ? `?schoolId=${schoolId}` : ''}`),
    myStudentRoleAssignments: () => http<RoleAssignment[]>('identity', '/api/identity/school-roles/student-me'),
    roleAssignments: (query?: { schoolId?: string; roleCode?: string }) => {
      const params = new URLSearchParams();
      if (query?.schoolId) params.set('schoolId', query.schoolId);
      if (query?.roleCode) params.set('roleCode', query.roleCode);
      return http<RoleAssignment[]>('identity', `/api/identity/school-roles${params.size > 0 ? `?${params.toString()}` : ''}`);
    },
    assignRole: (payload: { userProfileId: string; schoolId: string; roleCode: string }) => http<RoleAssignment>('identity', '/api/identity/school-roles', { method: 'POST', body: JSON.stringify(payload) }),
    deleteRoleAssignment: (id: string) => http<void>('identity', `/api/identity/school-roles/${id}`, { method: 'DELETE' }),
    myParentStudentLinks: () => http<ParentStudentLink[]>('identity', '/api/identity/parent-student-links/me'),
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
