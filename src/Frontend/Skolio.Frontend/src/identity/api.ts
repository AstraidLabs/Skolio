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
  gender?: string | null;
  dateOfBirth?: string | null;
  nationalIdNumber?: string | null;
  birthPlace?: string | null;
  permanentAddress?: string | null;
  correspondenceAddress?: string | null;
  contactEmail?: string | null;
  legalGuardian1?: string | null;
  legalGuardian2?: string | null;
  schoolPlacement?: string | null;
  healthInsuranceProvider?: string | null;
  pediatrician?: string | null;
  healthSafetyNotes?: string | null;
  supportMeasuresSummary?: string | null;
  positionTitle?: string | null;
  teacherRoleLabel?: string | null;
  qualificationSummary?: string | null;
  schoolContextSummary?: string | null;
  parentRelationshipSummary?: string | null;
  deliveryContactName?: string | null;
  deliveryContactPhone?: string | null;
  preferredContactChannel?: string | null;
  communicationPreferencesSummary?: string | null;
  publicContactNote?: string | null;
  preferredContactNote?: string | null;
  administrativeWorkDesignation?: string | null;
  administrativeOrganizationSummary?: string | null;
  platformRoleContextSummary?: string | null;
  managedPlatformAreasSummary?: string | null;
  administrativeBoundarySummary?: string | null;
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
  gender?: string | null;
  dateOfBirth?: string | null;
  nationalIdNumber?: string | null;
  birthPlace?: string | null;
  permanentAddress?: string | null;
  correspondenceAddress?: string | null;
  contactEmail?: string | null;
  legalGuardian1?: string | null;
  legalGuardian2?: string | null;
  schoolPlacement?: string | null;
  healthInsuranceProvider?: string | null;
  pediatrician?: string | null;
  healthSafetyNotes?: string | null;
  supportMeasuresSummary?: string | null;
  positionTitle?: string | null;
  teacherRoleLabel?: string | null;
  qualificationSummary?: string | null;
  schoolContextSummary?: string | null;
  parentRelationshipSummary?: string | null;
  deliveryContactName?: string | null;
  deliveryContactPhone?: string | null;
  preferredContactChannel?: string | null;
  communicationPreferencesSummary?: string | null;
  publicContactNote?: string | null;
  preferredContactNote?: string | null;
  administrativeWorkDesignation?: string | null;
  administrativeOrganizationSummary?: string | null;
  platformRoleContextSummary?: string | null;
  managedPlatformAreasSummary?: string | null;
  administrativeBoundarySummary?: string | null;
};

export type AdminProfileUpdatePayload = SelfProfileUpdatePayload;
export type SchoolPositionOption = { code: string; label: string };
export type SecuritySummary = {
  userId: string;
  currentEmail: string;
  emailConfirmed: boolean;
  mfaEnabled: boolean;
  hasAuthenticatorKey: boolean;
  recoveryCodesLeft: number;
};
export type MfaStatus = { enabled: boolean; hasAuthenticatorKey: boolean; recoveryCodesLeft: number };
export type MfaSetupStart = {
  sharedKey: string;
  authenticatorUri: string;
  issuer: string;
  accountLabel: string;
};
export type RecoveryCodeResult = { recoveryCodes: string[] };


export type PagedResult<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type AdminUserListQuery = {
  name?: string;
  emailOrUsername?: string;
  role?: string;
  accountStatus?: string;
  activationStatus?: string;
  blockStatus?: string;
  mfaStatus?: string;
  school?: string;
  schoolType?: string;
  inactivityState?: string;
  sortField?: string;
  sortDirection?: 'asc' | 'desc';
  pageNumber?: number;
  pageSize?: 10 | 20 | 50 | 100;
};
export type IdentityManagedUser = {
  userId: string;
  email: string;
  accountLifecycleStatus: string;
  emailConfirmed: boolean;
  lockoutEndUtc?: string | null;
  lastLoginAtUtc?: string | null;
  lastActivityAtUtc?: string | null;
  userName: string;
  mfaEnabled: boolean;
  activatedAtUtc?: string | null;
  blockedAtUtc?: string | null;
  displayName: string;
  school?: string | null;
  schoolType?: string | null;
  roles: string[];
  schoolIds: string[];
};


export type IdentityManagedUserRolesDetail = {
  roles: string[];
  availableRoles: string[];
  canManagePlatformAdministratorRole: boolean;
};

export type IdentityManagedUserLifecycleDetail = {
  status: string;
  activationRequestedAtUtc?: string | null;
  activatedAtUtc?: string | null;
  deactivatedAtUtc?: string | null;
  deactivationReason?: string | null;
  blockedAtUtc?: string | null;
  blockedReason?: string | null;
  lastLoginAtUtc?: string | null;
  lastActivityAtUtc?: string | null;
};

export type IdentityManagedUserSecurityDetail = {
  emailConfirmed: boolean;
  mfaEnabled: boolean;
  lockoutEndUtc?: string | null;
  lastLoginAtUtc?: string | null;
  lastActivityAtUtc?: string | null;
  recoveryCodesSummary: string;
};

export type IdentityManagedUserSchoolContextDetail = {
  school?: string | null;
  schoolType?: string | null;
  schoolIds: string[];
  isPlatformScopeView: boolean;
};

export type IdentityManagedUserLinksSummary = {
  parentStudentLinksCount: number;
  teacherAssignmentCount: number;
  studentAssignmentCount: number;
};

export type IdentityManagedUserDetail = {
  userId: string;
  email: string;
  userName: string;
  emailConfirmed: boolean;
  accountLifecycleStatus: string;
  lockoutEndUtc?: string | null;
  activatedAtUtc?: string | null;
  deactivatedAtUtc?: string | null;
  deactivationReason?: string | null;
  blockedAtUtc?: string | null;
  blockedReason?: string | null;
  lastLoginAtUtc?: string | null;
  lastActivityAtUtc?: string | null;
  firstName: string;
  lastName: string;
  school?: string | null;
  schoolType?: string | null;
  roles: string[];
  schoolIds: string[];
};

export function createIdentityApi(http: ReturnType<typeof createHttpClient>) {
  return {
    myProfile: () => http<UserProfile>('identity', '/api/identity/user-profiles/me'),
    myProfileSummary: () => http<MyProfileSummary>('identity', '/api/identity/user-profiles/me/summary'),
    mySchoolPositionOptions: (schoolId?: string) => http<SchoolPositionOption[]>('identity', `/api/identity/user-profiles/me/school-position-options${schoolId ? `?schoolId=${schoolId}` : ''}`),
    userSchoolPositionOptions: (id: string, schoolId?: string) => http<SchoolPositionOption[]>('identity', `/api/identity/user-profiles/${id}/school-position-options${schoolId ? `?schoolId=${schoolId}` : ''}`),
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
    deleteParentStudentLink: (id: string) => http<void>('identity', `/api/identity/parent-student-links/${id}`, { method: 'DELETE' }),
    securitySummary: () => http<SecuritySummary>('identity', '/api/identity/security/summary'),
    changePassword: (payload: { currentPassword: string; newPassword: string; confirmNewPassword: string }) => http<{ message: string }>('identity', '/api/identity/security/change-password', { method: 'POST', body: JSON.stringify(payload) }),

    resendActivation: (payload: { email: string }) => http<{ message: string }>('identity', '/api/identity/security/activation/resend', { method: 'POST', body: JSON.stringify(payload) }),
    confirmActivation: (payload: { userId: string; token: string }) => http<{ message: string }>('identity', '/api/identity/security/activation/confirm', { method: 'POST', body: JSON.stringify(payload) }),
    adminUsers: (query?: AdminUserListQuery) => {
      const params = new URLSearchParams();
      if (query?.name) params.set('name', query.name);
      if (query?.emailOrUsername) params.set('emailOrUsername', query.emailOrUsername);
      if (query?.role) params.set('role', query.role);
      if (query?.accountStatus) params.set('accountStatus', query.accountStatus);
      if (query?.activationStatus) params.set('activationStatus', query.activationStatus);
      if (query?.blockStatus) params.set('blockStatus', query.blockStatus);
      if (query?.mfaStatus) params.set('mfaStatus', query.mfaStatus);
      if (query?.school) params.set('school', query.school);
      if (query?.schoolType) params.set('schoolType', query.schoolType);
      if (query?.inactivityState) params.set('inactivityState', query.inactivityState);
      if (query?.sortField) params.set('sortField', query.sortField);
      if (query?.sortDirection) params.set('sortDirection', query.sortDirection);
      if (query?.pageNumber) params.set('pageNumber', String(query.pageNumber));
      if (query?.pageSize) params.set('pageSize', String(query.pageSize));
      return http<PagedResult<IdentityManagedUser>>('identity', `/api/identity/user-management/users${params.size > 0 ? `?${params.toString()}` : ''}`);
    },
    adminUserDetail: (userId: string) => http<IdentityManagedUserDetail>('identity', `/api/identity/user-management/users/${userId}`),

    adminUserRolesDetail: (userId: string) => http<IdentityManagedUserRolesDetail>('identity', `/api/identity/user-management/users/${userId}/roles-detail`),
    adminUserLifecycleDetail: (userId: string) => http<IdentityManagedUserLifecycleDetail>('identity', `/api/identity/user-management/users/${userId}/lifecycle-detail`),
    adminUserSecurityDetail: (userId: string) => http<IdentityManagedUserSecurityDetail>('identity', `/api/identity/user-management/users/${userId}/security-detail`),
    adminUserSchoolContextDetail: (userId: string) => http<IdentityManagedUserSchoolContextDetail>('identity', `/api/identity/user-management/users/${userId}/school-context-detail`),
    adminUserLinksSummary: (userId: string) => http<IdentityManagedUserLinksSummary>('identity', `/api/identity/user-management/users/${userId}/links-summary`),
    adminActivate: (userId: string) => http<void>('identity', `/api/identity/user-management/users/${userId}/activate`, { method: 'POST' }),
    adminDeactivate: (userId: string, reason: string) => http<void>('identity', `/api/identity/user-management/users/${userId}/deactivate`, { method: 'POST', body: JSON.stringify({ reason }) }),
    adminReactivate: (userId: string) => http<void>('identity', `/api/identity/user-management/users/${userId}/reactivate`, { method: 'POST' }),
    adminBlock: (userId: string, reason?: string) => http<void>('identity', `/api/identity/user-management/users/${userId}/block`, { method: 'POST', body: JSON.stringify({ reason }) }),
    adminUnblock: (userId: string) => http<void>('identity', `/api/identity/user-management/users/${userId}/unblock`, { method: 'POST' }),
    adminResendActivation: (userId: string) => http<{ message: string }>('identity', `/api/identity/user-management/users/${userId}/resend-activation`, { method: 'POST' }),
    adminRoles: (userId: string) => http<string[]>('identity', `/api/identity/user-management/users/${userId}/roles`),
    adminAssignRole: (userId: string, role: string) => http<void>('identity', `/api/identity/user-management/users/${userId}/roles/assign`, { method: 'POST', body: JSON.stringify({ role }) }),
    adminRemoveRole: (userId: string, role: string) => http<void>('identity', `/api/identity/user-management/users/${userId}/roles/remove`, { method: 'POST', body: JSON.stringify({ role }) }),
    adminUpdateRoleSet: (userId: string, roles: string[]) => http<void>('identity', `/api/identity/user-management/users/${userId}/roles`, { method: 'PUT', body: JSON.stringify({ roles }) }),
    forgotPassword: (payload: { email: string }) => http<{ message: string }>('identity', '/api/identity/security/forgot-password', { method: 'POST', body: JSON.stringify(payload) }),
    resetPassword: (payload: { userId: string; token: string; newPassword: string; confirmNewPassword: string }) => http<{ message: string }>('identity', '/api/identity/security/reset-password', { method: 'POST', body: JSON.stringify(payload) }),
    requestEmailChange: (payload: { currentPassword: string; newEmail: string }) => http<{ message: string }>('identity', '/api/identity/security/change-email/request', { method: 'POST', body: JSON.stringify(payload) }),
    confirmEmailChange: (payload: { userId: string; newEmail: string; token: string }) => http<{ message: string }>('identity', '/api/identity/security/change-email/confirm', { method: 'POST', body: JSON.stringify(payload) }),
    mfaStatus: () => http<MfaStatus>('identity', '/api/identity/security/mfa/status'),
    startMfaSetup: () => http<MfaSetupStart>('identity', '/api/identity/security/mfa/setup/start', { method: 'POST' }),
    confirmMfaSetup: (payload: { verificationCode: string }) => http<RecoveryCodeResult>('identity', '/api/identity/security/mfa/setup/confirm', { method: 'POST', body: JSON.stringify(payload) }),
    disableMfa: (payload: { currentPassword: string; verificationCode: string }) => http<{ message: string }>('identity', '/api/identity/security/mfa/disable', { method: 'POST', body: JSON.stringify(payload) }),
    regenerateRecoveryCodes: (payload: { currentPassword: string }) => http<RecoveryCodeResult>('identity', '/api/identity/security/mfa/recovery-codes/regenerate', { method: 'POST', body: JSON.stringify(payload) })
  };
}
