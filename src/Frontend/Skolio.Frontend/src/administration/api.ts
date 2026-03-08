import type { createHttpClient } from '../shared/http/httpClient';

export type SystemSetting = { id: string; key: string; value: string; isSensitive: boolean };
export type FeatureToggle = { id: string; featureCode: string; isEnabled: boolean };
export type AuditLog = { id: string; actorUserId: string; actionCode: string; payload: string; createdAtUtc: string };
export type SchoolYearPolicy = { id: string; schoolId: string; policyName: string; closureGraceDays: number; status: string };
export type HousekeepingPolicy = { id: string; policyName: string; retentionDays: number; status: string };
export type OperationalSummary = { recentAuditCount: number; enabledFeatureToggles: number; activeLifecyclePolicies: number; activeHousekeepingPolicies: number; latestAuditActions: string[] };
export type ParentContext = { activeSchoolFeatureToggles: string[]; schoolLifecyclePolicySummaries: string[] };
export type TeacherContext = { recentTeacherAuditActions: string[]; activeLifecycleHints: string[] };
export type StudentContext = { activeSchoolFeatureToggles: string[]; schoolLifecyclePolicySummaries: string[]; recentStudentAuditActions: string[] };

type PagedResult<T> = { items: T[]; pageNumber: number; pageSize: number; totalCount: number };

export function createAdministrationApi(http: ReturnType<typeof createHttpClient>) {
  return {
    // System settings
    settings: () => http<SystemSetting[]>('administration', '/api/administration/system-settings'),
    createSetting: (payload: { key: string; value: string; isSensitive: boolean }) => http<SystemSetting>('administration', '/api/administration/system-settings', { method: 'POST', body: JSON.stringify(payload) }),
    updateSetting: (id: string, payload: { value: string; isSensitive: boolean }) => http<SystemSetting>('administration', `/api/administration/system-settings/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    deleteSetting: (id: string) => http<void>('administration', `/api/administration/system-settings/${id}`, { method: 'DELETE' }),

    // Feature toggles
    toggles: () => http<FeatureToggle[]>('administration', '/api/administration/feature-toggles'),
    createToggle: (payload: { featureCode: string; isEnabled: boolean }) => http<FeatureToggle>('administration', '/api/administration/feature-toggles', { method: 'POST', body: JSON.stringify(payload) }),
    updateToggle: (id: string, payload: { isEnabled: boolean }) => http<FeatureToggle>('administration', `/api/administration/feature-toggles/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    deleteToggle: (id: string) => http<void>('administration', `/api/administration/feature-toggles/${id}`, { method: 'DELETE' }),

    // Audit log
    auditLogs: async (query?: { actionCode?: string; actorUserId?: string }) => {
      const params = new URLSearchParams();
      if (query?.actionCode) params.set('actionCode', query.actionCode);
      if (query?.actorUserId) params.set('actorUserId', query.actorUserId);
      const result = await http<PagedResult<AuditLog>>('administration', `/api/administration/audit-log${params.size > 0 ? `?${params.toString()}` : ''}`);
      return result.items;
    },
    auditLogDetail: (id: string) => http<AuditLog>('administration', `/api/administration/audit-log/${id}`),

    // School year lifecycle policies
    schoolYearPolicies: () => http<SchoolYearPolicy[]>('administration', '/api/administration/school-year-policies'),
    createSchoolYearPolicy: (payload: { schoolId: string; policyName: string; closureGraceDays: number; activate: boolean }) => http<SchoolYearPolicy>('administration', '/api/administration/school-year-policies', { method: 'POST', body: JSON.stringify(payload) }),
    updateSchoolYearPolicy: (id: string, payload: { closureGraceDays: number; status: string }) => http<SchoolYearPolicy>('administration', `/api/administration/school-year-policies/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    deleteSchoolYearPolicy: (id: string) => http<void>('administration', `/api/administration/school-year-policies/${id}`, { method: 'DELETE' }),

    // Housekeeping policies
    housekeepingPolicies: () => http<HousekeepingPolicy[]>('administration', '/api/administration/housekeeping-policies'),
    createHousekeepingPolicy: (payload: { policyName: string; retentionDays: number; activate: boolean }) => http<HousekeepingPolicy>('administration', '/api/administration/housekeeping-policies', { method: 'POST', body: JSON.stringify(payload) }),
    updateHousekeepingPolicy: (id: string, payload: { retentionDays: number; status: string }) => http<HousekeepingPolicy>('administration', `/api/administration/housekeeping-policies/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    deleteHousekeepingPolicy: (id: string) => http<void>('administration', `/api/administration/housekeeping-policies/${id}`, { method: 'DELETE' }),

    // Operational summary
    operationalSummary: () => http<OperationalSummary>('administration', '/api/administration/operational-summary'),

    // Role-specific context endpoints
    parentContext: () => http<ParentContext>('administration', '/api/administration/parent-context'),
    teacherContext: () => http<TeacherContext>('administration', '/api/administration/teacher-context'),
    studentContext: () => http<StudentContext>('administration', '/api/administration/student-context')
  };
}
