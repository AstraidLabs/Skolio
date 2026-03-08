import React, { useEffect, useMemo, useState } from 'react';
import type { createIdentityApi, MyProfileSummary, SelfProfileUpdatePayload, UserProfile } from './api';
import type { SessionState } from '../shared/auth/session';
import type { createOrganizationApi, TeacherAssignment } from '../organization/api';
import { Card, SectionHeader, StatusBadge, WidgetGrid } from '../shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from '../shared/ui/states';
import { localeLabels, supportedLocales } from '../i18n';

type ProfileDraft = SelfProfileUpdatePayload;

const EMPTY_DRAFT: ProfileDraft = {
  firstName: '',
  lastName: '',
  preferredDisplayName: '',
  preferredLanguage: '',
  phoneNumber: '',
  positionTitle: '',
  publicContactNote: '',
  preferredContactNote: ''
};

export function IdentityParityPage({
  api,
  organizationApi,
  session
}: {
  api: ReturnType<typeof createIdentityApi>;
  organizationApi?: ReturnType<typeof createOrganizationApi>;
  session: SessionState;
}) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [summary, setSummary] = useState<MyProfileSummary | null>(null);
  const [linkedStudents, setLinkedStudents] = useState<UserProfile[]>([]);
  const [teacherAssignments, setTeacherAssignments] = useState<TeacherAssignment[]>([]);
  const [users, setUsers] = useState<UserProfile[]>([]);
  const [selectedUserId, setSelectedUserId] = useState('');
  const [selfDraft, setSelfDraft] = useState<ProfileDraft>(EMPTY_DRAFT);
  const [adminDraft, setAdminDraft] = useState<ProfileDraft>(EMPTY_DRAFT);

  const isPlatformAdministrator = session.roles.includes('PlatformAdministrator');
  const isSchoolAdministrator = session.roles.includes('SchoolAdministrator');
  const isTeacher = session.roles.includes('Teacher');
  const isParent = session.roles.includes('Parent');
  const isStudentOnly = session.roles.includes('Student')
    && !session.roles.includes('Teacher')
    && !session.roles.includes('Parent')
    && !session.roles.includes('SchoolAdministrator')
    && !session.roles.includes('PlatformAdministrator');

  const canAdminProfiles = summary?.isPlatformAdministrator || summary?.isSchoolAdministrator || false;

  const selfEditRules = useMemo(() => ({
    canEditName: !isStudentOnly,
    canEditPositionTitle: isPlatformAdministrator || isSchoolAdministrator || isTeacher,
    canEditPublicContactNote: isTeacher,
    canEditPreferredContactNote: isParent
  }), [isParent, isPlatformAdministrator, isSchoolAdministrator, isStudentOnly, isTeacher]);

  const selectedSchoolId = session.schoolIds[0] ?? '';

  const mapToDraft = (profile: UserProfile): ProfileDraft => ({
    firstName: profile.firstName ?? '',
    lastName: profile.lastName ?? '',
    preferredDisplayName: profile.preferredDisplayName ?? '',
    preferredLanguage: profile.preferredLanguage ?? '',
    phoneNumber: profile.phoneNumber ?? '',
    positionTitle: profile.positionTitle ?? '',
    publicContactNote: profile.publicContactNote ?? '',
    preferredContactNote: profile.preferredContactNote ?? ''
  });

  const load = () => {
    setLoading(true);
    setError('');
    setSuccess('');

    void api.myProfileSummary()
      .then(async (result) => {
        setSummary(result);
        setSelfDraft(mapToDraft(result.profile));

        const tasks: Promise<unknown>[] = [];

        if (isParent) {
          tasks.push(api.linkedStudents().then(setLinkedStudents));
        }

        if (isTeacher && selectedSchoolId && organizationApi) {
          tasks.push(organizationApi.myTeacherAssignments(selectedSchoolId).then(setTeacherAssignments));
        }

        if (result.isPlatformAdministrator || result.isSchoolAdministrator) {
          tasks.push(api.userProfiles().then(setUsers));
        } else {
          setUsers([]);
          setSelectedUserId('');
          setAdminDraft(EMPTY_DRAFT);
        }

        await Promise.all(tasks);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  };

  useEffect(load, [api, organizationApi, session.accessToken]);

  const saveSelfProfile = () => {
    setError('');
    setSuccess('');

    const payload: ProfileDraft = {
      ...selfDraft,
      firstName: selfEditRules.canEditName ? selfDraft.firstName : (summary?.profile.firstName ?? ''),
      lastName: selfEditRules.canEditName ? selfDraft.lastName : (summary?.profile.lastName ?? ''),
      positionTitle: selfEditRules.canEditPositionTitle ? selfDraft.positionTitle : (summary?.profile.positionTitle ?? ''),
      publicContactNote: selfEditRules.canEditPublicContactNote ? selfDraft.publicContactNote : (summary?.profile.publicContactNote ?? ''),
      preferredContactNote: selfEditRules.canEditPreferredContactNote ? selfDraft.preferredContactNote : (summary?.profile.preferredContactNote ?? '')
    };

    void api.updateMyProfile(payload)
      .then(() => {
        setSuccess('Profile saved.');
        load();
      })
      .catch((e: Error) => setError(e.message));
  };

  const loadAdminTarget = (userId: string) => {
    setSelectedUserId(userId);
    if (!userId) {
      setAdminDraft(EMPTY_DRAFT);
      return;
    }

    setError('');
    void api.userProfile(userId)
      .then((profile) => setAdminDraft(mapToDraft(profile)))
      .catch((e: Error) => setError(e.message));
  };

  const saveAdminProfile = () => {
    if (!selectedUserId) return;

    setError('');
    setSuccess('');
    const payload = isPlatformAdministrator
      ? adminDraft
      : { ...adminDraft, publicContactNote: '', preferredContactNote: '' };

    void api.updateUserProfile(selectedUserId, payload)
      .then(() => {
        setSuccess('Administrative profile edit saved.');
        load();
      })
      .catch((e: Error) => setError(e.message));
  };

  if (loading) return <LoadingState text="Loading profile..." />;
  if (error) return <ErrorState text={error} />;
  if (!summary) return <EmptyState text="Profile is not available." />;

  return (
    <section className="space-y-4">
      <SectionHeader
        title="My Profile"
        description="Business profile self-service with strict read-only boundaries for identity assignments and links."
      />

      {success ? (
        <Card className="border-emerald-200 bg-emerald-50 text-emerald-900">
          <p className="text-sm font-medium">{success}</p>
        </Card>
      ) : null}

      <WidgetGrid>
        <Card>
          <p className="sk-metric-label">Role</p>
          <p className="sk-metric-value">{session.roles.join(', ')}</p>
        </Card>
        <Card>
          <p className="sk-metric-label">School context</p>
          <p className="sk-metric-value">{session.schoolType}</p>
        </Card>
        <Card>
          <p className="sk-metric-label">Assigned schools</p>
          <p className="sk-metric-value">{summary.schoolIds.length}</p>
        </Card>
      </WidgetGrid>

      <Card>
        <p className="font-semibold text-sm">Self profile edit</p>
        <div className="mt-3 grid gap-2 md:grid-cols-2">
          <Field label="First name" value={selfDraft.firstName} disabled={!selfEditRules.canEditName} onChange={(value) => setSelfDraft((v) => ({ ...v, firstName: value }))} />
          <Field label="Last name" value={selfDraft.lastName} disabled={!selfEditRules.canEditName} onChange={(value) => setSelfDraft((v) => ({ ...v, lastName: value }))} />
          <Field label="Preferred display name" value={selfDraft.preferredDisplayName ?? ''} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredDisplayName: value }))} />
          <LanguageField label="Preferred language" value={selfDraft.preferredLanguage ?? ''} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredLanguage: value }))} />
          <Field label="Phone number" value={selfDraft.phoneNumber ?? ''} onChange={(value) => setSelfDraft((v) => ({ ...v, phoneNumber: value }))} />
          <Field label="Position title" value={selfDraft.positionTitle ?? ''} disabled={!selfEditRules.canEditPositionTitle} onChange={(value) => setSelfDraft((v) => ({ ...v, positionTitle: value }))} />
          <Field label="Public contact note" value={selfDraft.publicContactNote ?? ''} disabled={!selfEditRules.canEditPublicContactNote} onChange={(value) => setSelfDraft((v) => ({ ...v, publicContactNote: value }))} />
          <Field label="Preferred contact note" value={selfDraft.preferredContactNote ?? ''} disabled={!selfEditRules.canEditPreferredContactNote} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredContactNote: value }))} />
        </div>
        <div className="mt-3 flex gap-2">
          <button className="sk-btn sk-btn-primary" onClick={saveSelfProfile} type="button">Save my profile</button>
          <StatusBadge label="Email / Username / Role assignments are read-only" tone="info" />
        </div>
      </Card>

      <Card>
        <p className="font-semibold text-sm">Read-only account and assignment scope</p>
        <ul className="mt-2 space-y-1 text-sm text-slate-700">
          <li>UserId: {summary.profile.id}</li>
          <li>Email (login): {summary.profile.email}</li>
          <li>Main role type: {summary.profile.userType}</li>
          <li>Account status: {summary.profile.isActive ? 'Active' : 'Inactive'}</li>
        </ul>
      </Card>

      <div className="grid gap-3 lg:grid-cols-2">
        <Card>
          <p className="font-semibold text-sm">Role assignments (read-only)</p>
          {summary.roleAssignments.length === 0 ? <EmptyState text="No role assignments." /> : (
            <ul className="sk-list">
              {summary.roleAssignments.map((assignment) => (
                <li key={assignment.id} className="sk-list-item">{assignment.roleCode} | {assignment.schoolId}</li>
              ))}
            </ul>
          )}
        </Card>
        <Card>
          <p className="font-semibold text-sm">Parent-student links (read-only)</p>
          {summary.parentStudentLinks.length === 0 ? <EmptyState text="No parent-student links." /> : (
            <ul className="sk-list">
              {summary.parentStudentLinks.map((link) => (
                <li key={link.id} className="sk-list-item">{link.parentUserProfileId} {'->'} {link.studentUserProfileId} ({link.relationship})</li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      {isTeacher ? (
        <Card>
          <p className="font-semibold text-sm">Teacher assignments (read-only)</p>
          {teacherAssignments.length === 0 ? <EmptyState text="No teacher assignments in selected school context." /> : (
            <ul className="sk-list">
              {teacherAssignments.map((assignment) => (
                <li key={assignment.id} className="sk-list-item">{assignment.scope} | class: {assignment.classRoomId ?? '-'} | group: {assignment.teachingGroupId ?? '-'} | subject: {assignment.subjectId ?? '-'}</li>
              ))}
            </ul>
          )}
        </Card>
      ) : null}

      {isParent ? (
        <Card>
          <p className="font-semibold text-sm">Linked students summary</p>
          {linkedStudents.length === 0 ? <EmptyState text="No linked students." /> : (
            <ul className="sk-list">
              {linkedStudents.map((student) => (
                <li key={student.id} className="sk-list-item">{student.firstName} {student.lastName} ({student.email})</li>
              ))}
            </ul>
          )}
        </Card>
      ) : null}

      {canAdminProfiles ? (
        <Card>
          <p className="font-semibold text-sm">Administrative profile edit</p>
          <p className="mt-1 text-xs text-slate-600">Role assignments remain in dedicated role-management boundary and are not edited here.</p>
          <div className="mt-3 grid gap-2 md:grid-cols-2">
            <label className="sk-label" htmlFor="admin-user">User profile</label>
            <select id="admin-user" className="sk-input" value={selectedUserId} onChange={(e) => loadAdminTarget(e.target.value)}>
              <option value="">Select user</option>
              {users.map((user) => (
                <option key={user.id} value={user.id}>{user.firstName} {user.lastName} ({user.userType})</option>
              ))}
            </select>
          </div>

          {selectedUserId ? (
            <div className="mt-3 grid gap-2 md:grid-cols-2">
              <Field label="First name" value={adminDraft.firstName} onChange={(value) => setAdminDraft((v) => ({ ...v, firstName: value }))} />
              <Field label="Last name" value={adminDraft.lastName} onChange={(value) => setAdminDraft((v) => ({ ...v, lastName: value }))} />
              <Field label="Preferred display name" value={adminDraft.preferredDisplayName ?? ''} onChange={(value) => setAdminDraft((v) => ({ ...v, preferredDisplayName: value }))} />
              <LanguageField label="Preferred language" value={adminDraft.preferredLanguage ?? ''} onChange={(value) => setAdminDraft((v) => ({ ...v, preferredLanguage: value }))} />
              <Field label="Phone number" value={adminDraft.phoneNumber ?? ''} onChange={(value) => setAdminDraft((v) => ({ ...v, phoneNumber: value }))} />
              <Field label="Position title" value={adminDraft.positionTitle ?? ''} onChange={(value) => setAdminDraft((v) => ({ ...v, positionTitle: value }))} />
              <Field label="Public contact note" value={adminDraft.publicContactNote ?? ''} disabled={!isPlatformAdministrator} onChange={(value) => setAdminDraft((v) => ({ ...v, publicContactNote: value }))} />
              <Field label="Preferred contact note" value={adminDraft.preferredContactNote ?? ''} disabled={!isPlatformAdministrator} onChange={(value) => setAdminDraft((v) => ({ ...v, preferredContactNote: value }))} />
            </div>
          ) : null}

          <div className="mt-3">
            <button className="sk-btn sk-btn-primary" disabled={!selectedUserId} onClick={saveAdminProfile} type="button">Save administrative edit</button>
          </div>
        </Card>
      ) : null}
    </section>
  );
}

function Field({
  label,
  value,
  onChange,
  disabled = false
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label">{label}</label>
      <input
        className="sk-input"
        value={value}
        disabled={disabled}
        onChange={(e) => onChange(e.target.value)}
      />
    </div>
  );
}

function LanguageField({
  label,
  value,
  onChange,
  disabled = false
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label">{label}</label>
      <select
        className="sk-input"
        value={value}
        disabled={disabled}
        onChange={(e) => onChange(e.target.value)}
      >
        <option value="">Select language</option>
        {supportedLocales.map((locale) => (
          <option key={locale} value={locale}>
            {localeLabels[locale]} ({locale.toUpperCase()})
          </option>
        ))}
      </select>
    </div>
  );
}
