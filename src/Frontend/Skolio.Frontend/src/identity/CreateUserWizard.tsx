import React, { useCallback, useEffect, useState } from 'react';
import type { CreateUserWizardPayload, CreateUserWizardResult, WizardStudentCandidate, createIdentityApi } from './api';
import type { SessionState } from '../shared/auth/session';
import type { createOrganizationApi } from '../organization/api';
import { useI18n, localeLabels, supportedLocales } from '../i18n';
import { extractValidationErrors } from '../shared/http/httpClient';

// ── Types ─────────────────────────────────────────────────────────────────────

type WizardStep = 1 | 2 | 3 | 4 | 5;

type WizardDraft = {
  // Step 1 – basic account
  email: string;
  userName: string;
  firstName: string;
  lastName: string;
  displayName: string;
  preferredLanguage: string;
  // Step 2 – role and scope
  role: string;
  schoolId: string;
  // Step 3 – profile data
  phoneNumber: string;
  positionTitle: string;
  schoolPlacement: string;
  schoolContextSummary: string;
  parentRelationshipSummary: string;
  contactEmail: string;
  // Step 4 – role-specific links
  linkedStudentProfileId: string;
  parentStudentRelationship: string;
  // Step 5 – activation
  activationPolicy: 'SendActivationEmail';
};

type SchoolOption = { id: string; name: string; schoolType: string };
type FieldErrors = Record<string, string>;

const EMPTY_DRAFT: WizardDraft = {
  email: '',
  userName: '',
  firstName: '',
  lastName: '',
  displayName: '',
  preferredLanguage: '',
  role: '',
  schoolId: '',
  phoneNumber: '',
  positionTitle: '',
  schoolPlacement: '',
  schoolContextSummary: '',
  parentRelationshipSummary: '',
  contactEmail: '',
  linkedStudentProfileId: '',
  parentStudentRelationship: '',
  activationPolicy: 'SendActivationEmail',
};

const SUPPORTED_ROLES_WIZARD = [
  'SchoolAdministrator',
  'Teacher',
  'Parent',
  'Student',
];

// ── Component ─────────────────────────────────────────────────────────────────

export function CreateUserWizard({
  api,
  organizationApi,
  session,
  initialSchoolId,
  onSuccess,
  onCancel,
}: {
  api: ReturnType<typeof createIdentityApi>;
  organizationApi?: ReturnType<typeof createOrganizationApi>;
  session: SessionState;
  initialSchoolId?: string;
  onSuccess: (result: CreateUserWizardResult) => void;
  onCancel: () => void;
}) {
  const { t } = useI18n();
  const isPlatformAdministrator = session.roles.includes('PlatformAdministrator');

  const [step, setStep] = useState<WizardStep>(1);
  const [draft, setDraft] = useState<WizardDraft>(() => ({
    ...EMPTY_DRAFT,
    schoolId: initialSchoolId ?? (isPlatformAdministrator ? '' : (session.schoolIds[0] ?? '')),
    preferredLanguage: 'cs',
  }));
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [formError, setFormError] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [submitResult, setSubmitResult] = useState<CreateUserWizardResult | null>(null);

  // School options (for PlatformAdministrator dropdown)
  const [schoolOptions, setSchoolOptions] = useState<SchoolOption[]>([]);
  const [schoolsLoading, setSchoolsLoading] = useState(false);

  // Student candidates (for Parent role link)
  const [studentCandidates, setStudentCandidates] = useState<WizardStudentCandidate[]>([]);
  const [studentsLoading, setStudentsLoading] = useState(false);
  const [studentsError, setStudentsError] = useState('');

  // Load school options when PlatformAdministrator
  useEffect(() => {
    if (!isPlatformAdministrator || !organizationApi) return;
    setSchoolsLoading(true);
    organizationApi
      .schools({ isActive: true })
      .then((result) => {
        setSchoolOptions(
          result.items.map((s) => ({ id: s.id, name: s.name, schoolType: s.schoolType }))
        );
      })
      .catch(() => setSchoolOptions([]))
      .finally(() => setSchoolsLoading(false));
  }, [isPlatformAdministrator, organizationApi]);

  // For SchoolAdministrator: resolve school names from org API (best-effort)
  const [schoolAdminSchoolNames, setSchoolAdminSchoolNames] = useState<Record<string, string>>({});
  useEffect(() => {
    if (isPlatformAdministrator || !organizationApi || session.schoolIds.length === 0) return;
    const ids = session.schoolIds;
    Promise.allSettled(ids.map((id) => organizationApi.school(id))).then((results) => {
      const map: Record<string, string> = {};
      results.forEach((r, i) => {
        if (r.status === 'fulfilled') {
          map[ids[i]] = r.value.name;
        }
      });
      setSchoolAdminSchoolNames(map);
    });
  }, [isPlatformAdministrator, organizationApi, session.schoolIds]);

  // Load student candidates when role=Parent and schoolId known
  const loadStudentCandidates = useCallback(
    (schoolId: string) => {
      setStudentsLoading(true);
      setStudentsError('');
      api
        .adminWizardStudentCandidates(schoolId || undefined)
        .then(setStudentCandidates)
        .catch((e: Error) => setStudentsError(e.message))
        .finally(() => setStudentsLoading(false));
    },
    [api]
  );

  // Trigger student load when arriving at step 4 with role=Parent
  useEffect(() => {
    if (step === 4 && draft.role === 'Parent') {
      loadStudentCandidates(draft.schoolId);
    }
  }, [step, draft.role, draft.schoolId, loadStudentCandidates]);

  const set = (key: keyof WizardDraft, value: string) => {
    setDraft((d) => ({ ...d, [key]: value }));
    setFieldErrors((e) => ({ ...e, [key]: '' }));
    setFormError('');
  };

  // ── Step validation ───────────────────────────────────────────────────────

  const validateStep = (s: WizardStep): FieldErrors => {
    const errors: FieldErrors = {};

    if (s === 1) {
      if (!draft.email.trim()) errors.email = t('createUserWizardValidationEmailRequired');
      else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(draft.email.trim()))
        errors.email = t('createUserWizardValidationEmailInvalid');
      if (!draft.userName.trim()) errors.userName = t('createUserWizardValidationUserNameRequired');
      if (!draft.firstName.trim()) errors.firstName = t('createUserWizardValidationFirstNameRequired');
      if (!draft.lastName.trim()) errors.lastName = t('createUserWizardValidationLastNameRequired');
    }

    if (s === 2) {
      if (!draft.role) errors.role = t('createUserWizardValidationRoleRequired');
      if (draft.role && draft.role !== 'PlatformAdministrator' && !draft.schoolId)
        errors.schoolId = t('createUserWizardValidationSchoolRequired');
    }

    if (s === 4) {
      if (draft.role === 'Parent') {
        if (!draft.linkedStudentProfileId)
          errors.linkedStudentProfileId = t('createUserWizardValidationLinkedStudentRequired');
        if (!draft.parentStudentRelationship.trim())
          errors.parentStudentRelationship = t('createUserWizardValidationRelationshipRequired');
      }
    }

    return errors;
  };

  const canSkipStep4 = draft.role !== 'Parent';

  const handleNext = () => {
    const errors = validateStep(step);
    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors);
      return;
    }
    // Skip step 4 if role has no links
    const nextStep = step === 3 && canSkipStep4 ? 5 : ((step + 1) as WizardStep);
    setStep(nextStep);
  };

  const handleBack = () => {
    // If going back from step 5 and role has no links, skip step 4
    const prevStep = step === 5 && canSkipStep4 ? 3 : ((step - 1) as WizardStep);
    setStep(prevStep as WizardStep);
    setFieldErrors({});
    setFormError('');
  };

  const handleSubmit = async () => {
    const errors = validateStep(5);
    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors);
      return;
    }

    setSubmitting(true);
    setFormError('');

    const payload: CreateUserWizardPayload = {
      email: draft.email.trim(),
      userName: draft.userName.trim(),
      firstName: draft.firstName.trim(),
      lastName: draft.lastName.trim(),
      displayName: draft.displayName.trim() || null,
      preferredLanguage: draft.preferredLanguage || null,
      role: draft.role,
      schoolId: draft.schoolId || null,
      phoneNumber: draft.phoneNumber.trim() || null,
      positionTitle: draft.positionTitle.trim() || null,
      schoolPlacement: draft.schoolPlacement.trim() || null,
      schoolContextSummary: draft.schoolContextSummary.trim() || null,
      parentRelationshipSummary: draft.parentRelationshipSummary.trim() || null,
      contactEmail: draft.contactEmail.trim() || null,
      linkedStudentProfileId: draft.linkedStudentProfileId || null,
      parentStudentRelationship: draft.parentStudentRelationship.trim() || null,
      activationPolicy: draft.activationPolicy,
    };

    try {
      const result = await api.adminCreateWizard(payload);
      setSubmitResult(result);
    } catch (e: unknown) {
      const fieldErrs = extractValidationErrors(e);
      if (fieldErrs && Object.keys(fieldErrs).length > 0) {
        setFieldErrors(fieldErrs);
        setFormError(t('createUserWizardError'));
      } else {
        setFormError(e instanceof Error ? e.message : t('createUserWizardError'));
      }
    } finally {
      setSubmitting(false);
    }
  };

  // ── Success screen ────────────────────────────────────────────────────────

  if (submitResult) {
    return (
      <div className="sk-wizard-success">
        <div className="flex flex-col items-center gap-4 py-8">
          <WizardSuccessIcon className="h-12 w-12 text-emerald-500" />
          <div className="text-center">
            <p className="text-lg font-semibold text-slate-900">{t('createUserWizardSuccessTitle')}</p>
            <p className="mt-1 text-sm text-slate-600">{t('createUserWizardSuccessMessage')}</p>
            {submitResult.activationEmailSent && (
              <p className="mt-1 text-sm text-slate-600">
                {t('createUserWizardSuccessActivationSent', { email: submitResult.email })}
              </p>
            )}
            {!submitResult.activationEmailSent && submitResult.accountLifecycleStatus === 'Active' && (
              <p className="mt-1 text-sm text-emerald-600">{t('createUserWizardSuccessAccountActive')}</p>
            )}
          </div>
          <div className="mt-2 rounded-lg border border-slate-200 bg-slate-50 p-4 text-sm">
            <dl className="grid grid-cols-2 gap-x-4 gap-y-1">
              <dt className="text-slate-500">{t('createUserWizardEmailLabel')}</dt>
              <dd className="font-medium text-slate-900">{submitResult.email}</dd>
              <dt className="text-slate-500">{t('createUserWizardUserNameLabel')}</dt>
              <dd className="font-medium text-slate-900">{submitResult.userName}</dd>
              <dt className="text-slate-500">{t('userManagementSectionRoles')}</dt>
              <dd className="font-medium text-slate-900">{submitResult.role}</dd>
            </dl>
          </div>
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              className="sk-btn sk-btn-primary text-sm"
              onClick={() => onSuccess(submitResult)}
            >
              {t('createUserWizardSuccessGoToDetail')}
            </button>
            <button
              type="button"
              className="sk-btn sk-btn-secondary text-sm"
              onClick={() => {
                setSubmitResult(null);
                setDraft({ ...EMPTY_DRAFT, schoolId: initialSchoolId ?? (isPlatformAdministrator ? '' : (session.schoolIds[0] ?? '')), preferredLanguage: 'cs' });
                setStep(1);
                setFieldErrors({});
                setFormError('');
              }}
            >
              {t('createUserWizardSuccessCreateAnother')}
            </button>
          </div>
        </div>
      </div>
    );
  }

  // ── Step indicator ────────────────────────────────────────────────────────

  const totalSteps = 5;
  const stepLabels: Record<WizardStep, string> = {
    1: t('createUserWizardStepBasicAccount'),
    2: t('createUserWizardStepRoleScope'),
    3: t('createUserWizardStepProfileData'),
    4: t('createUserWizardStepRoleLinks'),
    5: t('createUserWizardStepActivation'),
  };

  const isLastStep = step === 5;
  const isFirstStep = step === 1;

  return (
    <div className="sk-wizard">
      {/* Header */}
      <div className="sk-wizard-header flex items-start justify-between">
        <div>
          <p className="font-semibold text-sm inline-flex items-center gap-2">
            <WizardCreateIcon className="h-4 w-4 text-slate-600" />
            {t('createUserWizardTitle')}
          </p>
          <p className="mt-0.5 text-xs text-slate-500">{t('createUserWizardSubtitle')}</p>
        </div>
        <button
          type="button"
          className="text-slate-400 hover:text-slate-700"
          onClick={onCancel}
          aria-label={t('createUserWizardCancel')}
        >
          <WizardCloseIcon className="h-5 w-5" />
        </button>
      </div>

      {/* Step indicator */}
      <div className="sk-wizard-steps mt-4 flex items-center gap-1 overflow-x-auto">
        {([1, 2, 3, 4, 5] as WizardStep[]).map((s) => {
          const isActive = s === step;
          const isDone = s < step;
          const isSkipped = (s === 4 && canSkipStep4);
          if (isSkipped) return null;
          return (
            <React.Fragment key={s}>
              <div
                className={`flex shrink-0 items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium transition-colors ${
                  isActive
                    ? 'bg-blue-600 text-white'
                    : isDone
                    ? 'bg-emerald-100 text-emerald-700'
                    : 'bg-slate-100 text-slate-500'
                }`}
              >
                {isDone ? (
                  <WizardStepDoneIcon className="h-3 w-3" />
                ) : (
                  <span className="h-3 w-3 shrink-0 flex items-center justify-center text-[10px] font-bold">{s}</span>
                )}
                <span className="hidden sm:inline">{stepLabels[s]}</span>
              </div>
              {s < 5 && !((s === 3 && canSkipStep4)) ? (
                <WizardStepArrowIcon className="h-3 w-3 shrink-0 text-slate-300" />
              ) : null}
            </React.Fragment>
          );
        })}
      </div>

      {/* Step content */}
      <div className="sk-wizard-body mt-4">
        {step === 1 && (
          <Step1BasicAccount
            draft={draft}
            fieldErrors={fieldErrors}
            set={set}
            t={t}
          />
        )}
        {step === 2 && (
          <Step2RoleScope
            draft={draft}
            fieldErrors={fieldErrors}
            set={set}
            t={t}
            isPlatformAdministrator={isPlatformAdministrator}
            schoolOptions={schoolOptions}
            schoolsLoading={schoolsLoading}
            sessionSchoolIds={session.schoolIds}
            schoolAdminSchoolNames={schoolAdminSchoolNames}
          />
        )}
        {step === 3 && (
          <Step3ProfileData
            draft={draft}
            fieldErrors={fieldErrors}
            set={set}
            t={t}
          />
        )}
        {step === 4 && (
          <Step4RoleLinks
            draft={draft}
            fieldErrors={fieldErrors}
            set={set}
            t={t}
            studentCandidates={studentCandidates}
            studentsLoading={studentsLoading}
            studentsError={studentsError}
          />
        )}
        {step === 5 && (
          <Step5Activation
            draft={draft}
            fieldErrors={fieldErrors}
            set={set}
            t={t}
            isPlatformAdministrator={isPlatformAdministrator}
            summaryEmail={draft.email}
            summaryRole={draft.role}
            summaryName={`${draft.firstName} ${draft.lastName}`.trim()}
          />
        )}
      </div>

      {/* Form error */}
      {formError ? (
        <div className="mt-3 rounded-md bg-rose-50 border border-rose-200 px-3 py-2 text-sm text-rose-700">
          {formError}
        </div>
      ) : null}

      {/* Navigation */}
      <div className="sk-wizard-footer mt-4 flex items-center justify-between border-t border-slate-100 pt-4">
        <button
          type="button"
          className="sk-btn sk-btn-secondary text-sm"
          onClick={onCancel}
          disabled={submitting}
        >
          {t('createUserWizardCancel')}
        </button>
        <div className="flex gap-2">
          {!isFirstStep && (
            <button
              type="button"
              className="sk-btn sk-btn-secondary text-sm"
              onClick={handleBack}
              disabled={submitting}
            >
              <WizardBackIcon className="mr-1 h-3.5 w-3.5" />
              {t('createUserWizardBack')}
            </button>
          )}
          {!isLastStep && (
            <button
              type="button"
              className="sk-btn sk-btn-primary text-sm"
              onClick={handleNext}
              disabled={submitting}
            >
              {t('createUserWizardNext')}
              <WizardNextIcon className="ml-1 h-3.5 w-3.5" />
            </button>
          )}
          {isLastStep && (
            <button
              type="button"
              className="sk-btn sk-btn-primary text-sm"
              onClick={handleSubmit}
              disabled={submitting}
            >
              {submitting ? t('createUserWizardSubmitting') : t('createUserWizardSubmit')}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

// ── Step 1 – Basic account ────────────────────────────────────────────────────

function Step1BasicAccount({
  draft,
  fieldErrors,
  set,
  t,
}: {
  draft: WizardDraft;
  fieldErrors: FieldErrors;
  set: (k: keyof WizardDraft, v: string) => void;
  t: (key: string, params?: Record<string, string | number>) => string;
}) {
  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2 text-xs font-semibold text-slate-600 uppercase tracking-wide">
        <WizardStep1Icon className="h-4 w-4" />
        {t('createUserWizardStepBasicAccount')}
      </div>
      <div className="grid gap-3 sm:grid-cols-2">
        <WizardField
          label={t('createUserWizardFirstNameLabel')}
          required
          value={draft.firstName}
          onChange={(v) => set('firstName', v)}
          error={fieldErrors.firstName}
          autoFocus
        />
        <WizardField
          label={t('createUserWizardLastNameLabel')}
          required
          value={draft.lastName}
          onChange={(v) => set('lastName', v)}
          error={fieldErrors.lastName}
        />
      </div>
      <WizardField
        label={t('createUserWizardDisplayNameLabel')}
        value={draft.displayName}
        onChange={(v) => set('displayName', v)}
        error={fieldErrors.displayName}
        hint={`${draft.firstName} ${draft.lastName}`.trim() || undefined}
      />
      <WizardField
        label={t('createUserWizardEmailLabel')}
        required
        value={draft.email}
        onChange={(v) => set('email', v)}
        error={fieldErrors.email}
        type="email"
        placeholder={t('createUserWizardEmailPlaceholder')}
      />
      <WizardField
        label={t('createUserWizardUserNameLabel')}
        required
        value={draft.userName}
        onChange={(v) => set('userName', v)}
        error={fieldErrors.userName}
        placeholder={t('createUserWizardUserNamePlaceholder')}
      />
      <WizardSelectField
        label={t('createUserWizardPreferredLanguageLabel')}
        value={draft.preferredLanguage}
        onChange={(v) => set('preferredLanguage', v)}
        error={fieldErrors.preferredLanguage}
        options={supportedLocales.map((loc) => ({ value: loc, label: localeLabels[loc] }))}
      />
    </div>
  );
}

// ── Step 2 – Role and scope ───────────────────────────────────────────────────

function Step2RoleScope({
  draft,
  fieldErrors,
  set,
  t,
  isPlatformAdministrator,
  schoolOptions,
  schoolsLoading,
  sessionSchoolIds,
  schoolAdminSchoolNames,
}: {
  draft: WizardDraft;
  fieldErrors: FieldErrors;
  set: (k: keyof WizardDraft, v: string) => void;
  t: (key: string, params?: Record<string, string | number>) => string;
  isPlatformAdministrator: boolean;
  schoolOptions: SchoolOption[];
  schoolsLoading: boolean;
  sessionSchoolIds: string[];
  schoolAdminSchoolNames: Record<string, string>;
}) {
  const roleOptions = [
    { value: '', label: `– ${t('createUserWizardRolePlaceholder')} –` },
    ...SUPPORTED_ROLES_WIZARD.map((r) => ({ value: r, label: r })),
    ...(isPlatformAdministrator ? [{ value: 'PlatformAdministrator', label: 'PlatformAdministrator' }] : []),
  ];

  const schoolScopeRequired = draft.role && draft.role !== 'PlatformAdministrator';

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2 text-xs font-semibold text-slate-600 uppercase tracking-wide">
        <WizardStep2Icon className="h-4 w-4" />
        {t('createUserWizardStepRoleScope')}
      </div>

      <WizardSelectField
        label={t('createUserWizardRoleLabel')}
        required
        value={draft.role}
        onChange={(v) => set('role', v)}
        error={fieldErrors.role}
        options={roleOptions}
      />

      {draft.role ? (
        <p className="text-xs text-slate-500 bg-slate-50 rounded px-2 py-1.5">
          {t(`createUserWizardRoleInfo_${draft.role}` as Parameters<typeof t>[0])}
        </p>
      ) : null}

      {schoolScopeRequired ? (
        isPlatformAdministrator ? (
          <div>
            <label className="sk-label">
              {t('createUserWizardSchoolLabel')}
              <span className="ml-1 text-rose-500">*</span>
            </label>
            {schoolsLoading ? (
              <p className="mt-1 text-xs text-slate-500">{t('createUserWizardSchoolLoadingLabel')}</p>
            ) : (
              <select
                className={`sk-input mt-1 ${fieldErrors.schoolId ? 'border-rose-400' : ''}`}
                value={draft.schoolId}
                onChange={(e) => set('schoolId', e.target.value)}
              >
                <option value="">{`– ${t('createUserWizardSchoolPlaceholder')} –`}</option>
                {schoolOptions.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name} ({s.schoolType})
                  </option>
                ))}
              </select>
            )}
            {fieldErrors.schoolId ? (
              <p className="mt-0.5 text-xs text-rose-600">{fieldErrors.schoolId}</p>
            ) : null}
          </div>
        ) : (
          <div>
            <label className="sk-label">{t('createUserWizardSchoolLabel')}</label>
            {sessionSchoolIds.length === 1 ? (
              <p className="mt-1 text-sm font-medium text-slate-700">
                {schoolAdminSchoolNames[sessionSchoolIds[0]] ?? sessionSchoolIds[0]}
              </p>
            ) : sessionSchoolIds.length > 1 ? (
              <select
                className="sk-input mt-1"
                value={draft.schoolId}
                onChange={(e) => set('schoolId', e.target.value)}
              >
                {sessionSchoolIds.map((id) => (
                  <option key={id} value={id}>
                    {schoolAdminSchoolNames[id] ?? id}
                  </option>
                ))}
              </select>
            ) : null}
            <p className="mt-1 text-xs text-slate-500">{t('createUserWizardSchoolLockedInfo')}</p>
          </div>
        )
      ) : null}
    </div>
  );
}

// ── Step 3 – Profile data ─────────────────────────────────────────────────────

function Step3ProfileData({
  draft,
  fieldErrors,
  set,
  t,
}: {
  draft: WizardDraft;
  fieldErrors: FieldErrors;
  set: (k: keyof WizardDraft, v: string) => void;
  t: (key: string, params?: Record<string, string | number>) => string;
}) {
  const role = draft.role;
  const showTeacherFields = role === 'Teacher' || role === 'SchoolAdministrator';
  const showParentFields = role === 'Parent';
  const showStudentFields = role === 'Student';

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2 text-xs font-semibold text-slate-600 uppercase tracking-wide">
        <WizardStep3Icon className="h-4 w-4" />
        {t('createUserWizardStepProfileData')}
      </div>

      <WizardField
        label={t('createUserWizardPhoneLabel')}
        value={draft.phoneNumber}
        onChange={(v) => set('phoneNumber', v)}
        error={fieldErrors.phoneNumber}
        type="tel"
      />

      <WizardField
        label={t('createUserWizardContactEmailLabel')}
        value={draft.contactEmail}
        onChange={(v) => set('contactEmail', v)}
        error={fieldErrors.contactEmail}
        type="email"
      />

      {showTeacherFields ? (
        <>
          <WizardField
            label={t('createUserWizardPositionTitleLabel')}
            value={draft.positionTitle}
            onChange={(v) => set('positionTitle', v)}
            error={fieldErrors.positionTitle}
          />
          <WizardField
            label={t('createUserWizardSchoolContextSummaryLabel')}
            value={draft.schoolContextSummary}
            onChange={(v) => set('schoolContextSummary', v)}
            error={fieldErrors.schoolContextSummary}
          />
        </>
      ) : null}

      {showStudentFields ? (
        <WizardField
          label={t('createUserWizardStudentSchoolPlacementLabel')}
          value={draft.schoolPlacement}
          onChange={(v) => set('schoolPlacement', v)}
          error={fieldErrors.schoolPlacement}
          placeholder="e.g. 3.A"
        />
      ) : null}

      {showParentFields ? (
        <WizardField
          label={t('createUserWizardParentRelationshipSummaryLabel')}
          value={draft.parentRelationshipSummary}
          onChange={(v) => set('parentRelationshipSummary', v)}
          error={fieldErrors.parentRelationshipSummary}
        />
      ) : null}

      {!showTeacherFields && !showParentFields && !showStudentFields ? (
        <p className="text-xs text-slate-500 bg-slate-50 rounded px-2 py-2">
          {t('createUserWizardStep3NoExtraFieldsInfo')}
        </p>
      ) : null}
    </div>
  );
}

// ── Step 4 – Role-specific links ──────────────────────────────────────────────

function Step4RoleLinks({
  draft,
  fieldErrors,
  set,
  t,
  studentCandidates,
  studentsLoading,
  studentsError,
}: {
  draft: WizardDraft;
  fieldErrors: FieldErrors;
  set: (k: keyof WizardDraft, v: string) => void;
  t: (key: string, params?: Record<string, string | number>) => string;
  studentCandidates: WizardStudentCandidate[];
  studentsLoading: boolean;
  studentsError: string;
}) {
  const role = draft.role;

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2 text-xs font-semibold text-slate-600 uppercase tracking-wide">
        <WizardStep4Icon className="h-4 w-4" />
        {t('createUserWizardStepRoleLinks')}
      </div>

      {role === 'Parent' ? (
        <>
          <div>
            <label className="sk-label">
              {t('createUserWizardLinkedStudentLabel')}
              <span className="ml-1 text-rose-500">*</span>
            </label>
            {studentsLoading ? (
              <p className="mt-1 text-xs text-slate-500">{t('createUserWizardStudentLoadingLabel')}</p>
            ) : studentsError ? (
              <p className="mt-1 text-xs text-rose-600">{studentsError}</p>
            ) : (
              <select
                className={`sk-input mt-1 ${fieldErrors.linkedStudentProfileId ? 'border-rose-400' : ''}`}
                value={draft.linkedStudentProfileId}
                onChange={(e) => set('linkedStudentProfileId', e.target.value)}
              >
                <option value="">{`– ${t('createUserWizardLinkedStudentPlaceholder')} –`}</option>
                {studentCandidates.map((s) => (
                  <option key={s.profileId} value={s.profileId}>
                    {s.displayName} ({s.email}){s.schoolPlacement ? ` – ${s.schoolPlacement}` : ''}
                  </option>
                ))}
              </select>
            )}
            {fieldErrors.linkedStudentProfileId ? (
              <p className="mt-0.5 text-xs text-rose-600">{fieldErrors.linkedStudentProfileId}</p>
            ) : null}
          </div>

          <WizardField
            label={t('createUserWizardParentStudentRelationshipLabel')}
            required
            value={draft.parentStudentRelationship}
            onChange={(v) => set('parentStudentRelationship', v)}
            error={fieldErrors.parentStudentRelationship}
            placeholder={t('createUserWizardParentStudentRelationshipPlaceholder')}
          />
        </>
      ) : (
        <p className="text-xs text-slate-500 bg-slate-50 rounded px-2 py-2">
          {t('createUserWizardStep4NoLinksRequiredInfo')}
        </p>
      )}
    </div>
  );
}

// ── Step 5 – Activation ───────────────────────────────────────────────────────

function Step5Activation({
  draft,
  fieldErrors,
  set,
  t,
  isPlatformAdministrator,
  summaryEmail,
  summaryRole,
  summaryName,
}: {
  draft: WizardDraft;
  fieldErrors: FieldErrors;
  set: (k: keyof WizardDraft, v: string) => void;
  t: (key: string, params?: Record<string, string | number>) => string;
  isPlatformAdministrator: boolean;
  summaryEmail: string;
  summaryRole: string;
  summaryName: string;
}) {
  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2 text-xs font-semibold text-slate-600 uppercase tracking-wide">
        <WizardStep5Icon className="h-4 w-4" />
        {t('createUserWizardStepActivation')}
      </div>

      {/* Summary */}
      <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm">
        <p className="font-medium text-slate-700 mb-2">{t('createUserWizardSummaryTitle')}</p>
        <dl className="grid grid-cols-2 gap-x-4 gap-y-1 text-xs">
          <dt className="text-slate-500">{t('createUserWizardFirstNameLabel')} / {t('createUserWizardLastNameLabel')}</dt>
          <dd className="font-medium text-slate-900">{summaryName}</dd>
          <dt className="text-slate-500">{t('createUserWizardEmailLabel')}</dt>
          <dd className="font-medium text-slate-900">{summaryEmail}</dd>
          <dt className="text-slate-500">{t('createUserWizardRoleLabel')}</dt>
          <dd className="font-medium text-slate-900">{summaryRole}</dd>
        </dl>
      </div>

      {/* Activation policy */}
      <div>
        <label className="sk-label">{t('createUserWizardActivationPolicyLabel')}</label>
        <div className="mt-1 space-y-2">
          <label className="flex items-start gap-2 cursor-pointer">
            <input
              type="radio"
              name="activationPolicy"
              value="SendActivationEmail"
              checked={draft.activationPolicy === 'SendActivationEmail'}
              onChange={() => set('activationPolicy', 'SendActivationEmail')}
              className="mt-0.5"
            />
            <div>
              <p className="text-sm font-medium text-slate-800">{t('createUserWizardActivationPolicySendEmail')}</p>
              <p className="text-xs text-slate-500">{t('createUserWizardActivationInfo')}</p>
            </div>
          </label>

        </div>
        {fieldErrors.activationPolicy ? (
          <p className="mt-0.5 text-xs text-rose-600">{fieldErrors.activationPolicy}</p>
        ) : null}
      </div>
    </div>
  );
}

// ── Shared field primitives ───────────────────────────────────────────────────

function WizardField({
  label,
  required = false,
  value,
  onChange,
  error,
  type = 'text',
  placeholder,
  hint,
  autoFocus = false,
}: {
  label: string;
  required?: boolean;
  value: string;
  onChange: (v: string) => void;
  error?: string;
  type?: string;
  placeholder?: string;
  hint?: string;
  autoFocus?: boolean;
}) {
  return (
    <div>
      <label className="sk-label">
        {label}
        {required ? <span className="ml-1 text-rose-500">*</span> : null}
      </label>
      <input
        className={`sk-input mt-1 ${error ? 'border-rose-400' : ''}`}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        autoFocus={autoFocus}
      />
      {hint && !value ? <p className="mt-0.5 text-xs text-slate-400">{hint}</p> : null}
      {error ? <p className="mt-0.5 text-xs text-rose-600">{error}</p> : null}
    </div>
  );
}

function WizardSelectField({
  label,
  required = false,
  value,
  onChange,
  error,
  options,
}: {
  label: string;
  required?: boolean;
  value: string;
  onChange: (v: string) => void;
  error?: string;
  options: { value: string; label: string }[];
}) {
  return (
    <div>
      <label className="sk-label">
        {label}
        {required ? <span className="ml-1 text-rose-500">*</span> : null}
      </label>
      <select
        className={`sk-input mt-1 ${error ? 'border-rose-400' : ''}`}
        value={value}
        onChange={(e) => onChange(e.target.value)}
      >
        {options.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
      {error ? <p className="mt-0.5 text-xs text-rose-600">{error}</p> : null}
    </div>
  );
}

// ── Icons ─────────────────────────────────────────────────────────────────────

function WizardCreateIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <circle cx="12" cy="8" r="4" stroke="currentColor" strokeWidth="1.8" />
      <path d="M4 20c0-4 3.6-7 8-7" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <path d="M18 14v6M15 17h6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function WizardCloseIcon({ className = 'h-5 w-5' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M18 6 6 18M6 6l12 12" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function WizardBackIcon({ className = 'h-3.5 w-3.5' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="m15 18-6-6 6-6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function WizardNextIcon({ className = 'h-3.5 w-3.5' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="m9 18 6-6-6-6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function WizardStepDoneIcon({ className = 'h-3 w-3' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="m5 13 5 5L19 7" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function WizardStepArrowIcon({ className = 'h-3 w-3' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="m9 18 6-6-6-6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function WizardStep1Icon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <circle cx="12" cy="8" r="4" stroke="currentColor" strokeWidth="1.8" />
      <path d="M4 20c0-4 3.6-7 8-7s8 3 8 7" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function WizardStep2Icon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="1.8" />
      <path d="M9 10.5c0-1.7 1.3-3 3-3s3 1.3 3 3c0 2-3 3-3 4.5M12 18h.01" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function WizardStep3Icon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <rect x="3" y="4" width="18" height="16" rx="2" stroke="currentColor" strokeWidth="1.8" />
      <path d="M7 9h10M7 13h6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function WizardStep4Icon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M10 14 7.5 16.5a3 3 0 0 1-4.2-4.2L6 9.6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <path d="m14 10 2.5-2.5a3 3 0 0 1 4.2 4.2L18 14.4" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <path d="m8.5 15.5 7-7" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function WizardStep5Icon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M4 4h16v16H4z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
      <path d="M8 10h8M8 14h5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <path d="m15 14 2 2 3-3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function WizardSuccessIcon({ className = 'h-12 w-12' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="1.8" />
      <path d="m8 12 3 3 5-6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
