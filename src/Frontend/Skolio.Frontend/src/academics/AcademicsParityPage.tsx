import React, { useEffect, useState } from 'react';
import type { SessionState } from '../shared/auth/session';
import type { createAcademicsApi } from './api';
import type { createAdministrationApi } from '../administration/api';
import { Card, SectionHeader, StatusBadge } from '../shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from '../shared/ui/states';

type AcademicsView =
  | 'overview'
  | 'timetable'
  | 'lesson-records'
  | 'attendance'
  | 'excuses'
  | 'grades'
  | 'homework'
  | 'daily-reports';

export function AcademicsParityPage({
  api,
  administrationApi,
  session,
  initialView = 'overview'
}: {
  api: ReturnType<typeof createAcademicsApi>;
  administrationApi: ReturnType<typeof createAdministrationApi>;
  session: SessionState;
  initialView?: AcademicsView;
}) {
  const schoolId = session.schoolIds[0] ?? '00000000-0000-0000-0000-000000000000';
  const studentId = session.roles.includes('Parent') ? (session.linkedStudentIds[0] ?? '') : (session.roles.includes('Student') ? session.subject : '');
  const isPlatformAdmin = session.roles.includes('PlatformAdministrator');
  const isSchoolAdmin = session.roles.includes('SchoolAdministrator');
  const isTeacher = session.roles.includes('Teacher') && !isSchoolAdmin && !isPlatformAdmin;
  const isParent = session.roles.includes('Parent');
  const isStudent = session.roles.includes('Student') && !isTeacher && !isSchoolAdmin && !isPlatformAdmin;
  const canWritePedagogy = isTeacher || isSchoolAdmin || isPlatformAdmin;
  const canParentExcuse = isParent;
  const [activeView, setActiveView] = useState<AcademicsView>(initialView);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [timetable, setTimetable] = useState<any[]>([]);
  const [lessons, setLessons] = useState<any[]>([]);
  const [attendance, setAttendance] = useState<any[]>([]);
  const [excuses, setExcuses] = useState<any[]>([]);
  const [grades, setGrades] = useState<any[]>([]);
  const [homework, setHomework] = useState<any[]>([]);
  const [dailyReports, setDailyReports] = useState<any[]>([]);
  const [overrideAudit, setOverrideAudit] = useState<any[]>([]);

  const [newTimetable, setNewTimetable] = useState({ schoolYearId: '', dayOfWeek: '1', startTime: '08:00', endTime: '08:45', audienceType: 'ClassRoom', audienceId: '', subjectId: '', teacherUserId: '' });
  const [newLesson, setNewLesson] = useState({ timetableEntryId: '', lessonDate: '', topic: '', summary: '' });
  const [newAttendance, setNewAttendance] = useState({ audienceId: '', studentUserId: '', attendanceDate: '', status: 'Present' });
  const [newExcuse, setNewExcuse] = useState({ attendanceRecordId: '', parentUserId: session.subject, reason: '' });
  const [newGrade, setNewGrade] = useState({ studentUserId: studentId, subjectId: '', gradeValue: '', note: '', gradedOn: '' });
  const [newHomework, setNewHomework] = useState({ audienceId: '', subjectId: '', title: '', instructions: '', dueDate: '' });
  const [newReport, setNewReport] = useState({ audienceId: '', reportDate: '', summary: '', notes: '' });

  const load = () => {
    setLoading(true);
    setError('');
    void Promise.all([
      api.timetable(schoolId, isStudent ? session.subject : undefined),
      api.lessons(schoolId, undefined, isStudent ? session.subject : undefined),
      api.attendance(schoolId, undefined, studentId || undefined),
      api.excuses(schoolId, studentId || undefined),
      studentId ? api.grades(schoolId, studentId, newGrade.subjectId || '00000000-0000-0000-0000-000000000000').catch(() => []) : Promise.resolve([]),
      api.homework(schoolId, undefined, studentId || undefined),
      api.dailyReports(schoolId, undefined, studentId || undefined),
      isPlatformAdmin ? administrationApi.auditLogs({ actionCode: 'academics.daily-report.override' }) : Promise.resolve([])
    ])
      .then(([tt, lr, at, ex, gr, hw, dr, oa]) => {
        setTimetable(tt);
        setLessons(lr);
        setAttendance(at);
        setExcuses(ex);
        setGrades(gr);
        setHomework(hw);
        setDailyReports(dr);
        setOverrideAudit(oa);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  };

  useEffect(load, [session.accessToken, newGrade.subjectId]);
  useEffect(() => setActiveView(initialView), [initialView]);

  const onCreateTimetable = () => void api.createTimetableEntry({ id: '', schoolId, ...newTimetable }).then(load).catch((e: Error) => setError(e.message));
  const onCreateLesson = () => void api.createLesson({ id: '', ...newLesson }).then(load).catch((e: Error) => setError(e.message));
  const onCreateAttendance = () => void api.createAttendance({ id: '', schoolId, ...newAttendance }).then(load).catch((e: Error) => setError(e.message));
  const onCreateExcuse = () => void api.createExcuse({ id: '', submittedAtUtc: '', ...newExcuse }).then(load).catch((e: Error) => setError(e.message));
  const onCreateGrade = () => void api.createGrade({ id: '', schoolId, ...newGrade }).then(load).catch((e: Error) => setError(e.message));
  const onCreateHomework = () => void api.createHomework({ id: '', schoolId, ...newHomework }).then(load).catch((e: Error) => setError(e.message));
  const onCreateDailyReport = () => void api.createDailyReport({ id: '', schoolId, ...newReport }).then(load).catch((e: Error) => setError(e.message));

  if (loading) return <LoadingState text="Loading academics capabilities..." />;
  if (error) return <ErrorState text={error} />;

  const show = (view: AcademicsView) => activeView === 'overview' || activeView === view;
  const activeViewTitle = activeView === 'overview'
    ? 'Academics Overview'
    : activeView === 'lesson-records'
      ? 'Lesson Records'
      : activeView === 'daily-reports'
        ? 'Daily Reports'
        : activeView.charAt(0).toUpperCase() + activeView.slice(1);

  return (
    <section className="space-y-3">
      <SectionHeader title={activeViewTitle} description="Frontend flows mapped only to existing academics backend endpoints." action={<button className="sk-btn sk-btn-secondary" onClick={load} type="button">Reload</button>} />

      {(canWritePedagogy || canParentExcuse) ? (
        <div className="grid gap-3 lg:grid-cols-2">
          {canWritePedagogy && show('timetable') ? (
            <>
              <Card>
                <p className="font-semibold text-sm">Create timetable entry</p>
                <div className="mt-2 grid gap-2">
                  <input className="sk-input" placeholder="School year id" value={newTimetable.schoolYearId} onChange={(e) => setNewTimetable((v) => ({ ...v, schoolYearId: e.target.value }))} />
                  <input className="sk-input" placeholder="Day of week (1-7)" value={newTimetable.dayOfWeek} onChange={(e) => setNewTimetable((v) => ({ ...v, dayOfWeek: e.target.value }))} />
                  <input className="sk-input" placeholder="Audience id" value={newTimetable.audienceId} onChange={(e) => setNewTimetable((v) => ({ ...v, audienceId: e.target.value }))} />
                  <input className="sk-input" placeholder="Subject id" value={newTimetable.subjectId} onChange={(e) => setNewTimetable((v) => ({ ...v, subjectId: e.target.value }))} />
                  <input className="sk-input" placeholder="Teacher user id" value={newTimetable.teacherUserId} onChange={(e) => setNewTimetable((v) => ({ ...v, teacherUserId: e.target.value }))} />
                </div>
                <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={onCreateTimetable} type="button">Create timetable</button></div>
              </Card>
            </>
          ) : null}

          {canWritePedagogy && show('lesson-records') ? (
            <>
              <Card>
                <p className="font-semibold text-sm">Create lesson record</p>
                <div className="mt-2 grid gap-2">
                  <input className="sk-input" placeholder="Timetable entry id" value={newLesson.timetableEntryId} onChange={(e) => setNewLesson((v) => ({ ...v, timetableEntryId: e.target.value }))} />
                  <input className="sk-input" type="date" value={newLesson.lessonDate} onChange={(e) => setNewLesson((v) => ({ ...v, lessonDate: e.target.value }))} />
                  <input className="sk-input" placeholder="Topic" value={newLesson.topic} onChange={(e) => setNewLesson((v) => ({ ...v, topic: e.target.value }))} />
                  <input className="sk-input" placeholder="Summary" value={newLesson.summary} onChange={(e) => setNewLesson((v) => ({ ...v, summary: e.target.value }))} />
                </div>
                <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={onCreateLesson} type="button">Create lesson</button></div>
              </Card>
            </>
          ) : null}

          {canWritePedagogy && show('attendance') ? (
            <>
              <Card>
                <p className="font-semibold text-sm">Create attendance</p>
                <div className="mt-2 grid gap-2">
                  <input className="sk-input" placeholder="Audience id" value={newAttendance.audienceId} onChange={(e) => setNewAttendance((v) => ({ ...v, audienceId: e.target.value }))} />
                  <input className="sk-input" placeholder="Student user id" value={newAttendance.studentUserId} onChange={(e) => setNewAttendance((v) => ({ ...v, studentUserId: e.target.value }))} />
                  <input className="sk-input" type="date" value={newAttendance.attendanceDate} onChange={(e) => setNewAttendance((v) => ({ ...v, attendanceDate: e.target.value }))} />
                  <input className="sk-input" placeholder="Status" value={newAttendance.status} onChange={(e) => setNewAttendance((v) => ({ ...v, status: e.target.value }))} />
                </div>
                <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={onCreateAttendance} type="button">Create attendance</button></div>
              </Card>
            </>
          ) : null}

          {canWritePedagogy && show('grades') ? (
            <>
              <Card>
                <p className="font-semibold text-sm">Create grade entry</p>
                <div className="mt-2 grid gap-2">
                  <input className="sk-input" placeholder="Student user id" value={newGrade.studentUserId} onChange={(e) => setNewGrade((v) => ({ ...v, studentUserId: e.target.value }))} />
                  <input className="sk-input" placeholder="Subject id" value={newGrade.subjectId} onChange={(e) => setNewGrade((v) => ({ ...v, subjectId: e.target.value }))} />
                  <input className="sk-input" placeholder="Grade value" value={newGrade.gradeValue} onChange={(e) => setNewGrade((v) => ({ ...v, gradeValue: e.target.value }))} />
                  <input className="sk-input" placeholder="Note" value={newGrade.note} onChange={(e) => setNewGrade((v) => ({ ...v, note: e.target.value }))} />
                  <input className="sk-input" type="date" value={newGrade.gradedOn} onChange={(e) => setNewGrade((v) => ({ ...v, gradedOn: e.target.value }))} />
                </div>
                <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={onCreateGrade} type="button">Create grade</button></div>
              </Card>
            </>
          ) : null}

          {canWritePedagogy && show('homework') ? (
            <>
              <Card>
                <p className="font-semibold text-sm">Create homework</p>
                <div className="mt-2 grid gap-2">
                  <input className="sk-input" placeholder="Audience id" value={newHomework.audienceId} onChange={(e) => setNewHomework((v) => ({ ...v, audienceId: e.target.value }))} />
                  <input className="sk-input" placeholder="Subject id" value={newHomework.subjectId} onChange={(e) => setNewHomework((v) => ({ ...v, subjectId: e.target.value }))} />
                  <input className="sk-input" placeholder="Title" value={newHomework.title} onChange={(e) => setNewHomework((v) => ({ ...v, title: e.target.value }))} />
                  <input className="sk-input" placeholder="Instructions" value={newHomework.instructions} onChange={(e) => setNewHomework((v) => ({ ...v, instructions: e.target.value }))} />
                  <input className="sk-input" type="date" value={newHomework.dueDate} onChange={(e) => setNewHomework((v) => ({ ...v, dueDate: e.target.value }))} />
                </div>
                <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={onCreateHomework} type="button">Create homework</button></div>
              </Card>
            </>
          ) : null}

          {canWritePedagogy && show('daily-reports') ? (
            <>
              <Card>
                <p className="font-semibold text-sm">Create daily report</p>
                <div className="mt-2 grid gap-2">
                  <input className="sk-input" placeholder="Audience id" value={newReport.audienceId} onChange={(e) => setNewReport((v) => ({ ...v, audienceId: e.target.value }))} />
                  <input className="sk-input" type="date" value={newReport.reportDate} onChange={(e) => setNewReport((v) => ({ ...v, reportDate: e.target.value }))} />
                  <input className="sk-input" placeholder="Summary" value={newReport.summary} onChange={(e) => setNewReport((v) => ({ ...v, summary: e.target.value }))} />
                  <input className="sk-input" placeholder="Notes" value={newReport.notes} onChange={(e) => setNewReport((v) => ({ ...v, notes: e.target.value }))} />
                </div>
                <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={onCreateDailyReport} type="button">Create daily report</button></div>
              </Card>
            </>
          ) : null}

          {canParentExcuse && show('excuses') ? (
            <Card>
              <p className="font-semibold text-sm">Submit excuse request</p>
              <div className="mt-2 grid gap-2">
                <input className="sk-input" placeholder="Attendance record id" value={newExcuse.attendanceRecordId} onChange={(e) => setNewExcuse((v) => ({ ...v, attendanceRecordId: e.target.value }))} />
                <input className="sk-input" placeholder="Reason" value={newExcuse.reason} onChange={(e) => setNewExcuse((v) => ({ ...v, reason: e.target.value }))} />
              </div>
              <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={onCreateExcuse} type="button">Create excuse</button></div>
            </Card>
          ) : null}
        </div>
      ) : null}

      <div className="grid gap-3 lg:grid-cols-2">
        {show('timetable') ? <Card><p className="font-semibold text-sm">Timetable</p>{timetable.length === 0 ? <EmptyState text="No timetable entries in scope." /> : <ul className="sk-list">{timetable.map((x) => <li className="sk-list-item" key={x.id}>{x.dayOfWeek} {x.startTime}-{x.endTime}</li>)}</ul>}</Card> : null}
        {show('lesson-records') ? <Card><p className="font-semibold text-sm">Lesson records</p>{lessons.length === 0 ? <EmptyState text="No lesson records in scope." /> : <ul className="sk-list">{lessons.map((x) => <li className="sk-list-item" key={x.id}>{x.lessonDate} | {x.topic}</li>)}</ul>}</Card> : null}
        {show('attendance') ? <Card><p className="font-semibold text-sm">Attendance</p>{attendance.length === 0 ? <EmptyState text="No attendance records in scope." /> : <ul className="sk-list">{attendance.map((x) => <li className="sk-list-item" key={x.id}><span>{x.attendanceDate} | {x.studentUserId}</span><StatusBadge label={x.status} tone={x.status === 'Present' ? 'good' : 'warn'} /></li>)}</ul>}</Card> : null}
        {show('excuses') ? <Card><p className="font-semibold text-sm">Excuses</p>{excuses.length === 0 ? <EmptyState text="No excuse records in scope." /> : <ul className="sk-list">{excuses.map((x) => <li className="sk-list-item" key={x.id}><span>{x.reason}</span>{canParentExcuse ? <button className="sk-btn sk-btn-secondary" type="button" onClick={() => void api.cancelExcuse(x.id).then(load).catch((e: Error) => setError(e.message))}>Cancel</button> : <StatusBadge label="Read" tone="info" />}</li>)}</ul>}</Card> : null}
        {show('grades') ? <Card><p className="font-semibold text-sm">Grades</p>{grades.length === 0 ? <EmptyState text="No grades in scope." /> : <ul className="sk-list">{grades.map((x) => <li className="sk-list-item" key={x.id}>{x.gradedOn} | {x.gradeValue}</li>)}</ul>}</Card> : null}
        {show('homework') ? <Card><p className="font-semibold text-sm">Homework</p>{homework.length === 0 ? <EmptyState text="No homework in scope." /> : <ul className="sk-list">{homework.map((x) => <li className="sk-list-item" key={x.id}>{x.title}</li>)}</ul>}</Card> : null}
        {show('daily-reports') ? <Card className="lg:col-span-2"><p className="font-semibold text-sm">Daily reports</p>{dailyReports.length === 0 ? <EmptyState text="No daily reports in scope." /> : <ul className="sk-list">{dailyReports.map((x) => <li className="sk-list-item" key={x.id}>{x.reportDate} | {x.summary}</li>)}</ul>}</Card> : null}
      </div>

      {isPlatformAdmin ? (
        <Card>
          <p className="font-semibold text-sm">Override audit summary</p>
          {overrideAudit.length === 0 ? <EmptyState text="No override audit entries." /> : (
            <ul className="sk-list">{overrideAudit.map((x) => <li className="sk-list-item" key={x.id}>{x.actionCode}</li>)}</ul>
          )}
        </Card>
      ) : null}
    </section>
  );
}
