import React, { useEffect, useMemo, useState } from 'react';
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
import { getPlatformStatusLabel, getSchoolTypeLabel } from './schoolLabels';
import { SkolioHttpError } from '../shared/http/httpClient';
import { Card, SectionHeader, StatusBadge } from '../shared/ui/primitives';
import { ErrorState, LoadingState } from '../shared/ui/states';
import { useClientGrid } from './hooks/useClientGrid';
import { OrganizationGridSection, type OrganizationGridColumn } from './components/OrganizationGridSection';
import { OrganizationEntityModal } from './components/OrganizationEntityModal';

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
type SchoolYearWindowFilter = 'all' | 'current' | 'upcoming' | 'past';
type TeachingGroupFilter = 'all' | 'daily' | 'custom';
type EntityModalState =
  | { kind: 'school-year'; mode: 'create' | 'edit'; item?: SchoolYear }
  | { kind: 'grade-level'; mode: 'create' | 'edit'; item?: GradeLevel }
  | { kind: 'class-room'; mode: 'create' | 'edit'; item?: ClassRoom }
  | { kind: 'teaching-group'; mode: 'create' | 'edit'; item?: TeachingGroup }
  | { kind: 'subject'; mode: 'create' | 'edit'; item?: Subject }
  | { kind: 'teacher-assignment'; mode: 'create' | 'edit'; item?: TeacherAssignment }
  | null;
type SchoolYearDraft = { label: string; startDate: string; endDate: string };
type GradeLevelDraft = { level: number; displayName: string };
type ClassRoomCreateDraft = { gradeLevelId: string; code: string; displayName: string };
type ClassRoomEditDraft = { code: string; displayName: string; overrideReason: string };
type TeachingGroupCreateDraft = { classRoomId: string; name: string; isDailyOperationsGroup: boolean };
type TeachingGroupEditDraft = { classRoomId: string; name: string; isDailyOperationsGroup: boolean; overrideReason: string };
type SubjectCreateDraft = { code: string; name: string };
type SubjectEditDraft = { code: string; name: string; overrideReason: string };
type TeacherAssignmentCreateDraft = { teacherUserId: string; scope: string; classRoomId: string; teachingGroupId: string; subjectId: string };
type TeacherAssignmentEditDraft = { teacherUserId: string; scope: string; classRoomId: string; teachingGroupId: string; subjectId: string; overrideReason: string };

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
const EMPTY_SCHOOL_YEAR: SchoolYearDraft = { label: '', startDate: '', endDate: '' };
const EMPTY_GRADE_LEVEL: GradeLevelDraft = { level: 1, displayName: '' };
const EMPTY_CLASS_ROOM_CREATE: ClassRoomCreateDraft = { gradeLevelId: '', code: '', displayName: '' };
const EMPTY_CLASS_ROOM_EDIT: ClassRoomEditDraft = { code: '', displayName: '', overrideReason: '' };
const EMPTY_TEACHING_GROUP_CREATE: TeachingGroupCreateDraft = { classRoomId: '', name: '', isDailyOperationsGroup: true };
const EMPTY_TEACHING_GROUP_EDIT: TeachingGroupEditDraft = { classRoomId: '', name: '', isDailyOperationsGroup: true, overrideReason: '' };
const EMPTY_SUBJECT_CREATE: SubjectCreateDraft = { code: '', name: '' };
const EMPTY_SUBJECT_EDIT: SubjectEditDraft = { code: '', name: '', overrideReason: '' };
const EMPTY_ASSIGNMENT_CREATE: TeacherAssignmentCreateDraft = { teacherUserId: '', scope: 'SubjectTeacher', classRoomId: '', teachingGroupId: '', subjectId: '' };
const EMPTY_ASSIGNMENT_EDIT: TeacherAssignmentEditDraft = { teacherUserId: '', scope: 'SubjectTeacher', classRoomId: '', teachingGroupId: '', subjectId: '', overrideReason: '' };

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
  const [saving, setSaving] = useState(false);

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
  const [schoolTypeFilter, setSchoolTypeFilter] = useState('');
  const [schoolStatusFilter, setSchoolStatusFilter] = useState<SchoolStatusFilter>('all');
  const [schoolWizardOpen, setSchoolWizardOpen] = useState(false);
  const [schoolWizardStep, setSchoolWizardStep] = useState(1);
  const [schoolDetailOpen, setSchoolDetailOpen] = useState(false);
  const [modalState, setModalState] = useState<EntityModalState>(null);
  const [schoolYearDraft, setSchoolYearDraft] = useState<SchoolYearDraft>(EMPTY_SCHOOL_YEAR);
  const [gradeLevelDraft, setGradeLevelDraft] = useState<GradeLevelDraft>(EMPTY_GRADE_LEVEL);
  const [classRoomCreateDraft, setClassRoomCreateDraft] = useState<ClassRoomCreateDraft>(EMPTY_CLASS_ROOM_CREATE);
  const [classRoomEditDraft, setClassRoomEditDraft] = useState<ClassRoomEditDraft>(EMPTY_CLASS_ROOM_EDIT);
  const [teachingGroupCreateDraft, setTeachingGroupCreateDraft] = useState<TeachingGroupCreateDraft>(EMPTY_TEACHING_GROUP_CREATE);
  const [teachingGroupEditDraft, setTeachingGroupEditDraft] = useState<TeachingGroupEditDraft>(EMPTY_TEACHING_GROUP_EDIT);
  const [subjectCreateDraft, setSubjectCreateDraft] = useState<SubjectCreateDraft>(EMPTY_SUBJECT_CREATE);
  const [subjectEditDraft, setSubjectEditDraft] = useState<SubjectEditDraft>(EMPTY_SUBJECT_EDIT);
  const [teacherAssignmentCreateDraft, setTeacherAssignmentCreateDraft] = useState<TeacherAssignmentCreateDraft>(EMPTY_ASSIGNMENT_CREATE);
  const [teacherAssignmentEditDraft, setTeacherAssignmentEditDraft] = useState<TeacherAssignmentEditDraft>(EMPTY_ASSIGNMENT_EDIT);
  const [teacherAssignmentWizardStep, setTeacherAssignmentWizardStep] = useState(1);

  const [schoolYearWindowFilter, setSchoolYearWindowFilter] = useState<SchoolYearWindowFilter>('all');
  const [classRoomGradeFilter, setClassRoomGradeFilter] = useState('');
  const [teachingGroupClassRoomFilter, setTeachingGroupClassRoomFilter] = useState('');
  const [teachingGroupTypeFilter, setTeachingGroupTypeFilter] = useState<TeachingGroupFilter>('all');
  const [teacherAssignmentScopeFilter, setTeacherAssignmentScopeFilter] = useState('');

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

    const allowForbiddenEmpty = async <T,>(request: Promise<T>, fallback: T): Promise<T> => {
      try {
        return await request;
      } catch (nextError) {
        if (nextError instanceof SkolioHttpError && nextError.status === 403) {
          return fallback;
        }

        throw nextError;
      }
    };

    const teacherAssignmentsPromise = canManageOrganization
      ? allowForbiddenEmpty(api.teacherAssignments(schoolId), [] as TeacherAssignment[])
      : canTeacherScopedOrganization
        ? allowForbiddenEmpty(api.myTeacherAssignments(schoolId), [] as TeacherAssignment[])
        : Promise.resolve([] as TeacherAssignment[]);

    const gradeLevelsPromise = canManageOrganization
      ? allowForbiddenEmpty(api.gradeLevels(schoolId), [] as GradeLevel[])
      : Promise.resolve([] as GradeLevel[]);

    const [nextSchoolYears, nextGradeLevels, nextClassRooms, nextTeachingGroups, nextSubjects, nextAssignments] = await Promise.all([
      allowForbiddenEmpty(api.schoolYears(schoolId), [] as SchoolYear[]),
      gradeLevelsPromise,
      allowForbiddenEmpty(api.classRooms(schoolId), [] as ClassRoom[]),
      allowForbiddenEmpty(api.teachingGroups(schoolId), [] as TeachingGroup[]),
      allowForbiddenEmpty(api.subjects(schoolId), [] as Subject[]),
      teacherAssignmentsPromise
    ]);

    setSchoolYears(nextSchoolYears);
    setGradeLevels(nextGradeLevels);
    setClassRooms(nextClassRooms);
    setTeachingGroups(nextTeachingGroups);
    setSubjects(nextSubjects);
    setTeacherAssignments(nextAssignments);
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
        .catch((nextError: Error) => setError(nextError.message))
        .finally(() => setLoading(false));
      return;
    }

    void api.schools()
      .then((result) => {
        setSchools(result);
        const scopedSchoolId = session.schoolIds[0] ?? result[0]?.id ?? '';
        const nextSchoolId = activeSchoolId && result.some((school) => school.id === activeSchoolId)
          ? activeSchoolId
          : scopedSchoolId;
        setActiveSchoolId(nextSchoolId);
      })
      .catch((nextError: Error) => setError(nextError.message))
      .finally(() => setLoading(false));
  };

  useEffect(load, [api, session.accessToken]);

  useEffect(() => {
    setActiveView(initialView === 'secondary-fields' ? 'grade-levels' : initialView);
  }, [initialView]);

  useEffect(() => {
    if (isStudent || !activeSchoolId || schools.length === 0 || !schools.some((school) => school.id === activeSchoolId)) {
      return;
    }

    void loadSchoolBoundaries(activeSchoolId).catch((nextError: Error) => setError(nextError.message));
  }, [activeSchoolId, canManageOrganization, canTeacherScopedOrganization, isStudent, schools]);

  const currentSchool = schools.find((school) => school.id === activeSchoolId) ?? schools[0] ?? null;
  const gradeLevelNameById = useMemo(
    () => Object.fromEntries(gradeLevels.map((gradeLevel) => [gradeLevel.id, `${gradeLevel.level} - ${gradeLevel.displayName}`])),
    [gradeLevels]
  );
  const classRoomNameById = useMemo(
    () => Object.fromEntries(classRooms.map((classRoom) => [classRoom.id, `${classRoom.code} - ${classRoom.displayName}`])),
    [classRooms]
  );
  const subjectNameById = useMemo(
    () => Object.fromEntries(subjects.map((subject) => [subject.id, `${subject.code} - ${subject.name}`])),
    [subjects]
  );

  const guardedRefresh = async (action: () => Promise<unknown>, successMessage: string) => {
    setSaving(true);
    setError('');
    setNotice('');

    try {
      await action();
      await loadSchoolBoundaries(activeSchoolId);
      setNotice(successMessage);
      return true;
    } catch (nextError) {
      setError(nextError instanceof Error ? nextError.message : String(nextError));
      return false;
    } finally {
      setSaving(false);
    }
  };

  const goTo = (route: string) => {
    window.history.pushState({}, '', route);
    window.dispatchEvent(new PopStateEvent('popstate'));
  };

  const createSchool = () => {
    setSaving(true);
    setError('');
    setNotice('');
    void api.createSchool(newSchool)
      .then(() => {
        resetSchoolWizard();
        setNotice(t('orgCreateSchoolSuccess'));
        load();
      })
      .catch((nextError: Error) => setError(nextError.message))
      .finally(() => setSaving(false));
  };

  const overviewEntryPoints = useMemo(() => {
    if (isParent || isStudent) {
      return [] as { label: string; description: string; count: number; route: string; emphasize?: boolean }[];
    }

    return [
      { label: t('orgSchoolsTitle'), description: t('orgSchoolsHeroDescription'), count: schools.length, route: '/organization/schools' },
      { label: t('orgSchoolYearsTitle'), description: t('orgSchoolYearsDescription'), count: schoolYears.length, route: '/organization/school-years' },
      { label: t('orgGradeLevelsTitle'), description: t('orgGradeLevelsDescription'), count: gradeLevels.length, route: '/organization/grade-levels' },
      { label: t('orgClassRoomsTitle'), description: t('orgClassRoomsDescription'), count: classRooms.length, route: '/organization/class-rooms' },
      { label: t('orgTeachingGroupsTitle'), description: t('orgTeachingGroupsDescription'), count: teachingGroups.length, route: '/organization/teaching-groups' },
      { label: t('orgSubjectsTitle'), description: t('orgSubjectsDescription'), count: subjects.length, route: '/organization/subjects' },
      { label: t('orgTeacherAssignmentsTitle'), description: t('orgTeacherAssignmentsDescription'), count: teacherAssignments.length, route: '/organization/teacher-assignments', emphasize: true }
    ];
  }, [classRooms.length, gradeLevels.length, isParent, isStudent, schoolYears.length, schools.length, subjects.length, t, teacherAssignments.length, teachingGroups.length]);

  const filteredSchools = useMemo(() => {
    return schools.filter((school) => {
      const matchesType = !schoolTypeFilter || school.schoolType === schoolTypeFilter;
      const matchesStatus = schoolStatusFilter === 'all'
        || (schoolStatusFilter === 'active' && school.isActive)
        || (schoolStatusFilter === 'inactive' && !school.isActive);

      return matchesType && matchesStatus;
    });
  }, [schoolStatusFilter, schoolTypeFilter, schools]);

  const schoolSummary = useMemo(() => ({
    filtered: filteredSchools.length,
    active: schools.filter((school) => school.isActive).length,
    kindergarten: schools.filter((school) => school.schoolType === 'Kindergarten').length,
    elementaryAndSecondary: schools.filter((school) => school.schoolType !== 'Kindergarten').length
  }), [filteredSchools.length, schools]);

  const filteredSchoolYears = useMemo(
    () => schoolYears.filter((schoolYear) => matchesSchoolYearWindowFilter(schoolYear, schoolYearWindowFilter)),
    [schoolYearWindowFilter, schoolYears]
  );
  const filteredClassRooms = useMemo(
    () => classRooms.filter((classRoom) => !classRoomGradeFilter || classRoom.gradeLevelId === classRoomGradeFilter),
    [classRoomGradeFilter, classRooms]
  );
  const filteredTeachingGroups = useMemo(
    () => teachingGroups.filter((teachingGroup) => {
      const matchesClassRoom = !teachingGroupClassRoomFilter || teachingGroup.classRoomId === teachingGroupClassRoomFilter;
      const matchesType = teachingGroupTypeFilter === 'all'
        || (teachingGroupTypeFilter === 'daily' && teachingGroup.isDailyOperationsGroup)
        || (teachingGroupTypeFilter === 'custom' && !teachingGroup.isDailyOperationsGroup);
      return matchesClassRoom && matchesType;
    }),
    [teachingGroupClassRoomFilter, teachingGroupTypeFilter, teachingGroups]
  );
  const filteredTeacherAssignments = useMemo(
    () => teacherAssignments.filter((assignment) => !teacherAssignmentScopeFilter || assignment.scope === teacherAssignmentScopeFilter),
    [teacherAssignmentScopeFilter, teacherAssignments]
  );

  const schoolYearGrid = useClientGrid({
    items: filteredSchoolYears,
    getSearchText: (item) => `${item.label} ${item.startDate} ${item.endDate}`,
    sorters: {
      label: (left, right) => left.label.localeCompare(right.label),
      startDate: (left, right) => left.startDate.localeCompare(right.startDate),
      endDate: (left, right) => left.endDate.localeCompare(right.endDate)
    },
    initialSortKey: 'startDate',
    resetKeys: [activeSchoolId, schoolYearWindowFilter]
  });
  const gradeLevelGrid = useClientGrid({
    items: gradeLevels,
    getSearchText: (item) => `${item.level} ${item.displayName}`,
    sorters: {
      level: (left, right) => left.level - right.level,
      displayName: (left, right) => left.displayName.localeCompare(right.displayName)
    },
    initialSortKey: 'level',
    resetKeys: [activeSchoolId]
  });
  const classRoomGrid = useClientGrid({
    items: filteredClassRooms,
    getSearchText: (item) => `${item.code} ${item.displayName} ${gradeLevelNameById[item.gradeLevelId] ?? ''}`,
    sorters: {
      code: (left, right) => left.code.localeCompare(right.code),
      displayName: (left, right) => left.displayName.localeCompare(right.displayName),
      gradeLevel: (left, right) => (gradeLevelNameById[left.gradeLevelId] ?? '').localeCompare(gradeLevelNameById[right.gradeLevelId] ?? '')
    },
    initialSortKey: 'code',
    resetKeys: [activeSchoolId, classRoomGradeFilter, gradeLevels]
  });
  const teachingGroupGrid = useClientGrid({
    items: filteredTeachingGroups,
    getSearchText: (item) => `${item.name} ${classRoomNameById[item.classRoomId ?? ''] ?? ''} ${item.isDailyOperationsGroup ? 'daily' : 'custom'}`,
    sorters: {
      name: (left, right) => left.name.localeCompare(right.name),
      classRoom: (left, right) => (classRoomNameById[left.classRoomId ?? ''] ?? '').localeCompare(classRoomNameById[right.classRoomId ?? ''] ?? ''),
      dailyOps: (left, right) => Number(left.isDailyOperationsGroup) - Number(right.isDailyOperationsGroup)
    },
    initialSortKey: 'name',
    resetKeys: [activeSchoolId, teachingGroupClassRoomFilter, teachingGroupTypeFilter, classRooms]
  });
  const subjectGrid = useClientGrid({
    items: subjects,
    getSearchText: (item) => `${item.code} ${item.name}`,
    sorters: {
      code: (left, right) => left.code.localeCompare(right.code),
      name: (left, right) => left.name.localeCompare(right.name)
    },
    initialSortKey: 'code',
    resetKeys: [activeSchoolId]
  });
  const teacherAssignmentGrid = useClientGrid({
    items: filteredTeacherAssignments,
    getSearchText: (item) => `${item.teacherUserId} ${item.scope} ${classRoomNameById[item.classRoomId ?? ''] ?? ''} ${classRoomNameById[item.teachingGroupId ?? ''] ?? ''} ${subjectNameById[item.subjectId ?? ''] ?? ''}`,
    sorters: {
      teacher: (left, right) => left.teacherUserId.localeCompare(right.teacherUserId),
      scope: (left, right) => left.scope.localeCompare(right.scope),
      classRoom: (left, right) => (classRoomNameById[left.classRoomId ?? ''] ?? '').localeCompare(classRoomNameById[right.classRoomId ?? ''] ?? ''),
      subject: (left, right) => (subjectNameById[left.subjectId ?? ''] ?? '').localeCompare(subjectNameById[right.subjectId ?? ''] ?? '')
    },
    initialSortKey: 'teacher',
    resetKeys: [activeSchoolId, teacherAssignmentScopeFilter, classRooms, subjects]
  });
  const schoolGrid = useClientGrid({
    items: filteredSchools,
    getSearchText: (item) => [
      item.name,
      item.mainAddress.city,
      item.schoolIzo,
      item.schoolOperator?.legalEntityName,
      item.founder?.founderName
    ].filter(Boolean).join(' '),
    sorters: {
      name: (left, right) => left.name.localeCompare(right.name),
      schoolType: (left, right) => getSchoolTypeLabel(t, left.schoolType).localeCompare(getSchoolTypeLabel(t, right.schoolType)),
      city: (left, right) => (left.mainAddress.city ?? '').localeCompare(right.mainAddress.city ?? ''),
      operator: (left, right) => (left.schoolOperator?.legalEntityName ?? '').localeCompare(right.schoolOperator?.legalEntityName ?? ''),
      platformStatus: (left, right) => getPlatformStatusLabel(t, left.platformStatus).localeCompare(getPlatformStatusLabel(t, right.platformStatus)),
      active: (left, right) => Number(left.isActive) - Number(right.isActive),
      capacity: (left, right) => (left.maxStudentCapacity ?? -1) - (right.maxStudentCapacity ?? -1)
    },
    initialSortKey: 'name',
    resetKeys: [schoolTypeFilter, schoolStatusFilter, schools]
  });

  const contextSwitcherBlock = (showHelperText: boolean) => {
    if (!canSwitchSchoolContext && !showReadOnlySchoolContext) {
      return null;
    }

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
              <OrganizationContextSwitcher schools={schools} activeSchoolId={activeSchoolId} onSelectSchool={setActiveSchoolId} />
            ) : (
              <div className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm">
        <p className="font-semibold text-slate-900">{currentSchool?.name ?? t('orgNotAvailable')}</p>
                <p className="text-xs text-slate-500">
                  {currentSchool?.schoolType ? getSchoolTypeLabel(t, currentSchool.schoolType) : '-'}
                </p>
              </div>
            )}
          </div>
        </div>
        {showHelperText ? <div className="mt-3 text-xs text-slate-500">{t('orgActiveSchoolContextBoundaryHint')}</div> : null}
      </Card>
    );
  };

  const resetModal = () => {
    setModalState(null);
    setSchoolYearDraft(EMPTY_SCHOOL_YEAR);
    setGradeLevelDraft(EMPTY_GRADE_LEVEL);
    setClassRoomCreateDraft(EMPTY_CLASS_ROOM_CREATE);
    setClassRoomEditDraft(EMPTY_CLASS_ROOM_EDIT);
    setTeachingGroupCreateDraft(EMPTY_TEACHING_GROUP_CREATE);
    setTeachingGroupEditDraft(EMPTY_TEACHING_GROUP_EDIT);
    setSubjectCreateDraft(EMPTY_SUBJECT_CREATE);
    setSubjectEditDraft(EMPTY_SUBJECT_EDIT);
    setTeacherAssignmentCreateDraft(EMPTY_ASSIGNMENT_CREATE);
    setTeacherAssignmentEditDraft(EMPTY_ASSIGNMENT_EDIT);
    setTeacherAssignmentWizardStep(1);
  };

  const openSchoolYearCreate = () => {
    setSchoolYearDraft(EMPTY_SCHOOL_YEAR);
    setModalState({ kind: 'school-year', mode: 'create' });
  };

  const openSchoolYearEdit = (item: SchoolYear) => {
    setSchoolYearDraft({ label: item.label, startDate: item.startDate, endDate: item.endDate });
    setModalState({ kind: 'school-year', mode: 'edit', item });
  };

  const openGradeLevelCreate = () => {
    setGradeLevelDraft(EMPTY_GRADE_LEVEL);
    setModalState({ kind: 'grade-level', mode: 'create' });
  };

  const openGradeLevelEdit = (item: GradeLevel) => {
    setGradeLevelDraft({ level: item.level, displayName: item.displayName });
    setModalState({ kind: 'grade-level', mode: 'edit', item });
  };

  const openClassRoomCreate = () => {
    setClassRoomCreateDraft({ ...EMPTY_CLASS_ROOM_CREATE, gradeLevelId: gradeLevels[0]?.id ?? '' });
    setModalState({ kind: 'class-room', mode: 'create' });
  };

  const openClassRoomEdit = (item: ClassRoom) => {
    setClassRoomEditDraft({ code: item.code, displayName: item.displayName, overrideReason: '' });
    setModalState({ kind: 'class-room', mode: 'edit', item });
  };

  const openTeachingGroupCreate = () => {
    setTeachingGroupCreateDraft({ ...EMPTY_TEACHING_GROUP_CREATE, classRoomId: classRooms[0]?.id ?? '' });
    setModalState({ kind: 'teaching-group', mode: 'create' });
  };

  const openTeachingGroupEdit = (item: TeachingGroup) => {
    setTeachingGroupEditDraft({
      classRoomId: item.classRoomId ?? '',
      name: item.name,
      isDailyOperationsGroup: item.isDailyOperationsGroup,
      overrideReason: ''
    });
    setModalState({ kind: 'teaching-group', mode: 'edit', item });
  };

  const openSubjectCreate = () => {
    setSubjectCreateDraft(EMPTY_SUBJECT_CREATE);
    setModalState({ kind: 'subject', mode: 'create' });
  };

  const openSubjectEdit = (item: Subject) => {
    setSubjectEditDraft({ code: item.code, name: item.name, overrideReason: '' });
    setModalState({ kind: 'subject', mode: 'edit', item });
  };

  const openTeacherAssignmentCreate = () => {
    setTeacherAssignmentCreateDraft({
      ...EMPTY_ASSIGNMENT_CREATE,
      classRoomId: classRooms[0]?.id ?? '',
      teachingGroupId: teachingGroups[0]?.id ?? '',
      subjectId: subjects[0]?.id ?? ''
    });
    setTeacherAssignmentWizardStep(1);
    setModalState({ kind: 'teacher-assignment', mode: 'create' });
  };

  const openTeacherAssignmentEdit = (item: TeacherAssignment) => {
    setTeacherAssignmentEditDraft({
      teacherUserId: item.teacherUserId,
      scope: item.scope,
      classRoomId: item.classRoomId ?? '',
      teachingGroupId: item.teachingGroupId ?? '',
      subjectId: item.subjectId ?? '',
      overrideReason: ''
    });
    setTeacherAssignmentWizardStep(1);
    setModalState({ kind: 'teacher-assignment', mode: 'edit', item });
  };

  const handleSchoolYearSubmit = () => {
    if (modalState?.kind !== 'school-year') {
      return;
    }

    const action = modalState.mode === 'create'
      ? () => api.createSchoolYear({ id: '', schoolId: activeSchoolId, ...schoolYearDraft })
      : () => api.updateSchoolYear(modalState.item!.id, { startDate: schoolYearDraft.startDate, endDate: schoolYearDraft.endDate });

    void guardedRefresh(action, t(modalState.mode === 'create' ? 'orgSchoolYearCreated' : 'orgSchoolYearUpdated')).then((success) => {
      if (success) {
        resetModal();
      }
    });
  };

  const handleGradeLevelSubmit = () => {
    if (modalState?.kind !== 'grade-level') {
      return;
    }

    const action = modalState.mode === 'create'
      ? () => api.createGradeLevel({ id: '', schoolId: activeSchoolId, ...gradeLevelDraft })
      : () => api.updateGradeLevel(modalState.item!.id, { level: gradeLevelDraft.level, displayName: gradeLevelDraft.displayName });

    void guardedRefresh(action, t(modalState.mode === 'create' ? 'orgGradeLevelCreated' : 'orgGradeLevelUpdated')).then((success) => {
      if (success) {
        resetModal();
      }
    });
  };

  const handleClassRoomSubmit = () => {
    if (modalState?.kind !== 'class-room') {
      return;
    }

    const action = modalState.mode === 'create'
      ? () => api.createClassRoom({ id: '', schoolId: activeSchoolId, ...classRoomCreateDraft })
      : () => api.overrideClassRoom(modalState.item!.id, {
        code: classRoomEditDraft.code,
        displayName: classRoomEditDraft.displayName,
        overrideReason: classRoomEditDraft.overrideReason
      });

    void guardedRefresh(action, t(modalState.mode === 'create' ? 'orgClassRoomCreated' : 'orgClassRoomUpdated')).then((success) => {
      if (success) {
        resetModal();
      }
    });
  };

  const handleTeachingGroupSubmit = () => {
    if (modalState?.kind !== 'teaching-group') {
      return;
    }

    const action = modalState.mode === 'create'
      ? () => api.createTeachingGroup({ id: '', schoolId: activeSchoolId, ...teachingGroupCreateDraft, classRoomId: teachingGroupCreateDraft.classRoomId || undefined })
      : () => api.overrideTeachingGroup(modalState.item!.id, {
        classRoomId: teachingGroupEditDraft.classRoomId || undefined,
        name: teachingGroupEditDraft.name,
        isDailyOperationsGroup: teachingGroupEditDraft.isDailyOperationsGroup,
        overrideReason: teachingGroupEditDraft.overrideReason
      });

    void guardedRefresh(action, t(modalState.mode === 'create' ? 'orgTeachingGroupCreated' : 'orgTeachingGroupUpdated')).then((success) => {
      if (success) {
        resetModal();
      }
    });
  };

  const handleSubjectSubmit = () => {
    if (modalState?.kind !== 'subject') {
      return;
    }

    const action = modalState.mode === 'create'
      ? () => api.createSubject({ id: '', schoolId: activeSchoolId, ...subjectCreateDraft })
      : () => api.overrideSubject(modalState.item!.id, {
        code: subjectEditDraft.code,
        name: subjectEditDraft.name,
        overrideReason: subjectEditDraft.overrideReason
      });

    void guardedRefresh(action, t(modalState.mode === 'create' ? 'orgSubjectCreated' : 'orgSubjectUpdated')).then((success) => {
      if (success) {
        resetModal();
      }
    });
  };

  const handleTeacherAssignmentSubmit = () => {
    if (modalState?.kind !== 'teacher-assignment') {
      return;
    }

    const action = modalState.mode === 'create'
      ? () => api.createTeacherAssignment({ id: '', schoolId: activeSchoolId, ...teacherAssignmentCreateDraft })
      : () => api.overrideTeacherAssignment({
        existingAssignmentId: modalState.item!.id,
        schoolId: activeSchoolId,
        teacherUserId: teacherAssignmentEditDraft.teacherUserId,
        scope: teacherAssignmentEditDraft.scope,
        classRoomId: teacherAssignmentEditDraft.classRoomId || undefined,
        teachingGroupId: teacherAssignmentEditDraft.teachingGroupId || undefined,
        subjectId: teacherAssignmentEditDraft.subjectId || undefined,
        overrideReason: teacherAssignmentEditDraft.overrideReason
      });

    void guardedRefresh(action, t(modalState.mode === 'create' ? 'orgAssignmentCreated' : 'orgAssignmentUpdated')).then((success) => {
      if (success) {
        resetModal();
      }
    });
  };

  const schoolYearColumns: OrganizationGridColumn<SchoolYear>[] = [
    { key: 'label', label: t('orgSchoolYearLabel'), sortable: true, render: (item) => <span className="font-medium text-slate-900">{item.label}</span> },
    { key: 'startDate', label: t('orgSchoolYearStartDate'), sortable: true, render: (item) => item.startDate || '-' },
    { key: 'endDate', label: t('orgSchoolYearEndDate'), sortable: true, render: (item) => item.endDate || '-' }
  ];
  const gradeLevelColumns: OrganizationGridColumn<GradeLevel>[] = [
    { key: 'level', label: t('orgGradeLevelLevel'), sortable: true, render: (item) => <span className="font-medium text-slate-900">{item.level}</span> },
    { key: 'displayName', label: t('orgGradeLevelDisplayName'), sortable: true, render: (item) => item.displayName }
  ];
  const classRoomColumns: OrganizationGridColumn<ClassRoom>[] = [
    { key: 'code', label: t('orgClassRoomCode'), sortable: true, render: (item) => <span className="font-medium text-slate-900">{item.code}</span> },
    { key: 'displayName', label: t('orgClassRoomDisplayName'), sortable: true, render: (item) => item.displayName },
    { key: 'gradeLevel', label: t('orgClassRoomGradeLevel'), sortable: true, render: (item) => gradeLevelNameById[item.gradeLevelId] ?? '-' }
  ];
  const teachingGroupColumns: OrganizationGridColumn<TeachingGroup>[] = [
    { key: 'name', label: t('orgTeachingGroupName'), sortable: true, render: (item) => <span className="font-medium text-slate-900">{item.name}</span> },
    { key: 'classRoom', label: t('orgTeachingGroupClassRoom'), sortable: true, render: (item) => classRoomNameById[item.classRoomId ?? ''] ?? t('orgNone') },
    { key: 'dailyOps', label: t('orgTeachingGroupDailyOps'), sortable: true, render: (item) => item.isDailyOperationsGroup ? t('orgYes') : t('orgNo') }
  ];
  const subjectColumns: OrganizationGridColumn<Subject>[] = [
    { key: 'code', label: t('orgSubjectCode'), sortable: true, render: (item) => <span className="font-medium text-slate-900">{item.code}</span> },
    { key: 'name', label: t('orgSubjectName'), sortable: true, render: (item) => item.name }
  ];
  const teacherAssignmentColumns: OrganizationGridColumn<TeacherAssignment>[] = [
    { key: 'teacher', label: t('orgAssignmentTeacher'), sortable: true, render: (item) => <span className="font-medium text-slate-900">{item.teacherUserId}</span> },
    { key: 'scope', label: t('orgAssignmentScope'), sortable: true, render: (item) => item.scope },
    { key: 'classRoom', label: t('orgAssignmentClassRoom'), sortable: true, render: (item) => classRoomNameById[item.classRoomId ?? ''] ?? t('orgNone') },
    { key: 'subject', label: t('orgAssignmentSubject'), sortable: true, render: (item) => subjectNameById[item.subjectId ?? ''] ?? t('orgNone') }
  ];
  const schoolColumns: OrganizationGridColumn<School>[] = [
    {
      key: 'name',
      label: t('orgSchoolName'),
      sortable: true,
      render: (item) => (
        <div className="min-w-0">
          <div className="font-medium text-slate-900">{item.name}</div>
          <div className="mt-1 text-xs text-slate-500">{item.schoolIzo || '-'}</div>
        </div>
      )
    },
    { key: 'schoolType', label: t('orgSchoolTypeLabel'), sortable: true, render: (item) => getSchoolTypeLabel(t, item.schoolType) },
    { key: 'city', label: t('orgAddressCity'), sortable: true, render: (item) => <span className="text-xs text-slate-600">{item.mainAddress.city || '-'}</span> },
    { key: 'operator', label: t('orgSchoolCardOperator'), sortable: true, render: (item) => <span className="text-xs text-slate-600">{item.schoolOperator?.legalEntityName || '-'}</span> },
    { key: 'platformStatus', label: t('orgSchoolPlatformStatusLabel'), sortable: true, render: (item) => <span className="text-xs text-slate-600">{getPlatformStatusLabel(t, item.platformStatus)}</span> },
    {
      key: 'active',
      label: t('stateActive'),
      sortable: true,
      render: (item) => (
        <span className="inline-flex items-center text-xs">
          <span className={`sk-status-dot ${item.isActive ? 'sk-status-dot-active' : 'sk-status-dot-deactivated'}`} />
          {item.isActive ? t('orgSchoolActive') : t('orgSchoolInactive')}
        </span>
      )
    },
    { key: 'capacity', label: t('orgSchoolCardCapacity'), sortable: true, render: (item) => <span className="text-xs text-slate-600">{item.maxStudentCapacity?.toString() || '-'}</span> }
  ];

  if (loading) {
    return <LoadingState text={t('loadingOrganization')} />;
  }

  if (error && !schools.length && !studentContext) {
    return <ErrorState text={error} />;
  }

  if (isStudent && studentContext) {
    return (
      <section className="space-y-3">
        <SectionHeader title={t('routeOrganization')} description={t('orgStudentContextDescription')} />
        <Card>
          <p className="font-semibold text-sm">{studentContext.school.name} ({studentContext.school.schoolType})</p>
          <p className="mt-1 text-sm text-slate-600">{t('orgStudentReadOnly')}</p>
        </Card>
        <div className="grid gap-3 md:grid-cols-2">
          <Card><p className="font-semibold text-sm">{t('orgSchoolYearsTitle')}</p><ul className="sk-list">{studentContext.schoolYears.map((item) => <li className="sk-list-item" key={item.id}>{item.label}</li>)}</ul></Card>
          <Card><p className="font-semibold text-sm">{t('orgClassRoomsTitle')}</p><ul className="sk-list">{studentContext.classRooms.map((item) => <li className="sk-list-item" key={item.id}>{item.displayName}</li>)}</ul></Card>
          <Card><p className="font-semibold text-sm">{t('orgTeachingGroupsTitle')}</p><ul className="sk-list">{studentContext.teachingGroups.map((item) => <li className="sk-list-item" key={item.id}>{item.name}</li>)}</ul></Card>
          <Card><p className="font-semibold text-sm">{t('orgSubjectsTitle')}</p><ul className="sk-list">{studentContext.subjects.map((item) => <li className="sk-list-item" key={item.id}>{item.name}</li>)}</ul></Card>
        </div>
      </section>
    );
  }

  if (activeView === 'overview') {
    return (
      <section className="space-y-3">
        <SectionHeader title={t('routeOrganization')} description={t('orgOverviewDescription')} />
        {contextSwitcherBlock(true)}
        <Card>
          <div className="grid gap-3 md:grid-cols-3">
            <Metric label={t('orgSchoolsTitle')} value={schools.length} />
            <Metric label={t('orgSchoolYearsTitle')} value={schoolYears.length} />
            <Metric label={t('orgGradeLevelsTitle')} value={gradeLevels.length} />
            <Metric label={t('orgClassRoomsTitle')} value={classRooms.length} />
            <Metric label={t('orgTeachingGroupsTitle')} value={teachingGroups.length} />
            <Metric label={t('orgSubjectsTitle')} value={subjects.length} />
          </div>
          <div className="mt-3 text-sm text-slate-600">
            {currentSchool
              ? t('orgOverviewActiveContext', { name: currentSchool.name, type: getSchoolTypeLabel(t, currentSchool.schoolType) })
              : t('orgOverviewNoContext')}
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
                  <button className="sk-btn sk-btn-secondary" type="button" onClick={() => goTo(entry.route)}>{t('orgSchoolsSelect')}</button>
                </div>
              </Card>
            ))}
          </div>
        ) : (
          <Card>
            <p className="text-sm text-slate-700">{t('orgOverviewManageUnavailable')}</p>
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
            <button type="button" className={`sk-btn ${schoolWizardOpen ? 'sk-btn-secondary' : 'sk-btn-primary'} inline-flex items-center gap-2`} onClick={() => setSchoolWizardOpen((current) => !current)}>
              <CreatePlusIcon className="h-3.5 w-3.5" />
              {schoolWizardOpen ? t('orgCreateSchoolCloseWizard') : t('orgCreateSchoolOpenWizard')}
            </button>
          ) : undefined}
        />
        {notice ? <FeedbackBanner tone="success" text={notice} /> : null}
        {error ? <FeedbackBanner tone="error" text={error} /> : null}
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          <Metric label={t('orgSchoolsSummaryFiltered')} value={schoolSummary.filtered} />
          <Metric label={t('orgSchoolsSummaryActive')} value={schoolSummary.active} />
          <Metric label={t('orgSchoolsSummaryKindergarten')} value={schoolSummary.kindergarten} />
          <Metric label={t('orgSchoolsSummaryElementarySecondary')} value={schoolSummary.elementaryAndSecondary} />
        </div>
        {schoolWizardOpen ? (
          <SchoolCreateWizard t={t} wizardStep={schoolWizardStep} setWizardStep={setSchoolWizardStep} form={newSchool} setForm={setNewSchool} onCancel={resetSchoolWizard} onSubmit={createSchool} stepValid={schoolStepValid} busy={saving} />
        ) : null}
        <OrganizationGridSection
          title={t('orgSchoolsList')}
          description={t('orgSchoolsDescription')}
          searchLabel={t('orgSchoolsSearchLabel')}
          searchPlaceholder={t('orgSearchSchools')}
          searchValue={schoolGrid.search}
          onSearchChange={schoolGrid.setSearch}
          filters={(
            <div className="grid gap-3 md:grid-cols-3">
              <SelectField label={t('orgSchoolsFilterTypeLabel')} value={schoolTypeFilter} onChange={setSchoolTypeFilter} options={[
                { value: '', label: t('orgFilterAll') },
                { value: 'Kindergarten', label: t('orgSchoolTypeKindergarten') },
                { value: 'ElementarySchool', label: t('orgSchoolTypeElementarySchool') },
                { value: 'SecondarySchool', label: t('orgSchoolTypeSecondarySchool') }
              ]} />
              <SelectField label={t('orgSchoolsFilterStatusLabel')} value={schoolStatusFilter} onChange={(value) => setSchoolStatusFilter(value as SchoolStatusFilter)} options={[
                { value: 'all', label: t('orgFilterAll') },
                { value: 'active', label: t('orgSchoolActive') },
                { value: 'inactive', label: t('orgSchoolInactive') }
              ]} />
              <div className="flex items-end">
                <button type="button" className="sk-btn sk-btn-secondary w-full" onClick={() => {
                  setSchoolTypeFilter('');
                  setSchoolStatusFilter('all');
                  schoolGrid.setSearch('');
                }}>
                  {t('orgSchoolsResetFilters')}
                </button>
              </div>
            </div>
          )}
          actionsLabel={t('userManagementColActions')}
          pageLabel={(page, pageCount) => t('orgGridPageOf', { page: String(page), pages: String(pageCount) })}
          previousLabel={t('orgGridPreviousPage')}
          nextLabel={t('orgGridNextPage')}
          pageSizeLabel={(pageSize) => t('orgGridPageSizeOption', { size: String(pageSize) })}
          columns={schoolColumns}
          items={schoolGrid.pageItems}
          getRowKey={(item) => item.id}
          emptyText={t('orgNoSchools')}
          sortKey={schoolGrid.sortKey}
          sortDirection={schoolGrid.sortDirection}
          onSort={schoolGrid.requestSort}
          page={schoolGrid.page}
          pageCount={schoolGrid.pageCount}
          pageSize={schoolGrid.pageSize}
          onPageChange={schoolGrid.setPage}
          onPageSizeChange={schoolGrid.setPageSize}
          pageSizeOptions={[10, 20, 50]}
          rangeStart={schoolGrid.rangeStart}
          rangeEnd={schoolGrid.rangeEnd}
          totalCount={schoolGrid.totalCount}
          renderRowActions={(school, closeMenu) => (
            <>
              <button type="button" className="sk-action-menu-item" onClick={() => {
                closeMenu();
                setActiveSchoolId(school.id);
                setSchoolDetailOpen(true);
              }}>
                <EditPencilIcon className="h-3.5 w-3.5" />
                {t('orgGridEdit')}
              </button>
              {isPlatformAdmin ? (
                <button type="button" className="sk-action-menu-item danger" onClick={() => {
                  closeMenu();
                  setSaving(true);
                  void api.setSchoolStatus(school.id, !school.isActive)
                    .then(() => {
                      setNotice(t('orgSchoolDetailSavedSuccess'));
                      load();
                    })
                    .catch((nextError: Error) => setError(nextError.message))
                    .finally(() => setSaving(false));
                }}>
                  {school.isActive ? t('deactivate') : t('activate')}
                </button>
              ) : null}
            </>
          )}
        />
        {schoolDetailOpen && currentSchool ? (
          <OrganizationEntityModal
            open={schoolDetailOpen}
            title={currentSchool.name}
            description={getSchoolTypeLabel(t, currentSchool.schoolType)}
            onClose={() => setSchoolDetailOpen(false)}
            closeLabel={t('cancel')}
            className="sk-modal-wide"
          >
            <OrganizationSchoolIdentityCard
              school={currentSchool}
              editable={canWriteSchoolContext}
              onSave={(schoolId, payload) => api.updateSchool(schoolId, payload).then(() => setNotice(t('orgSchoolDetailSavedSuccess'))).then(() => load())}
            />
          </OrganizationEntityModal>
        ) : null}
      </section>
    );
  }

  const sharedGridProps = {
    actionsLabel: t('orgGridActions'),
    pageLabel: (page: number, pageCount: number) => t('orgGridPageOf', { page: String(page), pages: String(pageCount) }),
    previousLabel: t('orgGridPreviousPage'),
    nextLabel: t('orgGridNextPage'),
    pageSizeLabel: (pageSize: number) => t('orgGridPageSizeOption', { size: String(pageSize) }),
    searchLabel: t('orgGridSearchLabel')
  };

  return (
    <section className="space-y-3">
      {contextSwitcherBlock(false)}
      {notice ? <FeedbackBanner tone="success" text={notice} /> : null}
      {error ? <FeedbackBanner tone="error" text={error} /> : null}

      {activeView === 'school-years' ? <OrganizationSectionView
        canWrite={canWriteSchoolContext}
        gridProps={sharedGridProps}
        title={t('orgSchoolYearsTitle')}
        description={t('orgSchoolYearsDescription')}
        createLabel={t('orgSchoolYearCreateTitle')}
        searchPlaceholder={t('orgGridSearchPlaceholder')}
        searchValue={schoolYearGrid.search}
        onSearchChange={schoolYearGrid.setSearch}
        filters={<SelectField label={t('orgGridFilterLabel')} value={schoolYearWindowFilter} onChange={(value) => setSchoolYearWindowFilter(value as SchoolYearWindowFilter)} options={[{ value: 'all', label: t('orgFilterAll') }, { value: 'current', label: t('orgGridSchoolYearCurrent') }, { value: 'upcoming', label: t('orgGridSchoolYearUpcoming') }, { value: 'past', label: t('orgGridSchoolYearPast') }]} />}
        onCreate={openSchoolYearCreate}
        columns={schoolYearColumns}
        items={schoolYearGrid.pageItems}
        getRowKey={(item) => item.id}
        emptyText={t('orgGridNoResults')}
        sortKey={schoolYearGrid.sortKey}
        sortDirection={schoolYearGrid.sortDirection}
        onSort={schoolYearGrid.requestSort}
        page={schoolYearGrid.page}
        pageCount={schoolYearGrid.pageCount}
        pageSize={schoolYearGrid.pageSize}
        onPageChange={schoolYearGrid.setPage}
        onPageSizeChange={schoolYearGrid.setPageSize}
        rangeStart={schoolYearGrid.rangeStart}
        rangeEnd={schoolYearGrid.rangeEnd}
        totalCount={schoolYearGrid.totalCount}
        onEdit={openSchoolYearEdit}
        editLabel={t('orgGridEdit')}
      /> : null}

      {activeView === 'grade-levels' ? <OrganizationSectionView canWrite={canWriteSchoolContext} gridProps={sharedGridProps} title={t('orgGradeLevelsTitle')} description={t('orgGradeLevelsDescription')} createLabel={t('orgGradeLevelCreateTitle')} searchPlaceholder={t('orgGridSearchPlaceholder')} searchValue={gradeLevelGrid.search} onSearchChange={gradeLevelGrid.setSearch} onCreate={openGradeLevelCreate} columns={gradeLevelColumns} items={gradeLevelGrid.pageItems} getRowKey={(item) => item.id} emptyText={t('orgGridNoResults')} sortKey={gradeLevelGrid.sortKey} sortDirection={gradeLevelGrid.sortDirection} onSort={gradeLevelGrid.requestSort} page={gradeLevelGrid.page} pageCount={gradeLevelGrid.pageCount} pageSize={gradeLevelGrid.pageSize} onPageChange={gradeLevelGrid.setPage} onPageSizeChange={gradeLevelGrid.setPageSize} rangeStart={gradeLevelGrid.rangeStart} rangeEnd={gradeLevelGrid.rangeEnd} totalCount={gradeLevelGrid.totalCount} onEdit={openGradeLevelEdit} editLabel={t('orgGridEdit')} /> : null}

      {activeView === 'class-rooms' ? <OrganizationSectionView canWrite={canWriteSchoolContext} gridProps={sharedGridProps} title={t('orgClassRoomsTitle')} description={t('orgClassRoomsDescription')} createLabel={t('orgClassRoomCreateTitle')} searchPlaceholder={t('orgGridSearchPlaceholder')} searchValue={classRoomGrid.search} onSearchChange={classRoomGrid.setSearch} filters={<SelectField label={t('orgClassRoomGradeLevel')} value={classRoomGradeFilter} onChange={setClassRoomGradeFilter} options={[{ value: '', label: t('orgFilterAll') }, ...gradeLevels.map((gradeLevel) => ({ value: gradeLevel.id, label: `${gradeLevel.level} - ${gradeLevel.displayName}` }))]} />} onCreate={openClassRoomCreate} columns={classRoomColumns} items={classRoomGrid.pageItems} getRowKey={(item) => item.id} emptyText={t('orgGridNoResults')} sortKey={classRoomGrid.sortKey} sortDirection={classRoomGrid.sortDirection} onSort={classRoomGrid.requestSort} page={classRoomGrid.page} pageCount={classRoomGrid.pageCount} pageSize={classRoomGrid.pageSize} onPageChange={classRoomGrid.setPage} onPageSizeChange={classRoomGrid.setPageSize} rangeStart={classRoomGrid.rangeStart} rangeEnd={classRoomGrid.rangeEnd} totalCount={classRoomGrid.totalCount} onEdit={openClassRoomEdit} editLabel={t('orgGridEdit')} /> : null}

      {activeView === 'teaching-groups' ? <OrganizationSectionView canWrite={canWriteSchoolContext} gridProps={sharedGridProps} title={t('orgTeachingGroupsTitle')} description={t('orgTeachingGroupsDescription')} createLabel={t('orgTeachingGroupCreateTitle')} searchPlaceholder={t('orgGridSearchPlaceholder')} searchValue={teachingGroupGrid.search} onSearchChange={teachingGroupGrid.setSearch} filters={<div className="grid gap-3 md:grid-cols-2"><SelectField label={t('orgTeachingGroupClassRoom')} value={teachingGroupClassRoomFilter} onChange={setTeachingGroupClassRoomFilter} options={[{ value: '', label: t('orgFilterAll') }, ...classRooms.map((classRoom) => ({ value: classRoom.id, label: `${classRoom.code} - ${classRoom.displayName}` }))]} /><SelectField label={t('orgGridFilterLabel')} value={teachingGroupTypeFilter} onChange={(value) => setTeachingGroupTypeFilter(value as TeachingGroupFilter)} options={[{ value: 'all', label: t('orgFilterAll') }, { value: 'daily', label: t('orgTeachingGroupDailyOps') }, { value: 'custom', label: t('orgGridTeachingGroupCustom') }]} /></div>} onCreate={openTeachingGroupCreate} columns={teachingGroupColumns} items={teachingGroupGrid.pageItems} getRowKey={(item) => item.id} emptyText={t('orgGridNoResults')} sortKey={teachingGroupGrid.sortKey} sortDirection={teachingGroupGrid.sortDirection} onSort={teachingGroupGrid.requestSort} page={teachingGroupGrid.page} pageCount={teachingGroupGrid.pageCount} pageSize={teachingGroupGrid.pageSize} onPageChange={teachingGroupGrid.setPage} onPageSizeChange={teachingGroupGrid.setPageSize} rangeStart={teachingGroupGrid.rangeStart} rangeEnd={teachingGroupGrid.rangeEnd} totalCount={teachingGroupGrid.totalCount} onEdit={openTeachingGroupEdit} editLabel={t('orgGridEdit')} /> : null}

      {activeView === 'subjects' ? <OrganizationSectionView canWrite={canWriteSchoolContext} gridProps={sharedGridProps} title={t('orgSubjectsTitle')} description={t('orgSubjectsDescription')} createLabel={t('orgSubjectCreateTitle')} searchPlaceholder={t('orgGridSearchPlaceholder')} searchValue={subjectGrid.search} onSearchChange={subjectGrid.setSearch} onCreate={openSubjectCreate} columns={subjectColumns} items={subjectGrid.pageItems} getRowKey={(item) => item.id} emptyText={t('orgGridNoResults')} sortKey={subjectGrid.sortKey} sortDirection={subjectGrid.sortDirection} onSort={subjectGrid.requestSort} page={subjectGrid.page} pageCount={subjectGrid.pageCount} pageSize={subjectGrid.pageSize} onPageChange={subjectGrid.setPage} onPageSizeChange={subjectGrid.setPageSize} rangeStart={subjectGrid.rangeStart} rangeEnd={subjectGrid.rangeEnd} totalCount={subjectGrid.totalCount} onEdit={openSubjectEdit} editLabel={t('orgGridEdit')} /> : null}

      {activeView === 'teacher-assignments' ? <OrganizationSectionView canWrite={canWriteSchoolContext} gridProps={sharedGridProps} title={t('orgTeacherAssignmentsTitle')} description={t('orgTeacherAssignmentsDescription')} createLabel={t('orgAssignmentCreateTitle')} searchPlaceholder={t('orgGridSearchPlaceholder')} searchValue={teacherAssignmentGrid.search} onSearchChange={teacherAssignmentGrid.setSearch} filters={<SelectField label={t('orgAssignmentScope')} value={teacherAssignmentScopeFilter} onChange={setTeacherAssignmentScopeFilter} options={[{ value: '', label: t('orgFilterAll') }, ...Array.from(new Set(teacherAssignments.map((assignment) => assignment.scope))).map((scope) => ({ value: scope, label: scope }))]} />} onCreate={openTeacherAssignmentCreate} columns={teacherAssignmentColumns} items={teacherAssignmentGrid.pageItems} getRowKey={(item) => item.id} emptyText={t('orgGridNoResults')} sortKey={teacherAssignmentGrid.sortKey} sortDirection={teacherAssignmentGrid.sortDirection} onSort={teacherAssignmentGrid.requestSort} page={teacherAssignmentGrid.page} pageCount={teacherAssignmentGrid.pageCount} pageSize={teacherAssignmentGrid.pageSize} onPageChange={teacherAssignmentGrid.setPage} onPageSizeChange={teacherAssignmentGrid.setPageSize} rangeStart={teacherAssignmentGrid.rangeStart} rangeEnd={teacherAssignmentGrid.rangeEnd} totalCount={teacherAssignmentGrid.totalCount} onEdit={openTeacherAssignmentEdit} editLabel={t('orgGridEdit')} /> : null}

      <OrganizationEntityModal open={Boolean(modalState)} title={resolveModalTitle(t, modalState)} description={resolveModalDescription(t, modalState)} onClose={resetModal} closeLabel={t('orgGridClose')} className={modalState?.kind === 'teacher-assignment' ? 'sk-modal-wide' : 'sk-modal-large'}>
        {modalState?.kind === 'school-year' ? <SingleStepWizardShell stepTitle={t('orgGridSingleStepWizard')} submitLabel={modalState.mode === 'create' ? t('orgGridCreateNew') : t('orgGridSaveChanges')} cancelLabel={t('cancel')} onCancel={resetModal} onSubmit={handleSchoolYearSubmit} submitDisabled={!activeSchoolId || !schoolYearDraft.label.trim() || !schoolYearDraft.startDate || !schoolYearDraft.endDate || saving} busy={saving}><div className="grid gap-3 md:grid-cols-3"><InputField label={t('orgSchoolYearLabel')} value={schoolYearDraft.label} onChange={(value) => setSchoolYearDraft((current) => ({ ...current, label: value }))} /><InputField label={t('orgSchoolYearStartDate')} type="date" value={schoolYearDraft.startDate} onChange={(value) => setSchoolYearDraft((current) => ({ ...current, startDate: value }))} /><InputField label={t('orgSchoolYearEndDate')} type="date" value={schoolYearDraft.endDate} onChange={(value) => setSchoolYearDraft((current) => ({ ...current, endDate: value }))} /></div></SingleStepWizardShell> : null}
        {modalState?.kind === 'grade-level' ? <SingleStepWizardShell stepTitle={t('orgGridSingleStepWizard')} submitLabel={modalState.mode === 'create' ? t('orgGridCreateNew') : t('orgGridSaveChanges')} cancelLabel={t('cancel')} onCancel={resetModal} onSubmit={handleGradeLevelSubmit} submitDisabled={!activeSchoolId || !gradeLevelDraft.displayName.trim() || saving} busy={saving}><div className="grid gap-3 md:grid-cols-2"><InputField label={t('orgGradeLevelLevel')} type="number" value={String(gradeLevelDraft.level)} onChange={(value) => setGradeLevelDraft((current) => ({ ...current, level: Number(value) || 1 }))} /><InputField label={t('orgGradeLevelDisplayName')} value={gradeLevelDraft.displayName} onChange={(value) => setGradeLevelDraft((current) => ({ ...current, displayName: value }))} /></div></SingleStepWizardShell> : null}
        {modalState?.kind === 'class-room' ? <ClassRoomModal mode={modalState.mode} t={t} createDraft={classRoomCreateDraft} setCreateDraft={setClassRoomCreateDraft} editDraft={classRoomEditDraft} setEditDraft={setClassRoomEditDraft} gradeLevels={gradeLevels} saving={saving} activeSchoolId={activeSchoolId} onCancel={resetModal} onSubmit={handleClassRoomSubmit} /> : null}
        {modalState?.kind === 'teaching-group' ? <TeachingGroupModal mode={modalState.mode} t={t} createDraft={teachingGroupCreateDraft} setCreateDraft={setTeachingGroupCreateDraft} editDraft={teachingGroupEditDraft} setEditDraft={setTeachingGroupEditDraft} classRooms={classRooms} saving={saving} activeSchoolId={activeSchoolId} onCancel={resetModal} onSubmit={handleTeachingGroupSubmit} /> : null}
        {modalState?.kind === 'subject' ? <SubjectModal mode={modalState.mode} t={t} createDraft={subjectCreateDraft} setCreateDraft={setSubjectCreateDraft} editDraft={subjectEditDraft} setEditDraft={setSubjectEditDraft} saving={saving} activeSchoolId={activeSchoolId} onCancel={resetModal} onSubmit={handleSubjectSubmit} /> : null}
        {modalState?.kind === 'teacher-assignment' ? <TeacherAssignmentWizard mode={modalState.mode} t={t} step={teacherAssignmentWizardStep} setStep={setTeacherAssignmentWizardStep} createDraft={teacherAssignmentCreateDraft} setCreateDraft={setTeacherAssignmentCreateDraft} editDraft={teacherAssignmentEditDraft} setEditDraft={setTeacherAssignmentEditDraft} classRoomOptions={classRooms.map((classRoom) => ({ value: classRoom.id, label: `${classRoom.code} - ${classRoom.displayName}` }))} teachingGroupOptions={teachingGroups.map((group) => ({ value: group.id, label: group.name }))} subjectOptions={subjects.map((subject) => ({ value: subject.id, label: `${subject.code} - ${subject.name}` }))} onCancel={resetModal} onSubmit={handleTeacherAssignmentSubmit} busy={saving} /> : null}
      </OrganizationEntityModal>
    </section>
  );
}

function OrganizationSectionView<T>({ canWrite, gridProps, onEdit, editLabel, ...props }: any) {
  return (
    <OrganizationGridSection
      {...gridProps}
      {...props}
      pageSizeOptions={[10, 20, 50, 100]}
      createLabel={canWrite ? props.createLabel : undefined}
      onCreate={canWrite ? props.onCreate : undefined}
      renderRowActions={canWrite ? (item: T, closeMenu: () => void) => (
        <button type="button" className="sk-action-menu-item" onClick={() => { closeMenu(); onEdit(item); }}>
          <EditPencilIcon className="h-3.5 w-3.5" />
          {editLabel}
        </button>
      ) : undefined}
    />
  );
}

function resolveModalTitle(t: (key: string, params?: Record<string, string>) => string, modalState: EntityModalState) {
  if (!modalState) return '';
  if (modalState.kind === 'school-year') return t(modalState.mode === 'create' ? 'orgSchoolYearCreateTitle' : 'orgSchoolYearEditTitle');
  if (modalState.kind === 'grade-level') return t(modalState.mode === 'create' ? 'orgGradeLevelCreateTitle' : 'orgGradeLevelEditTitle');
  if (modalState.kind === 'class-room') return t(modalState.mode === 'create' ? 'orgClassRoomCreateTitle' : 'orgClassRoomEditTitle');
  if (modalState.kind === 'teaching-group') return t(modalState.mode === 'create' ? 'orgTeachingGroupCreateTitle' : 'orgTeachingGroupEditTitle');
  if (modalState.kind === 'subject') return t(modalState.mode === 'create' ? 'orgSubjectCreateTitle' : 'orgSubjectEditTitle');
  return t(modalState.mode === 'create' ? 'orgAssignmentCreateTitle' : 'orgAssignmentEditTitle');
}

function resolveModalDescription(t: (key: string, params?: Record<string, string>) => string, modalState: EntityModalState) {
  if (!modalState) return '';
  return modalState.mode === 'create' ? t('orgGridCreateWizardDescription') : t('orgGridEditModalDescription');
}

function matchesSchoolYearWindowFilter(item: SchoolYear, filter: SchoolYearWindowFilter) {
  if (filter === 'all') return true;
  const today = new Date().toISOString().slice(0, 10);
  if (filter === 'current') return item.startDate <= today && item.endDate >= today;
  if (filter === 'upcoming') return item.startDate > today;
  return item.endDate < today;
}

function Metric({ label, value }: { label: string; value: number }) {
  return <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-sm"><p className="text-xs uppercase tracking-wide text-slate-500">{label}</p><p className="mt-1 text-2xl font-semibold text-slate-900">{value}</p></div>;
}

function FeedbackBanner({ tone, text }: { tone: 'success' | 'error'; text: string }) {
  return <div className={`rounded-lg px-4 py-3 text-sm ${tone === 'success' ? 'border border-emerald-200 bg-emerald-50 text-emerald-800' : 'border border-rose-200 bg-rose-50 text-rose-800'}`}>{text}</div>;
}

function SingleStepWizardShell({ stepTitle, submitLabel, cancelLabel, onCancel, onSubmit, submitDisabled, busy, children }: { stepTitle: string; submitLabel: string; cancelLabel: string; onCancel: () => void; onSubmit: () => void; submitDisabled: boolean; busy: boolean; children: React.ReactNode; }) {
  return <div className="sk-wizard space-y-4"><div className="sk-wizard-steps flex items-center gap-2"><div className="rounded-full bg-blue-600 px-3 py-1 text-xs font-medium text-white">1</div><p className="text-sm font-medium text-slate-700">{stepTitle}</p></div><div className="sk-wizard-body">{children}</div><div className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-200 pt-4"><button type="button" className="sk-btn sk-btn-secondary" onClick={onCancel}>{cancelLabel}</button><button type="button" className="sk-btn sk-btn-primary" disabled={submitDisabled} onClick={onSubmit}>{busy ? '...' : submitLabel}</button></div></div>;
}

function ClassRoomModal({ mode, t, createDraft, setCreateDraft, editDraft, setEditDraft, gradeLevels, saving, activeSchoolId, onCancel, onSubmit }: { mode: 'create' | 'edit'; t: (key: string, params?: Record<string, string>) => string; createDraft: ClassRoomCreateDraft; setCreateDraft: React.Dispatch<React.SetStateAction<ClassRoomCreateDraft>>; editDraft: ClassRoomEditDraft; setEditDraft: React.Dispatch<React.SetStateAction<ClassRoomEditDraft>>; gradeLevels: GradeLevel[]; saving: boolean; activeSchoolId: string; onCancel: () => void; onSubmit: () => void; }) {
  const disabled = mode === 'create' ? !activeSchoolId || !createDraft.gradeLevelId || !createDraft.code.trim() || !createDraft.displayName.trim() || saving : !editDraft.code.trim() || !editDraft.displayName.trim() || !editDraft.overrideReason.trim() || saving;
  return <SingleStepWizardShell stepTitle={t(mode === 'create' ? 'orgGridSingleStepWizard' : 'orgGridEditStep')} submitLabel={mode === 'create' ? t('orgGridCreateNew') : t('orgGridSaveChanges')} cancelLabel={t('cancel')} onCancel={onCancel} onSubmit={onSubmit} submitDisabled={disabled} busy={saving}>{mode === 'create' ? <div className="grid gap-3 md:grid-cols-3"><SelectField label={t('orgClassRoomGradeLevel')} value={createDraft.gradeLevelId} onChange={(value) => setCreateDraft((current) => ({ ...current, gradeLevelId: value }))} options={gradeLevels.map((gradeLevel) => ({ value: gradeLevel.id, label: `${gradeLevel.level} - ${gradeLevel.displayName}` }))} /><InputField label={t('orgClassRoomCode')} value={createDraft.code} onChange={(value) => setCreateDraft((current) => ({ ...current, code: value }))} /><InputField label={t('orgClassRoomDisplayName')} value={createDraft.displayName} onChange={(value) => setCreateDraft((current) => ({ ...current, displayName: value }))} /></div> : <div className="space-y-3"><div className="grid gap-3 md:grid-cols-2"><InputField label={t('orgClassRoomCode')} value={editDraft.code} onChange={(value) => setEditDraft((current) => ({ ...current, code: value }))} /><InputField label={t('orgClassRoomDisplayName')} value={editDraft.displayName} onChange={(value) => setEditDraft((current) => ({ ...current, displayName: value }))} /></div><TextAreaField label={t('orgGridOverrideReason')} value={editDraft.overrideReason} onChange={(value) => setEditDraft((current) => ({ ...current, overrideReason: value }))} /></div>}</SingleStepWizardShell>;
}

function TeachingGroupModal({ mode, t, createDraft, setCreateDraft, editDraft, setEditDraft, classRooms, saving, activeSchoolId, onCancel, onSubmit }: { mode: 'create' | 'edit'; t: (key: string, params?: Record<string, string>) => string; createDraft: TeachingGroupCreateDraft; setCreateDraft: React.Dispatch<React.SetStateAction<TeachingGroupCreateDraft>>; editDraft: TeachingGroupEditDraft; setEditDraft: React.Dispatch<React.SetStateAction<TeachingGroupEditDraft>>; classRooms: ClassRoom[]; saving: boolean; activeSchoolId: string; onCancel: () => void; onSubmit: () => void; }) {
  const options = [{ value: '', label: t('orgNone') }, ...classRooms.map((classRoom) => ({ value: classRoom.id, label: `${classRoom.code} - ${classRoom.displayName}` }))];
  const disabled = mode === 'create' ? !activeSchoolId || !createDraft.name.trim() || saving : !editDraft.name.trim() || !editDraft.overrideReason.trim() || saving;
  return <SingleStepWizardShell stepTitle={t(mode === 'create' ? 'orgGridSingleStepWizard' : 'orgGridEditStep')} submitLabel={mode === 'create' ? t('orgGridCreateNew') : t('orgGridSaveChanges')} cancelLabel={t('cancel')} onCancel={onCancel} onSubmit={onSubmit} submitDisabled={disabled} busy={saving}>{mode === 'create' ? <div className="space-y-3"><div className="grid gap-3 md:grid-cols-2"><SelectField label={t('orgTeachingGroupClassRoom')} value={createDraft.classRoomId} onChange={(value) => setCreateDraft((current) => ({ ...current, classRoomId: value }))} options={options} /><InputField label={t('orgTeachingGroupName')} value={createDraft.name} onChange={(value) => setCreateDraft((current) => ({ ...current, name: value }))} /></div><CheckboxField label={t('orgTeachingGroupDailyOps')} checked={createDraft.isDailyOperationsGroup} onChange={(value) => setCreateDraft((current) => ({ ...current, isDailyOperationsGroup: value }))} /></div> : <div className="space-y-3"><div className="grid gap-3 md:grid-cols-2"><SelectField label={t('orgTeachingGroupClassRoom')} value={editDraft.classRoomId} onChange={(value) => setEditDraft((current) => ({ ...current, classRoomId: value }))} options={options} /><InputField label={t('orgTeachingGroupName')} value={editDraft.name} onChange={(value) => setEditDraft((current) => ({ ...current, name: value }))} /></div><CheckboxField label={t('orgTeachingGroupDailyOps')} checked={editDraft.isDailyOperationsGroup} onChange={(value) => setEditDraft((current) => ({ ...current, isDailyOperationsGroup: value }))} /><TextAreaField label={t('orgGridOverrideReason')} value={editDraft.overrideReason} onChange={(value) => setEditDraft((current) => ({ ...current, overrideReason: value }))} /></div>}</SingleStepWizardShell>;
}

function SubjectModal({ mode, t, createDraft, setCreateDraft, editDraft, setEditDraft, saving, activeSchoolId, onCancel, onSubmit }: { mode: 'create' | 'edit'; t: (key: string, params?: Record<string, string>) => string; createDraft: SubjectCreateDraft; setCreateDraft: React.Dispatch<React.SetStateAction<SubjectCreateDraft>>; editDraft: SubjectEditDraft; setEditDraft: React.Dispatch<React.SetStateAction<SubjectEditDraft>>; saving: boolean; activeSchoolId: string; onCancel: () => void; onSubmit: () => void; }) {
  const disabled = mode === 'create' ? !activeSchoolId || !createDraft.code.trim() || !createDraft.name.trim() || saving : !editDraft.code.trim() || !editDraft.name.trim() || !editDraft.overrideReason.trim() || saving;
  return <SingleStepWizardShell stepTitle={t(mode === 'create' ? 'orgGridSingleStepWizard' : 'orgGridEditStep')} submitLabel={mode === 'create' ? t('orgGridCreateNew') : t('orgGridSaveChanges')} cancelLabel={t('cancel')} onCancel={onCancel} onSubmit={onSubmit} submitDisabled={disabled} busy={saving}>{mode === 'create' ? <div className="grid gap-3 md:grid-cols-2"><InputField label={t('orgSubjectCode')} value={createDraft.code} onChange={(value) => setCreateDraft((current) => ({ ...current, code: value }))} /><InputField label={t('orgSubjectName')} value={createDraft.name} onChange={(value) => setCreateDraft((current) => ({ ...current, name: value }))} /></div> : <div className="space-y-3"><div className="grid gap-3 md:grid-cols-2"><InputField label={t('orgSubjectCode')} value={editDraft.code} onChange={(value) => setEditDraft((current) => ({ ...current, code: value }))} /><InputField label={t('orgSubjectName')} value={editDraft.name} onChange={(value) => setEditDraft((current) => ({ ...current, name: value }))} /></div><TextAreaField label={t('orgGridOverrideReason')} value={editDraft.overrideReason} onChange={(value) => setEditDraft((current) => ({ ...current, overrideReason: value }))} /></div>}</SingleStepWizardShell>;
}

function TeacherAssignmentWizard({ mode, t, step, setStep, createDraft, setCreateDraft, editDraft, setEditDraft, classRoomOptions, teachingGroupOptions, subjectOptions, onCancel, onSubmit, busy }: { mode: 'create' | 'edit'; t: (key: string, params?: Record<string, string>) => string; step: number; setStep: React.Dispatch<React.SetStateAction<number>>; createDraft: TeacherAssignmentCreateDraft; setCreateDraft: React.Dispatch<React.SetStateAction<TeacherAssignmentCreateDraft>>; editDraft: TeacherAssignmentEditDraft; setEditDraft: React.Dispatch<React.SetStateAction<TeacherAssignmentEditDraft>>; classRoomOptions: { value: string; label: string }[]; teachingGroupOptions: { value: string; label: string }[]; subjectOptions: { value: string; label: string }[]; onCancel: () => void; onSubmit: () => void; busy: boolean; }) {
  const draft = mode === 'create' ? createDraft : editDraft;
  const setDraft = mode === 'create' ? setCreateDraft : setEditDraft;
  const stepOneValid = Boolean(draft.teacherUserId.trim() && draft.scope.trim());
  const canSubmit = mode === 'create' ? stepOneValid : stepOneValid && Boolean(editDraft.overrideReason.trim());
  return <div className="sk-wizard space-y-4"><div className="sk-wizard-steps flex items-center gap-1 overflow-x-auto">{[1, 2].map((wizardStep) => <React.Fragment key={wizardStep}><div className={`rounded-full px-3 py-1 text-xs font-medium ${wizardStep === step ? 'bg-blue-600 text-white' : wizardStep < step ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>{wizardStep === 1 ? t('orgAssignmentWizardStep1') : t('orgAssignmentWizardStep2')}</div>{wizardStep === 1 ? <span className="text-slate-300">→</span> : null}</React.Fragment>)}</div>{step === 1 ? <div className="grid gap-3 md:grid-cols-2"><InputField label={t('orgAssignmentTeacher')} value={draft.teacherUserId} onChange={(value) => setDraft((current: any) => ({ ...current, teacherUserId: value }))} /><SelectField label={t('orgAssignmentScope')} value={draft.scope} onChange={(value) => setDraft((current: any) => ({ ...current, scope: value }))} options={[{ value: 'ClassTeacher', label: t('orgAssignmentScopeClassTeacher') }, { value: 'SubjectTeacher', label: t('orgAssignmentScopeSubjectTeacher') }, { value: 'GroupLeader', label: t('orgAssignmentScopeGroupLeader') }]} /></div> : <div className="space-y-3"><div className="grid gap-3 md:grid-cols-3"><SelectField label={t('orgAssignmentClassRoom')} value={draft.classRoomId} onChange={(value) => setDraft((current: any) => ({ ...current, classRoomId: value }))} options={[{ value: '', label: t('orgNone') }, ...classRoomOptions]} /><SelectField label={t('orgAssignmentGroup')} value={draft.teachingGroupId} onChange={(value) => setDraft((current: any) => ({ ...current, teachingGroupId: value }))} options={[{ value: '', label: t('orgNone') }, ...teachingGroupOptions]} /><SelectField label={t('orgAssignmentSubject')} value={draft.subjectId} onChange={(value) => setDraft((current: any) => ({ ...current, subjectId: value }))} options={[{ value: '', label: t('orgNone') }, ...subjectOptions]} /></div>{mode === 'edit' ? <TextAreaField label={t('orgGridOverrideReason')} value={editDraft.overrideReason} onChange={(value) => setEditDraft((current) => ({ ...current, overrideReason: value }))} /> : null}</div>}<div className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-200 pt-4"><button type="button" className="sk-btn sk-btn-secondary" onClick={step === 1 ? onCancel : () => setStep(1)}>{step === 1 ? t('cancel') : t('orgGridPreviousPage')}</button><button type="button" className="sk-btn sk-btn-primary" disabled={step === 1 ? !stepOneValid : !canSubmit || busy} onClick={step === 1 ? () => setStep(2) : onSubmit}>{step === 1 ? t('orgGridNextPage') : busy ? '...' : mode === 'create' ? t('orgGridCreateNew') : t('orgGridSaveChanges')}</button></div></div>;
}

function SchoolCreateWizard({ t, wizardStep, setWizardStep, form, setForm, onCancel, onSubmit, stepValid, busy }: { t: (key: string, params?: Record<string, string>) => string; wizardStep: number; setWizardStep: React.Dispatch<React.SetStateAction<number>>; form: SchoolMutation; setForm: React.Dispatch<React.SetStateAction<SchoolMutation>>; onCancel: () => void; onSubmit: () => void; stepValid: (step: number) => boolean; busy: boolean; }) {
  return <Card className="border-sky-200 bg-sky-50/40"><div className="space-y-4"><div className="sk-wizard-steps flex items-center gap-1 overflow-x-auto">{[1, 2, 3, 4].map((step) => <React.Fragment key={step}><div className={`rounded-full px-3 py-1 text-xs font-medium ${wizardStep === step ? 'bg-blue-600 text-white' : wizardStep > step ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>{step}</div>{step < 4 ? <span className="text-slate-300">→</span> : null}</React.Fragment>)}</div>{wizardStep === 1 ? <div className="grid gap-3 md:grid-cols-2"><InputField label={t('orgSchoolName')} value={form.name} onChange={(value) => setForm((current) => ({ ...current, name: value }))} /><SelectField label={t('orgSchoolTypeLabel')} value={form.schoolType} onChange={(value) => setForm((current) => ({ ...current, schoolType: value }))} options={[{ value: 'Kindergarten', label: t('orgSchoolTypeKindergarten') }, { value: 'ElementarySchool', label: t('orgSchoolTypeElementarySchool') }, { value: 'SecondarySchool', label: t('orgSchoolTypeSecondarySchool') }]} /><InputField label={t('orgAddressStreet')} value={form.mainAddress.street} onChange={(value) => setForm((current) => ({ ...current, mainAddress: { ...current.mainAddress, street: value } }))} /><InputField label={t('orgAddressCity')} value={form.mainAddress.city} onChange={(value) => setForm((current) => ({ ...current, mainAddress: { ...current.mainAddress, city: value } }))} /><InputField label={t('orgAddressPostalCode')} value={form.mainAddress.postalCode} onChange={(value) => setForm((current) => ({ ...current, mainAddress: { ...current.mainAddress, postalCode: value } }))} /></div> : null}{wizardStep === 2 ? <div className="grid gap-3 md:grid-cols-2"><InputField label={t('orgSchoolOperatorTitle')} value={form.schoolOperator.legalEntityName} onChange={(value) => setForm((current) => ({ ...current, schoolOperator: { ...current.schoolOperator, legalEntityName: value } }))} /><InputField label={t('orgSchoolOperatorIco')} value={form.schoolOperator.companyNumberIco ?? ''} onChange={(value) => setForm((current) => ({ ...current, schoolOperator: { ...current.schoolOperator, companyNumberIco: value } }))} /></div> : null}{wizardStep === 3 ? <div className="grid gap-3 md:grid-cols-2"><InputField label={t('orgFounderTitle')} value={form.founder.founderName} onChange={(value) => setForm((current) => ({ ...current, founder: { ...current.founder, founderName: value } }))} /><InputField label={t('orgAddressStreet')} value={form.founder.founderAddress.street} onChange={(value) => setForm((current) => ({ ...current, founder: { ...current.founder, founderAddress: { ...current.founder.founderAddress, street: value } } }))} /><InputField label={t('orgAddressCity')} value={form.founder.founderAddress.city} onChange={(value) => setForm((current) => ({ ...current, founder: { ...current.founder, founderAddress: { ...current.founder.founderAddress, city: value } } }))} /><InputField label={t('orgAddressPostalCode')} value={form.founder.founderAddress.postalCode} onChange={(value) => setForm((current) => ({ ...current, founder: { ...current.founder, founderAddress: { ...current.founder.founderAddress, postalCode: value } } }))} /></div> : null}{wizardStep === 4 ? <div className="rounded-xl border border-slate-200 bg-white p-4 text-sm"><p className="font-semibold text-slate-900">{t('orgCreateSchoolWizardReviewTitle')}</p><p className="mt-1 text-slate-600">{form.name}</p></div> : null}<div className="flex flex-wrap items-center justify-between gap-3"><button type="button" className="sk-btn sk-btn-secondary" onClick={onCancel}>{t('orgCreateSchoolWizardReset')}</button><div className="flex flex-wrap gap-2"><button type="button" className="sk-btn sk-btn-secondary" disabled={wizardStep === 1} onClick={() => setWizardStep((step) => Math.max(1, step - 1))}>{t('orgCreateSchoolWizardBack')}</button>{wizardStep < 4 ? <button type="button" className="sk-btn sk-btn-primary" disabled={!stepValid(wizardStep)} onClick={() => setWizardStep((step) => Math.min(4, step + 1))}>{t('orgCreateSchoolWizardNext')}</button> : <button type="button" className="sk-btn sk-btn-primary" disabled={!stepValid(1) || !stepValid(2) || !stepValid(3) || busy} onClick={onSubmit}>{t('orgCreateSchoolWizardCreate')}</button>}</div></div></div></Card>;
}

function InputField({ label, value, onChange, placeholder, type = 'text' }: { label: string; value: string; onChange: (value: string) => void; placeholder?: string; type?: string; }) {
  return <div className="flex flex-col gap-1"><label className="sk-label">{label}</label><input className="sk-input" value={value} type={type} placeholder={placeholder} onChange={(event) => onChange(event.target.value)} /></div>;
}

function SelectField({ label, value, onChange, options }: { label: string; value: string; onChange: (value: string) => void; options: { value: string; label: string }[]; }) {
  return <div className="flex flex-col gap-1"><label className="sk-label">{label}</label><select className="sk-input" value={value} onChange={(event) => onChange(event.target.value)}>{options.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select></div>;
}

function TextAreaField({ label, value, onChange, rows = 4 }: { label: string; value: string; onChange: (value: string) => void; rows?: number; }) {
  return <div className="flex flex-col gap-1"><label className="sk-label">{label}</label><textarea className="sk-input min-h-24" rows={rows} value={value} onChange={(event) => onChange(event.target.value)} /></div>;
}

function CheckboxField({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void; }) {
  return <label className="inline-flex items-center gap-2 text-sm text-slate-700"><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} />{label}</label>;
}

function EditPencilIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="m6 18 1-4 8-8 3 3-8 8-4 1Z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
      <path d="m13.5 7.5 3 3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function CreatePlusIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M12 5v14M5 12h14" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}
