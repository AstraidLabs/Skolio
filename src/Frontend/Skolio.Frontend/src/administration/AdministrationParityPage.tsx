import React, { useEffect, useState } from 'react';
import type { SessionState } from '../shared/auth/session';
import type { createAdministrationApi } from './api';
import { Card, SectionHeader, StatusBadge, WidgetGrid } from '../shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from '../shared/ui/states';

export function AdministrationParityPage({
  api,
  session
}: {
  api: ReturnType<typeof createAdministrationApi>;
  session: SessionState;
}) {
  const isPlatformAdmin = session.roles.includes('PlatformAdministrator');
  const isSchoolAdmin = session.roles.includes('SchoolAdministrator');
  const isTeacher = session.roles.includes('Teacher');
  const isParent = session.roles.includes('Parent');
  const isStudent = session.roles.includes('Student');
  const canWrite = isPlatformAdmin || isSchoolAdmin;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [settings, setSettings] = useState<any[]>([]);
  const [toggles, setToggles] = useState<any[]>([]);
  const [lifecycle, setLifecycle] = useState<any[]>([]);
  const [housekeeping, setHousekeeping] = useState<any[]>([]);
  const [audit, setAudit] = useState<any[]>([]);
  const [summary, setSummary] = useState<any>(null);
  const [teacherContext, setTeacherContext] = useState<any>(null);
  const [parentContext, setParentContext] = useState<any>(null);
  const [studentContext, setStudentContext] = useState<any>(null);
  const [auditFilter, setAuditFilter] = useState({ actionCode: '', actorUserId: '' });

  const load = () => {
    setLoading(true);
    setError('');

    if (isTeacher) {
      void api.teacherContext().then(setTeacherContext).catch((e: Error) => setError(e.message)).finally(() => setLoading(false));
      return;
    }

    if (isParent) {
      void api.parentContext().then(setParentContext).catch((e: Error) => setError(e.message)).finally(() => setLoading(false));
      return;
    }

    if (isStudent) {
      void api.studentContext().then(setStudentContext).catch((e: Error) => setError(e.message)).finally(() => setLoading(false));
      return;
    }

    void Promise.all([
      api.settings(),
      api.toggles(),
      api.schoolYearPolicies(),
      api.housekeepingPolicies(),
      api.auditLogs(auditFilter.actionCode || auditFilter.actorUserId ? auditFilter : undefined),
      api.operationalSummary()
    ])
      .then(([settingsResult, togglesResult, lifecycleResult, housekeepingResult, auditResult, summaryResult]) => {
        setSettings(settingsResult);
        setToggles(togglesResult);
        setLifecycle(lifecycleResult);
        setHousekeeping(housekeepingResult);
        setAudit(auditResult);
        setSummary(summaryResult);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  };

  useEffect(load, [session.accessToken]);

  if (loading) return <LoadingState text="Loading administration capabilities..." />;
  if (error) return <ErrorState text={error} />;

  if (isTeacher && teacherContext) {
    return (
      <section className="space-y-3">
        <SectionHeader title="Teacher Administration Read Scope" />
        <Card><p className="font-semibold text-sm">Lifecycle hints</p><ul className="sk-list">{teacherContext.activeLifecycleHints.map((x: string) => <li key={x} className="sk-list-item">{x}</li>)}</ul></Card>
        <Card><p className="font-semibold text-sm">Recent teacher audit actions</p><ul className="sk-list">{teacherContext.recentTeacherAuditActions.map((x: string) => <li key={x} className="sk-list-item">{x}</li>)}</ul></Card>
      </section>
    );
  }

  if (isParent && parentContext) {
    return (
      <section className="space-y-3">
        <SectionHeader title="Parent Administration Read Scope" />
        <Card><p className="font-semibold text-sm">Feature toggles</p><ul className="sk-list">{parentContext.activeSchoolFeatureToggles.map((x: string) => <li key={x} className="sk-list-item">{x}</li>)}</ul></Card>
        <Card><p className="font-semibold text-sm">Lifecycle policy summaries</p><ul className="sk-list">{parentContext.schoolLifecyclePolicySummaries.map((x: string) => <li key={x} className="sk-list-item">{x}</li>)}</ul></Card>
      </section>
    );
  }

  if (isStudent && studentContext) {
    return (
      <section className="space-y-3">
        <SectionHeader title="Student Administration Read Scope" />
        <Card><p className="font-semibold text-sm">Feature toggles</p><ul className="sk-list">{studentContext.activeSchoolFeatureToggles.map((x: string) => <li key={x} className="sk-list-item">{x}</li>)}</ul></Card>
        <Card><p className="font-semibold text-sm">Lifecycle summaries</p><ul className="sk-list">{studentContext.schoolLifecyclePolicySummaries.map((x: string) => <li key={x} className="sk-list-item">{x}</li>)}</ul></Card>
      </section>
    );
  }

  return (
    <section className="space-y-3">
      <SectionHeader
        title="Administration Parity"
        description="Audit, settings, toggles, lifecycle and housekeeping mapped to existing backend endpoints."
        action={<button className="sk-btn sk-btn-secondary" onClick={load} type="button">Reload</button>}
      />

      {summary ? (
        <WidgetGrid>
          <Card><p className="sk-metric-label">Recent audit</p><p className="sk-metric-value">{summary.recentAuditCount}</p></Card>
          <Card><p className="sk-metric-label">Enabled toggles</p><p className="sk-metric-value">{summary.enabledFeatureToggles}</p></Card>
          <Card><p className="sk-metric-label">Active lifecycle policies</p><p className="sk-metric-value">{summary.activeLifecyclePolicies}</p></Card>
          <Card><p className="sk-metric-label">Housekeeping policies</p><p className="sk-metric-value">{summary.activeHousekeepingPolicies}</p></Card>
        </WidgetGrid>
      ) : null}

      <Card>
        <p className="font-semibold text-sm">Audit filter</p>
        <div className="mt-2 grid gap-2 md:grid-cols-3">
          <input className="sk-input" placeholder="Action code" value={auditFilter.actionCode} onChange={(e) => setAuditFilter((v) => ({ ...v, actionCode: e.target.value }))} />
          <input className="sk-input" placeholder="Actor user id" value={auditFilter.actorUserId} onChange={(e) => setAuditFilter((v) => ({ ...v, actorUserId: e.target.value }))} />
          <button className="sk-btn sk-btn-secondary" type="button" onClick={load}>Apply filter</button>
        </div>
      </Card>

      <div className="grid gap-3 lg:grid-cols-2">
        <Card>
          <p className="font-semibold text-sm">System settings</p>
          {settings.length === 0 ? <EmptyState text="No settings in scope." /> : (
            <ul className="sk-list">
              {settings.map((setting) => (
                <li key={setting.id} className="sk-list-item">
                  <span>{setting.key}</span>
                  <div className="flex gap-2">
                    <StatusBadge label={setting.isSensitive ? 'Sensitive' : 'Standard'} tone={setting.isSensitive ? 'warn' : 'info'} />
                    {canWrite ? <button className="sk-btn sk-btn-secondary" type="button" onClick={() => void api.updateSetting(setting.id, { value: setting.value, isSensitive: setting.isSensitive }).then(load).catch((e: Error) => setError(e.message))}>Save</button> : null}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card>
          <p className="font-semibold text-sm">Feature toggles</p>
          {toggles.length === 0 ? <EmptyState text="No feature toggles in scope." /> : (
            <ul className="sk-list">
              {toggles.map((toggle) => (
                <li key={toggle.id} className="sk-list-item">
                  <span>{toggle.featureCode}</span>
                  <div className="flex gap-2">
                    <StatusBadge label={toggle.isEnabled ? 'ON' : 'OFF'} tone={toggle.isEnabled ? 'good' : 'warn'} />
                    {canWrite ? <button className="sk-btn sk-btn-secondary" type="button" onClick={() => void api.updateToggle(toggle.id, { isEnabled: !toggle.isEnabled }).then(load).catch((e: Error) => setError(e.message))}>Toggle</button> : null}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card>
          <p className="font-semibold text-sm">School year lifecycle policies</p>
          {lifecycle.length === 0 ? <EmptyState text="No lifecycle policies in scope." /> : (
            <ul className="sk-list">
              {lifecycle.map((policy) => (
                <li key={policy.id} className="sk-list-item">
                  <span>{policy.policyName} ({policy.closureGraceDays} days)</span>
                  {canWrite ? <button className="sk-btn sk-btn-secondary" type="button" onClick={() => void api.updateSchoolYearPolicy(policy.id, { closureGraceDays: policy.closureGraceDays, status: policy.status }).then(load).catch((e: Error) => setError(e.message))}>Save</button> : <StatusBadge label={policy.status} tone="info" />}
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card>
          <p className="font-semibold text-sm">Housekeeping policies</p>
          {housekeeping.length === 0 ? <EmptyState text="No housekeeping policies in scope." /> : (
            <ul className="sk-list">
              {housekeeping.map((policy) => (
                <li key={policy.id} className="sk-list-item">
                  <span>{policy.policyName} ({policy.retentionDays} days)</span>
                  {canWrite ? <button className="sk-btn sk-btn-secondary" type="button" onClick={() => void api.updateHousekeepingPolicy(policy.id, { retentionDays: policy.retentionDays, status: policy.status }).then(load).catch((e: Error) => setError(e.message))}>Save</button> : <StatusBadge label={policy.status} tone="info" />}
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      <Card>
        <p className="font-semibold text-sm">Audit log</p>
        {audit.length === 0 ? <EmptyState text="No audit logs in selected scope." /> : (
          <ul className="sk-list">
            {audit.map((entry) => (
              <li key={entry.id} className="sk-list-item">
                <span>{entry.actionCode}</span>
                <span className="text-xs">{entry.createdAtUtc}</span>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </section>
  );
}
