import React, { useEffect, useMemo, useState } from 'react';
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
  const [activeView, setActiveView] = useState<OrganizationView>(initialView);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

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

  const currentSchool = schools.find((x) => x.id === activeSchoolId) ?? schools[0] ?? null;

  const guarded = (action: () => Promise<unknown>) => {
    setError('');
    void action().then(() => load()).catch((e: Error) => setError(e.message));
  };

  const goTo = (route: string) => {
    window.history.pushState({}, '', route);
    window.dispatchEvent(new PopStateEvent('popstate'));
  };

  const createSchool = () => guarded(async () => {
    await api.createSchool(newSchool);
    setNewSchool({ ...EMPTY_SCHOOL, schoolType: session.schoolType });
  });

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
      { label: 'Teacher Assignments', description: 'Organizační přiřazení učitelů.', count: teacherAssignments.length, route: '/organization/teacher-assignments' }
    ];

    if (session.schoolType === 'SecondarySchool') {
      return [entries[0], entries[1], entries[2], entries[3], entries[4], entries[5], entries[6].emphasize ? entries[6] : { ...entries[6], emphasize: true }];
    }

    return entries;
  }, [isParent, isStudent, schools.length, schoolYears.length, gradeLevels.length, classRooms.length, teachingGroups.length, subjects.length, teacherAssignments.length, session.schoolType]);

  const contextSwitcherBlock = (showHelperText: boolean) => {
    if (!canSwitchSchoolContext && !showReadOnlySchoolContext) return null;

    return (
      <Card>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <p className="text-sm font-semibold text-slate-900">Aktivní školní kontext</p>
            <p className="mt-1 text-xs text-slate-600">
              {canSwitchSchoolContext
                ? 'Přepnutí kontextu aktualizuje organizační přehledy, seznamy a akce v Organization části.'
                : 'K dispozici je pouze jeden školní kontext v tomto organizačním rozsahu.'}
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
                <p className="text-xs text-slate-500">{currentSchool?.schoolType ?? '-'}</p>
              </div>
            )}
          </div>
        </div>
        {showHelperText ? (
          <div className="mt-3 text-xs text-slate-500">
            Přepínač je page-level pro Organization boundary a nenahrazuje sidebar navigaci.
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
        <SectionHeader title="Organization" description="Read-only student context." />
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
      <section className="space-y-3">
        <SectionHeader title="Školy" description="Entry point pro schools list/detail flow." />
        {contextSwitcherBlock(false)}
        <Card>
          <p className="font-semibold text-sm">Seznam škol</p>
          {schools.length === 0 ? <EmptyState text="No schools available." /> : (
            <ul className="sk-list">
              {schools.map((school) => (
                <li key={school.id} className="sk-list-item">
                  <button className="text-left" type="button" onClick={() => setActiveSchoolId(school.id)}>
                    {school.name} ({school.schoolType})
                  </button>
                  <div className="flex items-center gap-2">
                    <StatusBadge label={school.isActive ? 'Active' : 'Inactive'} tone={school.isActive ? 'good' : 'warn'} />
                    {isPlatformAdmin ? (
                      <button
                        className="sk-btn sk-btn-secondary"
                        onClick={() => guarded(() => api.setSchoolStatus(school.id, !school.isActive))}
                        type="button"
                      >
                        {school.isActive ? 'Deactivate' : 'Activate'}
                      </button>
                    ) : null}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </Card>
        {currentSchool ? (
          <OrganizationSchoolIdentityCard
            school={currentSchool}
            editable={canWriteSchoolContext}
            onSave={(schoolId, payload) => api.updateSchool(schoolId, payload).then(() => load())}
          />
        ) : null}
        {canCreateSchool ? (
          <Card>
            <p className="font-semibold text-sm">Vytvořit školu</p>
            <div className="mt-2 grid gap-2 md:grid-cols-3">
              <input className="sk-input" placeholder="School name" value={newSchool.name} onChange={(e) => setNewSchool((v) => ({ ...v, name: e.target.value }))} />
              <select className="sk-input" value={newSchool.schoolType} onChange={(e) => setNewSchool((v) => ({ ...v, schoolType: e.target.value }))}>
                <option value="Kindergarten">Kindergarten</option>
                <option value="ElementarySchool">ElementarySchool</option>
                <option value="SecondarySchool">SecondarySchool</option>
              </select>
              <input className="sk-input" placeholder="Main street" value={newSchool.mainAddress.street} onChange={(e) => setNewSchool((v) => ({ ...v, mainAddress: { ...v.mainAddress, street: e.target.value } }))} />
              <input className="sk-input" placeholder="Main city" value={newSchool.mainAddress.city} onChange={(e) => setNewSchool((v) => ({ ...v, mainAddress: { ...v.mainAddress, city: e.target.value } }))} />
              <input className="sk-input" placeholder="Main postal code" value={newSchool.mainAddress.postalCode} onChange={(e) => setNewSchool((v) => ({ ...v, mainAddress: { ...v.mainAddress, postalCode: e.target.value } }))} />
              <input className="sk-input" placeholder="School operator legal entity" value={newSchool.schoolOperator.legalEntityName} onChange={(e) => setNewSchool((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, legalEntityName: e.target.value } }))} />
              <input className="sk-input" placeholder="Founder name" value={newSchool.founder.founderName} onChange={(e) => setNewSchool((v) => ({ ...v, founder: { ...v.founder, founderName: e.target.value } }))} />
            </div>
            <div className="mt-3">
              <button className="sk-btn sk-btn-primary" disabled={!newSchool.name.trim() || !newSchool.mainAddress.street.trim() || !newSchool.mainAddress.city.trim() || !newSchool.mainAddress.postalCode.trim() || !newSchool.schoolOperator.legalEntityName.trim() || !newSchool.founder.founderName.trim()} onClick={createSchool} type="button">Create school</button>
            </div>
          </Card>
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
    <div className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
      <p className="text-xs uppercase tracking-wide text-slate-500">{label}</p>
      <p className="mt-1 text-xl font-semibold text-slate-900">{value}</p>
    </div>
  );
}
