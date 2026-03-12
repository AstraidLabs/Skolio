import React, { useEffect, useMemo, useRef, useState } from 'react';
import { useI18n } from '../i18n';
import type { SessionState } from '../shared/auth/session';
import type {
  ClassRoom,
  GradeLevel,
  School,
  SchoolMutation,
  SchoolYear,
  Subject,
  TeacherAssignment,
  TeachingGroup,
  StudentContext,
  createOrganizationApi
} from './api';
import { OrganizationContextSwitcher } from './OrganizationContextSwitcher';
import { OrganizationSchoolIdentityCard } from './OrganizationSchoolIdentityCard';
import { getPlatformStatusLabel, getSchoolKindLabel, getSchoolTypeLabel } from './schoolLabels';
import { Card, SectionHeader, StatusBadge } from '../shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from '../shared/ui/states';

type OrganizationView =
  | 'overview'
  | 'schools'
  | 'school-years'
  | 'grade-levels'
  | 'secondary-fields'
  | 'class-rooms'
  | 'teaching-groups'
  | 'subjects'
  | 'teacher-assignments';

type SchoolStatusFilter = 'all' | 'active' | 'inactive';

const EMPTY_SCHOOL: SchoolMutation = {
  name: '',
  schoolType: 'ElementarySchool',
  schoolKind: 'General',
  schoolIzo: '',
  schoolEmail: '',
  schoolPhone: '',
  schoolWebsite: '',
  mainAddress: { street: '', city: '', postalCode: '', country: 'CZ' },
  educationLocationsSummary: '',
  registryEntryDate: '',
  educationStartDate: '',
  maxStudentCapacity: undefined,
  teachingLanguage: 'cs',
  platformStatus: 'Active',
  schoolOperator: {
    legalEntityName: '',
    legalForm: 'PublicInstitution',
    companyNumberIco: '',
    registeredOfficeAddress: { street: '', city: '', postalCode: '', country: 'CZ' },
    resortIdentifier: '',
    directorSummary: '',
    statutoryBodySummary: ''
  },
  founder: {
    founderType: 'Municipality',
    founderCategory: 'Public',
    founderName: '',
    founderLegalForm: 'Municipality',
    founderIco: '',
    founderAddress: { street: '', city: '', postalCode: '', country: 'CZ' },
    founderEmail: ''
  }
};
const EMPTY_SCHOOL_YEAR = { label: '', startDate: '', endDate: '' };
const EMPTY_GRADE_LEVEL = { level: 1, displayName: '' };
const EMPTY_CLASS_ROOM = { gradeLevelId: '', code: '', displayName: '' };
const EMPTY_GROUP = { classRoomId: '', name: '', isDailyOperationsGroup: true };
const EMPTY_SUBJECT = { code: '', name: '' };
const EMPTY_ASSIGNMENT = { teacherUserId: '', scope: 'SubjectTeacher', classRoomId: '', teachingGroupId: '', subjectId: '' };

export function OrganizationParityPage({
  api,
  session,
  initialView = 'overview'
}: {
  api: ReturnType<typeof createOrganizationApi>;
  session: SessionState;
  initialView?: OrganizationView;
}) {
  const { t } = useI18n();
  const [activeView, setActiveView] = useState<OrganizationView>(initialView);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const [schools, setSchools] = useState<School[]>([]);
  const [schoolYears, setSchoolYears] = useState<SchoolYear[]>([]);
  const [gradeLevels, setGradeLevels] = useState<GradeLevel[]>([]);
  const [classRooms, setClassRooms] = useState<ClassRoom[]>([]);
  const [teachingGroups, setTeachingGroups] = useState<TeachingGroup[]>([]);
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [teacherAssignments, setTeacherAssignments] = useState<TeacherAssignment[]>([]);
  const [studentContext, setStudentContext] = useState<StudentContext | null>(null);

  const [activeSchoolId, setActiveSchoolId] = useState(session.schoolIds[0] ?? '');

  const [newSchool, setNewSchool] = useState<SchoolMutation>({ ...EMPTY_SCHOOL, schoolType: session.schoolType });
  const [newSchoolYear, setNewSchoolYear] = useState(EMPTY_SCHOOL_YEAR);
  const [newGradeLevel, setNewGradeLevel] = useState(EMPTY_GRADE_LEVEL);
  const [newClassRoom, setNewClassRoom] = useState(EMPTY_CLASS_ROOM);
  const [newGroup, setNewGroup] = useState(EMPTY_GROUP);
  const [newSubject, setNewSubject] = useState(EMPTY_SUBJECT);
  const [newAssignment, setNewAssignment] = useState(EMPTY_ASSIGNMENT);
  const [schoolSearch, setSchoolSearch] = useState('');
  const [schoolTypeFilter, setSchoolTypeFilter] = useState('');
  const [schoolStatusFilter, setSchoolStatusFilter] = useState<SchoolStatusFilter>('all');
  const [schoolWizardOpen, setSchoolWizardOpen] = useState(false);
  const [schoolWizardStep, setSchoolWizardStep] = useState(1);
  const [schoolDetailOpen, setSchoolDetailOpen] = useState(false);
  const [schoolActionMenuId, setSchoolActionMenuId] = useState('');
  const schoolActionMenuRef = useRef<HTMLDivElement | null>(null);

  const isPlatformAdmin = session.roles.includes('PlatformAdministrator');
  const isSchoolAdmin = session.roles.includes('SchoolAdministrator');
  const isTeacher = session.roles.includes('Teacher') && !isSchoolAdmin && !isPlatformAdmin;
  const isParent = session.roles.includes('Parent');
  const isStudent = session.roles.includes('Student') && !isTeacher && !isSchoolAdmin && !isPlatformAdmin;

  const canWriteSchoolContext = isPlatformAdmin || isSchoolAdmin;
  const canCreateSchool = isPlatformAdmin;
  const canManageOrganization = isPlatformAdmin || isSchoolAdmin;
  const canTeacherScopedOrganization = isTeacher;
  const canSwitchSchoolContext = (isPlatformAdmin || isSchoolAdmin) && schools.length > 1;
  const showReadOnlySchoolContext = (isPlatformAdmin || isSchoolAdmin) && schools.length <= 1;

  const resetSchoolWizard = () => {
    setSchoolWizardOpen(false);
    setSchoolWizardStep(1);
    setNewSchool({ ...EMPTY_SCHOOL, schoolType: session.schoolType });
  };

  const schoolStepValid = (step: number) => {
    if (step === 1) {
      return Boolean(
        newSchool.name.trim()
        && newSchool.mainAddress.street.trim()
        && newSchool.mainAddress.city.trim()
        && newSchool.mainAddress.postalCode.trim()
      );
    }

    if (step === 2) {
      return Boolean(newSchool.schoolOperator.legalEntityName.trim());
    }

    if (step === 3) {
      return Boolean(
        newSchool.founder.founderName.trim()
        && newSchool.founder.founderAddress.street.trim()
        && newSchool.founder.founderAddress.city.trim()
        && newSchool.founder.founderAddress.postalCode.trim()
      );
    }

    return true;
  };

  const loadSchoolBoundaries = async (schoolId: string) => {
    if (!schoolId) {
      setSchoolYears([]);
      setGradeLevels([]);
      setClassRooms([]);
      setTeachingGroups([]);
      setSubjects([]);
      setTeacherAssignments([]);
      return;
    }

    await Promise.all([
      api.schoolYears(schoolId).then(setSchoolYears),
      api.gradeLevels(schoolId).then(setGradeLevels),
      api.classRooms(schoolId).then(setClassRooms),
      api.teachingGroups(schoolId).then(setTeachingGroups),
      api.subjects(schoolId).then(setSubjects),
      api.teacherAssignments(schoolId).then(setTeacherAssignments)
    ]);
  };

  const load = () => {
    setLoading(true);
    setError('');

    if (isStudent) {
      const studentSchoolId = session.schoolIds[0] ?? '';
      if (!studentSchoolId) {
        setStudentContext(null);
        setLoading(false);
        return;
      }

      void api.studentContext(studentSchoolId)
        .then((context) => {
          setStudentContext(context);
          setActiveSchoolId(context.school.id);
        })
        .catch((e: Error) => setError(e.message))
        .finally(() => setLoading(false));
      return;
    }

    void api.schools()
      .then(async (result) => {
        setSchools(result);
        const scopedSchoolId = session.schoolIds[0] ?? result[0]?.id ?? '';
        setActiveSchoolId((current) => current || scopedSchoolId);
        await loadSchoolBoundaries(scopedSchoolId);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  };

  useEffect(load, [api, session.accessToken]);

  useEffect(() => {
    setActiveView(initialView === 'secondary-fields' ? 'grade-levels' : initialView);
  }, [initialView]);

  useEffect(() => {
    if (isStudent || !activeSchoolId) return;
    void loadSchoolBoundaries(activeSchoolId).catch((e: Error) => setError(e.message));
  }, [activeSchoolId, isStudent]);

  useEffect(() => {
    if (!schoolActionMenuId) return;

    const onMouseDown = (event: MouseEvent) => {
      if (schoolActionMenuRef.current && !schoolActionMenuRef.current.contains(event.target as Node)) {
        setSchoolActionMenuId('');
      }
    };

    document.addEventListener('mousedown', onMouseDown);
    return () => document.removeEventListener('mousedown', onMouseDown);
  }, [schoolActionMenuId]);

  const currentSchool = schools.find((x) => x.id === activeSchoolId) ?? schools[0] ?? null;

  const guarded = (action: () => Promise<unknown>, successMessage?: string) => {
    setError('');
    setNotice('');
    void action()
      .then(() => {
        if (successMessage) {
          setNotice(successMessage);
        }

        load();
      })
      .catch((e: Error) => setError(e.message));
  };

  const goTo = (route: string) => {
    window.history.pushState({}, '', route);
    window.dispatchEvent(new PopStateEvent('popstate'));
  };

  const createSchool = () => guarded(async () => {
    await api.createSchool(newSchool);
    resetSchoolWizard();
  }, t('orgCreateSchoolSuccess'));

  const createSchoolYear = () => guarded(async () => {
    await api.createSchoolYear({ id: '', schoolId: activeSchoolId, ...newSchoolYear });
    setNewSchoolYear(EMPTY_SCHOOL_YEAR);
  });

  const createGradeLevel = () => guarded(async () => {
    await api.createGradeLevel({ id: '', schoolId: activeSchoolId, ...newGradeLevel });
    setNewGradeLevel(EMPTY_GRADE_LEVEL);
  });

  const createClassRoom = () => guarded(async () => {
    await api.createClassRoom({ id: '', schoolId: activeSchoolId, ...newClassRoom });
    setNewClassRoom(EMPTY_CLASS_ROOM);
  });

  const createGroup = () => guarded(async () => {
    await api.createTeachingGroup({ id: '', schoolId: activeSchoolId, ...newGroup });
    setNewGroup(EMPTY_GROUP);
  });

  const createSubject = () => guarded(async () => {
    await api.createSubject({ id: '', schoolId: activeSchoolId, ...newSubject });
    setNewSubject(EMPTY_SUBJECT);
  });

  const createAssignment = () => guarded(async () => {
    await api.createTeacherAssignment({ id: '', schoolId: activeSchoolId, ...newAssignment });
    setNewAssignment(EMPTY_ASSIGNMENT);
  });

  const overviewEntryPoints = useMemo(() => {
    if (isParent || isStudent) return [] as { label: string; description: string; count: number; route: string; emphasize?: boolean }[];

    const entries = [
      { label: 'Školy', description: 'Přehled škol a jejich aktivace.', count: schools.length, route: '/organization/schools' },
      { label: 'Přehled školních roků', description: 'Aktivní a historické školní roky.', count: schoolYears.length, route: '/organization/school-years' },
      { label: 'Ročníky', description: 'Organizační struktura ročníků.', count: gradeLevels.length, route: '/organization/grade-levels' },
      { label: 'Třídy', description: 'Třídní struktura školy.', count: classRooms.length, route: '/organization/classes', emphasize: session.schoolType === 'ElementarySchool' },
      { label: 'Skupiny', description: 'Skupinová struktura a provozní skupiny.', count: teachingGroups.length, route: '/organization/groups', emphasize: session.schoolType === 'Kindergarten' },
      { label: 'Předměty', description: 'Předmětový kontext školy.', count: subjects.length, route: '/organization/subjects', emphasize: session.schoolType === 'ElementarySchool' },
      { label: t('navTeacherAssignments'), description: 'Organizační přiřazení učitelů.', count: teacherAssignments.length, route: '/organization/teacher-assignments' }
    ];

    if (session.schoolType === 'SecondarySchool') {
      return [entries[0], entries[1], entries[2], entries[3], entries[4], entries[5], entries[6].emphasize ? entries[6] : { ...entries[6], emphasize: true }];
    }

    return entries;
  }, [isParent, isStudent, schools.length, schoolYears.length, gradeLevels.length, classRooms.length, teachingGroups.length, subjects.length, teacherAssignments.length, session.schoolType]);

  const filteredSchools = useMemo(() => {
    const search = schoolSearch.trim().toLowerCase();

    return schools.filter((school) => {
      const matchesSearch = !search || [
        school.name,
        school.mainAddress.city,
        school.schoolIzo,
        school.schoolOperator?.legalEntityName,
        school.founder?.founderName
      ].filter(Boolean).some((value) => value!.toLowerCase().includes(search));

      const matchesType = !schoolTypeFilter || school.schoolType === schoolTypeFilter;
      const matchesStatus = schoolStatusFilter === 'all'
        || (schoolStatusFilter === 'active' && school.isActive)
        || (schoolStatusFilter === 'inactive' && !school.isActive);

      return matchesSearch && matchesType && matchesStatus;
    });
  }, [schoolSearch, schoolStatusFilter, schoolTypeFilter, schools]);

  const schoolSummary = useMemo(() => ({
    filtered: filteredSchools.length,
    active: schools.filter((school) => school.isActive).length,
    kindergarten: schools.filter((school) => school.schoolType === 'Kindergarten').length,
    elementaryAndSecondary: schools.filter((school) => school.schoolType !== 'Kindergarten').length
  }), [filteredSchools.length, schools]);

  const contextSwitcherBlock = (showHelperText: boolean) => {
    if (!canSwitchSchoolContext && !showReadOnlySchoolContext) return null;

    return (
      <Card>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <p className="text-sm font-semibold text-slate-900">{t('orgActiveSchoolContextTitle')}</p>
            <p className="mt-1 text-xs text-slate-600">
              {canSwitchSchoolContext
                ? t('orgActiveSchoolContextSwitchDescription')
                : t('orgActiveSchoolContextSingleDescription')}
            </p>
          </div>
          <div className="w-full max-w-md">
            {canSwitchSchoolContext ? (
              <OrganizationContextSwitcher
                schools={schools}
                activeSchoolId={activeSchoolId}
                onSelectSchool={setActiveSchoolId}
              />
            ) : (
              <div className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm">
                <p className="font-semibold text-slate-900">{currentSchool?.name ?? 'N/A'}</p>
                <p className="text-xs text-slate-500">
                  {currentSchool?.schoolType ? getSchoolTypeLabel(t, currentSchool.schoolType) : '-'}
                </p>
              </div>
            )}
          </div>
        </div>
        {showHelperText ? (
          <div className="mt-3 text-xs text-slate-500">
            {t('orgActiveSchoolContextBoundaryHint')}
          </div>
        ) : null}
      </Card>
    );
  };

  if (loading) return <LoadingState text="Loading organization capabilities..." />;
  if (error) return <ErrorState text={error} />;

  if (isStudent && studentContext) {
    return (
      <section className="space-y-3">
        <SectionHeader title={t('routeOrganization')} description="?tec? ?koln? kontext studenta." />
        <Card>
          <p className="font-semibold text-sm">{studentContext.school.name} ({studentContext.school.schoolType})</p>
          <p className="mt-1 text-sm text-slate-600">Tento přístup je pouze čtecí.</p>
        </Card>
        <div className="grid gap-3 md:grid-cols-2">
          <Card><p className="font-semibold text-sm">Školní roky</p><ul className="sk-list">{studentContext.schoolYears.map((x) => <li className="sk-list-item" key={x.id}>{x.label}</li>)}</ul></Card>
          <Card><p className="font-semibold text-sm">Třídy</p><ul className="sk-list">{studentContext.classRooms.map((x) => <li className="sk-list-item" key={x.id}>{x.displayName}</li>)}</ul></Card>
          <Card><p className="font-semibold text-sm">Skupiny</p><ul className="sk-list">{studentContext.teachingGroups.map((x) => <li className="sk-list-item" key={x.id}>{x.name}</li>)}</ul></Card>
          <Card><p className="font-semibold text-sm">Předměty</p><ul className="sk-list">{studentContext.subjects.map((x) => <li className="sk-list-item" key={x.id}>{x.name}</li>)}</ul></Card>
        </div>
      </section>
    );
  }

  if (activeView === 'overview') {
    return (
      <section className="space-y-3">
        <SectionHeader title="Organization" description="Přehled organizační oblasti a vstupní body na jednotlivé organizační stránky. Sidebar zůstává hlavní navigace." />
        {contextSwitcherBlock(true)}

        <Card>
          <div className="grid gap-3 md:grid-cols-3">
            <Metric label="Školy" value={schools.length} />
            <Metric label="Školní roky" value={schoolYears.length} />
            <Metric label="Ročníky" value={gradeLevels.length} />
            <Metric label="Třídy" value={classRooms.length} />
            <Metric label="Skupiny" value={teachingGroups.length} />
            <Metric label="Předměty" value={subjects.length} />
          </div>
          <div className="mt-3 text-sm text-slate-600">
            {currentSchool ? `Aktivní školní kontext: ${currentSchool.name} (${currentSchool.schoolType})` : 'Není vybraný školní kontext.'}
          </div>
        </Card>

        {(canManageOrganization || canTeacherScopedOrganization) ? (
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            {overviewEntryPoints.map((entry) => (
              <Card key={entry.route} className={entry.emphasize ? 'border-sky-200 bg-sky-50/60' : ''}>
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <p className="font-semibold text-sm">{entry.label}</p>
                    <p className="mt-1 text-xs text-slate-600">{entry.description}</p>
                  </div>
                  <StatusBadge label={String(entry.count)} tone="info" />
                </div>
                <div className="mt-3">
                  <button className="sk-btn sk-btn-secondary" type="button" onClick={() => goTo(entry.route)}>Spravovat</button>
                </div>
              </Card>
            ))}
          </div>
        ) : (
          <Card>
            <p className="text-sm text-slate-700">Pro tuto roli není organizační management dostupný. Používejte pouze přidělené read-only kontexty.</p>
          </Card>
        )}
      </section>
    );
  }

  if (activeView === 'schools') {
    return (
      <section className="space-y-4">
        <SectionHeader
          title={t('orgSchoolsTitle')}
          description={t('orgSchoolsHeroDescription')}
          action={canCreateSchool ? (
            <button
              type="button"
              className={`sk-btn ${schoolWizardOpen ? 'sk-btn-secondary' : 'sk-btn-primary'}`}
              onClick={() => setSchoolWizardOpen((current) => !current)}
            >
              {schoolWizardOpen ? t('orgCreateSchoolCloseWizard') : t('orgCreateSchoolOpenWizard')}
            </button>
          ) : undefined}
        />
        {notice ? (
          <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
            {notice}
          </div>
        ) : null}
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          <Metric label={t('orgSchoolsSummaryFiltered')} value={schoolSummary.filtered} />
          <Metric label={t('orgSchoolsSummaryActive')} value={schoolSummary.active} />
          <Metric label={t('orgSchoolsSummaryKindergarten')} value={schoolSummary.kindergarten} />
          <Metric label={t('orgSchoolsSummaryElementarySecondary')} value={schoolSummary.elementaryAndSecondary} />
        </div>
        {schoolWizardOpen ? (
          <SchoolCreateWizard
            t={t}
            wizardStep={schoolWizardStep}
            setWizardStep={setSchoolWizardStep}
            form={newSchool}
            setForm={setNewSchool}
            onCancel={resetSchoolWizard}
            onSubmit={createSchool}
            stepValid={schoolStepValid}
          />
        ) : null}
        <div className="space-y-4">
          <Card>
            <div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_220px_220px_auto]">
              <InputField
                label={t('orgSchoolsSearchLabel')}
                value={schoolSearch}
                placeholder={t('orgSearchSchools')}
                onChange={setSchoolSearch}
              />
              <SelectField
                label={t('orgSchoolsFilterTypeLabel')}
                value={schoolTypeFilter}
                onChange={setSchoolTypeFilter}
                options={[
                  { value: '', label: t('orgFilterAll') },
                  { value: 'Kindergarten', label: t('orgSchoolTypeKindergarten') },
                  { value: 'ElementarySchool', label: t('orgSchoolTypeElementarySchool') },
                  { value: 'SecondarySchool', label: t('orgSchoolTypeSecondarySchool') }
                ]}
              />
              <SelectField
                label={t('orgSchoolsFilterStatusLabel')}
                value={schoolStatusFilter}
                onChange={(value) => setSchoolStatusFilter(value as SchoolStatusFilter)}
                options={[
                  { value: 'all', label: t('orgFilterAll') },
                  { value: 'active', label: t('orgSchoolActive') },
                  { value: 'inactive', label: t('orgSchoolInactive') }
                ]}
              />
              <div className="flex items-end">
                <button
                  type="button"
                  className="sk-btn sk-btn-secondary w-full"
                  onClick={() => {
                    setSchoolSearch('');
                    setSchoolTypeFilter('');
                    setSchoolStatusFilter('all');
                  }}
                >
                  {t('orgSchoolsResetFilters')}
                </button>
              </div>
            </div>
          </Card>
          <Card className="sk-user-management overflow-hidden">
            <div className="sk-user-management-panel mt-0 rounded-none border-0 border-b border-slate-200 bg-slate-50 p-3">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <p className="text-sm font-semibold text-slate-900">{t('orgSchoolsList')}</p>
                  <p className="mt-1 text-xs text-slate-500">{t('orgSchoolsSummaryFiltered')}: {filteredSchools.length}</p>
                </div>
              </div>
            </div>
            {filteredSchools.length === 0 ? (
              <div className="p-4">
                <EmptyState text={t('orgNoSchools')} />
              </div>
            ) : (
              <div className="sk-table-wrap mt-0 overflow-x-auto border-0 rounded-none">
                <table className="sk-table sk-user-management-table sk-sticky">
                  <thead>
                    <tr className="border-b text-left">
                      <th>{t('orgSchoolName')}</th>
                      <th>{t('orgSchoolTypeLabel')}</th>
                      <th>{t('orgAddressCity')}</th>
                      <th>{t('orgSchoolCardOperator')}</th>
                      <th>{t('orgSchoolPlatformStatusLabel')}</th>
                      <th>{t('stateActive')}</th>
                      <th>{t('orgSchoolCardCapacity')}</th>
                      <th>{t('userManagementColActions')}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredSchools.map((school) => (
                      <SchoolTableRow
                        key={school.id}
                        school={school}
                        selected={school.id === activeSchoolId}
                        t={t}
                        actionMenuOpen={schoolActionMenuId === school.id}
                        actionMenuRef={schoolActionMenuId === school.id ? schoolActionMenuRef : undefined}
                        onSelect={() => {
                          setActiveSchoolId(school.id);
                          setSchoolDetailOpen(true);
                        }}
                        onToggleMenu={() => setSchoolActionMenuId((current) => current === school.id ? '' : school.id)}
                        onOpenDetail={() => {
                          setSchoolActionMenuId('');
                          setActiveSchoolId(school.id);
                          setSchoolDetailOpen(true);
                        }}
                        onToggleStatus={isPlatformAdmin ? () => {
                          setSchoolActionMenuId('');
                          guarded(() => api.setSchoolStatus(school.id, !school.isActive));
                        } : undefined}
                      />
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </Card>
        </div>
        {schoolDetailOpen && currentSchool ? (
          <>
            <div className="sk-drawer-overlay" onClick={() => setSchoolDetailOpen(false)} role="presentation" />
            <div className="sk-drawer" role="dialog" aria-modal="true" aria-label={t('orgSchoolDetail')}>
              <div className="sk-drawer-header">
                <div className="min-w-0">
                  <p className="truncate text-sm font-semibold text-slate-900">{currentSchool.name}</p>
                  <p className="mt-1 text-xs text-slate-500">{getSchoolTypeLabel(t, currentSchool.schoolType)}</p>
                </div>
                <button type="button" className="sk-btn sk-btn-secondary" onClick={() => setSchoolDetailOpen(false)}>
                  {t('cancel')}
                </button>
              </div>
              <OrganizationSchoolIdentityCard
                school={currentSchool}
                editable={canWriteSchoolContext}
                onSave={(schoolId, payload) => api.updateSchool(schoolId, payload).then(() => setNotice(t('orgSchoolDetailSavedSuccess'))).then(() => load())}
              />
            </div>
          </>
        ) : null}
      </section>
    );
  }

  if (activeView === 'school-years') {
    return (
      <section className="space-y-3">
        <SectionHeader title="Přehled školních roků" description="Entry point pro school years list/detail flow." />
        {contextSwitcherBlock(false)}
        <Card>
          <p className="font-semibold text-sm">Školní roky</p>
          {schoolYears.length === 0 ? <EmptyState text="No school years." /> : (
            <ul className="sk-list">{schoolYears.map((x) => <li className="sk-list-item" key={x.id}>{x.label} ({x.startDate} - {x.endDate})</li>)}</ul>
          )}
        </Card>
        {canWriteSchoolContext ? (
          <Card>
            <p className="font-semibold text-sm">Vytvořit školní rok</p>
            <div className="mt-2 grid gap-2 md:grid-cols-3">
              <input className="sk-input" placeholder="Label" value={newSchoolYear.label} onChange={(e) => setNewSchoolYear((v) => ({ ...v, label: e.target.value }))} />
              <input className="sk-input" type="date" value={newSchoolYear.startDate} onChange={(e) => setNewSchoolYear((v) => ({ ...v, startDate: e.target.value }))} />
              <input className="sk-input" type="date" value={newSchoolYear.endDate} onChange={(e) => setNewSchoolYear((v) => ({ ...v, endDate: e.target.value }))} />
            </div>
            <div className="mt-3">
              <button className="sk-btn sk-btn-primary" disabled={!activeSchoolId || !newSchoolYear.label.trim()} onClick={createSchoolYear} type="button">Create school year</button>
            </div>
          </Card>
        ) : null}
      </section>
    );
  }

  if (activeView === 'grade-levels') {
    return (
      <section className="space-y-3">
        <SectionHeader title="Ročníky" description="Organizační struktura ročníků (nikoli grading)." />
        {contextSwitcherBlock(false)}
        <Card>
          <p className="font-semibold text-sm">Ročníky</p>
          {gradeLevels.length === 0 ? <EmptyState text="No grade levels." /> : (
            <ul className="sk-list">{gradeLevels.map((x) => <li className="sk-list-item" key={x.id}>{x.level} - {x.displayName}</li>)}</ul>
          )}
        </Card>
        {canWriteSchoolContext ? (
          <Card>
            <p className="font-semibold text-sm">Vytvořit ročník</p>
            <div className="mt-2 grid gap-2 md:grid-cols-2">
              <input className="sk-input" type="number" value={newGradeLevel.level} onChange={(e) => setNewGradeLevel((v) => ({ ...v, level: Number(e.target.value) || 1 }))} />
              <input className="sk-input" placeholder="Display name" value={newGradeLevel.displayName} onChange={(e) => setNewGradeLevel((v) => ({ ...v, displayName: e.target.value }))} />
            </div>
            <div className="mt-3"><button className="sk-btn sk-btn-primary" disabled={!activeSchoolId || !newGradeLevel.displayName.trim()} onClick={createGradeLevel} type="button">Create grade level</button></div>
          </Card>
        ) : null}
      </section>
    );
  }

  if (activeView === 'class-rooms') {
    return (
      <section className="space-y-3">
        <SectionHeader title="Třídy" description="Entry point pro classes list/detail flow." />
        {contextSwitcherBlock(false)}
        <Card>
          <p className="font-semibold text-sm">Třídy</p>
          {classRooms.length === 0 ? <EmptyState text="No class rooms." /> : (
            <ul className="sk-list">{classRooms.map((x) => <li className="sk-list-item" key={x.id}>{x.code} - {x.displayName}</li>)}</ul>
          )}
        </Card>
        {canWriteSchoolContext ? (
          <Card>
            <p className="font-semibold text-sm">Vytvořit třídu</p>
            <div className="mt-2 grid gap-2 md:grid-cols-3">
              <input className="sk-input" placeholder="Grade level id" value={newClassRoom.gradeLevelId} onChange={(e) => setNewClassRoom((v) => ({ ...v, gradeLevelId: e.target.value }))} />
              <input className="sk-input" placeholder="Code" value={newClassRoom.code} onChange={(e) => setNewClassRoom((v) => ({ ...v, code: e.target.value }))} />
              <input className="sk-input" placeholder="Display name" value={newClassRoom.displayName} onChange={(e) => setNewClassRoom((v) => ({ ...v, displayName: e.target.value }))} />
            </div>
            <div className="mt-3"><button className="sk-btn sk-btn-primary" disabled={!activeSchoolId || !newClassRoom.code.trim()} onClick={createClassRoom} type="button">Create class room</button></div>
          </Card>
        ) : null}
      </section>
    );
  }

  if (activeView === 'teaching-groups') {
    return (
      <section className="space-y-3">
        <SectionHeader title="Skupiny" description="Entry point pro groups list/detail flow." />
        {contextSwitcherBlock(false)}
        <Card>
          <p className="font-semibold text-sm">Skupiny</p>
          {teachingGroups.length === 0 ? <EmptyState text="No teaching groups." /> : (
            <ul className="sk-list">{teachingGroups.map((x) => <li className="sk-list-item" key={x.id}>{x.name}</li>)}</ul>
          )}
        </Card>
        {canWriteSchoolContext ? (
          <Card>
            <p className="font-semibold text-sm">Vytvořit skupinu</p>
            <div className="mt-2 grid gap-2 md:grid-cols-2">
              <input className="sk-input" placeholder="Class room id (optional)" value={newGroup.classRoomId} onChange={(e) => setNewGroup((v) => ({ ...v, classRoomId: e.target.value }))} />
              <input className="sk-input" placeholder="Name" value={newGroup.name} onChange={(e) => setNewGroup((v) => ({ ...v, name: e.target.value }))} />
            </div>
            <label className="mt-2 inline-flex items-center gap-2 text-sm"><input type="checkbox" checked={newGroup.isDailyOperationsGroup} onChange={(e) => setNewGroup((v) => ({ ...v, isDailyOperationsGroup: e.target.checked }))} />Daily operations group</label>
            <div className="mt-3"><button className="sk-btn sk-btn-primary" disabled={!activeSchoolId || !newGroup.name.trim()} onClick={createGroup} type="button">Create group</button></div>
          </Card>
        ) : null}
      </section>
    );
  }

  if (activeView === 'subjects') {
    return (
      <section className="space-y-3">
        <SectionHeader title="Předměty" description="Entry point pro subjects list/detail flow." />
        {contextSwitcherBlock(false)}
        <Card>
          <p className="font-semibold text-sm">Předměty</p>
          {subjects.length === 0 ? <EmptyState text="No subjects." /> : (
            <ul className="sk-list">{subjects.map((x) => <li className="sk-list-item" key={x.id}>{x.code} - {x.name}</li>)}</ul>
          )}
        </Card>
        {canWriteSchoolContext ? (
          <Card>
            <p className="font-semibold text-sm">Vytvořit předmět</p>
            <div className="mt-2 grid gap-2 md:grid-cols-2">
              <input className="sk-input" placeholder="Code" value={newSubject.code} onChange={(e) => setNewSubject((v) => ({ ...v, code: e.target.value }))} />
              <input className="sk-input" placeholder="Name" value={newSubject.name} onChange={(e) => setNewSubject((v) => ({ ...v, name: e.target.value }))} />
            </div>
            <div className="mt-3"><button className="sk-btn sk-btn-primary" disabled={!activeSchoolId || !newSubject.code.trim()} onClick={createSubject} type="button">Create subject</button></div>
          </Card>
        ) : null}
      </section>
    );
  }

  return (
    <section className="space-y-3">
      <SectionHeader title="Teacher Assignments" description="Entry point pro teacher assignments list/detail flow." />
      {contextSwitcherBlock(false)}
      <Card>
        <p className="font-semibold text-sm">Teacher assignments</p>
        {teacherAssignments.length === 0 ? <EmptyState text="No teacher assignments." /> : (
          <ul className="sk-list">{teacherAssignments.map((x) => <li className="sk-list-item" key={x.id}>{x.teacherUserId} | {x.scope}</li>)}</ul>
        )}
      </Card>
      {canWriteSchoolContext ? (
        <Card>
          <p className="font-semibold text-sm">Vytvořit teacher assignment</p>
          <div className="mt-2 grid gap-2 md:grid-cols-2">
            <input className="sk-input" placeholder="Teacher user id" value={newAssignment.teacherUserId} onChange={(e) => setNewAssignment((v) => ({ ...v, teacherUserId: e.target.value }))} />
            <input className="sk-input" placeholder="Scope" value={newAssignment.scope} onChange={(e) => setNewAssignment((v) => ({ ...v, scope: e.target.value }))} />
            <input className="sk-input" placeholder="Class room id" value={newAssignment.classRoomId} onChange={(e) => setNewAssignment((v) => ({ ...v, classRoomId: e.target.value }))} />
            <input className="sk-input" placeholder="Teaching group id" value={newAssignment.teachingGroupId} onChange={(e) => setNewAssignment((v) => ({ ...v, teachingGroupId: e.target.value }))} />
            <input className="sk-input" placeholder="Subject id" value={newAssignment.subjectId} onChange={(e) => setNewAssignment((v) => ({ ...v, subjectId: e.target.value }))} />
          </div>
          <div className="mt-3"><button className="sk-btn sk-btn-primary" disabled={!activeSchoolId || !newAssignment.teacherUserId.trim()} onClick={createAssignment} type="button">Create assignment</button></div>
        </Card>
      ) : null}
    </section>
  );
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-sm">
      <p className="text-xs uppercase tracking-wide text-slate-500">{label}</p>
      <p className="mt-1 text-2xl font-semibold text-slate-900">{value}</p>
    </div>
  );
}

function SchoolTableRow({
  school,
  selected,
  t,
  actionMenuOpen,
  actionMenuRef,
  onSelect,
  onToggleMenu,
  onOpenDetail,
  onToggleStatus
}: {
  school: School;
  selected: boolean;
  t: (key: string, params?: Record<string, string>) => string;
  actionMenuOpen: boolean;
  actionMenuRef?: React.RefObject<HTMLDivElement | null>;
  onSelect: () => void;
  onToggleMenu: () => void;
  onOpenDetail: () => void;
  onToggleStatus?: () => void;
}) {
  return (
    <tr
      className={`sk-user-management-row sk-user-management-row-clickable border-b ${selected ? 'bg-sky-50/80' : ''}`}
      onClick={(e) => {
        const target = e.target as HTMLElement;
        if (target.closest('button') || target.closest('.sk-action-menu')) return;
        onSelect();
      }}
    >
      <td>
        <div className="min-w-0">
          <div className="font-medium text-slate-900">{school.name}</div>
          <div className="mt-1 text-xs text-slate-500">{school.schoolIzo || '-'}</div>
        </div>
      </td>
      <td>{getSchoolTypeLabel(t, school.schoolType)}</td>
      <td className="text-xs text-slate-600">{school.mainAddress.city || '-'}</td>
      <td className="text-xs text-slate-600">{school.schoolOperator?.legalEntityName || '-'}</td>
      <td className="text-xs text-slate-600">{getPlatformStatusLabel(t, school.platformStatus)}</td>
      <td>
        <span className="inline-flex items-center text-xs">
          <span className={`sk-status-dot ${school.isActive ? 'sk-status-dot-active' : 'sk-status-dot-deactivated'}`} />
          {school.isActive ? t('orgSchoolActive') : t('orgSchoolInactive')}
        </span>
      </td>
      <td className="text-xs text-slate-600">{school.maxStudentCapacity?.toString() || '-'}</td>
      <td onClick={(e) => e.stopPropagation()}>
        <div className="sk-action-menu" ref={actionMenuOpen ? actionMenuRef : undefined}>
          <button
            type="button"
            className="sk-action-menu-trigger"
            onClick={onToggleMenu}
            aria-label={t('userManagementColActions')}
          >
            <ThreeDotsIcon className="h-4 w-4" />
          </button>
          {actionMenuOpen ? (
            <div className="sk-action-menu-dropdown">
              <button type="button" className="sk-action-menu-item" onClick={onOpenDetail}>
                <EditPencilIcon className="h-3.5 w-3.5" />{t('orgSchoolsSelect')}
              </button>
              {onToggleStatus ? (
                <button type="button" className="sk-action-menu-item danger" onClick={onToggleStatus}>
                  <LifecycleDeactivateIcon className="h-3.5 w-3.5" />{school.isActive ? t('deactivate') : t('activate')}
                </button>
              ) : null}
            </div>
          ) : null}
        </div>
      </td>
    </tr>
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

function EditPencilIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="m6 18 1-4 8-8 3 3-8 8-4 1Z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
      <path d="m13.5 7.5 3 3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function LifecycleDeactivateIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="1.8" />
      <path d="M8 12h8" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function CompactInfo({ label, value }: { label: string; value?: string | null }) {
  return (
    <div className="flex items-start justify-between gap-3">
      <span className="text-slate-500">{label}</span>
      <span className="text-right font-medium text-slate-900">{value || '-'}</span>
    </div>
  );
}

function SchoolCreateWizard({
  t,
  wizardStep,
  setWizardStep,
  form,
  setForm,
  onCancel,
  onSubmit,
  stepValid
}: {
  t: (key: string, params?: Record<string, string>) => string;
  wizardStep: number;
  setWizardStep: React.Dispatch<React.SetStateAction<number>>;
  form: SchoolMutation;
  setForm: React.Dispatch<React.SetStateAction<SchoolMutation>>;
  onCancel: () => void;
  onSubmit: () => void;
  stepValid: (step: number) => boolean;
}) {
  return (
    <Card className="border-sky-200 bg-sky-50/40">
      <div className="space-y-4">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <p className="text-sm font-semibold text-slate-900">{t('orgCreateSchool')}</p>
            <p className="mt-1 text-sm text-slate-600">{t('orgCreateSchoolWizardDescription')}</p>
          </div>
          <button type="button" className="sk-btn sk-btn-secondary" onClick={onCancel}>
            {t('cancel')}
          </button>
        </div>

        <div className="flex flex-wrap gap-2">
          {[
            t('orgCreateSchoolWizardStep1'),
            t('orgCreateSchoolWizardStep2'),
            t('orgCreateSchoolWizardStep3'),
            t('orgCreateSchoolWizardStep4')
          ].map((label, index) => (
            <button
              key={label}
              type="button"
              className={`rounded-full px-3 py-1.5 text-xs font-medium ${wizardStep === index + 1 ? 'bg-sky-600 text-white' : 'border border-slate-200 bg-white text-slate-600'}`}
              onClick={() => {
                if (index + 1 <= wizardStep || stepValid(wizardStep)) {
                  setWizardStep(index + 1);
                }
              }}
            >
              {index + 1}. {label}
            </button>
          ))}
        </div>

        {wizardStep === 1 ? (
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            <InputField label={t('orgSchoolName')} value={form.name} onChange={(value) => setForm((v) => ({ ...v, name: value }))} />
            <SelectField label={t('orgSchoolTypeLabel')} value={form.schoolType} onChange={(value) => setForm((v) => ({ ...v, schoolType: value }))} options={[
              { value: 'Kindergarten', label: t('orgSchoolTypeKindergarten') },
              { value: 'ElementarySchool', label: t('orgSchoolTypeElementarySchool') },
              { value: 'SecondarySchool', label: t('orgSchoolTypeSecondarySchool') }
            ]} />
            <SelectField label={t('orgSchoolKindLabel')} value={form.schoolKind} onChange={(value) => setForm((v) => ({ ...v, schoolKind: value }))} options={[
              { value: 'General', label: t('orgSchoolKindGeneral') },
              { value: 'Specialized', label: t('orgSchoolKindSpecialized') }
            ]} />
            <InputField label={t('orgSchoolIzo')} value={form.schoolIzo ?? ''} onChange={(value) => setForm((v) => ({ ...v, schoolIzo: value }))} />
            <InputField label={t('orgSchoolEmail')} value={form.schoolEmail ?? ''} type="email" onChange={(value) => setForm((v) => ({ ...v, schoolEmail: value }))} />
            <InputField label={t('orgSchoolPhone')} value={form.schoolPhone ?? ''} onChange={(value) => setForm((v) => ({ ...v, schoolPhone: value }))} />
            <InputField label={t('orgSchoolWebsite')} value={form.schoolWebsite ?? ''} onChange={(value) => setForm((v) => ({ ...v, schoolWebsite: value }))} />
            <InputField label={t('orgAddressStreet')} value={form.mainAddress.street} onChange={(value) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, street: value } }))} />
            <InputField label={t('orgAddressCity')} value={form.mainAddress.city} onChange={(value) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, city: value } }))} />
            <InputField label={t('orgAddressPostalCode')} value={form.mainAddress.postalCode} onChange={(value) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, postalCode: value } }))} />
            <InputField label={t('orgAddressCountry')} value={form.mainAddress.country} onChange={(value) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, country: value } }))} />
            <InputField label={t('orgTeachingLanguage')} value={form.teachingLanguage ?? ''} onChange={(value) => setForm((v) => ({ ...v, teachingLanguage: value }))} />
            <InputField label={t('orgRegistryEntryDate')} value={form.registryEntryDate ?? ''} type="date" onChange={(value) => setForm((v) => ({ ...v, registryEntryDate: value || undefined }))} />
            <InputField label={t('orgEducationStartDate')} value={form.educationStartDate ?? ''} type="date" onChange={(value) => setForm((v) => ({ ...v, educationStartDate: value || undefined }))} />
            <InputField label={t('orgSchoolMaxStudentCapacity')} value={form.maxStudentCapacity?.toString() ?? ''} type="number" onChange={(value) => setForm((v) => ({ ...v, maxStudentCapacity: value ? Number(value) : undefined }))} />
            <SelectField label={t('orgSchoolPlatformStatusLabel')} value={form.platformStatus} onChange={(value) => setForm((v) => ({ ...v, platformStatus: value }))} options={PLATFORM_STATUS_OPTIONS(t)} />
            <div className="md:col-span-2 xl:col-span-3">
              <TextAreaField label={t('orgSchoolEducationLocationsSummary')} value={form.educationLocationsSummary ?? ''} onChange={(value) => setForm((v) => ({ ...v, educationLocationsSummary: value }))} rows={3} />
            </div>
          </div>
        ) : null}

        {wizardStep === 2 ? (
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            <InputField label={t('orgLegalEntityName')} value={form.schoolOperator.legalEntityName} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, legalEntityName: value } }))} />
            <SelectField label={t('orgLegalForm')} value={form.schoolOperator.legalForm} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, legalForm: value } }))} options={LEGAL_FORM_OPTIONS(t)} />
            <InputField label={t('orgCompanyNumberIco')} value={form.schoolOperator.companyNumberIco ?? ''} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, companyNumberIco: value } }))} />
            <InputField label={t('orgSchoolOperatorRedIzo')} value={form.schoolOperator.redIzo ?? ''} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, redIzo: value } }))} />
            <InputField label={t('orgSchoolOperatorEmail')} value={form.schoolOperator.operatorEmail ?? ''} type="email" onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, operatorEmail: value } }))} />
            <InputField label={t('orgSchoolOperatorDataBox')} value={form.schoolOperator.dataBox ?? ''} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, dataBox: value } }))} />
            <InputField label={t('orgSchoolOperatorResortIdentifier')} value={form.schoolOperator.resortIdentifier ?? ''} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, resortIdentifier: value } }))} />
            <InputField label={t('orgAddressStreet')} value={form.schoolOperator.registeredOfficeAddress.street} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, street: value } } }))} />
            <InputField label={t('orgAddressCity')} value={form.schoolOperator.registeredOfficeAddress.city} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, city: value } } }))} />
            <InputField label={t('orgAddressPostalCode')} value={form.schoolOperator.registeredOfficeAddress.postalCode} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, postalCode: value } } }))} />
            <InputField label={t('orgAddressCountry')} value={form.schoolOperator.registeredOfficeAddress.country} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, country: value } } }))} />
            <div className="md:col-span-2 xl:col-span-3 grid gap-3 xl:grid-cols-2">
              <TextAreaField label={t('orgSchoolDirectorSummary')} value={form.schoolOperator.directorSummary ?? ''} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, directorSummary: value } }))} rows={4} />
              <TextAreaField label={t('orgSchoolStatutoryBodySummary')} value={form.schoolOperator.statutoryBodySummary ?? ''} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, statutoryBodySummary: value } }))} rows={4} />
            </div>
          </div>
        ) : null}

        {wizardStep === 3 ? (
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            <SelectField label={t('orgFounderTypeLabel')} value={form.founder.founderType} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderType: value } }))} options={FOUNDER_TYPE_OPTIONS(t)} />
            <SelectField label={t('orgFounderCategoryLabel')} value={form.founder.founderCategory} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderCategory: value } }))} options={FOUNDER_CATEGORY_OPTIONS(t)} />
            <InputField label={t('orgFounderName')} value={form.founder.founderName} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderName: value } }))} />
            <SelectField label={t('orgFounderLegalFormLabel')} value={form.founder.founderLegalForm} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderLegalForm: value } }))} options={LEGAL_FORM_OPTIONS(t)} />
            <InputField label={t('orgFounderIco')} value={form.founder.founderIco ?? ''} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderIco: value } }))} />
            <InputField label={t('orgFounderEmail')} value={form.founder.founderEmail ?? ''} type="email" onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderEmail: value } }))} />
            <InputField label={t('orgFounderDataBox')} value={form.founder.founderDataBox ?? ''} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderDataBox: value } }))} />
            <InputField label={t('orgAddressStreet')} value={form.founder.founderAddress.street} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, street: value } } }))} />
            <InputField label={t('orgAddressCity')} value={form.founder.founderAddress.city} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, city: value } } }))} />
            <InputField label={t('orgAddressPostalCode')} value={form.founder.founderAddress.postalCode} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, postalCode: value } } }))} />
            <InputField label={t('orgAddressCountry')} value={form.founder.founderAddress.country} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, country: value } } }))} />
          </div>
        ) : null}

        {wizardStep === 4 ? (
          <div className="grid gap-4 xl:grid-cols-2">
            <Card>
              <p className="font-semibold text-sm">{t('orgCreateSchoolWizardReviewTitle')}</p>
              <p className="mt-1 text-sm text-slate-600">{t('orgCreateSchoolWizardReviewDescription')}</p>
              <div className="mt-4 space-y-2 text-sm text-slate-700">
                <CompactInfo label={t('orgSchoolName')} value={form.name} />
                <CompactInfo label={t('orgSchoolTypeLabel')} value={getSchoolTypeLabel(t, form.schoolType)} />
                <CompactInfo label={t('orgSchoolKindLabel')} value={getSchoolKindLabel(t, form.schoolKind)} />
                <CompactInfo label={t('orgAddressCity')} value={form.mainAddress.city} />
                <CompactInfo label={t('orgSchoolOperatorTitle')} value={form.schoolOperator.legalEntityName} />
                <CompactInfo label={t('orgFounderTitle')} value={form.founder.founderName} />
              </div>
            </Card>
            <Card>
              <p className="font-semibold text-sm">{t('orgCreateSchoolWizardValidationTitle')}</p>
              <ul className="mt-3 space-y-2 text-sm text-slate-700">
                <WizardCheck label={t('orgCreateSchoolWizardValidationSchool')} valid={Boolean(form.name.trim() && form.mainAddress.street.trim() && form.mainAddress.city.trim())} />
                <WizardCheck label={t('orgCreateSchoolWizardValidationOperator')} valid={Boolean(form.schoolOperator.legalEntityName.trim())} />
                <WizardCheck label={t('orgCreateSchoolWizardValidationFounder')} valid={Boolean(form.founder.founderName.trim())} />
              </ul>
            </Card>
          </div>
        ) : null}

        <div className="flex flex-wrap items-center justify-between gap-3">
          <button type="button" className="sk-btn sk-btn-secondary" onClick={onCancel}>
            {t('orgCreateSchoolWizardReset')}
          </button>
          <div className="flex flex-wrap gap-2">
            <button type="button" className="sk-btn sk-btn-secondary" disabled={wizardStep === 1} onClick={() => setWizardStep((step) => Math.max(1, step - 1))}>
              {t('orgCreateSchoolWizardBack')}
            </button>
            {wizardStep < 4 ? (
              <button type="button" className="sk-btn sk-btn-primary" disabled={!stepValid(wizardStep)} onClick={() => setWizardStep((step) => Math.min(4, step + 1))}>
                {t('orgCreateSchoolWizardNext')}
              </button>
            ) : (
              <button type="button" className="sk-btn sk-btn-primary" disabled={!stepValid(1) || !stepValid(2) || !stepValid(3)} onClick={onSubmit}>
                {t('orgCreateSchoolWizardCreate')}
              </button>
            )}
          </div>
        </div>
      </div>
    </Card>
  );
}

function InputField({
  label,
  value,
  onChange,
  placeholder,
  type = 'text'
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  type?: string;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label">{label}</label>
      <input className="sk-input" value={value} type={type} placeholder={placeholder} onChange={(e) => onChange(e.target.value)} />
    </div>
  );
}

function SelectField({
  label,
  value,
  onChange,
  options
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label">{label}</label>
      <select className="sk-input" value={value} onChange={(e) => onChange(e.target.value)}>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </div>
  );
}

function TextAreaField({
  label,
  value,
  onChange,
  rows = 4
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  rows?: number;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label">{label}</label>
      <textarea className="sk-input min-h-24" rows={rows} value={value} onChange={(e) => onChange(e.target.value)} />
    </div>
  );
}

function WizardCheck({ label, valid }: { label: string; valid: boolean }) {
  return (
    <li className="flex items-center justify-between gap-3 rounded-lg border border-slate-200 bg-white px-3 py-2">
      <span>{label}</span>
      <StatusBadge label={valid ? 'OK' : '...'} tone={valid ? 'good' : 'warn'} />
    </li>
  );
}

function LEGAL_FORM_OPTIONS(t: (key: string, params?: Record<string, string>) => string) {
  return [
    { value: 'PublicInstitution', label: t('orgLegalFormPublicInstitution') },
    { value: 'Municipality', label: t('orgLegalFormMunicipality') },
    { value: 'Region', label: t('orgLegalFormRegion') },
    { value: 'Association', label: t('orgLegalFormAssociation') },
    { value: 'ChurchLegalEntity', label: t('orgLegalFormChurchLegalEntity') },
    { value: 'LimitedLiabilityCompany', label: t('orgLegalFormLimitedLiabilityCompany') },
    { value: 'JointStockCompany', label: t('orgLegalFormJointStockCompany') },
    { value: 'NonProfitOrganization', label: t('orgLegalFormNonProfitOrganization') },
    { value: 'NaturalPersonEntrepreneur', label: t('orgLegalFormNaturalPersonEntrepreneur') }
  ];
}

function PLATFORM_STATUS_OPTIONS(t: (key: string, params?: Record<string, string>) => string) {
  return [
    { value: 'Draft', label: t('orgPlatformStatusDraft') },
    { value: 'Active', label: t('orgPlatformStatusActive') },
    { value: 'Suspended', label: t('orgPlatformStatusSuspended') },
    { value: 'Archived', label: t('orgPlatformStatusArchived') }
  ];
}

function FOUNDER_TYPE_OPTIONS(t: (key: string, params?: Record<string, string>) => string) {
  return [
    { value: 'State', label: t('orgFounderTypeState') },
    { value: 'Region', label: t('orgFounderTypeRegion') },
    { value: 'Municipality', label: t('orgFounderTypeMunicipality') },
    { value: 'AssociationOfMunicipalities', label: t('orgFounderTypeAssociationOfMunicipalities') },
    { value: 'Church', label: t('orgFounderTypeChurch') },
    { value: 'PrivateLegalEntity', label: t('orgFounderTypePrivateLegalEntity') },
    { value: 'NaturalPerson', label: t('orgFounderTypeNaturalPerson') }
  ];
}

function FOUNDER_CATEGORY_OPTIONS(t: (key: string, params?: Record<string, string>) => string) {
  return [
    { value: 'Public', label: t('orgFounderCategoryPublic') },
    { value: 'Church', label: t('orgFounderCategoryChurch') },
    { value: 'Private', label: t('orgFounderCategoryPrivate') }
  ];
}









