import type { createHttpClient } from '../shared/http/httpClient';

export type School = { id: string; name: string; schoolType: string; isActive: boolean; schoolAdministratorUserProfileId?: string };
export type SchoolYear = { id: string; schoolId: string; label: string; startDate: string; endDate: string };
export type GradeLevel = { id: string; schoolId: string; level: number; displayName: string };
export type ClassRoom = { id: string; schoolId: string; gradeLevelId: string; code: string; displayName: string };
export type TeachingGroup = { id: string; schoolId: string; classRoomId?: string; name: string; isDailyOperationsGroup: boolean };
export type Subject = { id: string; schoolId: string; code: string; name: string };
export type SecondaryFieldOfStudy = { id: string; schoolId: string; code: string; name: string };
export type TeacherAssignment = { id: string; schoolId: string; teacherUserId: string; scope: string; classRoomId?: string; teachingGroupId?: string; subjectId?: string };
export type StudentContext = {
  school: School;
  schoolYears: SchoolYear[];
  classRooms: ClassRoom[];
  teachingGroups: TeachingGroup[];
  subjects: Subject[];
  gradeLevels: GradeLevel[];
  secondaryFieldsOfStudy: SecondaryFieldOfStudy[];
};

type PagedResult<T> = { items: T[]; pageNumber: number; pageSize: number; totalCount: number };

export function createOrganizationApi(http: ReturnType<typeof createHttpClient>) {
  return {
    schools: async (query?: { search?: string; schoolType?: string; isActive?: boolean }) => {
      const params = new URLSearchParams();
      if (query?.search) params.set('search', query.search);
      if (query?.schoolType) params.set('schoolType', query.schoolType);
      if (typeof query?.isActive === 'boolean') params.set('isActive', String(query.isActive));
      const result = await http<PagedResult<School>>('organization', `/api/organization/schools${params.size > 0 ? `?${params.toString()}` : ''}`);
      return result.items;
    },
    school: (id: string) => http<School>('organization', `/api/organization/schools/${id}`),
    createSchool: (payload: { name: string; schoolType: string }) => http<School>('organization', '/api/organization/schools', { method: 'POST', body: JSON.stringify(payload) }),
    updateSchool: (id: string, payload: { name: string; schoolType: string }) => http<School>('organization', `/api/organization/schools/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    setSchoolStatus: (id: string, isActive: boolean) => http<School>('organization', `/api/organization/schools/${id}/status`, { method: 'PUT', body: JSON.stringify({ isActive }) }),
    assignSchoolAdministrator: (id: string, userProfileId: string) => http<School>('organization', `/api/organization/schools/${id}/school-administrator`, { method: 'PUT', body: JSON.stringify({ userProfileId }) }),
    createInitialSchoolYear: (id: string, payload: { label: string; startDate: string; endDate: string }) => http<SchoolYear>('organization', `/api/organization/schools/${id}/initial-school-year`, { method: 'POST', body: JSON.stringify(payload) }),
    createSchoolYear: (payload: Omit<SchoolYear, 'id'>) => http<SchoolYear>('organization', '/api/organization/school-years', { method: 'POST', body: JSON.stringify(payload) }),
    updateSchoolYear: (id: string, payload: { startDate: string; endDate: string }) => http<SchoolYear>('organization', `/api/organization/school-years/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    schoolYears: (schoolId: string) => http<SchoolYear[]>('organization', `/api/organization/school-years?schoolId=${schoolId}`),
    gradeLevels: (schoolId: string) => http<GradeLevel[]>('organization', `/api/organization/grade-levels?schoolId=${schoolId}`),
    createGradeLevel: (payload: Omit<GradeLevel, 'id'>) => http<GradeLevel>('organization', '/api/organization/grade-levels', { method: 'POST', body: JSON.stringify(payload) }),
    updateGradeLevel: (id: string, payload: { level: number; displayName: string }) => http<GradeLevel>('organization', `/api/organization/grade-levels/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    classRooms: (schoolId: string) => http<ClassRoom[]>('organization', `/api/organization/class-rooms?schoolId=${schoolId}`),
    createClassRoom: (payload: Omit<ClassRoom, 'id'>) => http<ClassRoom>('organization', '/api/organization/class-rooms', { method: 'POST', body: JSON.stringify(payload) }),
    overrideClassRoom: (id: string, payload: { code: string; displayName: string; overrideReason: string }) => http<ClassRoom>('organization', `/api/organization/class-rooms/${id}/override`, { method: 'PUT', body: JSON.stringify(payload) }),
    teachingGroups: (schoolId: string) => http<TeachingGroup[]>('organization', `/api/organization/teaching-groups?schoolId=${schoolId}`),
    createTeachingGroup: (payload: Omit<TeachingGroup, 'id'>) => http<TeachingGroup>('organization', '/api/organization/teaching-groups', { method: 'POST', body: JSON.stringify(payload) }),
    overrideTeachingGroup: (id: string, payload: { classRoomId?: string; name: string; isDailyOperationsGroup: boolean; overrideReason: string }) => http<TeachingGroup>('organization', `/api/organization/teaching-groups/${id}/override`, { method: 'PUT', body: JSON.stringify(payload) }),
    subjects: async (schoolId: string) => {
      const result = await http<PagedResult<Subject>>('organization', `/api/organization/subjects?schoolId=${schoolId}`);
      return result.items;
    },
    createSubject: (payload: Omit<Subject, 'id'>) => http<Subject>('organization', '/api/organization/subjects', { method: 'POST', body: JSON.stringify(payload) }),
    overrideSubject: (id: string, payload: { code: string; name: string; overrideReason: string }) => http<Subject>('organization', `/api/organization/subjects/${id}/override`, { method: 'PUT', body: JSON.stringify(payload) }),
    secondaryFieldsOfStudy: (schoolId: string) => http<SecondaryFieldOfStudy[]>('organization', `/api/organization/secondary-fields-of-study?schoolId=${schoolId}`),
    createSecondaryFieldOfStudy: (payload: Omit<SecondaryFieldOfStudy, 'id'>) => http<SecondaryFieldOfStudy>('organization', '/api/organization/secondary-fields-of-study', { method: 'POST', body: JSON.stringify(payload) }),
    updateSecondaryFieldOfStudy: (id: string, payload: { code: string; name: string }) => http<SecondaryFieldOfStudy>('organization', `/api/organization/secondary-fields-of-study/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    teacherAssignments: (schoolId: string, teacherUserId?: string) => http<TeacherAssignment[]>('organization', `/api/organization/teacher-assignments?schoolId=${schoolId}${teacherUserId ? `&teacherUserId=${teacherUserId}` : ''}`),
    myTeacherAssignments: (schoolId: string) => http<TeacherAssignment[]>('organization', `/api/organization/teacher-assignments/me?schoolId=${schoolId}`),
    createTeacherAssignment: (payload: Omit<TeacherAssignment, 'id'>) => http<TeacherAssignment>('organization', '/api/organization/teacher-assignments', { method: 'POST', body: JSON.stringify(payload) }),
    overrideTeacherAssignment: (payload: { existingAssignmentId?: string; schoolId: string; teacherUserId: string; scope: string; classRoomId?: string; teachingGroupId?: string; subjectId?: string; overrideReason: string }) => http<TeacherAssignment>('organization', '/api/organization/teacher-assignments/override/reassign', { method: 'POST', body: JSON.stringify(payload) }),
    studentContext: (schoolId: string) => http<StudentContext>('organization', `/api/organization/student-context?schoolId=${schoolId}`)
  };
}
