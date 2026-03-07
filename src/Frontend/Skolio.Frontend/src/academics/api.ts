import type { createHttpClient } from '../shared/http/httpClient';

export type TimetableEntry = { id: string; schoolId: string; schoolYearId: string; dayOfWeek: string; startTime: string; endTime: string; audienceType: string; audienceId: string; subjectId: string; teacherUserId: string };
export type LessonRecord = { id: string; timetableEntryId: string; lessonDate: string; topic: string; summary: string };
export type Attendance = { id: string; schoolId: string; audienceId: string; studentUserId: string; attendanceDate: string; status: string };
export type ExcuseNote = { id: string; attendanceRecordId: string; parentUserId: string; reason: string; submittedAtUtc: string };
export type Grade = { id: string; studentUserId: string; subjectId: string; gradeValue: string; note: string; gradedOn: string };
export type Homework = { id: string; schoolId: string; audienceId: string; subjectId: string; title: string; instructions: string; dueDate: string };
export type DailyReport = { id: string; schoolId: string; audienceId: string; reportDate: string; summary: string; notes: string };

export function createAcademicsApi(http: ReturnType<typeof createHttpClient>) {
  return {
    timetable: (schoolId: string) => http<TimetableEntry[]>('academics', `/api/academics/timetable?schoolId=${schoolId}`),
    createTimetableEntry: (payload: Omit<TimetableEntry, 'id'>) => http<TimetableEntry>('academics', '/api/academics/timetable', { method: 'POST', body: JSON.stringify(payload) }),
    lessons: (schoolId: string) => http<LessonRecord[]>('academics', `/api/academics/lessons?schoolId=${schoolId}`),
    createLesson: (payload: Omit<LessonRecord, 'id'>) => http<LessonRecord>('academics', '/api/academics/lessons', { method: 'POST', body: JSON.stringify(payload) }),
    overrideLesson: (id: string, payload: Omit<LessonRecord, 'id'> & { overrideReason: string }) => http<LessonRecord>('academics', `/api/academics/lessons/${id}/override`, { method: 'PUT', body: JSON.stringify(payload) }),
    attendance: (schoolId: string) => http<Attendance[]>('academics', `/api/academics/attendance/records?schoolId=${schoolId}`),
    createAttendance: (payload: Omit<Attendance, 'id'>) => http<Attendance>('academics', '/api/academics/attendance/records', { method: 'POST', body: JSON.stringify(payload) }),
    overrideAttendance: (id: string, payload: Omit<Attendance, 'id' | 'schoolId'> & { overrideReason: string }) => http<Attendance>('academics', `/api/academics/attendance/records/${id}/override`, { method: 'PUT', body: JSON.stringify(payload) }),
    createExcuse: (payload: Omit<ExcuseNote, 'id' | 'submittedAtUtc'>) => http<ExcuseNote>('academics', '/api/academics/attendance/excuse-notes', { method: 'POST', body: JSON.stringify(payload) }),
    overrideExcuse: (id: string, payload: { reason: string; submittedAtUtc: string; overrideReason: string }) => http<ExcuseNote>('academics', `/api/academics/attendance/excuse-notes/${id}/override`, { method: 'PUT', body: JSON.stringify(payload) }),
    grades: (studentUserId: string) => http<Grade[]>('academics', `/api/academics/grades?studentUserId=${studentUserId}`),
    createGrade: (payload: Omit<Grade, 'id'>) => http<Grade>('academics', '/api/academics/grades', { method: 'POST', body: JSON.stringify(payload) }),
    overrideGrade: (id: string, payload: Omit<Grade, 'id'> & { overrideReason: string }) => http<Grade>('academics', `/api/academics/grades/${id}/override`, { method: 'PUT', body: JSON.stringify(payload) }),
    homework: (schoolId: string) => http<Homework[]>('academics', `/api/academics/homework?schoolId=${schoolId}`),
    createHomework: (payload: Omit<Homework, 'id'>) => http<Homework>('academics', '/api/academics/homework', { method: 'POST', body: JSON.stringify(payload) }),
    overrideHomework: (id: string, payload: Omit<Homework, 'id' | 'schoolId'> & { overrideReason: string }) => http<Homework>('academics', `/api/academics/homework/${id}/override`, { method: 'PUT', body: JSON.stringify(payload) }),
    dailyReports: (schoolId: string) => http<DailyReport[]>('academics', `/api/academics/daily-reports?schoolId=${schoolId}`),
    createDailyReport: (payload: Omit<DailyReport, 'id'>) => http<DailyReport>('academics', '/api/academics/daily-reports', { method: 'POST', body: JSON.stringify(payload) }),
    overrideDailyReport: (id: string, payload: Omit<DailyReport, 'id' | 'schoolId'> & { overrideReason: string }) => http<DailyReport>('academics', `/api/academics/daily-reports/${id}/override`, { method: 'PUT', body: JSON.stringify(payload) })
  };
}