import React, { useEffect, useMemo, useState } from 'react';
import type { createIdentityApi, MyProfileSummary, SelfProfileUpdatePayload, UserProfile } from './api';
import type { SessionState } from '../shared/auth/session';
import type { createOrganizationApi, TeacherAssignment } from '../organization/api';
import { Card, StatusBadge } from '../shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from '../shared/ui/states';
import { localeLabels, supportedLocales, useI18n } from '../i18n';

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
  const { t } = useI18n();
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
        setSuccess(t('profileSaveSuccess'));
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
        setSuccess(t('profileAdminSaveSuccess'));
        load();
      })
      .catch((e: Error) => setError(e.message));
  };

  if (loading) return <LoadingState text={t('profileLoading')} />;
  if (error) return <ErrorState text={error} />;
  if (!summary) return <EmptyState text={t('profileNotAvailable')} />;

  const headerInitials = toProfileInitials(
    summary.profile.preferredDisplayName
    || `${summary.profile.firstName} ${summary.profile.lastName}`.trim()
    || summary.profile.email
  );

  return (
    <section className="space-y-4">
      <Card>
        <p className="mb-3 font-semibold text-sm">{t('myProfile.accountOverview')}</p>
        <div className="flex flex-wrap items-start gap-3">
          <span className="sk-profile-avatar !h-12 !w-12 !text-sm" aria-hidden="true">
            {headerInitials}
          </span>
          <div className="min-w-0 flex-1">
            <div className="space-y-3">
              <div className="space-y-1">
                <div className="flex items-center gap-2 text-base font-semibold text-slate-900">
                  <ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />
                  <span>{summary.profile.preferredDisplayName || `${summary.profile.firstName} ${summary.profile.lastName}`.trim()}</span>
                </div>
                <div className="flex items-center gap-2 text-sm text-slate-700">
                  <ProfileRoleIcon className="h-4 w-4 shrink-0 text-slate-500" />
                  <span className="text-slate-500">{t('profileLabelRole')}:</span>
                  <span>{session.roles.join(", ")}</span>
                </div>
              </div>
              <div className="grid gap-2 md:grid-cols-2">
                <div className="flex items-center gap-2 text-sm text-slate-700">
                  <ProfileEmailIcon className="h-4 w-4 shrink-0 text-slate-500" />
                  <span className="text-slate-500">{t('email')}:</span>
                  <span>{summary.profile.email}</span>
                </div>
                <div className="flex items-center gap-2 text-sm text-slate-700">
                  <ProfileStatusIcon className="h-4 w-4 shrink-0 text-slate-500" />
                  <span className="text-slate-500">{t('profileLabelAccountActive')}:</span>
                  <span>{summary.profile.isActive ? t('profileValueYes') : t('profileValueNo')}</span>
                </div>
                <div className="flex items-center gap-2 text-sm text-slate-700">
                  <ProfileSchoolIcon className="h-4 w-4 shrink-0 text-slate-500" />
                  <span className="text-slate-500">{t('profileLabelSchoolContext')}:</span>
                  <span>{session.schoolType}</span>
                </div>
                <div className="flex items-center gap-2 text-sm text-slate-700">
                  <ProfileAssignmentIcon className="h-4 w-4 shrink-0 text-slate-500" />
                  <span className="text-slate-500">{t('profileLabelAssignedSchools')}:</span>
                  <span>{summary.schoolIds.length}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </Card>

      {success ? (
        <Card className="border-emerald-200 bg-emerald-50 text-emerald-900">
          <p className="text-sm font-medium">{success}</p>
        </Card>
      ) : null}

      <Card>
        <p className="font-semibold text-sm">{t('myProfile.personalEdit')}</p>
        <div className="mt-3 grid gap-2 md:grid-cols-2">
          <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldFirstName')} value={selfDraft.firstName} disabled={!selfEditRules.canEditName} onChange={(value) => setSelfDraft((v) => ({ ...v, firstName: value }))} />
          <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldLastName')} value={selfDraft.lastName} disabled={!selfEditRules.canEditName} onChange={(value) => setSelfDraft((v) => ({ ...v, lastName: value }))} />
          <Field icon={<ProfileCardIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredDisplayName')} value={selfDraft.preferredDisplayName ?? ''} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredDisplayName: value }))} />
          <LanguageField icon={<ProfileLanguageIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredLanguage')} placeholder={t('profileSelectLanguagePlaceholder')} value={selfDraft.preferredLanguage ?? ''} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredLanguage: value }))} />
          <Field icon={<ProfilePhoneIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPhoneNumber')} value={selfDraft.phoneNumber ?? ''} onChange={(value) => setSelfDraft((v) => ({ ...v, phoneNumber: value }))} />
          <Field icon={<ProfilePositionIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPositionTitle')} value={selfDraft.positionTitle ?? ''} disabled={!selfEditRules.canEditPositionTitle} onChange={(value) => setSelfDraft((v) => ({ ...v, positionTitle: value }))} />
          <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPublicContactNote')} value={selfDraft.publicContactNote ?? ''} disabled={!selfEditRules.canEditPublicContactNote} onChange={(value) => setSelfDraft((v) => ({ ...v, publicContactNote: value }))} />
          <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredContactNote')} value={selfDraft.preferredContactNote ?? ''} disabled={!selfEditRules.canEditPreferredContactNote} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredContactNote: value }))} />
        </div>
        <div className="mt-3 flex gap-2">
          <button className="sk-btn sk-btn-primary gap-2" onClick={saveSelfProfile} type="button">
            <SaveDiskIcon className="h-4 w-4 shrink-0" />
            <span>{t('profileButtonSaveMyProfile')}</span>
          </button>
        </div>
      </Card>

      <div className="grid gap-3 lg:grid-cols-2">
        <Card>
          <p className="font-semibold text-sm">{t('myProfile.roleAssignments')}</p>
          {summary.roleAssignments.length === 0 ? <EmptyState text={t('profileNoRoleAssignments')} /> : (
            <ul className="sk-list">
              {summary.roleAssignments.map((assignment) => (
                <li key={assignment.id} className="sk-list-item">{assignment.roleCode} | {assignment.schoolId}</li>
              ))}
            </ul>
          )}
        </Card>
        <Card>
          <p className="font-semibold text-sm">{t('myProfile.parentStudentLinks')}</p>
          {summary.parentStudentLinks.length === 0 ? <EmptyState text={t('profileNoParentStudentLinks')} /> : (
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
          <p className="font-semibold text-sm">{t('profileTeacherAssignmentsTitle')}</p>
          {teacherAssignments.length === 0 ? <EmptyState text={t('profileNoTeacherAssignments')} /> : (
            <ul className="sk-list">
              {teacherAssignments.map((assignment) => (
                <li key={assignment.id} className="sk-list-item">{assignment.scope} | {t('profileLabelClass')}: {assignment.classRoomId ?? '-'} | {t('profileLabelGroup')}: {assignment.teachingGroupId ?? '-'} | {t('profileLabelSubject')}: {assignment.subjectId ?? '-'}</li>
              ))}
            </ul>
          )}
        </Card>
      ) : null}

      {isParent ? (
        <Card>
          <p className="font-semibold text-sm">{t('profileLinkedStudentsTitle')}</p>
          {linkedStudents.length === 0 ? <EmptyState text={t('profileNoLinkedStudents')} /> : (
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
          <p className="font-semibold text-sm">{t('profileAdminEditTitle')}</p>
          <p className="mt-1 text-xs text-slate-600">{t('profileAdminEditDescription')}</p>
          <div className="mt-3 grid gap-2 md:grid-cols-2">
            <label className="sk-label" htmlFor="admin-user">{t('profileAdminUserSelectLabel')}</label>
            <select id="admin-user" className="sk-input" value={selectedUserId} onChange={(e) => loadAdminTarget(e.target.value)}>
              <option value="">{t('profileAdminUserSelectPlaceholder')}</option>
              {users.map((user) => (
                <option key={user.id} value={user.id}>{user.firstName} {user.lastName} ({user.userType})</option>
              ))}
            </select>
          </div>

          {selectedUserId ? (
            <div className="mt-3 grid gap-2 md:grid-cols-2">
              <Field label={t('profileFieldFirstName')} value={adminDraft.firstName} onChange={(value) => setAdminDraft((v) => ({ ...v, firstName: value }))} />
              <Field label={t('profileFieldLastName')} value={adminDraft.lastName} onChange={(value) => setAdminDraft((v) => ({ ...v, lastName: value }))} />
              <Field label={t('profileFieldPreferredDisplayName')} value={adminDraft.preferredDisplayName ?? ''} onChange={(value) => setAdminDraft((v) => ({ ...v, preferredDisplayName: value }))} />
              <LanguageField label={t('profileFieldPreferredLanguage')} placeholder={t('profileSelectLanguagePlaceholder')} value={adminDraft.preferredLanguage ?? ''} onChange={(value) => setAdminDraft((v) => ({ ...v, preferredLanguage: value }))} />
              <Field label={t('profileFieldPhoneNumber')} value={adminDraft.phoneNumber ?? ''} onChange={(value) => setAdminDraft((v) => ({ ...v, phoneNumber: value }))} />
              <Field label={t('profileFieldPositionTitle')} value={adminDraft.positionTitle ?? ''} onChange={(value) => setAdminDraft((v) => ({ ...v, positionTitle: value }))} />
              <Field label={t('profileFieldPublicContactNote')} value={adminDraft.publicContactNote ?? ''} disabled={!isPlatformAdministrator} onChange={(value) => setAdminDraft((v) => ({ ...v, publicContactNote: value }))} />
              <Field label={t('profileFieldPreferredContactNote')} value={adminDraft.preferredContactNote ?? ''} disabled={!isPlatformAdministrator} onChange={(value) => setAdminDraft((v) => ({ ...v, preferredContactNote: value }))} />
            </div>
          ) : null}

          <div className="mt-3">
            <button className="sk-btn sk-btn-primary" disabled={!selectedUserId} onClick={saveAdminProfile} type="button">{t('profileButtonSaveAdminEdit')}</button>
          </div>
        </Card>
      ) : null}
    </section>
  );
}

function Field({
  icon,
  label,
  value,
  onChange,
  disabled = false
}: {
  icon?: React.ReactNode;
  label: string;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label inline-flex items-center gap-1.5">
        {icon}
        <span>{label}</span>
      </label>
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
  icon,
  label,
  value,
  placeholder,
  onChange,
  disabled = false
}: {
  icon?: React.ReactNode;
  label: string;
  value: string;
  placeholder: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label inline-flex items-center gap-1.5">
        {icon}
        <span>{label}</span>
      </label>
      <select
        className="sk-input"
        value={value}
        disabled={disabled}
        onChange={(e) => onChange(e.target.value)}
      >
        <option value="">{placeholder}</option>
        {supportedLocales.map((locale) => (
          <option key={locale} value={locale}>
            {localeLabels[locale]} ({locale.toUpperCase()})
          </option>
        ))}
      </select>
    </div>
  );
}

function SaveDiskIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M5 4h11l3 3v13H5V4Z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
      <path d="M8 4v6h8V4" stroke="currentColor" strokeWidth="1.8" />
      <path d="M8 20v-6h8v6" stroke="currentColor" strokeWidth="1.8" />
    </svg>
  );
}

function ProfileIdentityIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <circle cx="12" cy="8" r="4" stroke="currentColor" strokeWidth="1.8" />
      <path d="M5 20a7 7 0 0 1 14 0" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfileEmailIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <rect x="3.5" y="5" width="17" height="14" rx="2.5" stroke="currentColor" strokeWidth="1.8" />
      <path d="m5.5 7 6.5 5 6.5-5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function ProfileRoleIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M12 3 4 7v6c0 4.2 2.4 6.8 8 8 5.6-1.2 8-3.8 8-8V7l-8-4Z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
      <path d="m9.5 12 1.8 1.8 3.4-3.6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function ProfileSchoolIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M3 10 12 4l9 6-9 6-9-6Z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
      <path d="M6 12v6m6-2v4m6-8v6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfileAssignmentIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <rect x="3.5" y="4" width="17" height="16" rx="2.5" stroke="currentColor" strokeWidth="1.8" />
      <path d="M7 8h10M7 12h7M7 16h5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfileCardIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <rect x="4" y="5" width="16" height="14" rx="2.5" stroke="currentColor" strokeWidth="1.8" />
      <path d="M7.5 10.5h9M7.5 14h5.5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfileLanguageIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M4 6h10M9 4v2m-3 0c.8 2.6 2.3 4.8 4.5 6.5M5.5 15.5h7M9 15.5l2 4m-2-4-2 4" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
      <circle cx="17.5" cy="10.5" r="3.5" stroke="currentColor" strokeWidth="1.8" />
      <path d="M17.5 7v7M14 10.5h7" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfilePhoneIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M7 4h10v16H7z" stroke="currentColor" strokeWidth="1.8" />
      <path d="M10 7h4M11 17h2" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfilePositionIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <rect x="3.5" y="7" width="17" height="12" rx="2.5" stroke="currentColor" strokeWidth="1.8" />
      <path d="M9 7V5.5A2.5 2.5 0 0 1 11.5 3h1A2.5 2.5 0 0 1 15 5.5V7" stroke="currentColor" strokeWidth="1.8" />
      <path d="M3.5 12h17" stroke="currentColor" strokeWidth="1.8" />
    </svg>
  );
}

function ProfileContactIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M4 6h16v10H8l-4 3V6Z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
      <path d="M8 10h8M8 13h5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfileStatusIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="1.8" />
      <path d="m8.5 12.5 2.2 2.2 4.8-5.2" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function toProfileInitials(value: string) {
  const parts = value
    .split(/[\s._-]+/)
    .map((x) => x.trim())
    .filter((x) => x.length > 0);

  if (parts.length === 0) return 'SK';
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
}
