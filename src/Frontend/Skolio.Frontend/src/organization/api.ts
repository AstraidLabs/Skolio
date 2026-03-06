import type { createHttpClient } from '../shared/http/httpClient';

export type School = { id: string; name: string; schoolType: string };
export type SchoolYear = { id: string; schoolId: string; label: string; startDate: string; endDate: string };
export type ClassRoom = { id: string; schoolId: string; gradeLevelId: string; code: string; displayName: string };
export type TeachingGroup = { id: string; schoolId: string; classRoomId?: string; name: string; isDailyOperationsGroup: boolean };
export type Subject = { id: string; schoolId: string; code: string; name: string };
export type TeacherAssignment = { id: string; schoolId: string; teacherUserId: string; scope: string; classRoomId?: string; teachingGroupId?: string; subjectId?: string };

export function createOrganizationApi(http: ReturnType<typeof createHttpClient>) {
  return {
    schools: () => http<School[]>('organization', '/api/organization/schools'),
    school: (id: string) => http<School>('organization', `/api/organization/schools/${id}`),
    createSchoolYear: (payload: Omit<SchoolYear, 'id'>) => http<SchoolYear>('organization', '/api/organization/school-years', { method: 'POST', body: JSON.stringify(payload) }),
    schoolYears: (schoolId: string) => http<SchoolYear[]>('organization', `/api/organization/school-years?schoolId=${schoolId}`),
    classRooms: (schoolId: string) => http<ClassRoom[]>('organization', `/api/organization/class-rooms?schoolId=${schoolId}`),
    createClassRoom: (payload: Omit<ClassRoom, 'id'>) => http<ClassRoom>('organization', '/api/organization/class-rooms', { method: 'POST', body: JSON.stringify(payload) }),
    teachingGroups: (schoolId: string) => http<TeachingGroup[]>('organization', `/api/organization/teaching-groups?schoolId=${schoolId}`),
    createTeachingGroup: (payload: Omit<TeachingGroup, 'id'>) => http<TeachingGroup>('organization', '/api/organization/teaching-groups', { method: 'POST', body: JSON.stringify(payload) }),
    subjects: (schoolId: string) => http<Subject[]>('organization', `/api/organization/subjects?schoolId=${schoolId}`),
    createSubject: (payload: Omit<Subject, 'id'>) => http<Subject>('organization', '/api/organization/subjects', { method: 'POST', body: JSON.stringify(payload) }),
    teacherAssignments: (schoolId: string) => http<TeacherAssignment[]>('organization', `/api/organization/teacher-assignments?schoolId=${schoolId}`),
    createTeacherAssignment: (payload: Omit<TeacherAssignment, 'id'>) => http<TeacherAssignment>('organization', '/api/organization/teacher-assignments', { method: 'POST', body: JSON.stringify(payload) })
  };
}
