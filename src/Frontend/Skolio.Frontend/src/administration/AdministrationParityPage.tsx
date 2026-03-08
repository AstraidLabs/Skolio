import React, { useEffect, useState } from 'react';
import type { SessionState } from '../shared/auth/session';
import type { createAdministrationApi, SystemSetting, FeatureToggle, SchoolYearPolicy, HousekeepingPolicy } from './api';
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
  const [settings, setSettings] = useState<SystemSetting[]>([]);
  const [toggles, setToggles] = useState<FeatureToggle[]>([]);
  const [lifecycle, setLifecycle] = useState<SchoolYearPolicy[]>([]);
  const [housekeeping, setHousekeeping] = useState<HousekeepingPolicy[]>([]);
  const [audit, setAudit] = useState<any[]>([]);
  const [summary, setSummary] = useState<any>(null);
  const [teacherContext, setTeacherContext] = useState<any>(null);
  const [parentContext, setParentContext] = useState<any>(null);
  const [studentContext, setStudentContext] = useState<any>(null);
  const [auditFilter, setAuditFilter] = useState({ actionCode: '', actorUserId: '' });

  const handleError = (e: unknown) => setError(e instanceof Error ? e.message : 'Unknown error');

  const load = () => {
    setLoading(true);
    setError('');

    if (isTeacher) {
      void api.teacherContext().then(setTeacherContext).catch(handleError).finally(() => setLoading(false));
      return;
    }

    if (isParent) {
      void api.parentContext().then(setParentContext).catch(handleError).finally(() => setLoading(false));
      return;
    }

    if (isStudent) {
      void api.studentContext().then(setStudentContext).catch(handleError).finally(() => setLoading(false));
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
      .then(([s, t, l, h, a, sm]) => {
        setSettings(s);
        setToggles(t);
        setLifecycle(l);
        setHousekeeping(h);
        setAudit(a);
        setSummary(sm);
      })
      .catch(handleError)
      .finally(() => setLoading(false));
  };

  useEffect(load, [session.accessToken]);

  if (loading) return <LoadingState text="Loading administration..." />;
  if (error) return <ErrorState text={error} />;

  /* ── Teacher view ── */
  if (isTeacher && teacherContext) {
    return (
      <section className="space-y-3">
        <SectionHeader title="Teacher — Administration" />
        <Card>
          <p className="font-semibold text-sm">Active lifecycle hints</p>
          {teacherContext.activeLifecycleHints.length === 0
            ? <EmptyState text="No active lifecycle hints." />
            : <ul className="sk-list">{teacherContext.activeLifecycleHints.map((x: string) => <li key={x} className="sk-list-item">{x}</li>)}</ul>}
        </Card>
        <Card>
          <p className="font-semibold text-sm">Your recent audit actions</p>
          {teacherContext.recentTeacherAuditActions.length === 0
            ? <EmptyState text="No recent audit actions." />
            : <ul className="sk-list">{teacherContext.recentTeacherAuditActions.map((x: string, i: number) => <li key={i} className="sk-list-item">{x}</li>)}</ul>}
        </Card>
      </section>
    );
  }

  /* ── Parent view ── */
  if (isParent && parentContext) {
    return (
      <section className="space-y-3">
        <SectionHeader title="Parent — Administration" />
        <Card>
          <p className="font-semibold text-sm">School feature toggles</p>
          {parentContext.activeSchoolFeatureToggles.length === 0
            ? <EmptyState text="No school feature toggles." />
            : <ul className="sk-list">{parentContext.activeSchoolFeatureToggles.map((x: string) => <li key={x} className="sk-list-item">{x}</li>)}</ul>}
        </Card>
        <Card>
          <p className="font-semibold text-sm">Lifecycle policy summaries</p>
          {parentContext.schoolLifecyclePolicySummaries.length === 0
            ? <EmptyState text="No lifecycle policies." />
            : <ul className="sk-list">{parentContext.schoolLifecyclePolicySummaries.map((x: string) => <li key={x} className="sk-list-item">{x}</li>)}</ul>}
        </Card>
      </section>
    );
  }

  /* ── Student view ── */
  if (isStudent && studentContext) {
    return (
      <section className="space-y-3">
        <SectionHeader title="Student — Administration" />
        <Card>
          <p className="font-semibold text-sm">School feature toggles</p>
          {studentContext.activeSchoolFeatureToggles.length === 0
            ? <EmptyState text="No school feature toggles." />
            : <ul className="sk-list">{studentContext.activeSchoolFeatureToggles.map((x: string) => <li key={x} className="sk-list-item">{x}</li>)}</ul>}
        </Card>
        <Card>
          <p className="font-semibold text-sm">Lifecycle summaries</p>
          {studentContext.schoolLifecyclePolicySummaries.length === 0
            ? <EmptyState text="No lifecycle policies." />
            : <ul className="sk-list">{studentContext.schoolLifecyclePolicySummaries.map((x: string) => <li key={x} className="sk-list-item">{x}</li>)}</ul>}
        </Card>
        <Card>
          <p className="font-semibold text-sm">Your recent audit actions</p>
          {studentContext.recentStudentAuditActions.length === 0
            ? <EmptyState text="No recent audit actions." />
            : <ul className="sk-list">{studentContext.recentStudentAuditActions.map((x: string, i: number) => <li key={i} className="sk-list-item">{x}</li>)}</ul>}
        </Card>
      </section>
    );
  }

  /* ── Admin view (PlatformAdministrator / SchoolAdministrator) ── */
  return (
    <section className="space-y-3">
      <SectionHeader
        title="Administration"
        description={isPlatformAdmin ? 'Platform-wide administration.' : 'School-scoped administration.'}
        action={<button className="sk-btn sk-btn-secondary" onClick={load} type="button">Reload</button>}
      />

      {summary ? (
        <WidgetGrid>
          <Card><p className="sk-metric-label">Recent audit (7d)</p><p className="sk-metric-value">{summary.recentAuditCount}</p></Card>
          <Card><p className="sk-metric-label">Enabled toggles</p><p className="sk-metric-value">{summary.enabledFeatureToggles}</p></Card>
          <Card><p className="sk-metric-label">Active lifecycle policies</p><p className="sk-metric-value">{summary.activeLifecyclePolicies}</p></Card>
          <Card><p className="sk-metric-label">Active housekeeping policies</p><p className="sk-metric-value">{summary.activeHousekeepingPolicies}</p></Card>
        </WidgetGrid>
      ) : null}

      <div className="grid gap-3 lg:grid-cols-2">
        <SystemSettingsSection settings={settings} api={api} isPlatformAdmin={isPlatformAdmin} onReload={load} onError={handleError} />
        <FeatureTogglesSection toggles={toggles} api={api} isPlatformAdmin={isPlatformAdmin} canWrite={canWrite} onReload={load} onError={handleError} />
        <SchoolYearPoliciesSection policies={lifecycle} api={api} isPlatformAdmin={isPlatformAdmin} canWrite={canWrite} onReload={load} onError={handleError} />
        <HousekeepingPoliciesSection policies={housekeeping} api={api} isPlatformAdmin={isPlatformAdmin} onReload={load} onError={handleError} />
      </div>

      <AuditLogSection audit={audit} filter={auditFilter} onFilterChange={setAuditFilter} onApply={load} />
    </section>
  );
}

/* ───────────────────────── System Settings ───────────────────────── */

function SystemSettingsSection({ settings, api, isPlatformAdmin, onReload, onError }: {
  settings: SystemSetting[];
  api: ReturnType<typeof createAdministrationApi>;
  isPlatformAdmin: boolean;
  onReload: () => void;
  onError: (e: unknown) => void;
}) {
  const [showCreate, setShowCreate] = useState(false);
  const [form, setForm] = useState({ key: '', value: '', isSensitive: false });
  const [edits, setEdits] = useState<Record<string, string>>({});

  const handleCreate = () => {
    if (!form.key.trim()) return;
    void api.createSetting(form).then(() => { setShowCreate(false); setForm({ key: '', value: '', isSensitive: false }); onReload(); }).catch(onError);
  };

  const handleUpdate = (s: SystemSetting) => {
    const newValue = edits[s.id] ?? s.value;
    void api.updateSetting(s.id, { value: newValue, isSensitive: s.isSensitive }).then(() => { setEdits((prev) => { const next = { ...prev }; delete next[s.id]; return next; }); onReload(); }).catch(onError);
  };

  const handleDelete = (s: SystemSetting) => {
    if (!confirm(`Delete setting "${s.key}"?`)) return;
    void api.deleteSetting(s.id).then(onReload).catch(onError);
  };

  return (
    <Card>
      <div className="flex items-center justify-between">
        <p className="font-semibold text-sm">System settings</p>
        {isPlatformAdmin && <button className="sk-btn sk-btn-secondary text-xs" type="button" onClick={() => setShowCreate(!showCreate)}>{showCreate ? 'Cancel' : '+ Add'}</button>}
      </div>

      {showCreate && (
        <div className="mt-2 grid gap-2 sm:grid-cols-4">
          <input className="sk-input" placeholder="Key" value={form.key} onChange={(e) => setForm((v) => ({ ...v, key: e.target.value }))} />
          <input className="sk-input" placeholder="Value" value={form.value} onChange={(e) => setForm((v) => ({ ...v, value: e.target.value }))} />
          <label className="flex items-center gap-1 text-xs"><input type="checkbox" checked={form.isSensitive} onChange={(e) => setForm((v) => ({ ...v, isSensitive: e.target.checked }))} /> Sensitive</label>
          <button className="sk-btn sk-btn-secondary" type="button" onClick={handleCreate}>Create</button>
        </div>
      )}

      {settings.length === 0 ? <EmptyState text="No settings in scope." /> : (
        <ul className="sk-list mt-2">
          {settings.map((s) => (
            <li key={s.id} className="sk-list-item">
              <div className="flex-1 min-w-0">
                <span className="text-xs font-medium">{s.key}</span>
                {isPlatformAdmin ? (
                  <input className="sk-input mt-1 text-xs" value={edits[s.id] ?? s.value} onChange={(e) => setEdits((prev) => ({ ...prev, [s.id]: e.target.value }))} />
                ) : (
                  <p className="text-xs text-gray-500 truncate">{s.value}</p>
                )}
              </div>
              <div className="flex items-center gap-1 shrink-0">
                <StatusBadge label={s.isSensitive ? 'Sensitive' : 'Standard'} tone={s.isSensitive ? 'warn' : 'info'} />
                {isPlatformAdmin && <button className="sk-btn sk-btn-secondary text-xs" type="button" onClick={() => handleUpdate(s)}>Save</button>}
                {isPlatformAdmin && <button className="sk-btn text-xs text-red-600" type="button" onClick={() => handleDelete(s)}>Del</button>}
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

/* ───────────────────────── Feature Toggles ───────────────────────── */

function FeatureTogglesSection({ toggles, api, isPlatformAdmin, canWrite, onReload, onError }: {
  toggles: FeatureToggle[];
  api: ReturnType<typeof createAdministrationApi>;
  isPlatformAdmin: boolean;
  canWrite: boolean;
  onReload: () => void;
  onError: (e: unknown) => void;
}) {
  const [showCreate, setShowCreate] = useState(false);
  const [form, setForm] = useState({ featureCode: '', isEnabled: false });

  const handleCreate = () => {
    if (!form.featureCode.trim()) return;
    void api.createToggle(form).then(() => { setShowCreate(false); setForm({ featureCode: '', isEnabled: false }); onReload(); }).catch(onError);
  };

  const handleToggle = (t: FeatureToggle) => {
    void api.updateToggle(t.id, { isEnabled: !t.isEnabled }).then(onReload).catch(onError);
  };

  const handleDelete = (t: FeatureToggle) => {
    if (!confirm(`Delete toggle "${t.featureCode}"?`)) return;
    void api.deleteToggle(t.id).then(onReload).catch(onError);
  };

  return (
    <Card>
      <div className="flex items-center justify-between">
        <p className="font-semibold text-sm">Feature toggles</p>
        {isPlatformAdmin && <button className="sk-btn sk-btn-secondary text-xs" type="button" onClick={() => setShowCreate(!showCreate)}>{showCreate ? 'Cancel' : '+ Add'}</button>}
      </div>

      {showCreate && (
        <div className="mt-2 grid gap-2 sm:grid-cols-3">
          <input className="sk-input" placeholder="Feature code" value={form.featureCode} onChange={(e) => setForm((v) => ({ ...v, featureCode: e.target.value }))} />
          <label className="flex items-center gap-1 text-xs"><input type="checkbox" checked={form.isEnabled} onChange={(e) => setForm((v) => ({ ...v, isEnabled: e.target.checked }))} /> Enabled</label>
          <button className="sk-btn sk-btn-secondary" type="button" onClick={handleCreate}>Create</button>
        </div>
      )}

      {toggles.length === 0 ? <EmptyState text="No feature toggles in scope." /> : (
        <ul className="sk-list mt-2">
          {toggles.map((t) => (
            <li key={t.id} className="sk-list-item">
              <span className="text-xs">{t.featureCode}</span>
              <div className="flex items-center gap-1">
                <StatusBadge label={t.isEnabled ? 'ON' : 'OFF'} tone={t.isEnabled ? 'good' : 'warn'} />
                {canWrite && <button className="sk-btn sk-btn-secondary text-xs" type="button" onClick={() => handleToggle(t)}>Toggle</button>}
                {isPlatformAdmin && <button className="sk-btn text-xs text-red-600" type="button" onClick={() => handleDelete(t)}>Del</button>}
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

/* ───────────────────── School Year Lifecycle Policies ─────────────── */

function SchoolYearPoliciesSection({ policies, api, isPlatformAdmin, canWrite, onReload, onError }: {
  policies: SchoolYearPolicy[];
  api: ReturnType<typeof createAdministrationApi>;
  isPlatformAdmin: boolean;
  canWrite: boolean;
  onReload: () => void;
  onError: (e: unknown) => void;
}) {
  const [showCreate, setShowCreate] = useState(false);
  const [form, setForm] = useState({ schoolId: '', policyName: '', closureGraceDays: 14, activate: false });
  const [edits, setEdits] = useState<Record<string, { closureGraceDays: number; status: string }>>({});

  const handleCreate = () => {
    if (!form.schoolId.trim() || !form.policyName.trim()) return;
    void api.createSchoolYearPolicy(form).then(() => { setShowCreate(false); setForm({ schoolId: '', policyName: '', closureGraceDays: 14, activate: false }); onReload(); }).catch(onError);
  };

  const handleUpdate = (p: SchoolYearPolicy) => {
    const edit = edits[p.id] ?? { closureGraceDays: p.closureGraceDays, status: p.status };
    void api.updateSchoolYearPolicy(p.id, edit).then(() => { setEdits((prev) => { const next = { ...prev }; delete next[p.id]; return next; }); onReload(); }).catch(onError);
  };

  const handleDelete = (p: SchoolYearPolicy) => {
    if (!confirm(`Delete policy "${p.policyName}"?`)) return;
    void api.deleteSchoolYearPolicy(p.id).then(onReload).catch(onError);
  };

  const getEdit = (p: SchoolYearPolicy) => edits[p.id] ?? { closureGraceDays: p.closureGraceDays, status: p.status };

  return (
    <Card>
      <div className="flex items-center justify-between">
        <p className="font-semibold text-sm">School year lifecycle policies</p>
        {canWrite && <button className="sk-btn sk-btn-secondary text-xs" type="button" onClick={() => setShowCreate(!showCreate)}>{showCreate ? 'Cancel' : '+ Add'}</button>}
      </div>

      {showCreate && (
        <div className="mt-2 grid gap-2 sm:grid-cols-2">
          <input className="sk-input" placeholder="School ID" value={form.schoolId} onChange={(e) => setForm((v) => ({ ...v, schoolId: e.target.value }))} />
          <input className="sk-input" placeholder="Policy name" value={form.policyName} onChange={(e) => setForm((v) => ({ ...v, policyName: e.target.value }))} />
          <input className="sk-input" type="number" placeholder="Grace days" value={form.closureGraceDays} onChange={(e) => setForm((v) => ({ ...v, closureGraceDays: Number(e.target.value) }))} />
          <div className="flex items-center gap-2">
            <label className="flex items-center gap-1 text-xs"><input type="checkbox" checked={form.activate} onChange={(e) => setForm((v) => ({ ...v, activate: e.target.checked }))} /> Activate immediately</label>
            <button className="sk-btn sk-btn-secondary" type="button" onClick={handleCreate}>Create</button>
          </div>
        </div>
      )}

      {policies.length === 0 ? <EmptyState text="No lifecycle policies in scope." /> : (
        <ul className="sk-list mt-2">
          {policies.map((p) => (
            <li key={p.id} className="sk-list-item">
              <div className="flex-1 min-w-0">
                <span className="text-xs font-medium">{p.policyName}</span>
                {canWrite ? (
                  <div className="mt-1 flex gap-2 items-center">
                    <input className="sk-input text-xs w-20" type="number" value={getEdit(p).closureGraceDays} onChange={(e) => setEdits((prev) => ({ ...prev, [p.id]: { ...getEdit(p), closureGraceDays: Number(e.target.value) } }))} />
                    <span className="text-xs text-gray-500">days</span>
                    <select className="sk-input text-xs w-24" value={getEdit(p).status} onChange={(e) => setEdits((prev) => ({ ...prev, [p.id]: { ...getEdit(p), status: e.target.value } }))}>
                      <option value="Draft">Draft</option>
                      <option value="Active">Active</option>
                      <option value="Archived">Archived</option>
                    </select>
                  </div>
                ) : (
                  <p className="text-xs text-gray-500">{p.closureGraceDays} days</p>
                )}
              </div>
              <div className="flex items-center gap-1 shrink-0">
                <StatusBadge label={p.status} tone={p.status === 'Active' ? 'good' : p.status === 'Archived' ? 'warn' : 'info'} />
                {canWrite && <button className="sk-btn sk-btn-secondary text-xs" type="button" onClick={() => handleUpdate(p)}>Save</button>}
                {canWrite && <button className="sk-btn text-xs text-red-600" type="button" onClick={() => handleDelete(p)}>Del</button>}
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

/* ───────────────────────── Housekeeping Policies ──────────────────── */

function HousekeepingPoliciesSection({ policies, api, isPlatformAdmin, onReload, onError }: {
  policies: HousekeepingPolicy[];
  api: ReturnType<typeof createAdministrationApi>;
  isPlatformAdmin: boolean;
  onReload: () => void;
  onError: (e: unknown) => void;
}) {
  const [showCreate, setShowCreate] = useState(false);
  const [form, setForm] = useState({ policyName: '', retentionDays: 90, activate: false });
  const [edits, setEdits] = useState<Record<string, { retentionDays: number; status: string }>>({});

  const handleCreate = () => {
    if (!form.policyName.trim()) return;
    void api.createHousekeepingPolicy(form).then(() => { setShowCreate(false); setForm({ policyName: '', retentionDays: 90, activate: false }); onReload(); }).catch(onError);
  };

  const handleUpdate = (p: HousekeepingPolicy) => {
    const edit = edits[p.id] ?? { retentionDays: p.retentionDays, status: p.status };
    void api.updateHousekeepingPolicy(p.id, edit).then(() => { setEdits((prev) => { const next = { ...prev }; delete next[p.id]; return next; }); onReload(); }).catch(onError);
  };

  const handleDelete = (p: HousekeepingPolicy) => {
    if (!confirm(`Delete policy "${p.policyName}"?`)) return;
    void api.deleteHousekeepingPolicy(p.id).then(onReload).catch(onError);
  };

  const getEdit = (p: HousekeepingPolicy) => edits[p.id] ?? { retentionDays: p.retentionDays, status: p.status };

  return (
    <Card>
      <div className="flex items-center justify-between">
        <p className="font-semibold text-sm">Housekeeping policies</p>
        {isPlatformAdmin && <button className="sk-btn sk-btn-secondary text-xs" type="button" onClick={() => setShowCreate(!showCreate)}>{showCreate ? 'Cancel' : '+ Add'}</button>}
      </div>

      {showCreate && (
        <div className="mt-2 grid gap-2 sm:grid-cols-2">
          <input className="sk-input" placeholder="Policy name" value={form.policyName} onChange={(e) => setForm((v) => ({ ...v, policyName: e.target.value }))} />
          <input className="sk-input" type="number" placeholder="Retention days" value={form.retentionDays} onChange={(e) => setForm((v) => ({ ...v, retentionDays: Number(e.target.value) }))} />
          <label className="flex items-center gap-1 text-xs"><input type="checkbox" checked={form.activate} onChange={(e) => setForm((v) => ({ ...v, activate: e.target.checked }))} /> Activate immediately</label>
          <button className="sk-btn sk-btn-secondary" type="button" onClick={handleCreate}>Create</button>
        </div>
      )}

      {policies.length === 0 ? <EmptyState text="No housekeeping policies in scope." /> : (
        <ul className="sk-list mt-2">
          {policies.map((p) => (
            <li key={p.id} className="sk-list-item">
              <div className="flex-1 min-w-0">
                <span className="text-xs font-medium">{p.policyName}</span>
                {isPlatformAdmin ? (
                  <div className="mt-1 flex gap-2 items-center">
                    <input className="sk-input text-xs w-20" type="number" value={getEdit(p).retentionDays} onChange={(e) => setEdits((prev) => ({ ...prev, [p.id]: { ...getEdit(p), retentionDays: Number(e.target.value) } }))} />
                    <span className="text-xs text-gray-500">days</span>
                    <select className="sk-input text-xs w-24" value={getEdit(p).status} onChange={(e) => setEdits((prev) => ({ ...prev, [p.id]: { ...getEdit(p), status: e.target.value } }))}>
                      <option value="Draft">Draft</option>
                      <option value="Active">Active</option>
                      <option value="Archived">Archived</option>
                    </select>
                  </div>
                ) : (
                  <p className="text-xs text-gray-500">{p.retentionDays} days</p>
                )}
              </div>
              <div className="flex items-center gap-1 shrink-0">
                <StatusBadge label={p.status} tone={p.status === 'Active' ? 'good' : p.status === 'Archived' ? 'warn' : 'info'} />
                {isPlatformAdmin && <button className="sk-btn sk-btn-secondary text-xs" type="button" onClick={() => handleUpdate(p)}>Save</button>}
                {isPlatformAdmin && <button className="sk-btn text-xs text-red-600" type="button" onClick={() => handleDelete(p)}>Del</button>}
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

/* ───────────────────────── Audit Log ──────────────────────────────── */

function AuditLogSection({ audit, filter, onFilterChange, onApply }: {
  audit: any[];
  filter: { actionCode: string; actorUserId: string };
  onFilterChange: (f: { actionCode: string; actorUserId: string }) => void;
  onApply: () => void;
}) {
  const [expanded, setExpanded] = useState<string | null>(null);

  return (
    <Card>
      <p className="font-semibold text-sm">Audit log</p>
      <div className="mt-2 grid gap-2 md:grid-cols-3">
        <input className="sk-input" placeholder="Action code" value={filter.actionCode} onChange={(e) => onFilterChange({ ...filter, actionCode: e.target.value })} />
        <input className="sk-input" placeholder="Actor user ID" value={filter.actorUserId} onChange={(e) => onFilterChange({ ...filter, actorUserId: e.target.value })} />
        <button className="sk-btn sk-btn-secondary" type="button" onClick={onApply}>Apply filter</button>
      </div>
      {audit.length === 0 ? <EmptyState text="No audit logs in selected scope." /> : (
        <ul className="sk-list mt-2">
          {audit.map((entry) => (
            <li key={entry.id} className="sk-list-item flex-col items-start">
              <div className="flex w-full items-center justify-between cursor-pointer" onClick={() => setExpanded(expanded === entry.id ? null : entry.id)}>
                <span className="text-xs font-medium">{entry.actionCode}</span>
                <span className="text-xs text-gray-500">{new Date(entry.createdAtUtc).toLocaleString()}</span>
              </div>
              {expanded === entry.id && (
                <div className="mt-1 w-full">
                  <p className="text-xs text-gray-500">Actor: {entry.actorUserId}</p>
                  <pre className="mt-1 text-xs bg-gray-50 rounded p-2 overflow-x-auto max-h-32">{(() => { try { return JSON.stringify(JSON.parse(entry.payload), null, 2); } catch { return entry.payload; } })()}</pre>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
