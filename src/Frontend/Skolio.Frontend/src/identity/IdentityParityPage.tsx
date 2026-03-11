import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { AdminUserListQuery, CreateUserWizardResult, IdentityManagedUser, IdentityManagedUserDetail, IdentityManagedUserSummary, PagedResult, SchoolContextOption, createIdentityApi, MyProfileSummary, SchoolPositionOption, SelfProfileUpdatePayload, UserProfile } from './api';
import type { SessionState } from '../shared/auth/session';
import type { createOrganizationApi, TeacherAssignment } from '../organization/api';
import { Card } from '../shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from '../shared/ui/states';
import { localeLabels, supportedLocales, useI18n } from '../i18n';
import { extractValidationErrors } from '../shared/http/httpClient';
import { CreateUserWizard } from './CreateUserWizard';

type ProfileDraft = SelfProfileUpdatePayload;
type TeacherProfileTab = 'basic' | 'address' | 'employment' | 'schoolContext' | 'teachingAssignments';
type SchoolAdministratorProfileTab = 'basic' | 'addressContact' | 'employment' | 'schoolContext' | 'managedSchools' | 'administrativeOverview';
type ParentProfileTab = 'basic' | 'addressContact' | 'delivery' | 'linkedStudents' | 'relationshipsContext' | 'communication';
type PlatformAdministratorProfileTab = 'basic' | 'addressContact' | 'platformRoleContext' | 'managedAreas' | 'administrativeOverview';
type ManagedUserDetailTab = 'basic' | 'roles' | 'accountState' | 'security' | 'schoolContext' | 'links';
type ManagedUserRowVisualState = 'active' | 'deactivated' | 'locked' | 'pending';

const USER_MANAGEMENT_SCHOOL_CONTEXT_STORAGE_KEY = 'skolio.identity.userManagement.schoolContextId';

const EMPTY_DRAFT: ProfileDraft = {
  firstName: '',
  lastName: '',
  preferredDisplayName: '',
  preferredLanguage: '',
  phoneNumber: '',
  gender: '',
  dateOfBirth: '',
  nationalIdNumber: '',
  birthPlace: '',
  permanentAddress: '',
  correspondenceAddress: '',
  contactEmail: '',
  legalGuardian1: '',
  legalGuardian2: '',
  schoolPlacement: '',
  healthInsuranceProvider: '',
  pediatrician: '',
  healthSafetyNotes: '',
  supportMeasuresSummary: '',
  positionTitle: '',
  teacherRoleLabel: '',
  qualificationSummary: '',
  schoolContextSummary: '',
  parentRelationshipSummary: '',
  deliveryContactName: '',
  deliveryContactPhone: '',
  preferredContactChannel: '',
  communicationPreferencesSummary: '',
  publicContactNote: '',
  preferredContactNote: '',
  administrativeWorkDesignation: '',
  administrativeOrganizationSummary: '',
  platformRoleContextSummary: '',
  managedPlatformAreasSummary: '',
  administrativeBoundarySummary: ''
};

function resolveManagedUserRowVisualState(user: IdentityManagedUser): ManagedUserRowVisualState {
  const lifecycleStatus = user.accountLifecycleStatus.trim().toLowerCase();
  const isLockedOrBlocked = Boolean(user.blockedAtUtc)
    || lifecycleStatus.includes('locked')
    || lifecycleStatus.includes('blocked');

  if (isLockedOrBlocked) {
    return 'locked';
  }

  const isDeactivatedOrSoftDeleted = lifecycleStatus.includes('deactivated')
    || lifecycleStatus.includes('softdeleted')
    || lifecycleStatus.includes('soft deleted')
    || lifecycleStatus.includes('deleted');

  if (isDeactivatedOrSoftDeleted) {
    return 'deactivated';
  }

  const isPendingActivation = lifecycleStatus.includes('pending')
    || (!user.activatedAtUtc && lifecycleStatus !== 'active');

  if (isPendingActivation) {
    return 'pending';
  }

  return 'active';
}

function getManagedUserRowClassName(user: IdentityManagedUser): string {
  const state = resolveManagedUserRowVisualState(user);

  if (state === 'locked') return 'sk-user-management-row sk-user-management-row-locked';
  if (state === 'deactivated') return 'sk-user-management-row sk-user-management-row-deactivated';
  if (state === 'pending') return 'sk-user-management-row sk-user-management-row-pending';
  return 'sk-user-management-row';
}

export function IdentityParityPage({
  api,
  organizationApi,
  session,
  viewMode = 'full'
}: {
  api: ReturnType<typeof createIdentityApi>;
  organizationApi?: ReturnType<typeof createOrganizationApi>;
  session: SessionState;
  viewMode?: 'full' | 'user-management';
}) {
  const { t, locale } = useI18n();
  const [loading, setLoading] = useState(true);
  const [pageError, setPageError] = useState('');
  const [formError, setFormError] = useState('');
  const [formSuccess, setFormSuccess] = useState('');
  const [savingSelf, setSavingSelf] = useState(false);
  const [summary, setSummary] = useState<MyProfileSummary | null>(null);
  const [linkedStudents, setLinkedStudents] = useState<UserProfile[]>([]);
  const [teacherAssignments, setTeacherAssignments] = useState<TeacherAssignment[]>([]);
  const [users, setUsers] = useState<UserProfile[]>([]);
  const [managedUsers, setManagedUsers] = useState<PagedResult<IdentityManagedUser> | null>(null);
  const [managedUsersLoading, setManagedUsersLoading] = useState(false);
  const [managedUsersError, setManagedUsersError] = useState('');
  const [managedSummary, setManagedSummary] = useState<IdentityManagedUserSummary | null>(null);
  const [managedSummaryLoading, setManagedSummaryLoading] = useState(false);
  const [managedSummaryError, setManagedSummaryError] = useState('');
  const [managedActionBusyUserId, setManagedActionBusyUserId] = useState('');
  const [managedDetailUserId, setManagedDetailUserId] = useState('');
  const [managedUserDetail, setManagedUserDetail] = useState<IdentityManagedUserDetail | null>(null);
  const [managedUserDetailLoading, setManagedUserDetailLoading] = useState(false);
  const [managedUserDetailError, setManagedUserDetailError] = useState('');
  const [managedRoleSetDraft, setManagedRoleSetDraft] = useState<string[]>([]);
  const [managedBasicDraft, setManagedBasicDraft] = useState({ firstName: '', lastName: '', preferredDisplayName: '', preferredLanguage: '', phoneNumber: '', contactEmail: '', schoolPlacement: '', schoolContextSummary: '', positionTitle: '', parentRelationshipSummary: '' });
  const [managedSchoolIdsDraft, setManagedSchoolIdsDraft] = useState<string[]>([]);
  const [managedParentLinksDraft, setManagedParentLinksDraft] = useState('');
  const [activeManagedUserTab, setActiveManagedUserTab] = useState<ManagedUserDetailTab>('basic');
  const [userListFilters, setUserListFilters] = useState<AdminUserListQuery>({ pageNumber: 1, pageSize: 20, sortField: 'name', sortDirection: 'asc', search: '' });
  const [managedSchoolContextId, setManagedSchoolContextId] = useState<string>(() => localStorage.getItem(USER_MANAGEMENT_SCHOOL_CONTEXT_STORAGE_KEY) ?? '');
  const [managedSchoolOptions, setManagedSchoolOptions] = useState<SchoolContextOption[]>([]);
  const [managedSchoolOptionsLoading, setManagedSchoolOptionsLoading] = useState(false);
  const [managedSchoolOptionsError, setManagedSchoolOptionsError] = useState('');
  const [schoolPositionOptions, setSchoolPositionOptions] = useState<SchoolPositionOption[]>([]);
  const [schoolPositionLoading, setSchoolPositionLoading] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState('');
  const [selfDraft, setSelfDraft] = useState<ProfileDraft>(EMPTY_DRAFT);
  const [adminDraft, setAdminDraft] = useState<ProfileDraft>(EMPTY_DRAFT);
  const [activeTeacherTab, setActiveTeacherTab] = useState<TeacherProfileTab>('basic');
  const [activeSchoolAdminTab, setActiveSchoolAdminTab] = useState<SchoolAdministratorProfileTab>('basic');
  const [activeParentTab, setActiveParentTab] = useState<ParentProfileTab>('basic');
  const [activePlatformAdminTab, setActivePlatformAdminTab] = useState<PlatformAdministratorProfileTab>('basic');
  const [adminUserType, setAdminUserType] = useState('');
  const [adminSchoolPositionOptions, setAdminSchoolPositionOptions] = useState<SchoolPositionOption[]>([]);
  const [adminSchoolPositionLoading, setAdminSchoolPositionLoading] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const managedUsersSectionRef = useRef<HTMLElement | null>(null);

  // Create user wizard state
  const [wizardOpen, setWizardOpen] = useState(false);
  const [wizardSuccessMessage, setWizardSuccessMessage] = useState('');
  const [wizardHighlightUserId, setWizardHighlightUserId] = useState('');

  const isPlatformAdministrator = session.roles.includes('PlatformAdministrator');
  const isSchoolAdministrator = session.roles.includes('SchoolAdministrator');
  const isTeacher = session.roles.includes('Teacher');
  const isParent = session.roles.includes('Parent');
  const isStudentOnly = session.roles.includes('Student')
    && !session.roles.includes('Teacher')
    && !session.roles.includes('Parent')
    && !session.roles.includes('SchoolAdministrator')
    && !session.roles.includes('PlatformAdministrator');

  const canAdminProfiles = isPlatformAdministrator || isSchoolAdministrator;

  const selfEditRules = useMemo(() => ({
    canEditName: !isStudentOnly,
    canEditSchoolPosition: isSchoolAdministrator || isTeacher || isPlatformAdministrator,
    canEditTeacherSection: isSchoolAdministrator || isTeacher || isPlatformAdministrator,
    canEditSchoolContextSummary: isSchoolAdministrator || isPlatformAdministrator,
    canEditParentSection: isParent || isSchoolAdministrator || isPlatformAdministrator,
    canEditParentCommunication: isParent || isSchoolAdministrator || isPlatformAdministrator,
    canEditPublicContactNote: isTeacher || isSchoolAdministrator || isPlatformAdministrator,
    canEditPreferredContactNote: isParent,
    canEditPlatformAdminSummary: isPlatformAdministrator
  }), [isParent, isPlatformAdministrator, isSchoolAdministrator, isStudentOnly, isTeacher]);

  const selectedSchoolId = session.schoolIds[0] ?? '';
  const canShowSchoolPositionField = (isSchoolAdministrator || isTeacher || isPlatformAdministrator) && (schoolPositionLoading || schoolPositionOptions.length > 0);

  const mapToDraft = (profile: UserProfile): ProfileDraft => ({
    firstName: profile.firstName ?? '',
    lastName: profile.lastName ?? '',
    preferredDisplayName: profile.preferredDisplayName ?? '',
    preferredLanguage: profile.preferredLanguage ?? '',
    phoneNumber: profile.phoneNumber ?? '',
    gender: profile.gender ?? '',
    dateOfBirth: profile.dateOfBirth ?? '',
    nationalIdNumber: profile.nationalIdNumber ?? '',
    birthPlace: profile.birthPlace ?? '',
    permanentAddress: profile.permanentAddress ?? '',
    correspondenceAddress: profile.correspondenceAddress ?? '',
    contactEmail: profile.contactEmail ?? '',
    legalGuardian1: profile.legalGuardian1 ?? '',
    legalGuardian2: profile.legalGuardian2 ?? '',
    schoolPlacement: profile.schoolPlacement ?? '',
    healthInsuranceProvider: profile.healthInsuranceProvider ?? '',
    pediatrician: profile.pediatrician ?? '',
    healthSafetyNotes: profile.healthSafetyNotes ?? '',
    supportMeasuresSummary: profile.supportMeasuresSummary ?? '',
    positionTitle: profile.positionTitle ?? '',
    teacherRoleLabel: profile.teacherRoleLabel ?? '',
    qualificationSummary: profile.qualificationSummary ?? '',
    schoolContextSummary: profile.schoolContextSummary ?? '',
    parentRelationshipSummary: profile.parentRelationshipSummary ?? '',
    deliveryContactName: profile.deliveryContactName ?? '',
    deliveryContactPhone: profile.deliveryContactPhone ?? '',
    preferredContactChannel: profile.preferredContactChannel ?? '',
    communicationPreferencesSummary: profile.communicationPreferencesSummary ?? '',
    publicContactNote: profile.publicContactNote ?? '',
    preferredContactNote: profile.preferredContactNote ?? '',
    administrativeWorkDesignation: profile.administrativeWorkDesignation ?? '',
    administrativeOrganizationSummary: profile.administrativeOrganizationSummary ?? '',
    platformRoleContextSummary: profile.platformRoleContextSummary ?? '',
    managedPlatformAreasSummary: profile.managedPlatformAreasSummary ?? '',
    administrativeBoundarySummary: profile.administrativeBoundarySummary ?? ''
  });

  const loadManagedUsers = (next?: Partial<AdminUserListQuery>, forcedSchoolContextId?: string) => {
    const effectiveSchoolContextId = forcedSchoolContextId ?? managedSchoolContextId;
    const merged: AdminUserListQuery = { ...userListFilters, ...next, schoolContextId: effectiveSchoolContextId || undefined };
    setUserListFilters(merged);
    setManagedUsersLoading(true);
    setManagedUsersError('');

    void api.adminUsers(merged)
      .then(setManagedUsers)
      .catch((e: Error) => setManagedUsersError(mapProfileError(e, t)))
      .finally(() => setManagedUsersLoading(false));
  };


  const loadManagedSummary = (forcedSchoolContextId?: string) => {
    const effectiveSchoolContextId = forcedSchoolContextId ?? managedSchoolContextId;
    setManagedSummaryLoading(true);
    setManagedSummaryError('');

    void api.adminUserSummary(effectiveSchoolContextId || undefined)
      .then(setManagedSummary)
      .catch((e: Error) => {
        setManagedSummary(null);
        setManagedSummaryError(mapProfileError(e, t));
      })
      .finally(() => setManagedSummaryLoading(false));
  };

  const openManagedDetail = (userId: string) => {
    setManagedDetailUserId(userId);
    setManagedUserDetailLoading(true);
    setActiveManagedUserTab('basic');
    setManagedUserDetailError('');
    setManagedUserDetail(null);
    setManagedRoleSetDraft([]);
    void api.adminUserDetail(userId, managedSchoolContextId || undefined)
      .then((detail) => {
        setManagedUserDetail(detail);
        setManagedRoleSetDraft(detail.roles);
        setManagedBasicDraft({
          firstName: detail.firstName ?? '',
          lastName: detail.lastName ?? '',
          preferredDisplayName: detail.preferredDisplayName ?? '',
          preferredLanguage: detail.preferredLanguage ?? '',
          phoneNumber: detail.phoneNumber ?? '',
          contactEmail: detail.contactEmail ?? '',
          schoolPlacement: detail.school ?? '',
          schoolContextSummary: detail.schoolType ?? '',
          positionTitle: '',
          parentRelationshipSummary: ''
        });
        setManagedSchoolIdsDraft(detail.schoolIds ?? []);
        setManagedParentLinksDraft('');
      })
      .catch((e: Error) => setManagedUserDetailError(mapProfileError(e, t)))
      .finally(() => setManagedUserDetailLoading(false));
  };

  const closeManagedDetail = () => {
    setManagedDetailUserId('');
    setManagedUserDetail(null);
    setManagedUserDetailError('');
    setManagedUserDetailLoading(false);
    setManagedRoleSetDraft([]);
    setManagedSchoolIdsDraft([]);
    setManagedParentLinksDraft('');
  };

  const runManagedLifecycleAction = (userId: string, action: () => Promise<unknown>) => {
    setManagedActionBusyUserId(userId);
    setFormError('');
    setFormSuccess('');
    void action()
      .then(() => {
        setFormSuccess(t('userManagementActionSuccess'));
        loadManagedUsers();
        if (managedDetailUserId === userId) {
          openManagedDetail(userId);
        }
      })
      .catch((e: Error) => setFormError(mapProfileError(e, t)))
      .finally(() => setManagedActionBusyUserId(''));
  };

  const saveManagedRoleSet = () => {
    if (!managedUserDetail) return;
    if (managedRoleSetDraft.length === 0) {
      setFormError(t('userManagementRoleValidationEmpty'));
      return;
    }

    setFormError('');
    setFormSuccess('');
    setManagedActionBusyUserId(managedUserDetail.userId);
    void api.adminUpdateRoleSet(managedUserDetail.userId, managedRoleSetDraft, managedSchoolContextId || undefined)
      .then(() => {
        setFormSuccess(t('userManagementRolesSavedSuccess'));
        openManagedDetail(managedUserDetail.userId);
        loadManagedUsers();
      })
      .catch((e: Error) => setFormError(mapProfileError(e, t)))
      .finally(() => setManagedActionBusyUserId(''));
  };

  const saveManagedBasic = () => {
    if (!managedUserDetail) return;
    if (!managedBasicDraft.firstName.trim() || !managedBasicDraft.lastName.trim()) {
      setFormError(t('userManagementValidationBasicRequired'));
      return;
    }

    setManagedActionBusyUserId(managedUserDetail.userId);
    setFormError('');
    setFormSuccess('');
    void api.adminUpdateBasicProfile(managedUserDetail.userId, managedBasicDraft, managedSchoolContextId || undefined)
      .then((detail) => {
        setManagedUserDetail(detail);
        setFormSuccess(t('userManagementBasicSavedSuccess'));
        loadManagedUsers();
      })
      .catch((e: Error) => setFormError(mapProfileError(e, t)))
      .finally(() => setManagedActionBusyUserId(''));
  };

  const saveManagedSchoolContext = () => {
    if (!managedUserDetail) return;
    if (managedSchoolIdsDraft.length === 0) {
      setFormError(t('userManagementValidationSchoolContextRequired'));
      return;
    }

    setManagedActionBusyUserId(managedUserDetail.userId);
    setFormError('');
    setFormSuccess('');
    void api.adminUpdateSchoolContext(managedUserDetail.userId, managedSchoolIdsDraft, managedSchoolContextId || undefined)
      .then(() => {
        setFormSuccess(t('userManagementSchoolContextSavedSuccess'));
        openManagedDetail(managedUserDetail.userId);
        loadManagedUsers();
      })
      .catch((e: Error) => setFormError(mapProfileError(e, t)))
      .finally(() => setManagedActionBusyUserId(''));
  };

  const saveManagedParentLinks = () => {
    if (!managedUserDetail) return;
    const parsed = managedParentLinksDraft.split('\n').map((x) => x.trim()).filter((x) => x.length > 0).map((line) => {
      const [studentUserProfileId, relationship] = line.split(':').map((x) => x.trim());
      return { studentUserProfileId, relationship };
    }).filter((x) => x.studentUserProfileId && x.relationship);

    setManagedActionBusyUserId(managedUserDetail.userId);
    setFormError('');
    setFormSuccess('');
    void api.adminUpdateParentLinks(managedUserDetail.userId, parsed, managedSchoolContextId || undefined)
      .then(() => {
        setFormSuccess(t('userManagementLinksSavedSuccess'));
      })
      .catch((e: Error) => setFormError(mapProfileError(e, t)))
      .finally(() => setManagedActionBusyUserId(''));
  };

  const onManagedSchoolContextChange = (nextSchoolContextId: string) => {
    if (!isPlatformAdministrator) return;
    setManagedSchoolContextId(nextSchoolContextId);
    if (nextSchoolContextId) {
      localStorage.setItem(USER_MANAGEMENT_SCHOOL_CONTEXT_STORAGE_KEY, nextSchoolContextId);
    } else {
      localStorage.removeItem(USER_MANAGEMENT_SCHOOL_CONTEXT_STORAGE_KEY);
    }

    setManagedDetailUserId('');
    setManagedUserDetail(null);
    loadManagedUsers({ pageNumber: 1 }, nextSchoolContextId);
    loadManagedSummary(nextSchoolContextId);
  };


  const load = () => {
    setLoading(true);
    setPageError('');
    setFormError('');
    setFormSuccess('');
    setFieldErrors({});

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

        if (viewMode === 'user-management' && (isPlatformAdministrator || isSchoolAdministrator)) {
          if (isPlatformAdministrator) {
            setManagedSchoolOptionsLoading(true);
            setManagedSchoolOptionsError('');
            tasks.push(
              api.adminUserSchools()
                .then((options) => {
                  setManagedSchoolOptions(options);
                  const hasStoredContext = managedSchoolContextId && options.some((x) => x.schoolId === managedSchoolContextId);
                  const resolvedContextId = hasStoredContext ? managedSchoolContextId : '';
                  if (!hasStoredContext) {
                    setManagedSchoolContextId('');
                    localStorage.removeItem(USER_MANAGEMENT_SCHOOL_CONTEXT_STORAGE_KEY);
                  }
                  loadManagedUsers({ pageNumber: 1 }, resolvedContextId);
                  loadManagedSummary(resolvedContextId);
                })
                .catch((e: Error) => {
                  setManagedSchoolOptions([]);
                  setManagedSchoolOptionsError(mapProfileError(e, t));
                  loadManagedUsers({ pageNumber: 1 }, '');
                  loadManagedSummary('');
                })
                .finally(() => setManagedSchoolOptionsLoading(false))
            );
          } else {
            setManagedSchoolOptions([]);
            setManagedSchoolOptionsLoading(false);
            setManagedSchoolOptionsError('');
            setManagedSchoolContextId('');
            localStorage.removeItem(USER_MANAGEMENT_SCHOOL_CONTEXT_STORAGE_KEY);
            loadManagedUsers({ pageNumber: 1 }, '');
            loadManagedSummary('');
          }
        } else {
          setUsers([]);
          setManagedUsers(null);
          setManagedUsersError('');
          setManagedSummary(null);
          setManagedSummaryError('');
          setManagedSummaryLoading(false);
          setSelectedUserId('');
          setAdminDraft(EMPTY_DRAFT);
          setAdminUserType('');
          setAdminSchoolPositionOptions([]);
          setAdminSchoolPositionLoading(false);
        }

        if (selfEditRules.canEditSchoolPosition && selectedSchoolId) {
          setSchoolPositionLoading(true);
          tasks.push(
            api.mySchoolPositionOptions(selectedSchoolId)
              .then(setSchoolPositionOptions)
              .finally(() => setSchoolPositionLoading(false))
          );
        } else {
          setSchoolPositionOptions([]);
          setSchoolPositionLoading(false);
        }

        await Promise.all(tasks);
      })
      .catch((e: Error) => setPageError(mapProfileError(e, t)))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    if (!formSuccess) return;
    const timer = window.setTimeout(() => setFormSuccess(''), 4000);
    return () => window.clearTimeout(timer);
  }, [formSuccess]);

  useEffect(() => {
    if (!managedDetailUserId) return;

    const previousOverflow = document.body.style.overflow;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        closeManagedDetail();
      }
    };

    document.body.style.overflow = 'hidden';
    window.addEventListener('keydown', onKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener('keydown', onKeyDown);
    };
  }, [managedDetailUserId]);

  useEffect(load, [api, organizationApi, session.accessToken]);

  const saveSelfProfile = () => {
    setFormError('');
    setFormSuccess('');
    const nextFieldErrors: Record<string, string> = {};
    if (selfEditRules.canEditName && !selfDraft.firstName.trim()) {
      nextFieldErrors.firstName = t('profileFieldRequired');
    }
    if (selfEditRules.canEditName && !selfDraft.lastName.trim()) {
      nextFieldErrors.lastName = t('profileFieldRequired');
    }
    if (selfDraft.contactEmail && !selfDraft.contactEmail.includes('@')) {
      nextFieldErrors.contactEmail = t('profileValidationEmail');
    }
    if (selfDraft.dateOfBirth && !/^\d{4}-\d{2}-\d{2}$/.test(selfDraft.dateOfBirth)) {
      nextFieldErrors.dateOfBirth = t('profileValidationDate');
    }
    if (selfDraft.teacherRoleLabel && selfDraft.teacherRoleLabel.length > 120) {
      nextFieldErrors.teacherRoleLabel = t('profileSaveErrorValidation');
    }
    if (selfDraft.qualificationSummary && selfDraft.qualificationSummary.length > 1000) {
      nextFieldErrors.qualificationSummary = t('profileSaveErrorValidation');
    }
    if (selfDraft.schoolContextSummary && selfDraft.schoolContextSummary.length > 1000) {
      nextFieldErrors.schoolContextSummary = t('profileSaveErrorValidation');
    }
    if (selfDraft.parentRelationshipSummary && selfDraft.parentRelationshipSummary.length > 500) {
      nextFieldErrors.parentRelationshipSummary = t('profileSaveErrorValidation');
    }
    if (selfDraft.communicationPreferencesSummary && selfDraft.communicationPreferencesSummary.length > 500) {
      nextFieldErrors.communicationPreferencesSummary = t('profileSaveErrorValidation');
    }
    if (selfDraft.administrativeWorkDesignation && selfDraft.administrativeWorkDesignation.length > 120) {
      nextFieldErrors.administrativeWorkDesignation = t('profileSaveErrorValidation');
    }
    if (selfDraft.administrativeOrganizationSummary && selfDraft.administrativeOrganizationSummary.length > 500) {
      nextFieldErrors.administrativeOrganizationSummary = t('profileSaveErrorValidation');
    }
    if (selfDraft.platformRoleContextSummary && selfDraft.platformRoleContextSummary.length > 1000) {
      nextFieldErrors.platformRoleContextSummary = t('profileSaveErrorValidation');
    }
    if (selfDraft.managedPlatformAreasSummary && selfDraft.managedPlatformAreasSummary.length > 1000) {
      nextFieldErrors.managedPlatformAreasSummary = t('profileSaveErrorValidation');
    }
    if (selfDraft.administrativeBoundarySummary && selfDraft.administrativeBoundarySummary.length > 1000) {
      nextFieldErrors.administrativeBoundarySummary = t('profileSaveErrorValidation');
    }
    if (selfDraft.deliveryContactName && selfDraft.deliveryContactName.length > 160) {
      nextFieldErrors.deliveryContactName = t('profileSaveErrorValidation');
    }
    if (selfDraft.deliveryContactPhone && selfDraft.deliveryContactPhone.length > 32) {
      nextFieldErrors.deliveryContactPhone = t('profileSaveErrorValidation');
    }
    if (selfDraft.preferredContactChannel
      && !['EMAIL', 'PHONE', 'APP'].includes(selfDraft.preferredContactChannel.toUpperCase())) {
      nextFieldErrors.preferredContactChannel = t('profileSaveErrorValidation');
    }
    if (selfEditRules.canEditSchoolPosition
      && selfDraft.positionTitle
      && schoolPositionOptions.length > 0
      && !schoolPositionOptions.some((x) => x.code === selfDraft.positionTitle)) {
      nextFieldErrors.positionTitle = t('profileSaveErrorInvalidSchoolPosition');
    }
    setFieldErrors(nextFieldErrors);
    if (Object.keys(nextFieldErrors).length > 0) {
      setFormError(t('profileSaveErrorValidation'));
      return;
    }
    setSavingSelf(true);

    const payload: ProfileDraft = {
      ...selfDraft,
      firstName: selfEditRules.canEditName ? selfDraft.firstName : (summary?.profile.firstName ?? ''),
      lastName: selfEditRules.canEditName ? selfDraft.lastName : (summary?.profile.lastName ?? ''),
      positionTitle: selfEditRules.canEditSchoolPosition ? selfDraft.positionTitle : (summary?.profile.positionTitle ?? ''),
      teacherRoleLabel: selfEditRules.canEditTeacherSection ? selfDraft.teacherRoleLabel : (summary?.profile.teacherRoleLabel ?? ''),
      qualificationSummary: selfEditRules.canEditTeacherSection ? selfDraft.qualificationSummary : (summary?.profile.qualificationSummary ?? ''),
      schoolContextSummary: selfEditRules.canEditSchoolContextSummary ? selfDraft.schoolContextSummary : (summary?.profile.schoolContextSummary ?? ''),
      parentRelationshipSummary: selfEditRules.canEditParentSection ? selfDraft.parentRelationshipSummary : (summary?.profile.parentRelationshipSummary ?? ''),
      deliveryContactName: selfEditRules.canEditParentSection ? selfDraft.deliveryContactName : (summary?.profile.deliveryContactName ?? ''),
      deliveryContactPhone: selfEditRules.canEditParentSection ? selfDraft.deliveryContactPhone : (summary?.profile.deliveryContactPhone ?? ''),
      preferredContactChannel: selfEditRules.canEditParentCommunication ? selfDraft.preferredContactChannel : (summary?.profile.preferredContactChannel ?? ''),
      communicationPreferencesSummary: selfEditRules.canEditParentCommunication ? selfDraft.communicationPreferencesSummary : (summary?.profile.communicationPreferencesSummary ?? ''),
      publicContactNote: selfEditRules.canEditPublicContactNote ? selfDraft.publicContactNote : (summary?.profile.publicContactNote ?? ''),
      preferredContactNote: selfEditRules.canEditPreferredContactNote ? selfDraft.preferredContactNote : (summary?.profile.preferredContactNote ?? '')
    };

    void api.updateMyProfile(payload)
      .then(() => {
        setFormSuccess(t('profileSaveSuccess'));
        load();
      })
      .catch((e: Error) => setFormError(mapProfileError(e, t)))
      .finally(() => setSavingSelf(false));
  };

  const loadAdminTarget = (userId: string) => {
    setSelectedUserId(userId);
    setAdminUserType('');
    setAdminSchoolPositionOptions([]);
    setAdminSchoolPositionLoading(false);
    if (!userId) {
      setAdminDraft(EMPTY_DRAFT);
      return;
    }

    setFormError('');
    void api.userProfile(userId)
      .then(async (profile) => {
        setAdminDraft(mapToDraft(profile));
        setAdminUserType(profile.userType);

        if (profile.userType !== 'Teacher' && profile.userType !== 'SchoolAdministrator') {
          return;
        }

        setAdminSchoolPositionLoading(true);
        try {
          const options = await api.userSchoolPositionOptions(userId, selectedSchoolId || undefined);
          setAdminSchoolPositionOptions(options);
        } finally {
          setAdminSchoolPositionLoading(false);
        }
      })
      .catch((e: Error) => setFormError(mapProfileError(e, t)));
  };

  const saveAdminProfile = () => {
    if (!selectedUserId) return;

    setFormError('');
    setFormSuccess('');
    const payload = isPlatformAdministrator
      ? adminDraft
      : { ...adminDraft, publicContactNote: '', preferredContactNote: '' };

    void api.updateUserProfile(selectedUserId, payload)
      .then(() => {
        setFormSuccess(t('profileAdminSaveSuccess'));
        load();
      })
      .catch((e: Error) => setFormError(mapProfileError(e, t)));
  };

  if (loading) return <LoadingState text={t('profileLoading')} />;
  if (pageError) return <ErrorState text={pageError} />;
  if (!summary) return <EmptyState text={t('profileNotAvailable')} />;

  if (viewMode === 'user-management') {
    if (!canAdminProfiles) {
      return <ErrorState text={t('unauthorizedIdentity')} />;
    }

    const handleWizardSuccess = (result: CreateUserWizardResult) => {
      setWizardOpen(false);
      setWizardSuccessMessage(t('createUserWizardSuccessActivationSent', { email: result.email }));
      setWizardHighlightUserId(result.userId);
      void loadManagedUsers({ pageNumber: 1, search: result.email });
      setTimeout(() => setWizardSuccessMessage(''), 8000);
    };

    if (wizardOpen) {
      return (
        <section className="space-y-4">
          <Card>
            <CreateUserWizard
              api={api}
              organizationApi={organizationApi}
              session={session}
              initialSchoolId={managedSchoolContextId || undefined}
              onSuccess={handleWizardSuccess}
              onCancel={() => setWizardOpen(false)}
            />
          </Card>
        </section>
      );
    }

    return (
      <section className="space-y-4">
        {wizardSuccessMessage ? (
          <div className="rounded-md bg-emerald-50 border border-emerald-200 px-4 py-2 text-sm text-emerald-800 flex items-center justify-between">
            <span>{wizardSuccessMessage}</span>
            <button type="button" className="ml-3 text-emerald-600 hover:text-emerald-900" onClick={() => setWizardSuccessMessage('')}>&times;</button>
          </div>
        ) : null}
        <ManagedUsersSection
          sectionRef={managedUsersSectionRef}
          t={t}
          userListFilters={userListFilters}
          setUserListFilters={setUserListFilters}
          loadManagedUsers={loadManagedUsers}
          managedUsersLoading={managedUsersLoading}
          managedUsersError={managedUsersError}
          managedUsers={managedUsers}
          managedSummary={managedSummary}
          managedSummaryLoading={managedSummaryLoading}
          managedSummaryError={managedSummaryError}
          managedActionBusyUserId={managedActionBusyUserId}
          openManagedDetail={openManagedDetail}
          closeManagedDetail={closeManagedDetail}
          runManagedLifecycleAction={runManagedLifecycleAction}
          api={api}
          managedDetailUserId={managedDetailUserId}
          managedUserDetailLoading={managedUserDetailLoading}
          managedUserDetailError={managedUserDetailError}
          managedUserDetail={managedUserDetail}
          managedBasicDraft={managedBasicDraft}
          setManagedBasicDraft={setManagedBasicDraft}
          saveManagedBasic={saveManagedBasic}
          managedRoleSetDraft={managedRoleSetDraft}
          setManagedRoleSetDraft={setManagedRoleSetDraft}
          saveManagedRoleSet={saveManagedRoleSet}
          managedSchoolIdsDraft={managedSchoolIdsDraft}
          setManagedSchoolIdsDraft={setManagedSchoolIdsDraft}
          saveManagedSchoolContext={saveManagedSchoolContext}
          managedParentLinksDraft={managedParentLinksDraft}
          setManagedParentLinksDraft={setManagedParentLinksDraft}
          saveManagedParentLinks={saveManagedParentLinks}
          activeManagedUserTab={activeManagedUserTab}
          setActiveManagedUserTab={setActiveManagedUserTab}
          isPlatformAdministrator={isPlatformAdministrator}
          managedSchoolContextId={managedSchoolContextId}
          managedSchoolOptions={managedSchoolOptions}
          managedSchoolOptionsLoading={managedSchoolOptionsLoading}
          managedSchoolOptionsError={managedSchoolOptionsError}
          onManagedSchoolContextChange={onManagedSchoolContextChange}
          onOpenCreateWizard={() => setWizardOpen(true)}
          wizardHighlightUserId={wizardHighlightUserId}
        />
      </section>
    );
  }

  const headerInitials = toProfileInitials(
    summary.profile.preferredDisplayName
    || `${summary.profile.firstName} ${summary.profile.lastName}`.trim()
    || summary.profile.email
  );
  const isSchoolAdministratorScopedProfile = summary.profile.userType === 'SchoolAdministrator' || isSchoolAdministrator;
  const isTeacherScopedProfile = summary.profile.userType === 'Teacher' || isTeacher;
  const isParentScopedProfile = summary.profile.userType === 'Parent' || isParent;
  const isPlatformAdministratorScopedProfile = summary.profile.userType === 'SupportStaff' && isPlatformAdministrator;

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

      <Card>
        <p className="font-semibold text-sm">{t('myProfile.personalEdit')}</p>
        {formSuccess ? (
          <FeedbackBanner
            type="success"
            message={formSuccess}
            dismissLabel={t('profileDismiss')}
            onDismiss={() => setFormSuccess('')}
          />
        ) : null}
        {formError ? (
          <FeedbackBanner
            type="error"
            message={formError}
            dismissLabel={t('profileDismiss')}
            onDismiss={() => setFormError('')}
          />
        ) : null}
        {isPlatformAdministratorScopedProfile ? (
          <>
            <div className="mt-3 flex flex-wrap gap-2">
              <button type="button" className={`sk-btn ${activePlatformAdminTab === 'basic' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActivePlatformAdminTab('basic')}>{t('profileTabPlatformAdminBasic')}</button>
              <button type="button" className={`sk-btn ${activePlatformAdminTab === 'addressContact' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActivePlatformAdminTab('addressContact')}>{t('profileTabPlatformAdminAddressContact')}</button>
              <button type="button" className={`sk-btn ${activePlatformAdminTab === 'platformRoleContext' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActivePlatformAdminTab('platformRoleContext')}>{t('profileTabPlatformAdminRoleContext')}</button>
              <button type="button" className={`sk-btn ${activePlatformAdminTab === 'managedAreas' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActivePlatformAdminTab('managedAreas')}>{t('profileTabPlatformAdminManagedAreas')}</button>
              <button type="button" className={`sk-btn ${activePlatformAdminTab === 'administrativeOverview' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActivePlatformAdminTab('administrativeOverview')}>{t('profileTabPlatformAdminAdministrativeOverview')}</button>
            </div>
            <div className="mt-3 grid gap-2 md:grid-cols-2">
              {activePlatformAdminTab === 'basic' ? (
                <>
                  <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldFirstName')} value={selfDraft.firstName} disabled={!selfEditRules.canEditName || savingSelf} invalid={Boolean(fieldErrors.firstName)} errorText={fieldErrors.firstName} onChange={(value) => { setFieldErrors((v) => ({ ...v, firstName: undefined })); setSelfDraft((v) => ({ ...v, firstName: value })); }} />
                  <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldLastName')} value={selfDraft.lastName} disabled={!selfEditRules.canEditName || savingSelf} invalid={Boolean(fieldErrors.lastName)} errorText={fieldErrors.lastName} onChange={(value) => { setFieldErrors((v) => ({ ...v, lastName: undefined })); setSelfDraft((v) => ({ ...v, lastName: value })); }} />
                  <Field icon={<ProfileCardIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredDisplayName')} value={selfDraft.preferredDisplayName ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredDisplayName: value }))} />
                  <LanguageField icon={<ProfileLanguageIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredLanguage')} placeholder={t('profileSelectLanguagePlaceholder')} value={selfDraft.preferredLanguage ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredLanguage: value }))} />
                </>
              ) : null}

              {activePlatformAdminTab === 'addressContact' ? (
                <>
                  <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPermanentAddress')} value={selfDraft.permanentAddress ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, permanentAddress: value }))} />
                  <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldCorrespondenceAddress')} value={selfDraft.correspondenceAddress ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, correspondenceAddress: value }))} />
                  <Field icon={<ProfileEmailIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldContactEmail')} value={selfDraft.contactEmail ?? ''} disabled={savingSelf} invalid={Boolean(fieldErrors.contactEmail)} errorText={fieldErrors.contactEmail} onChange={(value) => setSelfDraft((v) => ({ ...v, contactEmail: value }))} />
                  <Field icon={<ProfilePhoneIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPhoneNumber')} value={selfDraft.phoneNumber ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, phoneNumber: value }))} />
                  <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPublicContactNote')} value={selfDraft.publicContactNote ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, publicContactNote: value }))} />
                </>
              ) : null}

              {activePlatformAdminTab === 'platformRoleContext' ? (
                <>
                  <Field icon={<ProfilePositionIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldAdministrativeWorkDesignation')} value={selfDraft.administrativeWorkDesignation ?? ''} disabled={!selfEditRules.canEditPlatformAdminSummary || savingSelf} invalid={Boolean(fieldErrors.administrativeWorkDesignation)} errorText={fieldErrors.administrativeWorkDesignation} onChange={(value) => setSelfDraft((v) => ({ ...v, administrativeWorkDesignation: value }))} />
                  <Field icon={<ProfileRoleIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPlatformRoleContextSummary')} value={selfDraft.platformRoleContextSummary ?? ''} disabled={!selfEditRules.canEditPlatformAdminSummary || savingSelf} invalid={Boolean(fieldErrors.platformRoleContextSummary)} errorText={fieldErrors.platformRoleContextSummary} onChange={(value) => setSelfDraft((v) => ({ ...v, platformRoleContextSummary: value }))} />
                  <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldAdministrativeOrganizationSummary')} value={selfDraft.administrativeOrganizationSummary ?? ''} disabled={!selfEditRules.canEditPlatformAdminSummary || savingSelf} invalid={Boolean(fieldErrors.administrativeOrganizationSummary)} errorText={fieldErrors.administrativeOrganizationSummary} onChange={(value) => setSelfDraft((v) => ({ ...v, administrativeOrganizationSummary: value }))} />
                </>
              ) : null}

              {activePlatformAdminTab === 'managedAreas' ? (
                <>
                  <p className="text-xs text-slate-500 md:col-span-2">{t('profileHelpPlatformManagedAreas')}</p>
                  <Field icon={<ProfileAssignmentIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldManagedPlatformAreasSummary')} value={selfDraft.managedPlatformAreasSummary ?? ''} disabled invalid={Boolean(fieldErrors.managedPlatformAreasSummary)} errorText={fieldErrors.managedPlatformAreasSummary} onChange={(value) => setSelfDraft((v) => ({ ...v, managedPlatformAreasSummary: value }))} />
                </>
              ) : null}

              {activePlatformAdminTab === 'administrativeOverview' ? (
                <>
                  <p className="text-xs text-slate-500 md:col-span-2">{t('profileHelpPlatformAdministrativeOverview')}</p>
                  <Field icon={<ProfileRoleIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldAdministrativeBoundarySummary')} value={selfDraft.administrativeBoundarySummary ?? ''} disabled invalid={Boolean(fieldErrors.administrativeBoundarySummary)} errorText={fieldErrors.administrativeBoundarySummary} onChange={(value) => setSelfDraft((v) => ({ ...v, administrativeBoundarySummary: value }))} />
                </>
              ) : null}
            </div>
          </>
        ) : isSchoolAdministratorScopedProfile ? (
          <>
            <div className="mt-3 flex flex-wrap gap-2">
              <button type="button" className={`sk-btn ${activeSchoolAdminTab === 'basic' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveSchoolAdminTab('basic')}>{t('profileTabSchoolAdminBasic')}</button>
              <button type="button" className={`sk-btn ${activeSchoolAdminTab === 'addressContact' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveSchoolAdminTab('addressContact')}>{t('profileTabSchoolAdminAddressContact')}</button>
              <button type="button" className={`sk-btn ${activeSchoolAdminTab === 'employment' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveSchoolAdminTab('employment')}>{t('profileTabSchoolAdminEmployment')}</button>
              <button type="button" className={`sk-btn ${activeSchoolAdminTab === 'schoolContext' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveSchoolAdminTab('schoolContext')}>{t('profileTabSchoolAdminSchoolContext')}</button>
              <button type="button" className={`sk-btn ${activeSchoolAdminTab === 'managedSchools' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveSchoolAdminTab('managedSchools')}>{t('profileTabSchoolAdminManagedSchools')}</button>
              <button type="button" className={`sk-btn ${activeSchoolAdminTab === 'administrativeOverview' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveSchoolAdminTab('administrativeOverview')}>{t('profileTabSchoolAdminAdministrativeOverview')}</button>
            </div>
            <div className="mt-3 grid gap-2 md:grid-cols-2">
              {activeSchoolAdminTab === 'basic' ? (<>
                <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldFirstName')} value={selfDraft.firstName} disabled={!selfEditRules.canEditName || savingSelf} invalid={Boolean(fieldErrors.firstName)} errorText={fieldErrors.firstName} onChange={(value) => { setFieldErrors((v) => ({ ...v, firstName: undefined })); setSelfDraft((v) => ({ ...v, firstName: value })); }} />
                <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldLastName')} value={selfDraft.lastName} disabled={!selfEditRules.canEditName || savingSelf} invalid={Boolean(fieldErrors.lastName)} errorText={fieldErrors.lastName} onChange={(value) => { setFieldErrors((v) => ({ ...v, lastName: undefined })); setSelfDraft((v) => ({ ...v, lastName: value })); }} />
                <Field icon={<ProfileCardIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredDisplayName')} value={selfDraft.preferredDisplayName ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredDisplayName: value }))} />
                <LanguageField icon={<ProfileLanguageIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredLanguage')} placeholder={t('profileSelectLanguagePlaceholder')} value={selfDraft.preferredLanguage ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredLanguage: value }))} />
              </>) : null}
              {activeSchoolAdminTab === 'addressContact' ? (<>
                <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldCorrespondenceAddress')} value={selfDraft.correspondenceAddress ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, correspondenceAddress: value }))} />
                <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldContactEmail')} value={selfDraft.contactEmail ?? ''} disabled={savingSelf} invalid={Boolean(fieldErrors.contactEmail)} errorText={fieldErrors.contactEmail} onChange={(value) => { setFieldErrors((v) => ({ ...v, contactEmail: undefined })); setSelfDraft((v) => ({ ...v, contactEmail: value })); }} />
                <Field icon={<ProfilePhoneIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPhoneNumber')} value={selfDraft.phoneNumber ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, phoneNumber: value }))} />
              </>) : null}
              {activeSchoolAdminTab === 'employment' ? (<>
                {canShowSchoolPositionField ? (
                  <SchoolPositionField icon={<ProfileBriefcaseIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldSchoolPosition')} value={selfDraft.positionTitle ?? ''} placeholder={t('profileSelectSchoolPositionPlaceholder')} disabled={!selfEditRules.canEditSchoolPosition || savingSelf || schoolPositionLoading} options={schoolPositionOptions} loadingLabel={t('profileSchoolPositionLoading')} unavailableLabel={t('profileSchoolPositionUnavailable')} invalid={Boolean(fieldErrors.positionTitle)} errorText={fieldErrors.positionTitle} onChange={(value) => { setFieldErrors((v) => ({ ...v, positionTitle: undefined })); setSelfDraft((v) => ({ ...v, positionTitle: value })); }} />
                ) : null}
                <Field icon={<ProfileBriefcaseIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldAdministrativeWorkDesignation')} value={selfDraft.administrativeWorkDesignation ?? ''} disabled={savingSelf} invalid={Boolean(fieldErrors.administrativeWorkDesignation)} errorText={fieldErrors.administrativeWorkDesignation} onChange={(value) => setSelfDraft((v) => ({ ...v, administrativeWorkDesignation: value }))} />
                <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPublicContactNote')} value={selfDraft.publicContactNote ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, publicContactNote: value }))} />
              </>) : null}
              {activeSchoolAdminTab === 'schoolContext' ? (<>
                <Field icon={<ProfileSchoolIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileLabelSchoolContext')} value={selfDraft.schoolContextSummary ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, schoolContextSummary: value }))} />
                <Field icon={<ProfileSchoolIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldAdministrativeOrganizationSummary')} value={selfDraft.administrativeOrganizationSummary ?? ''} disabled={savingSelf} invalid={Boolean(fieldErrors.administrativeOrganizationSummary)} errorText={fieldErrors.administrativeOrganizationSummary} onChange={(value) => setSelfDraft((v) => ({ ...v, administrativeOrganizationSummary: value }))} />
              </>) : null}
            </div>
            {activeSchoolAdminTab === 'managedSchools' ? (
              <div className="mt-3">
                <p className="mb-2 text-xs text-slate-500">{t('profileSchoolAdminManagedSchoolsHelp')}</p>
                {summary.schoolIds.length === 0 ? <EmptyState text={t('profileNoRoleAssignments')} /> : <ul className="sk-list">{summary.schoolIds.map((schoolId) => <li key={schoolId} className="sk-list-item">{schoolId}</li>)}</ul>}
              </div>
            ) : null}
            {activeSchoolAdminTab === 'administrativeOverview' ? (
              <div className="mt-3">
                <p className="mb-2 text-xs text-slate-500">{t('profileSchoolAdminAdministrativeOverviewHelp')}</p>
                <ul className="sk-list">
                  <li className="sk-list-item">{t('profileRoleAssignmentsTitle')}: {summary.roleAssignments.length}</li>
                  <li className="sk-list-item">{t('profileParentStudentLinksTitle')}: {summary.parentStudentLinks.length}</li>
                  <li className="sk-list-item">{t('profileLabelAssignedSchools')}: {summary.schoolIds.length}</li>
                </ul>
              </div>
            ) : null}
          </>
        ) : isTeacherScopedProfile ? (
          <>
            <div className="mt-3 flex flex-wrap gap-2">
              <button type="button" className={`sk-btn ${activeTeacherTab === 'basic' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveTeacherTab('basic')}>{t('myProfile.accountOverview')}</button>
              <button type="button" className={`sk-btn ${activeTeacherTab === 'address' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveTeacherTab('address')}>{t('profileFieldCorrespondenceAddress')}</button>
              <button type="button" className={`sk-btn ${activeTeacherTab === 'employment' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveTeacherTab('employment')}>{t('profileFieldSchoolPosition')}</button>
              <button type="button" className={`sk-btn ${activeTeacherTab === 'schoolContext' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveTeacherTab('schoolContext')}>{t('profileLabelSchoolContext')}</button>
              <button type="button" className={`sk-btn ${activeTeacherTab === 'teachingAssignments' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveTeacherTab('teachingAssignments')}>{t('profileTeacherAssignmentsTitle')}</button>
            </div>
            <div className="mt-3 grid gap-2 md:grid-cols-2">
              {activeTeacherTab === 'basic' ? (
                <>
                  <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldFirstName')} value={selfDraft.firstName} disabled={!selfEditRules.canEditName || savingSelf} invalid={Boolean(fieldErrors.firstName)} errorText={fieldErrors.firstName} onChange={(value) => { setFieldErrors((v) => ({ ...v, firstName: undefined })); setSelfDraft((v) => ({ ...v, firstName: value })); }} />
                  <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldLastName')} value={selfDraft.lastName} disabled={!selfEditRules.canEditName || savingSelf} invalid={Boolean(fieldErrors.lastName)} errorText={fieldErrors.lastName} onChange={(value) => { setFieldErrors((v) => ({ ...v, lastName: undefined })); setSelfDraft((v) => ({ ...v, lastName: value })); }} />
                  <Field icon={<ProfileCardIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredDisplayName')} value={selfDraft.preferredDisplayName ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredDisplayName: value }))} />
                  <LanguageField icon={<ProfileLanguageIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredLanguage')} placeholder={t('profileSelectLanguagePlaceholder')} value={selfDraft.preferredLanguage ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredLanguage: value }))} />
                  <Field icon={<ProfilePhoneIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPhoneNumber')} value={selfDraft.phoneNumber ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, phoneNumber: value }))} />
                  <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldDateOfBirth')} value={selfDraft.dateOfBirth ?? ''} disabled={isStudentOnly || savingSelf} invalid={Boolean(fieldErrors.dateOfBirth)} errorText={fieldErrors.dateOfBirth} onChange={(value) => setSelfDraft((v) => ({ ...v, dateOfBirth: value }))} />
                </>
              ) : null}
              {activeTeacherTab === 'address' ? (
                <>
                  <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPermanentAddress')} value={selfDraft.permanentAddress ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, permanentAddress: value }))} />
                  <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldCorrespondenceAddress')} value={selfDraft.correspondenceAddress ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, correspondenceAddress: value }))} />
                  <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldContactEmail')} value={selfDraft.contactEmail ?? ''} disabled={savingSelf} invalid={Boolean(fieldErrors.contactEmail)} errorText={fieldErrors.contactEmail} onChange={(value) => setSelfDraft((v) => ({ ...v, contactEmail: value }))} />
                </>
              ) : null}
              {activeTeacherTab === 'employment' ? (
                <>
                  {canShowSchoolPositionField ? (
                    <SchoolPositionField
                      icon={<ProfilePositionIcon className="h-4 w-4 shrink-0 text-slate-500" />}
                      label={t('profileFieldSchoolPosition')}
                      value={selfDraft.positionTitle ?? ''}
                      options={schoolPositionOptions}
                      loading={schoolPositionLoading || savingSelf}
                      onChange={(value) => setSelfDraft((v) => ({ ...v, positionTitle: value }))}
                      loadingText={t('profileSchoolPositionLoading')}
                      placeholder={t('profileSelectSchoolPositionPlaceholder')}
                      unavailableText={t('profileSchoolPositionUnavailable')}
                      invalid={Boolean(fieldErrors.positionTitle)}
                      errorText={fieldErrors.positionTitle}
                    />
                  ) : null}
                  <Field icon={<ProfileCardIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileLabelRole')} value={selfDraft.teacherRoleLabel ?? ''} disabled={!selfEditRules.canEditTeacherSection || savingSelf} invalid={Boolean(fieldErrors.teacherRoleLabel)} errorText={fieldErrors.teacherRoleLabel} onChange={(value) => setSelfDraft((v) => ({ ...v, teacherRoleLabel: value }))} />
                  <Field icon={<ProfileCardIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldSupportMeasuresSummary')} value={selfDraft.qualificationSummary ?? ''} disabled={!selfEditRules.canEditTeacherSection || savingSelf} invalid={Boolean(fieldErrors.qualificationSummary)} errorText={fieldErrors.qualificationSummary} onChange={(value) => setSelfDraft((v) => ({ ...v, qualificationSummary: value }))} />
                  <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPublicContactNote')} value={selfDraft.publicContactNote ?? ''} disabled={!selfEditRules.canEditPublicContactNote || savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, publicContactNote: value }))} />
                </>
              ) : null}
              {activeTeacherTab === 'schoolContext' ? (
                <>
                  <Field icon={<ProfileSchoolIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileLabelSchoolContext')} value={selectedSchoolId || '-'} disabled />
                  <Field icon={<ProfileSchoolIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('routeOrganization')} value={session.schoolType} disabled />
                  <Field icon={<ProfileSchoolIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileLabelAssignedSchools')} value={summary.schoolIds.length.toString()} disabled />
                  <Field icon={<ProfileSchoolIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldSchoolPlacement')} value={selfDraft.schoolContextSummary ?? ''} disabled={!selfEditRules.canEditSchoolContextSummary || savingSelf} invalid={Boolean(fieldErrors.schoolContextSummary)} errorText={fieldErrors.schoolContextSummary} onChange={(value) => setSelfDraft((v) => ({ ...v, schoolContextSummary: value }))} />
                </>
              ) : null}
            </div>
            {activeTeacherTab === 'teachingAssignments' ? (
              <div className="mt-3">
                <p className="mb-2 text-xs text-slate-500">{t('profileAdminEditDescription')}</p>
                {teacherAssignments.length === 0 ? <EmptyState text={t('profileNoTeacherAssignments')} /> : (
                  <ul className="sk-list">
                    {teacherAssignments.map((assignment) => (
                      <li key={assignment.id} className="sk-list-item">{assignment.scope} | {t('profileLabelClass')}: {assignment.classRoomId ?? '-'} | {t('profileLabelGroup')}: {assignment.teachingGroupId ?? '-'} | {t('profileLabelSubject')}: {assignment.subjectId ?? '-'}</li>
                    ))}
                  </ul>
                )}
              </div>
            ) : null}
          </>
        ) : isParentScopedProfile ? (
          <>
            <div className="mt-3 flex flex-wrap gap-2">
              <button type="button" className={`sk-btn ${activeParentTab === 'basic' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveParentTab('basic')}>{t('myProfile.accountOverview')}</button>
              <button type="button" className={`sk-btn ${activeParentTab === 'addressContact' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveParentTab('addressContact')}>{t('profileFieldCorrespondenceAddress')}</button>
              <button type="button" className={`sk-btn ${activeParentTab === 'delivery' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveParentTab('delivery')}>{t('profileFieldSchoolPlacement')}</button>
              <button type="button" className={`sk-btn ${activeParentTab === 'linkedStudents' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveParentTab('linkedStudents')}>{t('profileLinkedStudentsTitle')}</button>
              <button type="button" className={`sk-btn ${activeParentTab === 'relationshipsContext' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveParentTab('relationshipsContext')}>{t('profileParentStudentLinksTitle')}</button>
              <button type="button" className={`sk-btn ${activeParentTab === 'communication' ? 'sk-btn-primary' : 'sk-btn-secondary'}`} onClick={() => setActiveParentTab('communication')}>{t('profileFieldPreferredContactNote')}</button>
            </div>

            <div className="mt-3 grid gap-2 md:grid-cols-2">
              {activeParentTab === 'basic' ? (
                <>
                  <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldFirstName')} value={selfDraft.firstName} disabled={!selfEditRules.canEditName || savingSelf} invalid={Boolean(fieldErrors.firstName)} errorText={fieldErrors.firstName} onChange={(value) => { setFieldErrors((v) => ({ ...v, firstName: undefined })); setSelfDraft((v) => ({ ...v, firstName: value })); }} />
                  <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldLastName')} value={selfDraft.lastName} disabled={!selfEditRules.canEditName || savingSelf} invalid={Boolean(fieldErrors.lastName)} errorText={fieldErrors.lastName} onChange={(value) => { setFieldErrors((v) => ({ ...v, lastName: undefined })); setSelfDraft((v) => ({ ...v, lastName: value })); }} />
                  <Field icon={<ProfileCardIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredDisplayName')} value={selfDraft.preferredDisplayName ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredDisplayName: value }))} />
                  <LanguageField icon={<ProfileLanguageIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredLanguage')} placeholder={t('profileSelectLanguagePlaceholder')} value={selfDraft.preferredLanguage ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredLanguage: value }))} />
                </>
              ) : null}

              {activeParentTab === 'addressContact' ? (
                <>
                  <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPermanentAddress')} value={selfDraft.permanentAddress ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, permanentAddress: value }))} />
                  <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldCorrespondenceAddress')} value={selfDraft.correspondenceAddress ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, correspondenceAddress: value }))} />
                  <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldContactEmail')} value={selfDraft.contactEmail ?? ''} disabled={savingSelf} invalid={Boolean(fieldErrors.contactEmail)} errorText={fieldErrors.contactEmail} onChange={(value) => setSelfDraft((v) => ({ ...v, contactEmail: value }))} />
                  <Field icon={<ProfilePhoneIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPhoneNumber')} value={selfDraft.phoneNumber ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, phoneNumber: value }))} />
                </>
              ) : null}

              {activeParentTab === 'delivery' ? (
                <>
                  <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredDisplayName')} value={selfDraft.deliveryContactName ?? ''} disabled={!selfEditRules.canEditParentSection || savingSelf} invalid={Boolean(fieldErrors.deliveryContactName)} errorText={fieldErrors.deliveryContactName} onChange={(value) => setSelfDraft((v) => ({ ...v, deliveryContactName: value }))} />
                  <Field icon={<ProfilePhoneIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPhoneNumber')} value={selfDraft.deliveryContactPhone ?? ''} disabled={!selfEditRules.canEditParentSection || savingSelf} invalid={Boolean(fieldErrors.deliveryContactPhone)} errorText={fieldErrors.deliveryContactPhone} onChange={(value) => setSelfDraft((v) => ({ ...v, deliveryContactPhone: value }))} />
                  <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredContactNote')} value={selfDraft.preferredContactNote ?? ''} disabled={!selfEditRules.canEditPreferredContactNote || savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredContactNote: value }))} />
                </>
              ) : null}

              {activeParentTab === 'relationshipsContext' ? (
                <>
                  <Field icon={<ProfileRoleIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileParentStudentLinksTitle')} value={selfDraft.parentRelationshipSummary ?? ''} disabled={!selfEditRules.canEditParentSection || savingSelf} invalid={Boolean(fieldErrors.parentRelationshipSummary)} errorText={fieldErrors.parentRelationshipSummary} onChange={(value) => setSelfDraft((v) => ({ ...v, parentRelationshipSummary: value }))} />
                  <Field icon={<ProfileSchoolIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileLabelAssignedSchools')} value={summary.schoolIds.length.toString()} disabled />
                  <Field icon={<ProfileSchoolIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileLabelSchoolContext')} value={selectedSchoolId || '-'} disabled />
                </>
              ) : null}

              {activeParentTab === 'communication' ? (
                <>
                  <ParentContactChannelField
                    icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />}
                    label={t('profileFieldPreferredLanguage')}
                    value={selfDraft.preferredContactChannel ?? ''}
                    disabled={!selfEditRules.canEditParentCommunication || savingSelf}
                    invalid={Boolean(fieldErrors.preferredContactChannel)}
                    errorText={fieldErrors.preferredContactChannel}
                    onChange={(value) => setSelfDraft((v) => ({ ...v, preferredContactChannel: value }))}
                    placeholder={t('profileSelectLanguagePlaceholder')}
                    emailLabel={t('email')}
                    phoneLabel={t('profileFieldPhoneNumber')}
                    appLabel={t('routeCommunication')}
                  />
                  <Field icon={<ProfileContactIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredContactNote')} value={selfDraft.communicationPreferencesSummary ?? ''} disabled={!selfEditRules.canEditParentCommunication || savingSelf} invalid={Boolean(fieldErrors.communicationPreferencesSummary)} errorText={fieldErrors.communicationPreferencesSummary} onChange={(value) => setSelfDraft((v) => ({ ...v, communicationPreferencesSummary: value }))} />
                </>
              ) : null}
            </div>

            {activeParentTab === 'linkedStudents' ? (
              <div className="mt-3">
                <p className="mb-2 text-xs text-slate-500">{t('profileAdminEditDescription')}</p>
                {linkedStudents.length === 0 ? <EmptyState text={t('profileNoLinkedStudents')} /> : (
                  <ul className="sk-list">
                    {linkedStudents.map((student) => (
                      <li key={student.id} className="sk-list-item">{student.firstName} {student.lastName} ({student.email})</li>
                    ))}
                  </ul>
                )}
              </div>
            ) : null}
          </>
        ) : (
          <div className="mt-3 grid gap-2 md:grid-cols-2">
            <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldFirstName')} value={selfDraft.firstName} disabled={!selfEditRules.canEditName || savingSelf} invalid={Boolean(fieldErrors.firstName)} errorText={fieldErrors.firstName} onChange={(value) => { setFieldErrors((v) => ({ ...v, firstName: undefined })); setSelfDraft((v) => ({ ...v, firstName: value })); }} />
            <Field icon={<ProfileIdentityIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldLastName')} value={selfDraft.lastName} disabled={!selfEditRules.canEditName || savingSelf} invalid={Boolean(fieldErrors.lastName)} errorText={fieldErrors.lastName} onChange={(value) => { setFieldErrors((v) => ({ ...v, lastName: undefined })); setSelfDraft((v) => ({ ...v, lastName: value })); }} />
            <Field icon={<ProfileCardIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredDisplayName')} value={selfDraft.preferredDisplayName ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredDisplayName: value }))} />
            <LanguageField icon={<ProfileLanguageIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPreferredLanguage')} placeholder={t('profileSelectLanguagePlaceholder')} value={selfDraft.preferredLanguage ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, preferredLanguage: value }))} />
            <Field icon={<ProfilePhoneIcon className="h-4 w-4 shrink-0 text-slate-500" />} label={t('profileFieldPhoneNumber')} value={selfDraft.phoneNumber ?? ''} disabled={savingSelf} onChange={(value) => setSelfDraft((v) => ({ ...v, phoneNumber: value }))} />
          </div>
        )}
        <div className="mt-3 flex gap-2">
          <button className={`sk-btn sk-btn-primary gap-2 ${savingSelf ? 'sk-btn-busy' : ''}`} onClick={saveSelfProfile} type="button" disabled={savingSelf} aria-busy={savingSelf}>
            <SaveDiskIcon className="h-4 w-4 shrink-0" />
            <span>{savingSelf ? t('profileSaving') : t('profileButtonSaveMyProfile')}</span>
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

    </section>
  );
}

function ManagedUsersSection({
  t,
  userListFilters,
  setUserListFilters,
  loadManagedUsers,
  managedUsersLoading,
  managedUsersError,
  managedUsers,
  managedSummary,
  managedSummaryLoading,
  managedSummaryError,
  managedActionBusyUserId,
  openManagedDetail,
  closeManagedDetail,
  runManagedLifecycleAction,
  api,
  managedDetailUserId,
  managedUserDetailLoading,
  managedUserDetailError,
  managedUserDetail,
  managedBasicDraft,
  setManagedBasicDraft,
  saveManagedBasic,
  managedRoleSetDraft,
  setManagedRoleSetDraft,
  saveManagedRoleSet,
  managedSchoolIdsDraft,
  setManagedSchoolIdsDraft,
  saveManagedSchoolContext,
  managedParentLinksDraft,
  setManagedParentLinksDraft,
  saveManagedParentLinks,
  activeManagedUserTab,
  setActiveManagedUserTab,
  isPlatformAdministrator,
  managedSchoolContextId,
  managedSchoolOptions,
  managedSchoolOptionsLoading,
  managedSchoolOptionsError,
  onManagedSchoolContextChange,
  onOpenCreateWizard,
  wizardHighlightUserId,
  sectionRef
}: any) {
  const { locale, t: tSection } = useI18n();
  const [filtersExpanded, setFiltersExpanded] = useState(false);
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>([]);
  const [actionMenuUserId, setActionMenuUserId] = useState('');
  const debounceRef = useRef<number | null>(null);
  const actionMenuRef = useRef<HTMLDivElement | null>(null);

  const managedUserRow = managedUsers?.items.find((item: IdentityManagedUser) => item.userId === managedDetailUserId);

  useEffect(() => {
    if (!actionMenuUserId) return;
    const onClick = (e: MouseEvent) => {
      if (actionMenuRef.current && !actionMenuRef.current.contains(e.target as Node)) {
        setActionMenuUserId('');
      }
    };
    document.addEventListener('mousedown', onClick);
    return () => document.removeEventListener('mousedown', onClick);
  }, [actionMenuUserId]);

  const confirmAndRun = (message: string, callback: () => void) => {
    if (!window.confirm(message)) return;
    callback();
  };

  const scheduleFilterApply = useCallback(() => {
    if (debounceRef.current) window.clearTimeout(debounceRef.current);
    debounceRef.current = window.setTimeout(() => {
      loadManagedUsers({ pageNumber: 1 });
    }, 400) as unknown as number;
  }, [loadManagedUsers]);

  const updateFilter = (key: string, value: string) => {
    setUserListFilters((v: AdminUserListQuery) => ({ ...v, [key]: value }));
    scheduleFilterApply();
  };

  const clearAllFilters = () => {
    if (debounceRef.current) window.clearTimeout(debounceRef.current);
    const cleared: Partial<AdminUserListQuery> = {
      search: '', name: '', emailOrUsername: '', role: '',
      accountStatus: '', activationStatus: '', blockStatus: '',
      mfaStatus: '', school: '', schoolType: '', inactivityState: '',
      pageNumber: 1
    };
    setUserListFilters((v: AdminUserListQuery) => ({ ...v, ...cleared }));
    loadManagedUsers(cleared);
  };

  const removeFilter = (key: string) => {
    setUserListFilters((v: AdminUserListQuery) => ({ ...v, [key]: '' }));
    if (debounceRef.current) window.clearTimeout(debounceRef.current);
    loadManagedUsers({ [key]: '', pageNumber: 1 });
  };

  const toggleSort = (field: string) => {
    const currentField = userListFilters.sortField ?? 'name';
    const currentDir = userListFilters.sortDirection ?? 'asc';
    if (currentField === field) {
      loadManagedUsers({ sortDirection: currentDir === 'asc' ? 'desc' : 'asc', pageNumber: 1 });
    } else {
      loadManagedUsers({ sortField: field, sortDirection: 'asc', pageNumber: 1 });
    }
  };

  const sortIndicator = (field: string) => {
    const active = (userListFilters.sortField ?? 'name') === field;
    const dir = userListFilters.sortDirection ?? 'asc';
    return <span className={`sk-sort-indicator ${active ? 'is-active' : ''}`}>{active ? (dir === 'asc' ? '\u25B2' : '\u25BC') : '\u25B4'}</span>;
  };

  const allSelected = managedUsers?.items?.length > 0 && managedUsers.items.every((u: IdentityManagedUser) => selectedUserIds.includes(u.userId));
  const toggleSelectAll = () => {
    if (allSelected) {
      setSelectedUserIds([]);
    } else {
      setSelectedUserIds(managedUsers?.items.map((u: IdentityManagedUser) => u.userId) ?? []);
    }
  };
  const toggleSelectUser = (userId: string) => {
    setSelectedUserIds(prev => prev.includes(userId) ? prev.filter(id => id !== userId) : [...prev, userId]);
  };

  const bulkActivate = () => {
    if (!window.confirm(t('userManagementBulkActivate'))) return;
    for (const userId of selectedUserIds) {
      runManagedLifecycleAction(userId, () => api.adminActivate(userId, managedSchoolContextId || undefined));
    }
    setSelectedUserIds([]);
  };

  const bulkDeactivate = () => {
    const reason = window.prompt(t('userManagementPromptDeactivateReason'));
    if (!reason?.trim()) return;
    for (const userId of selectedUserIds) {
      runManagedLifecycleAction(userId, () => api.adminDeactivate(userId, reason.trim(), managedSchoolContextId || undefined));
    }
    setSelectedUserIds([]);
  };

  const getUserAvatarClass = (user: IdentityManagedUser) => {
    if (user.roles.includes('PlatformAdministrator') || user.roles.includes('SchoolAdministrator')) return 'sk-user-avatar sk-user-avatar-admin';
    if (user.roles.includes('Teacher')) return 'sk-user-avatar sk-user-avatar-teacher';
    if (user.roles.includes('Parent')) return 'sk-user-avatar sk-user-avatar-parent';
    if (user.roles.includes('Student')) return 'sk-user-avatar sk-user-avatar-student';
    return 'sk-user-avatar sk-user-avatar-default';
  };

  const getUserInitials = (user: IdentityManagedUser) => {
    const name = user.displayName || user.userName || '';
    const parts = name.split(/[\s._-]+/).filter(Boolean);
    if (parts.length === 0) return 'SK';
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
  };

  const getStatusDotClass = (user: IdentityManagedUser) => {
    const state = resolveManagedUserRowVisualState(user);
    if (state === 'locked') return 'sk-status-dot sk-status-dot-locked';
    if (state === 'deactivated') return 'sk-status-dot sk-status-dot-deactivated';
    if (state === 'pending') return 'sk-status-dot sk-status-dot-pending';
    return 'sk-status-dot sk-status-dot-active';
  };

  const roleOptions = [
    { value: 'PlatformAdministrator', label: t('userManagementFilterRoleOptions_PlatformAdministrator') },
    { value: 'SchoolAdministrator', label: t('userManagementFilterRoleOptions_SchoolAdministrator') },
    { value: 'Teacher', label: t('userManagementFilterRoleOptions_Teacher') },
    { value: 'Parent', label: t('userManagementFilterRoleOptions_Parent') },
    { value: 'Student', label: t('userManagementFilterRoleOptions_Student') }
  ];
  const accountStatusOptions = [
    { value: 'Active', label: t('userManagementFilterAccountStatusOptions_Active') },
    { value: 'Deactivated', label: t('userManagementFilterAccountStatusOptions_Deactivated') },
    { value: 'Locked', label: t('userManagementFilterAccountStatusOptions_Locked') }
  ];
  const activationStatusOptions = [
    { value: 'active', label: t('userManagementFilterActivationStatusOptions_active') },
    { value: 'pending', label: t('userManagementFilterActivationStatusOptions_pending') }
  ];
  const blockStatusOptions = [
    { value: 'clear', label: t('userManagementFilterBlockStatusOptions_clear') },
    { value: 'locked', label: t('userManagementFilterBlockStatusOptions_locked') }
  ];
  const mfaStatusOptions = [
    { value: 'enabled', label: t('userManagementFilterMfaStatusOptions_enabled') },
    { value: 'disabled', label: t('userManagementFilterMfaStatusOptions_disabled') }
  ];
  const schoolTypeOptions = [
    { value: 'Kindergarten', label: t('userManagementFilterSchoolTypeOptions_Kindergarten') },
    { value: 'ElementarySchool', label: t('userManagementFilterSchoolTypeOptions_ElementarySchool') },
    { value: 'SecondarySchool', label: t('userManagementFilterSchoolTypeOptions_SecondarySchool') }
  ];
  const inactivityStateOptions = [
    { value: 'active', label: t('userManagementFilterInactivityStateOptions_active') },
    { value: 'inactive', label: t('userManagementFilterInactivityStateOptions_inactive') }
  ];

  const filterLabelMap: Record<string, string> = {
    search: t('userManagementSearchLabel'),
    name: t('userManagementFilterName'),
    emailOrUsername: t('userManagementFilterEmailOrUsername'),
    role: t('userManagementFilterRole'),
    school: t('userManagementFilterSchool'),
    schoolType: t('userManagementFilterSchoolType'),
    accountStatus: t('userManagementFilterAccountStatus'),
    activationStatus: t('userManagementFilterActivationStatus'),
    blockStatus: t('userManagementFilterBlockStatus'),
    mfaStatus: t('userManagementFilterMfaStatus'),
    inactivityState: t('userManagementFilterInactivityState')
  };

  const translateManagedRole = (role: string) => {
    const labelMap: Record<string, string> = {
      PlatformAdministrator: t('userManagementFilterRoleOptions_PlatformAdministrator'),
      SchoolAdministrator: t('userManagementFilterRoleOptions_SchoolAdministrator'),
      Teacher: t('userManagementFilterRoleOptions_Teacher'),
      Parent: t('userManagementFilterRoleOptions_Parent'),
      Student: t('userManagementFilterRoleOptions_Student')
    };
    return labelMap[role] ?? role;
  };

  const translateManagedLifecycleStatus = (user: IdentityManagedUser | IdentityManagedUserDetail) => {
    const state = resolveManagedUserRowVisualState(user as IdentityManagedUser);
    if (state === 'locked') return t('userManagementFilterAccountStatusOptions_Locked');
    if (state === 'deactivated') return t('userManagementFilterAccountStatusOptions_Deactivated');
    if (state === 'pending') return t('userManagementActivationPending');
    return t('userManagementFilterAccountStatusOptions_Active');
  };

  const translateSchoolType = (schoolType?: string | null) => {
    if (!schoolType) return '-';
    const labelMap: Record<string, string> = {
      Kindergarten: t('userManagementFilterSchoolTypeOptions_Kindergarten'),
      ElementarySchool: t('userManagementFilterSchoolTypeOptions_ElementarySchool'),
      SecondarySchool: t('userManagementFilterSchoolTypeOptions_SecondarySchool')
    };
    return labelMap[schoolType] ?? schoolType;
  };

  const formatManagedDate = (value?: string | null) => {
    if (!value) return '-';
    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) return value;
    return parsed.toLocaleString(locale);
  };

  const filterValueMap: Record<string, Record<string, string>> = {
    role: Object.fromEntries(roleOptions.map((option) => [option.value, option.label])),
    schoolType: Object.fromEntries(schoolTypeOptions.map((option) => [option.value, option.label])),
    accountStatus: Object.fromEntries(accountStatusOptions.map((option) => [option.value, option.label])),
    activationStatus: Object.fromEntries(activationStatusOptions.map((option) => [option.value, option.label])),
    blockStatus: Object.fromEntries(blockStatusOptions.map((option) => [option.value, option.label])),
    mfaStatus: Object.fromEntries(mfaStatusOptions.map((option) => [option.value, option.label])),
    inactivityState: Object.fromEntries(inactivityStateOptions.map((option) => [option.value, option.label]))
  };

  const activeFilters: { key: string; label: string; value: string }[] = [];
  for (const key of Object.keys(filterLabelMap)) {
    const rawValue = (userListFilters as Record<string, string>)[key];
    if (!rawValue?.trim()) continue;
    activeFilters.push({
      key,
      label: filterLabelMap[key],
      value: filterValueMap[key]?.[rawValue] ?? rawValue
    });
  }

  const pageFrom = managedUsers ? ((managedUsers.pageNumber - 1) * managedUsers.pageSize + 1) : 0;
  const pageTo = managedUsers ? Math.min(managedUsers.pageNumber * managedUsers.pageSize, managedUsers.totalCount) : 0;

  return (
    <Card className="sk-user-management">
      <section ref={sectionRef} id="identity-user-management">
      <div className="flex items-center justify-between gap-2">
        <p className="font-semibold text-sm inline-flex items-center gap-2">
          <UserManagementSectionIcon className="h-4 w-4 text-slate-600" />
          {t('userManagementListTitle')}
        </p>
        {onOpenCreateWizard ? (
          <button
            type="button"
            className="sk-btn sk-btn-primary text-xs inline-flex items-center gap-1.5"
            onClick={onOpenCreateWizard}
          >
            <CreateUserActionIcon className="h-3.5 w-3.5" />
            {tSection('createUserAction')}
          </button>
        ) : null}
      </div>

      {/* School context switcher (platform admin only) */}
      {isPlatformAdministrator ? (
        <div className="sk-user-management-panel mt-3 rounded-md border border-slate-200 bg-slate-50 p-3">
          <label className="sk-label inline-flex items-center gap-1.5" htmlFor="user-management-school-context-switcher">
            <ProfileSchoolIcon className="h-4 w-4 text-slate-600" />
            <span>{t('userManagementSchoolContextLabel')}</span>
          </label>
          <select id="user-management-school-context-switcher" className="sk-input mt-1" value={managedSchoolContextId} disabled={managedSchoolOptionsLoading} onChange={(e) => onManagedSchoolContextChange(e.target.value)}>
            <option value="">{t('userManagementSchoolContextAllSchools')}</option>
            {managedSchoolOptions.map((option: SchoolContextOption) => (
              <option key={option.schoolId} value={option.schoolId}>{option.label}</option>
            ))}
          </select>
          <p className="mt-2 text-xs text-slate-600">
            {managedSchoolContextId ? t('userManagementSchoolContextSelectedSchool') : t('userManagementSchoolContextAllSchoolsMode')}
          </p>
          {managedSchoolOptionsError ? <ErrorState text={`${t('userManagementSchoolContextError')} ${managedSchoolOptionsError}`} /> : null}
        </div>
      ) : null}

      {/* Summary cards */}
      <div className="sk-user-management-summary mt-3">
        {managedSummaryLoading ? <LoadingState text={t('userManagementSummaryLoading')} /> : null}
        {managedSummaryError ? <ErrorState text={`${t('userManagementSummaryError')} ${managedSummaryError}`} /> : null}
        {managedSummary ? (
          <div className="grid gap-2 sm:grid-cols-2 xl:grid-cols-3">
            <SummaryCard icon={<SummaryUsersIcon className="h-4 w-4 text-slate-600" />} title={t('userManagementSummaryTotalUsers')} count={managedSummary.totalUsersCount} onClick={() => { clearAllFilters(); }} />
            <SummaryCard icon={<SummaryActiveIcon className="h-4 w-4 text-emerald-600" />} title={t('userManagementSummaryActiveUsers')} count={managedSummary.activeUsersCount} onClick={() => loadManagedUsers({ pageNumber: 1, accountStatus: 'Active', blockStatus: 'clear' })} />
            <SummaryCard icon={<SummaryLockedIcon className="h-4 w-4 text-rose-600" />} title={t('userManagementSummaryLockedUsers')} count={managedSummary.lockedUsersCount} onClick={() => loadManagedUsers({ pageNumber: 1, blockStatus: 'locked' })} />
            <SummaryCard icon={<SummaryDeactivatedIcon className="h-4 w-4 text-amber-600" />} title={t('userManagementSummaryDeactivatedUsers')} count={managedSummary.deactivatedUsersCount} onClick={() => loadManagedUsers({ pageNumber: 1, accountStatus: 'Deactivated' })} />
            <SummaryCard icon={<SummaryPendingIcon className="h-4 w-4 text-indigo-600" />} title={t('userManagementSummaryPendingActivationUsers')} count={managedSummary.pendingActivationUsersCount} onClick={() => loadManagedUsers({ pageNumber: 1, activationStatus: 'pending' })} />
            <SummaryCard icon={<SummaryMfaIcon className="h-4 w-4 text-cyan-600" />} title={t('userManagementSummaryMfaUsers')} count={managedSummary.mfaEnabledUsersCount} onClick={() => loadManagedUsers({ pageNumber: 1, mfaStatus: 'enabled' })} />
          </div>
        ) : null}
      </div>

      {/* Search bar */}
      <div className="sk-user-management-panel mt-3 rounded-md border border-slate-200 bg-slate-50 p-3">
        <div className="flex flex-col gap-2 md:flex-row md:items-end">
          <div className="flex-1">
            <Field icon={<SearchIcon className="h-4 w-4 text-slate-600" />} label={t('userManagementSearchLabel')} value={userListFilters.search ?? ''} onChange={(value) => updateFilter('search', value)} placeholder={t('userManagementSearchPlaceholder')} />
          </div>
          <div className="flex gap-2">
            <button className="sk-btn sk-btn-secondary text-xs" type="button" onClick={() => setFiltersExpanded(!filtersExpanded)}>
              <FilterIcon className="mr-1 h-3.5 w-3.5" />
              {filtersExpanded ? t('userManagementCollapseFilters') : t('userManagementAdvancedFilters')}
              {activeFilters.length > 0 ? <span className="ml-1 inline-flex h-4 w-4 items-center justify-center rounded-full bg-blue-600 text-[9px] font-bold text-white">{activeFilters.length}</span> : null}
            </button>
          </div>
        </div>
      </div>

      {/* Active filter chips */}
      {activeFilters.length > 0 ? (
        <div className="sk-filter-chip-bar mt-2">
          <span className="text-xs font-medium text-slate-500">{t('userManagementActiveFiltersLabel')}</span>
          {activeFilters.map((f) => (
            <span key={f.key} className="sk-filter-chip">
              {f.label}: {f.value}
              <button type="button" onClick={() => removeFilter(f.key)} aria-label={t('userManagementRemoveFilterAria', { label: f.label })}>&times;</button>
            </span>
          ))}
          <button type="button" className="text-xs font-medium text-blue-700 hover:text-blue-900 hover:underline" onClick={clearAllFilters}>{t('userManagementClearAllFilters')}</button>
        </div>
      ) : null}

      {/* Collapsible advanced filters */}
      {filtersExpanded ? (
        <div className="sk-user-management-filter-grid sk-collapsible-enter mt-3 grid gap-2 md:grid-cols-3 lg:grid-cols-4">
          <Field label={t('userManagementFilterName')} value={userListFilters.name ?? ''} onChange={(value) => updateFilter('name', value)} />
          <Field label={t('userManagementFilterEmailOrUsername')} value={userListFilters.emailOrUsername ?? ''} onChange={(value) => updateFilter('emailOrUsername', value)} />
          <SelectField label={t('userManagementFilterRole')} value={userListFilters.role ?? ''} options={roleOptions} onChange={(value) => updateFilter('role', value)} />
          <Field label={t('userManagementFilterSchool')} value={userListFilters.school ?? ''} onChange={(value) => updateFilter('school', value)} />
          <SelectField label={t('userManagementFilterSchoolType')} value={userListFilters.schoolType ?? ''} options={schoolTypeOptions} onChange={(value) => updateFilter('schoolType', value)} />
          <SelectField label={t('userManagementFilterAccountStatus')} value={userListFilters.accountStatus ?? ''} options={accountStatusOptions} onChange={(value) => updateFilter('accountStatus', value)} />
          <SelectField label={t('userManagementFilterActivationStatus')} value={userListFilters.activationStatus ?? ''} options={activationStatusOptions} onChange={(value) => updateFilter('activationStatus', value)} />
          <SelectField label={t('userManagementFilterBlockStatus')} value={userListFilters.blockStatus ?? ''} options={blockStatusOptions} onChange={(value) => updateFilter('blockStatus', value)} />
          <SelectField label={t('userManagementFilterMfaStatus')} value={userListFilters.mfaStatus ?? ''} options={mfaStatusOptions} onChange={(value) => updateFilter('mfaStatus', value)} />
          <SelectField label={t('userManagementFilterInactivityState')} value={userListFilters.inactivityState ?? ''} options={inactivityStateOptions} onChange={(value) => updateFilter('inactivityState', value)} />
        </div>
      ) : null}

      {/* Controls: page size */}
      <div className="sk-user-management-controls mt-3 flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <select className="sk-input !w-auto text-xs" value={userListFilters.pageSize ?? 20} onChange={(e) => loadManagedUsers({ pageSize: Number(e.target.value) as 10 | 20 | 50 | 100, pageNumber: 1 })}>
            {[10, 20, 50, 100].map((size) => <option key={size} value={size}>{t('userManagementPageSizeLabel')} {size}</option>)}
          </select>
        </div>
        {managedUsers && managedUsers.totalCount > 0 ? (
          <span className="text-xs text-slate-500">{t('userManagementShowingRange', { from: String(pageFrom), to: String(pageTo), total: String(managedUsers.totalCount) })}</span>
        ) : null}
      </div>

      {/* Bulk actions bar */}
      {selectedUserIds.length > 0 ? (
        <div className="sk-bulk-bar mt-2">
          <span className="text-xs font-semibold text-blue-900">{t('userManagementBulkSelectedCount', { count: String(selectedUserIds.length) })}</span>
          <button type="button" className="sk-btn sk-btn-primary text-xs" onClick={bulkActivate}>{t('userManagementBulkActivate')}</button>
          <button type="button" className="sk-btn sk-btn-secondary text-xs" onClick={bulkDeactivate}>{t('userManagementBulkDeactivate')}</button>
          <button type="button" className="text-xs text-slate-600 hover:text-slate-900 hover:underline" onClick={() => setSelectedUserIds([])}>&times;</button>
        </div>
      ) : null}

      {/* Loading / Error / Empty states */}
      {managedUsersLoading ? (
        <div className="mt-3 space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="flex items-center gap-3 rounded-lg border border-slate-100 bg-white p-3">
              <div className="sk-skeleton-circle" />
              <div className="flex-1 space-y-2">
                <div className="sk-skeleton" style={{ width: '40%' }} />
                <div className="sk-skeleton" style={{ width: '60%', height: '12px' }} />
              </div>
              <div className="sk-skeleton" style={{ width: '80px' }} />
            </div>
          ))}
        </div>
      ) : null}
      {!managedUsersLoading && managedUsersError ? <ErrorState text={`${(userListFilters.search ?? '').trim() ? `${t('userManagementSearchError')} ` : ''}${managedUsersError}`} /> : null}
      {!managedUsersLoading && !managedUsersError && managedUsers ? (managedUsers.items.length === 0 ? (
        <div className="mt-6 flex flex-col items-center gap-2 py-8 text-center">
          <SummaryUsersIcon className="h-10 w-10 text-slate-300" />
          <p className="text-sm font-medium text-slate-500">{(userListFilters.search ?? '').trim() ? t('userManagementSearchEmptyState') : t('userManagementEmptyState')}</p>
          {activeFilters.length > 0 ? <button type="button" className="text-xs font-medium text-blue-700 hover:underline" onClick={clearAllFilters}>{t('userManagementClearAllFilters')}</button> : null}
        </div>
      ) : (
        <div className="sk-table-wrap mt-3 overflow-x-auto">
          <table className="sk-table sk-user-management-table sk-sticky">
            <thead>
              <tr className="border-b text-left">
                <th className="w-8"><input type="checkbox" checked={allSelected} onChange={toggleSelectAll} aria-label={t('userManagementSelectAll')} /></th>
                <th className="sk-sortable-th" onClick={() => toggleSort('name')}>{t('userManagementColName')}{sortIndicator('name')}</th>
                <th className="sk-sortable-th" onClick={() => toggleSort('email')}>{t('userManagementColEmail')}{sortIndicator('email')}</th>
                <th>{t('userManagementColRole')}</th>
                <th className="sk-sortable-th" onClick={() => toggleSort('school')}>{t('userManagementColSchool')}{sortIndicator('school')}</th>
                <th className="sk-sortable-th" onClick={() => toggleSort('accountStatus')}>{t('userManagementColStatus')}{sortIndicator('accountStatus')}</th>
                <th>{t('userManagementColMfa')}</th>
                <th className="sk-sortable-th" onClick={() => toggleSort('lastLogin')}>{t('userManagementColLastLogin')}{sortIndicator('lastLogin')}</th>
                <th>{t('userManagementColActions')}</th>
              </tr>
            </thead>
            <tbody>
              {managedUsers.items.map((user: IdentityManagedUser) => (
                <tr
                  key={user.userId}
                  className={`${getManagedUserRowClassName(user)} sk-user-management-row-clickable border-b`}
                  onClick={(e) => {
                    const target = e.target as HTMLElement;
                    if (target.closest('button') || target.closest('input') || target.closest('.sk-action-menu')) return;
                    openManagedDetail(user.userId);
                  }}
                >
                  <td onClick={(e) => e.stopPropagation()}>
                    <input type="checkbox" checked={selectedUserIds.includes(user.userId)} onChange={() => toggleSelectUser(user.userId)} />
                  </td>
                  <td>
                    <div className="flex items-center gap-2">
                      <span className={getUserAvatarClass(user)} title={user.roles.map(translateManagedRole).join(', ')}>{getUserInitials(user)}</span>
                      <div className="sk-user-management-name">{user.displayName || user.userName}</div>
                    </div>
                  </td>
                  <td><div className="sk-user-management-email max-w-[200px] truncate" title={user.email}>{user.email}</div></td>
                  <td><div className="sk-user-management-role">{user.roles.map(translateManagedRole).join(', ') || '-'}</div></td>
                  <td className="text-xs text-slate-600">{user.school ?? '-'}</td>
                  <td>
                    <span className="inline-flex items-center text-xs">
                      <span className={getStatusDotClass(user)} />
                      {translateManagedLifecycleStatus(user)}
                    </span>
                  </td>
                  <td>
                    {user.mfaEnabled
                      ? <span className="inline-flex items-center gap-1 text-xs text-emerald-700"><SummaryMfaIcon className="h-3.5 w-3.5" />{t('profileValueYes')}</span>
                      : <span className="text-xs text-slate-400">{t('profileValueNo')}</span>}
                  </td>
                  <td className="text-xs text-slate-600">{formatManagedDate(user.lastLoginAtUtc)}</td>
                  <td onClick={(e) => e.stopPropagation()}>
                    <div className="sk-action-menu" ref={actionMenuUserId === user.userId ? actionMenuRef : undefined}>
                      <button
                        type="button"
                        className="sk-action-menu-trigger"
                        onClick={() => setActionMenuUserId(actionMenuUserId === user.userId ? '' : user.userId)}
                        aria-label={t('userManagementColActions')}
                      >
                        <ThreeDotsIcon className="h-4 w-4" />
                      </button>
                      {actionMenuUserId === user.userId ? (
                        <div className="sk-action-menu-dropdown">
                          <button type="button" className="sk-action-menu-item" onClick={() => { setActionMenuUserId(''); openManagedDetail(user.userId); }}>
                            <EditPencilIcon className="h-3.5 w-3.5" />{t('userManagementActionEdit')}
                          </button>
                          {(user.accountLifecycleStatus !== 'Active' || user.blockedAtUtc) ? (
                            <button type="button" className="sk-action-menu-item" disabled={managedActionBusyUserId === user.userId} onClick={() => { setActionMenuUserId(''); confirmAndRun(t('userManagementConfirmActivate'), () => runManagedLifecycleAction(user.userId, () => api.adminActivate(user.userId, managedSchoolContextId || undefined))); }}>
                              <LifecycleActivateIcon className="h-3.5 w-3.5" />{t('activate')}
                            </button>
                          ) : null}
                          {user.accountLifecycleStatus !== 'Deactivated' ? (
                            <button type="button" className="sk-action-menu-item danger" disabled={managedActionBusyUserId === user.userId} onClick={() => { setActionMenuUserId(''); const reason = window.prompt(t('userManagementPromptDeactivateReason')); if (!reason?.trim()) return; confirmAndRun(t('userManagementConfirmDeactivate'), () => runManagedLifecycleAction(user.userId, () => api.adminDeactivate(user.userId, reason.trim(), managedSchoolContextId || undefined))); }}>
                              <LifecycleDeactivateIcon className="h-3.5 w-3.5" />{t('deactivate')}
                            </button>
                          ) : null}
                          {!user.blockedAtUtc ? (
                            <button type="button" className="sk-action-menu-item danger" disabled={managedActionBusyUserId === user.userId} onClick={() => { setActionMenuUserId(''); const reason = window.prompt(t('userManagementPromptBlockReason')); confirmAndRun(t('userManagementConfirmBlock'), () => runManagedLifecycleAction(user.userId, () => api.adminBlock(user.userId, reason ?? undefined, managedSchoolContextId || undefined))); }}>
                              <LifecycleBlockIcon className="h-3.5 w-3.5" />{t('userManagementActionBlock')}
                            </button>
                          ) : null}
                          {user.blockedAtUtc ? (
                            <button type="button" className="sk-action-menu-item" disabled={managedActionBusyUserId === user.userId} onClick={() => { setActionMenuUserId(''); confirmAndRun(t('userManagementConfirmUnblock'), () => runManagedLifecycleAction(user.userId, () => api.adminUnblock(user.userId, managedSchoolContextId || undefined))); }}>
                              <LifecycleUnblockIcon className="h-3.5 w-3.5" />{t('userManagementActionUnblock')}
                            </button>
                          ) : null}
                          {!user.activatedAtUtc ? (
                            <button type="button" className="sk-action-menu-item" disabled={managedActionBusyUserId === user.userId} onClick={() => { setActionMenuUserId(''); confirmAndRun(t('userManagementConfirmResendActivation'), () => runManagedLifecycleAction(user.userId, () => api.adminResendActivation(user.userId, managedSchoolContextId || undefined))); }}>
                              <LifecycleResendIcon className="h-3.5 w-3.5" />{t('userManagementActionResendActivation')}
                            </button>
                          ) : null}
                        </div>
                      ) : null}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )) : null}

      {/* Pagination */}
      {managedUsers && managedUsers.totalCount > 0 ? (
        <div className="sk-user-management-footer mt-3 flex items-center justify-between">
          <p className="text-xs text-slate-600">{t('userManagementPagingSummary')} {managedUsers.totalCount}</p>
          <div className="flex items-center gap-2">
            <button className="sk-btn sk-btn-secondary" type="button" disabled={managedUsers.pageNumber <= 1} onClick={() => loadManagedUsers({ pageNumber: Math.max(1, managedUsers.pageNumber - 1) })}>{t('userManagementPrevious')}</button>
            <span className="text-xs text-slate-600">{managedUsers.pageNumber} / {managedUsers.totalPages}</span>
            <button className="sk-btn sk-btn-secondary" type="button" disabled={managedUsers.pageNumber >= managedUsers.totalPages} onClick={() => loadManagedUsers({ pageNumber: managedUsers.pageNumber + 1 })}>{t('userManagementNext')}</button>
          </div>
        </div>
      ) : null}

      {/* Detail drawer (slide-over from right) */}
      {managedDetailUserId ? (
        <>
          <div className="sk-drawer-overlay" onClick={() => closeManagedDetail()} role="presentation" />
          <div className="sk-drawer" role="dialog" aria-modal="true" aria-label={t('userManagementDetailTitle')}>
            <div className="sk-drawer-header">
              <p className="font-semibold text-sm">{t('userManagementDetailTitle')}</p>
              <button className="sk-btn sk-btn-secondary text-xs" type="button" onClick={() => closeManagedDetail()}>{t('userManagementDetailClose')}</button>
            </div>
            {managedUserDetailLoading ? <LoadingState text={t('loading')} /> : null}
            {managedUserDetailError ? <ErrorState text={managedUserDetailError} /> : null}
            {managedUserDetail ? (
              <div className="mt-3 space-y-3">
                <div className="flex items-center gap-3 rounded-lg border bg-slate-50 p-3">
                  <span className={getUserAvatarClass(managedUserDetail as unknown as IdentityManagedUser)} style={{ width: 40, height: 40, fontSize: 14 }}>
                    {getUserInitials(managedUserDetail as unknown as IdentityManagedUser)}
                  </span>
                  <div>
                    <p className="text-sm font-medium">{managedUserDetail.firstName} {managedUserDetail.lastName}</p>
                    <p className="text-xs text-slate-600">{managedUserDetail.email}</p>
                  </div>
                </div>
                <div className="flex flex-wrap gap-1.5">
                  {[
                    { key: 'basic', icon: <ProfileIdentityIcon className="h-3.5 w-3.5" />, label: t('userManagementTabBasic') },
                    { key: 'roles', icon: <ProfileRoleIcon className="h-3.5 w-3.5" />, label: t('userManagementTabRoles') },
                    { key: 'accountState', icon: <ProfileStatusIcon className="h-3.5 w-3.5" />, label: t('userManagementTabAccountState') },
                    { key: 'security', icon: <SecurityLockIcon className="h-3.5 w-3.5" />, label: t('userManagementTabSecurity') },
                    { key: 'schoolContext', icon: <ProfileSchoolIcon className="h-3.5 w-3.5" />, label: t('userManagementTabSchoolContext') },
                    { key: 'links', icon: <RelationshipLinkIcon className="h-3.5 w-3.5" />, label: t('userManagementTabLinks') }
                  ].map((tab) => (
                    <button key={tab.key} type="button" className={`sk-btn text-xs ${activeManagedUserTab === tab.key ? 'sk-btn-primary' : 'sk-btn-secondary'} inline-flex items-center gap-1`} onClick={() => setActiveManagedUserTab(tab.key as ManagedUserDetailTab)}>
                      {tab.icon}<span>{tab.label}</span>
                    </button>
                  ))}
                </div>

                {activeManagedUserTab === 'basic' ? (
                  <div className="grid gap-2">
                    <Field label={t('profileFieldFirstName')} value={managedBasicDraft.firstName} onChange={(value) => setManagedBasicDraft((prev) => ({ ...prev, firstName: value }))} />
                    <Field label={t('profileFieldLastName')} value={managedBasicDraft.lastName} onChange={(value) => setManagedBasicDraft((prev) => ({ ...prev, lastName: value }))} />
                    <Field label={t('profileFieldPreferredDisplayName')} value={managedBasicDraft.preferredDisplayName} onChange={(value) => setManagedBasicDraft((prev) => ({ ...prev, preferredDisplayName: value }))} />
                    <LanguageField
                      label={t('profileFieldPreferredLanguage')}
                      value={managedBasicDraft.preferredLanguage}
                      placeholder={t('profileSelectLanguagePlaceholder')}
                      onChange={(value) => setManagedBasicDraft((prev) => ({ ...prev, preferredLanguage: value }))}
                    />
                    <Field label={t('profileFieldPhoneNumber')} value={managedBasicDraft.phoneNumber} onChange={(value) => setManagedBasicDraft((prev) => ({ ...prev, phoneNumber: value }))} />
                    <Field label={t('profileFieldContactEmail')} value={managedBasicDraft.contactEmail} onChange={(value) => setManagedBasicDraft((prev) => ({ ...prev, contactEmail: value }))} />
                    <Field label={t('email')} value={managedUserDetail.email} onChange={() => {}} disabled />
                    <Field label={t('userManagementLabelUsername')} value={managedUserDetail.userName} onChange={() => {}} disabled />
                    <button className="sk-btn sk-btn-primary text-xs inline-flex items-center gap-1" type="button" disabled={managedActionBusyUserId === managedUserDetail.userId} onClick={() => confirmAndRun(t('userManagementConfirmSaveBasic'), saveManagedBasic)}>
                      <SaveDiskIcon className="h-4 w-4" />{t('userManagementSaveBasic')}
                    </button>
                  </div>
                ) : null}

                {activeManagedUserTab === 'roles' ? (
                  <div>
                    <p className="text-xs font-semibold uppercase text-slate-500 inline-flex items-center gap-1">
                      <ProfileRoleIcon className="h-4 w-4" />{t('userManagementSectionRoles')}
                    </p>
                    <div className="mt-2 flex flex-wrap gap-2">
                      {['PlatformAdministrator', 'SchoolAdministrator', 'Teacher', 'Parent', 'Student'].map((role) => (
                        <label key={role} className="text-xs flex items-center gap-1">
                          <input type="checkbox" disabled={!isPlatformAdministrator && role === 'PlatformAdministrator'} checked={managedRoleSetDraft.includes(role)} onChange={(e) => setManagedRoleSetDraft((prev: string[]) => e.target.checked ? [...prev, role] : prev.filter((x) => x !== role))} />
                          {translateManagedRole(role)}
                        </label>
                      ))}
                    </div>
                    <div className="mt-2">
                      <button className="sk-btn sk-btn-primary text-xs inline-flex items-center gap-1" type="button" disabled={managedActionBusyUserId === managedUserDetail.userId} onClick={() => confirmAndRun(t('userManagementConfirmSaveRoles'), saveManagedRoleSet)}>
                        <SaveDiskIcon className="h-4 w-4" />{t('userManagementSaveRoles')}
                      </button>
                    </div>
                  </div>
                ) : null}

                {activeManagedUserTab === 'accountState' ? (
                  <div className="grid gap-2">
                    <Field label={t('userManagementColStatus')} value={translateManagedLifecycleStatus(managedUserDetail)} onChange={() => {}} disabled />
                    <Field label={t('userManagementDetailActivationStatus')} value={managedUserDetail.activatedAtUtc ? t('stateActive') : t('userManagementActivationPending')} onChange={() => {}} disabled />
                    <Field label={t('userManagementColLastLogin')} value={formatManagedDate(managedUserDetail.lastLoginAtUtc)} onChange={() => {}} disabled />
                    <Field label={t('userManagementDetailLastActivity')} value={formatManagedDate(managedUserDetail.lastActivityAtUtc)} onChange={() => {}} disabled />
                  </div>
                ) : null}

                {activeManagedUserTab === 'security' ? (
                  <div className="grid gap-2">
                    <Field label={t('userManagementSecurityEmailConfirmed')} value={managedUserDetail.emailConfirmed ? t('profileValueYes') : t('profileValueNo')} onChange={() => {}} disabled />
                    <Field label={t('userManagementSecurityMfaEnabled')} value={managedUserRow?.mfaEnabled ? t('profileValueYes') : t('profileValueNo')} onChange={() => {}} disabled />
                    <Field label={t('userManagementSecurityLockout')} value={formatManagedDate(managedUserDetail.lockoutEndUtc)} onChange={() => {}} disabled />
                    <Field label={t('userManagementSecurityRecoverySummary')} value={t('userManagementSecurityRecoveryNotExposed')} onChange={() => {}} disabled />
                    <div className="flex flex-wrap gap-2">
                      <button className="sk-btn sk-btn-secondary text-xs" type="button" disabled={managedActionBusyUserId === managedUserDetail.userId} onClick={() => confirmAndRun(t('userManagementConfirmDisableMfa'), () => runManagedLifecycleAction(managedUserDetail.userId, () => api.adminDisableMfa(managedUserDetail.userId, managedSchoolContextId || undefined)))}>{t('userManagementActionDisableMfa')}</button>
                      <button className="sk-btn sk-btn-secondary text-xs" type="button" disabled={managedActionBusyUserId === managedUserDetail.userId} onClick={() => confirmAndRun(t('userManagementConfirmUnlockLockout'), () => runManagedLifecycleAction(managedUserDetail.userId, () => api.adminUnlockLockout(managedUserDetail.userId, managedSchoolContextId || undefined)))}>{t('userManagementActionUnlockLockout')}</button>
                    </div>
                  </div>
                ) : null}

                {activeManagedUserTab === 'schoolContext' ? (
                  <div className="grid gap-2">
                    <Field label={t('userManagementColSchool')} value={managedBasicDraft.schoolPlacement} onChange={(value) => setManagedBasicDraft((prev) => ({ ...prev, schoolPlacement: value }))} />
                    <Field label={t('userManagementFilterSchoolType')} value={managedBasicDraft.schoolContextSummary} onChange={(value) => setManagedBasicDraft((prev) => ({ ...prev, schoolContextSummary: value }))} />
                    <Field label={t('profileLabelAssignedSchools')} value={managedSchoolIdsDraft.join(', ')} onChange={(value) => setManagedSchoolIdsDraft(value.split(',').map((x) => x.trim()).filter((x) => x.length > 0))} />
                    <Field label={t('userManagementSchoolScopeHint')} value={isPlatformAdministrator ? t('userManagementScopePlatform') : t('userManagementScopeSchool')} onChange={() => {}} disabled />
                    <button className="sk-btn sk-btn-primary text-xs inline-flex items-center gap-1" type="button" disabled={managedActionBusyUserId === managedUserDetail.userId} onClick={() => confirmAndRun(t('userManagementConfirmSaveSchoolContext'), saveManagedSchoolContext)}>
                      <SaveDiskIcon className="h-4 w-4" />{t('userManagementSaveSchoolContext')}
                    </button>
                  </div>
                ) : null}

                {activeManagedUserTab === 'links' ? (
                  <div className="grid gap-2">
                    <p className="text-sm text-slate-700">{managedUserDetail.roles.includes('Parent') ? t('userManagementLinksParentSummary') : t('userManagementLinksNoParent')}</p>
                    <p className="text-sm text-slate-700">{managedUserDetail.roles.includes('Teacher') ? t('userManagementLinksTeacherSummary') : t('userManagementLinksNoTeacher')}</p>
                    <p className="text-sm text-slate-700">{managedUserDetail.roles.includes('Student') ? t('userManagementLinksStudentSummary') : t('userManagementLinksNoStudent')}</p>
                    {managedUserDetail.roles.includes('Parent') ? (
                      <>
                        <label className="sk-label">{t('userManagementParentLinksEditorLabel')}</label>
                        <textarea className="sk-input min-h-24" value={managedParentLinksDraft} onChange={(e) => setManagedParentLinksDraft(e.target.value)} />
                        <button className="sk-btn sk-btn-primary text-xs inline-flex items-center gap-1" type="button" disabled={managedActionBusyUserId === managedUserDetail.userId} onClick={() => confirmAndRun(t('userManagementConfirmSaveLinks'), saveManagedParentLinks)}>
                          <SaveDiskIcon className="h-4 w-4" />{t('userManagementSaveLinks')}
                        </button>
                      </>
                    ) : null}
                  </div>
                ) : null}
              </div>
            ) : null}
          </div>
        </>
      ) : null}
      </section>
    </Card>
  );
}


function SummaryCard({ icon, title, count, onClick }: { icon: React.ReactNode; title: string; count: number; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="w-full rounded-lg border border-slate-200 bg-white p-3 text-left shadow-sm transition hover:border-slate-300"
    >
      <div className="flex items-center justify-between gap-2">
        <p className="text-xs font-medium text-slate-600">{title}</p>
        <span>{icon}</span>
      </div>
      <p className="mt-2 text-2xl font-semibold text-slate-900">{count}</p>
    </button>
  );
}

function SelectField({
  label,
  value,
  options,
  onChange,
  placeholder = '\u2014'
}: {
  label: string;
  value: string;
  options: { value: string; label: string }[];
  onChange: (value: string) => void;
  placeholder?: string;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label">{label}</label>
      <select className="sk-input" value={value} onChange={(e) => onChange(e.target.value)}>
        <option value="">{placeholder}</option>
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>{opt.label}</option>
        ))}
      </select>
    </div>
  );
}

function Field({
  icon,
  label,
  value,
  onChange,
  disabled = false,
  invalid = false,
  errorText,
  placeholder
}: {
  icon?: React.ReactNode;
  label: string;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  invalid?: boolean;
  errorText?: string;
  placeholder?: string;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label inline-flex items-center gap-1.5">
        {icon}
        <span>{label}</span>
      </label>
      <input
        className={`sk-input ${invalid ? 'sk-input-invalid' : ''}`}
        value={value}
        disabled={disabled}
        aria-invalid={invalid}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
      />
      {errorText ? <span className="text-xs text-red-700">{errorText}</span> : null}
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

function SchoolPositionField({
  icon,
  label,
  value,
  options,
  loading,
  onChange,
  loadingText,
  placeholder,
  unavailableText,
  invalid = false,
  errorText
}: {
  icon?: React.ReactNode;
  label: string;
  value: string;
  options: SchoolPositionOption[];
  loading: boolean;
  onChange: (value: string) => void;
  loadingText: string;
  placeholder: string;
  unavailableText: string;
  invalid?: boolean;
  errorText?: string;
}) {
  const disabled = loading || options.length === 0;

  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label inline-flex items-center gap-1.5">
        {icon}
        <span>{label}</span>
      </label>
      <select
        className={`sk-input ${invalid ? 'sk-input-invalid' : ''}`}
        value={value}
        disabled={disabled}
        aria-invalid={invalid}
        onChange={(e) => onChange(e.target.value)}
      >
        <option value="">
          {loading ? loadingText : (options.length > 0 ? placeholder : unavailableText)}
        </option>
        {options.map((option) => (
          <option key={option.code} value={option.code}>
            {option.label}
          </option>
        ))}
      </select>
      {errorText ? <span className="text-xs text-red-700">{errorText}</span> : null}
    </div>
  );
}

function ParentContactChannelField({
  icon,
  label,
  value,
  onChange,
  placeholder,
  emailLabel,
  phoneLabel,
  appLabel,
  disabled = false,
  invalid = false,
  errorText
}: {
  icon?: React.ReactNode;
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder: string;
  emailLabel: string;
  phoneLabel: string;
  appLabel: string;
  disabled?: boolean;
  invalid?: boolean;
  errorText?: string;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label inline-flex items-center gap-1.5">
        {icon}
        <span>{label}</span>
      </label>
      <select
        className={`sk-input ${invalid ? 'sk-input-invalid' : ''}`}
        value={value}
        disabled={disabled}
        aria-invalid={invalid}
        onChange={(e) => onChange(e.target.value)}
      >
        <option value="">{placeholder}</option>
        <option value="EMAIL">{emailLabel}</option>
        <option value="PHONE">{phoneLabel}</option>
        <option value="APP">{appLabel}</option>
      </select>
      {errorText ? <span className="text-xs text-red-700">{errorText}</span> : null}
    </div>
  );
}

function FeedbackBanner({
  type,
  message,
  dismissLabel,
  onDismiss
}: {
  type: 'success' | 'error';
  message: string;
  dismissLabel: string;
  onDismiss: () => void;
}) {
  return (
    <div className={`sk-feedback-banner mt-3 ${type === 'success' ? 'success' : 'error'}`} role={type === 'error' ? 'alert' : 'status'} aria-live={type === 'error' ? 'assertive' : 'polite'}>
      <span className="text-sm font-medium">{message}</span>
      <button type="button" onClick={onDismiss} className="sk-feedback-dismiss">
        {dismissLabel}
      </button>
    </div>
  );
}

function mapProfileError(error: unknown, t: (key: 'profileSaveErrorInvalidSchoolPosition' | 'profileSaveErrorValidation' | 'profileSaveErrorGeneric') => string) {
  const validation = extractValidationErrors(error);
  const merged = [...validation.formErrors, ...Object.values(validation.fieldErrors).flat()].join(' ').toLowerCase();
  if (merged.includes('position') || merged.includes('school context')) {
    return t('profileSaveErrorInvalidSchoolPosition');
  }
  if (Object.keys(validation.fieldErrors).length > 0 || validation.formErrors.length > 0) {
    return t('profileSaveErrorValidation');
  }
  const normalized = (error instanceof Error ? error.message : '').toLowerCase();
  if (normalized.includes('selected school position is not allowed')) {
    return t('profileSaveErrorInvalidSchoolPosition');
  }
  if (normalized.includes('validation')) {
    return t('profileSaveErrorValidation');
  }
  return t('profileSaveErrorGeneric');
}


function SummaryUsersIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}><path d="M16 20v-1a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v1" /><circle cx="9.5" cy="7" r="3" /><path d="M22 20v-1a4 4 0 0 0-3-3.87" /><path d="M16 3.13a3 3 0 0 1 0 5.74" /></svg>;
}

function SummaryActiveIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}><circle cx="12" cy="12" r="9" /><path d="m8 12 2.5 2.5L16 9" /></svg>;
}

function SummaryLockedIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}><rect x="5" y="11" width="14" height="10" rx="2" /><path d="M8 11V8a4 4 0 1 1 8 0v3" /></svg>;
}

function SummaryDeactivatedIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}><circle cx="12" cy="12" r="9" /><path d="m9 8 6 4-6 4z" /></svg>;
}

function SummaryPendingIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></svg>;
}

function SummaryMfaIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}><path d="M12 3 5 6v6c0 4.4 2.9 8.4 7 9 4.1-.6 7-4.6 7-9V6l-7-3Z" /><path d="m9.5 12 1.7 1.7L14.8 10" /></svg>;
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

function UserManagementSectionIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return <ProfileAssignmentIcon className={className} />;
}


function SearchIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox='0 0 24 24' fill='none' stroke='currentColor' strokeWidth='1.8' className={className} aria-hidden='true'>
      <circle cx='11' cy='11' r='7' />
      <path d='M20 20L16.6 16.6' strokeLinecap='round' />
    </svg>
  );
}

function ReloadIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M20 12a8 8 0 1 1-2.3-5.6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <path d="M20 5v4h-4" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function LifecycleActivateIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return <ProfileStatusIcon className={className} />;
}

function LifecycleDeactivateIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="1.8" />
      <path d="M8 12h8" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function LifecycleBlockIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return <SecurityLockIcon className={className} />;
}

function LifecycleUnblockIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <rect x="5" y="11" width="14" height="9" rx="2" stroke="currentColor" strokeWidth="1.8" />
      <path d="M9 11V8a3 3 0 1 1 6 0" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function LifecycleResendIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return <ProfileEmailIcon className={className} />;
}

function EditPencilIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="m6 18 1-4 8-8 3 3-8 8-4 1Z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
      <path d="m13.5 7.5 3 3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function SecurityLockIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <rect x="5" y="11" width="14" height="9" rx="2" stroke="currentColor" strokeWidth="1.8" />
      <path d="M8 11V8a4 4 0 0 1 8 0v3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function RelationshipLinkIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M10 14 7.5 16.5a3 3 0 0 1-4.2-4.2L6 9.6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <path d="m14 10 2.5-2.5a3 3 0 0 1 4.2 4.2L18 14.4" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <path d="m8.5 15.5 7-7" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function FilterIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M3 4h18l-7 8.5V19l-4 2v-8.5L3 4Z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
    </svg>
  );
}

function ThreeDotsIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="currentColor" aria-hidden="true">
      <circle cx="12" cy="5" r="1.5" />
      <circle cx="12" cy="12" r="1.5" />
      <circle cx="12" cy="19" r="1.5" />
    </svg>
  );
}

function CreateUserActionIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <circle cx="10" cy="8" r="4" stroke="currentColor" strokeWidth="1.8" />
      <path d="M4 20c0-4 3.2-7 7-7" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <path d="M18 13v6M15 16h6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
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
