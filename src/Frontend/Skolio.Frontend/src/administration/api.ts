import type { createHttpClient } from '../shared/http/httpClient';

export type SystemSetting = { id: string; key: string; value: string; isSensitive: boolean };
export type FeatureToggle = { id: string; featureCode: string; isEnabled: boolean };
export type AuditLog = { id: string; actorUserId: string; actionCode: string; payload: string; createdAtUtc: string };
export type SchoolYearPolicy = { id: string; schoolId: string; policyName: string; closureGraceDays: number; status: string };
export type HousekeepingPolicy = { id: string; policyName: string; retentionDays: number; status: string };
export type OperationalSummary = { recentAuditCount: number; enabledFeatureToggles: number; activeLifecyclePolicies: number; activeHousekeepingPolicies: number; latestAuditActions: string[] };
export type ParentContext = { activeSchoolFeatureToggles: string[]; schoolLifecyclePolicySummaries: string[] };

type PagedResult<T> = { items: T[]; pageNumber: number; pageSize: number; totalCount: number };

export function createAdministrationApi(http: ReturnType<typeof createHttpClient>) {
  return {
    settings: () => http<SystemSetting[]>('administration', '/api/administration/system-settings'),
    updateSetting: (id: string, payload: { value: string; isSensitive: boolean }) => http<SystemSetting>('administration', `/api/administration/system-settings/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    toggles: () => http<FeatureToggle[]>('administration', '/api/administration/feature-toggles'),
    updateToggle: (id: string, payload: { isEnabled: boolean }) => http<FeatureToggle>('administration', `/api/administration/feature-toggles/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    auditLogs: async (query?: { actionCode?: string; actorUserId?: string }) => {
      const params = new URLSearchParams();
      if (query?.actionCode) params.set('actionCode', query.actionCode);
      if (query?.actorUserId) params.set('actorUserId', query.actorUserId);
      const result = await http<PagedResult<AuditLog>>('administration', `/api/administration/audit-log${params.size > 0 ? `?${params.toString()}` : ''}`);
      return result.items;
    },
    schoolYearPolicies: () => http<SchoolYearPolicy[]>('administration', '/api/administration/school-year-policies'),
    updateSchoolYearPolicy: (id: string, payload: { closureGraceDays: number; status: string }) => http<SchoolYearPolicy>('administration', `/api/administration/school-year-policies/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    housekeepingPolicies: () => http<HousekeepingPolicy[]>('administration', '/api/administration/housekeeping-policies'),
    updateHousekeepingPolicy: (id: string, payload: { retentionDays: number; status: string }) => http<HousekeepingPolicy>('administration', `/api/administration/housekeeping-policies/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    operationalSummary: () => http<OperationalSummary>('administration', '/api/administration/operational-summary'),
    parentContext: () => http<ParentContext>('administration', '/api/administration/parent-context')
  };
}
