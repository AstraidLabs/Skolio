import React, { useEffect, useMemo, useState } from 'react';
import type { SessionState } from '../shared/auth/session';
import type { createOrganizationApi } from './api';
import { Card, SectionHeader, StatusBadge } from '../shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from '../shared/ui/states';

export function OrganizationParityPage({
  api,
  session
}: {
  api: ReturnType<typeof createOrganizationApi>;
  session: SessionState;
}) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [schools, setSchools] = useState<any[]>([]);
  const [schoolYears, setSchoolYears] = useState<any[]>([]);
  const [gradeLevels, setGradeLevels] = useState<any[]>([]);
  const [classRooms, setClassRooms] = useState<any[]>([]);
  const [teachingGroups, setTeachingGroups] = useState<any[]>([]);
  const [subjects, setSubjects] = useState<any[]>([]);
  const [secondaryFields, setSecondaryFields] = useState<any[]>([]);
  const [teacherAssignments, setTeacherAssignments] = useState<any[]>([]);
  const [studentContext, setStudentContext] = useState<any>(null);

  const [newSchool, setNewSchool] = useState({ name: '', schoolType: session.schoolType });
  const [newSchoolYear, setNewSchoolYear] = useState({ label: '', startDate: '', endDate: '' });
  const [newGradeLevel, setNewGradeLevel] = useState({ level: 1, displayName: '' });
  const [newClassRoom, setNewClassRoom] = useState({ gradeLevelId: '', code: '', displayName: '' });
  const [newGroup, setNewGroup] = useState({ classRoomId: '', name: '', isDailyOperationsGroup: true });
  const [newSubject, setNewSubject] = useState({ code: '', name: '' });
  const [newSecondaryField, setNewSecondaryField] = useState({ code: '', name: '' });
  const [newAssignment, setNewAssignment] = useState({ teacherUserId: '', scope: 'SubjectTeacher', classRoomId: '', teachingGroupId: '', subjectId: '' });

  const isPlatformAdmin = session.roles.includes('PlatformAdministrator');
  const isSchoolAdmin = session.roles.includes('SchoolAdministrator');
  const isTeacher = session.roles.includes('Teacher') && !isSchoolAdmin && !isPlatformAdmin;
  const isStudent = session.roles.includes('Student') && !isSchoolAdmin && !isPlatformAdmin && !isTeacher;
  const canWrite = isPlatformAdmin || isSchoolAdmin;
  const selectedSchoolId = useMemo(() => session.schoolIds[0] ?? schools[0]?.id ?? '', [session.schoolIds, schools]);

  const loadSchoolBoundaries = (schoolId: string) => {
    if (!schoolId) return Promise.resolve();
    return Promise.all([
      api.schoolYears(schoolId).then(setSchoolYears),
      api.gradeLevels(schoolId).then(setGradeLevels),
      api.classRooms(schoolId).then(setClassRooms),
      api.teachingGroups(schoolId).then(setTeachingGroups),
      api.subjects(schoolId).then(setSubjects),
      api.teacherAssignments(schoolId).then(setTeacherAssignments),
      session.schoolType === 'SecondarySchool' ? api.secondaryFieldsOfStudy(schoolId).then(setSecondaryFields) : Promise.resolve()
    ]).then(() => undefined);
  };

  const load = () => {
    setLoading(true);
    setError('');

    if (isStudent) {
      void api.studentContext(session.schoolIds[0] ?? '00000000-0000-0000-0000-000000000000')
        .then((context) => setStudentContext(context))
        .catch((e: Error) => setError(e.message))
        .finally(() => setLoading(false));
      return;
    }

    void api.schools()
      .then(async (allSchools) => {
        setSchools(allSchools);
        const schoolId = session.schoolIds[0] ?? allSchools[0]?.id;
        if (schoolId) {
          await loadSchoolBoundaries(schoolId);
        }
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  };

  useEffect(load, [api, session.accessToken]);

  const createSchool = () => void api.createSchool(newSchool).then(() => {
    setNewSchool({ name: '', schoolType: session.schoolType });
    load();
  }).catch((e: Error) => setError(e.message));
  const createSchoolYear = () => void api.createSchoolYear({ id: '', schoolId: selectedSchoolId, ...newSchoolYear }).then(load).catch((e: Error) => setError(e.message));
  const createGradeLevel = () => void api.createGradeLevel({ id: '', schoolId: selectedSchoolId, ...newGradeLevel }).then(load).catch((e: Error) => setError(e.message));
  const createClassRoom = () => void api.createClassRoom({ id: '', schoolId: selectedSchoolId, ...newClassRoom }).then(load).catch((e: Error) => setError(e.message));
  const createGroup = () => void api.createTeachingGroup({ id: '', schoolId: selectedSchoolId, ...newGroup }).then(load).catch((e: Error) => setError(e.message));
  const createSubject = () => void api.createSubject({ id: '', schoolId: selectedSchoolId, ...newSubject }).then(load).catch((e: Error) => setError(e.message));
  const createSecondaryField = () => void api.createSecondaryFieldOfStudy({ id: '', schoolId: selectedSchoolId, ...newSecondaryField }).then(load).catch((e: Error) => setError(e.message));
  const createAssignment = () => void api.createTeacherAssignment({ id: '', schoolId: selectedSchoolId, ...newAssignment }).then(load).catch((e: Error) => setError(e.message));

  if (loading) return <LoadingState text="Loading organization capabilities..." />;
  if (error) return <ErrorState text={error} />;

  if (isStudent && studentContext) {
    return (
      <section className="space-y-3">
        <SectionHeader title="Student Organization Read Scope" description="Read-only organizational context from backend student-context endpoint." />
        <Card><p className="text-sm">{studentContext.school.name} ({studentContext.school.schoolType})</p></Card>
        <div className="grid gap-3 md:grid-cols-2">
          <Card><p className="font-semibold text-sm">School years</p><ul className="sk-list">{studentContext.schoolYears.map((x: any) => <li className="sk-list-item" key={x.id}>{x.label}</li>)}</ul></Card>
          <Card><p className="font-semibold text-sm">Classes</p><ul className="sk-list">{studentContext.classRooms.map((x: any) => <li className="sk-list-item" key={x.id}>{x.displayName}</li>)}</ul></Card>
          <Card><p className="font-semibold text-sm">Groups</p><ul className="sk-list">{studentContext.teachingGroups.map((x: any) => <li className="sk-list-item" key={x.id}>{x.name}</li>)}</ul></Card>
          <Card><p className="font-semibold text-sm">Subjects</p><ul className="sk-list">{studentContext.subjects.map((x: any) => <li className="sk-list-item" key={x.id}>{x.name}</li>)}</ul></Card>
        </div>
      </section>
    );
  }

  return (
    <section className="space-y-3">
      <SectionHeader title="Organization Parity" description="Frontend coverage for existing organization CRUD and override capabilities." />

      <Card>
        <p className="font-semibold text-sm">Schools</p>
        {schools.length === 0 ? <EmptyState text="No schools available." /> : (
          <ul className="sk-list">
            {schools.map((school) => (
              <li key={school.id} className="sk-list-item">
                <span>{school.name} ({school.schoolType})</span>
                <div className="flex gap-2">
                  <StatusBadge label={school.isActive ? 'Active' : 'Inactive'} tone={school.isActive ? 'good' : 'warn'} />
                  {isPlatformAdmin ? (
                    <button className="sk-btn sk-btn-secondary" onClick={() => void api.setSchoolStatus(school.id, !school.isActive).then(load).catch((e: Error) => setError(e.message))} type="button">
                      {school.isActive ? 'Deactivate' : 'Activate'}
                    </button>
                  ) : null}
                </div>
              </li>
            ))}
          </ul>
        )}
      </Card>

      {canWrite ? (
        <div className="grid gap-3 lg:grid-cols-2">
          <Card>
            <p className="font-semibold text-sm">Create school</p>
            <div className="mt-2 grid gap-2">
              <input className="sk-input" placeholder="School name" value={newSchool.name} onChange={(e) => setNewSchool((v) => ({ ...v, name: e.target.value }))} />
              <select className="sk-input" value={newSchool.schoolType} onChange={(e) => setNewSchool((v) => ({ ...v, schoolType: e.target.value }))}>
                <option value="Kindergarten">Kindergarten</option>
                <option value="ElementarySchool">ElementarySchool</option>
                <option value="SecondarySchool">SecondarySchool</option>
              </select>
            </div>
            <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={createSchool} type="button">Create school</button></div>
          </Card>

          <Card>
            <p className="font-semibold text-sm">Create school year</p>
            <div className="mt-2 grid gap-2">
              <input className="sk-input" placeholder="Label" value={newSchoolYear.label} onChange={(e) => setNewSchoolYear((v) => ({ ...v, label: e.target.value }))} />
              <input className="sk-input" type="date" value={newSchoolYear.startDate} onChange={(e) => setNewSchoolYear((v) => ({ ...v, startDate: e.target.value }))} />
              <input className="sk-input" type="date" value={newSchoolYear.endDate} onChange={(e) => setNewSchoolYear((v) => ({ ...v, endDate: e.target.value }))} />
            </div>
            <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={createSchoolYear} type="button">Create school year</button></div>
          </Card>

          <Card>
            <p className="font-semibold text-sm">Create grade level</p>
            <div className="mt-2 grid gap-2">
              <input className="sk-input" type="number" value={newGradeLevel.level} onChange={(e) => setNewGradeLevel((v) => ({ ...v, level: Number(e.target.value) || 1 }))} />
              <input className="sk-input" placeholder="Display name" value={newGradeLevel.displayName} onChange={(e) => setNewGradeLevel((v) => ({ ...v, displayName: e.target.value }))} />
            </div>
            <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={createGradeLevel} type="button">Create grade level</button></div>
          </Card>

          <Card>
            <p className="font-semibold text-sm">Create class room</p>
            <div className="mt-2 grid gap-2">
              <input className="sk-input" placeholder="Grade level id" value={newClassRoom.gradeLevelId} onChange={(e) => setNewClassRoom((v) => ({ ...v, gradeLevelId: e.target.value }))} />
              <input className="sk-input" placeholder="Code" value={newClassRoom.code} onChange={(e) => setNewClassRoom((v) => ({ ...v, code: e.target.value }))} />
              <input className="sk-input" placeholder="Display name" value={newClassRoom.displayName} onChange={(e) => setNewClassRoom((v) => ({ ...v, displayName: e.target.value }))} />
            </div>
            <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={createClassRoom} type="button">Create class room</button></div>
          </Card>

          <Card>
            <p className="font-semibold text-sm">Create teaching group</p>
            <div className="mt-2 grid gap-2">
              <input className="sk-input" placeholder="Class room id (optional)" value={newGroup.classRoomId} onChange={(e) => setNewGroup((v) => ({ ...v, classRoomId: e.target.value }))} />
              <input className="sk-input" placeholder="Group name" value={newGroup.name} onChange={(e) => setNewGroup((v) => ({ ...v, name: e.target.value }))} />
              <label className="inline-flex items-center gap-2 text-sm"><input type="checkbox" checked={newGroup.isDailyOperationsGroup} onChange={(e) => setNewGroup((v) => ({ ...v, isDailyOperationsGroup: e.target.checked }))} />Daily operations group</label>
            </div>
            <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={createGroup} type="button">Create group</button></div>
          </Card>

          <Card>
            <p className="font-semibold text-sm">Create subject</p>
            <div className="mt-2 grid gap-2">
              <input className="sk-input" placeholder="Code" value={newSubject.code} onChange={(e) => setNewSubject((v) => ({ ...v, code: e.target.value }))} />
              <input className="sk-input" placeholder="Subject name" value={newSubject.name} onChange={(e) => setNewSubject((v) => ({ ...v, name: e.target.value }))} />
            </div>
            <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={createSubject} type="button">Create subject</button></div>
          </Card>

          {session.schoolType === 'SecondarySchool' ? (
            <Card>
              <p className="font-semibold text-sm">Create secondary field of study</p>
              <div className="mt-2 grid gap-2">
                <input className="sk-input" placeholder="Code" value={newSecondaryField.code} onChange={(e) => setNewSecondaryField((v) => ({ ...v, code: e.target.value }))} />
                <input className="sk-input" placeholder="Name" value={newSecondaryField.name} onChange={(e) => setNewSecondaryField((v) => ({ ...v, name: e.target.value }))} />
              </div>
              <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={createSecondaryField} type="button">Create field</button></div>
            </Card>
          ) : null}

          <Card>
            <p className="font-semibold text-sm">Create teacher assignment</p>
            <div className="mt-2 grid gap-2">
              <input className="sk-input" placeholder="Teacher user id" value={newAssignment.teacherUserId} onChange={(e) => setNewAssignment((v) => ({ ...v, teacherUserId: e.target.value }))} />
              <input className="sk-input" placeholder="Scope" value={newAssignment.scope} onChange={(e) => setNewAssignment((v) => ({ ...v, scope: e.target.value }))} />
              <input className="sk-input" placeholder="Class room id" value={newAssignment.classRoomId} onChange={(e) => setNewAssignment((v) => ({ ...v, classRoomId: e.target.value }))} />
              <input className="sk-input" placeholder="Teaching group id" value={newAssignment.teachingGroupId} onChange={(e) => setNewAssignment((v) => ({ ...v, teachingGroupId: e.target.value }))} />
              <input className="sk-input" placeholder="Subject id" value={newAssignment.subjectId} onChange={(e) => setNewAssignment((v) => ({ ...v, subjectId: e.target.value }))} />
            </div>
            <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={createAssignment} type="button">Create assignment</button></div>
          </Card>
        </div>
      ) : null}

      <div className="grid gap-3 lg:grid-cols-2">
        <Card><p className="font-semibold text-sm">School years</p><ul className="sk-list">{schoolYears.map((x) => <li className="sk-list-item" key={x.id}>{x.label}</li>)}</ul></Card>
        <Card><p className="font-semibold text-sm">Grade levels</p><ul className="sk-list">{gradeLevels.map((x) => <li className="sk-list-item" key={x.id}>{x.level} - {x.displayName}</li>)}</ul></Card>
        <Card><p className="font-semibold text-sm">Class rooms</p><ul className="sk-list">{classRooms.map((x) => <li className="sk-list-item" key={x.id}>{x.code} - {x.displayName}</li>)}</ul></Card>
        <Card><p className="font-semibold text-sm">Teaching groups</p><ul className="sk-list">{teachingGroups.map((x) => <li className="sk-list-item" key={x.id}>{x.name}</li>)}</ul></Card>
        <Card><p className="font-semibold text-sm">Subjects</p><ul className="sk-list">{subjects.map((x) => <li className="sk-list-item" key={x.id}>{x.code} - {x.name}</li>)}</ul></Card>
        {session.schoolType === 'SecondarySchool' ? <Card><p className="font-semibold text-sm">Secondary fields</p><ul className="sk-list">{secondaryFields.map((x) => <li className="sk-list-item" key={x.id}>{x.code} - {x.name}</li>)}</ul></Card> : null}
        <Card className="lg:col-span-2"><p className="font-semibold text-sm">Teacher assignments</p><ul className="sk-list">{teacherAssignments.map((x) => <li className="sk-list-item" key={x.id}>{x.teacherUserId} | {x.scope}</li>)}</ul></Card>
      </div>
    </section>
  );
}
