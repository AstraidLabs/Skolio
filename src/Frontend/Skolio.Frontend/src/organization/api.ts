import type { createHttpClient } from '../shared/http/httpClient';
import { normalizePlatformStatus, normalizeSchoolKind, normalizeSchoolType } from './schoolLabels';

export type Address = { street: string; city: string; postalCode: string; country: string };

export type SchoolOperator = {
  id: string;
  legalEntityName: string;
  legalForm: string;
  companyNumberIco?: string;
  redIzo?: string;
  registeredOfficeAddress: Address;
  operatorEmail?: string;
  dataBox?: string;
  resortIdentifier?: string;
  directorSummary?: string;
  statutoryBodySummary?: string;
};

export type Founder = {
  id: string;
  founderType: string;
  founderCategory: string;
  founderName: string;
  founderLegalForm: string;
  founderIco?: string;
  founderAddress: Address;
  founderEmail?: string;
  founderDataBox?: string;
};

export type School = {
  id: string;
  name: string;
  schoolType: string;
  schoolKind: string;
  schoolIzo?: string;
  schoolEmail?: string;
  schoolPhone?: string;
  schoolWebsite?: string;
  mainAddress: Address;
  educationLocationsSummary?: string;
  registryEntryDate?: string;
  educationStartDate?: string;
  maxStudentCapacity?: number;
  teachingLanguage?: string;
  schoolOperatorId?: string;
  founderId?: string;
  platformStatus: string;
  isActive: boolean;
  schoolAdministratorUserProfileId?: string;
  schoolOperator?: SchoolOperator;
  founder?: Founder;
};

export type SchoolMutation = {
  name: string;
  schoolType: string;
  schoolKind: string;
  schoolIzo?: string;
  schoolEmail?: string;
  schoolPhone?: string;
  schoolWebsite?: string;
  mainAddress: Address;
  educationLocationsSummary?: string;
  registryEntryDate?: string;
  educationStartDate?: string;
  maxStudentCapacity?: number;
  teachingLanguage?: string;
  platformStatus: string;
  schoolOperator: {
    legalEntityName: string;
    legalForm: string;
    companyNumberIco?: string;
    redIzo?: string;
    registeredOfficeAddress: Address;
    operatorEmail?: string;
    dataBox?: string;
    resortIdentifier?: string;
    directorSummary?: string;
    statutoryBodySummary?: string;
  };
  founder: {
    founderType: string;
    founderCategory: string;
    founderName: string;
    founderLegalForm: string;
    founderIco?: string;
    founderAddress: Address;
    founderEmail?: string;
    founderDataBox?: string;
  };
};

export type SchoolYear = { id: string; schoolId: string; label: string; startDate: string; endDate: string };
export type GradeLevel = { id: string; schoolId: string; level: number; displayName: string };
export type ClassRoom = { id: string; schoolId: string; gradeLevelId: string; code: string; displayName: string };
export type TeachingGroup = { id: string; schoolId: string; classRoomId?: string; name: string; isDailyOperationsGroup: boolean };
export type Subject = { id: string; schoolId: string; code: string; name: string };
export type SecondaryFieldOfStudy = { id: string; schoolId: string; code: string; name: string };
export type TeacherAssignment = { id: string; schoolId: string; teacherUserId: string; scope: string; classRoomId?: string; teachingGroupId?: string; subjectId?: string };

export type SchoolPlaceOfEducation = {
  id: string;
  schoolId: string;
  name: string;
  address: Address;
  description?: string;
  isPrimary: boolean;
};

export type SchoolCapacity = {
  id: string;
  schoolId: string;
  capacityType: string;
  maxCapacity: number;
  description?: string;
};

export type RoleDefinition = {
  id: string;
  roleCode: string;
  translationKey: string;
  scopeType: string;
  isBootstrapAllowed: boolean;
  isCreateUserFlowAllowed: boolean;
  isUserManagementAllowed: boolean;
  sortOrder: number;
};

export type ChildMatrixEntry = {
  id: string;
  parentScopeMatrixId: string;
  parentSchoolType: string;
  code: string;
  translationKey: string;
};

export type SchoolStructureMatrix = ChildMatrixEntry & {
  usesGradeLevels: boolean;
  usesClasses: boolean;
  usesGroups: boolean;
  groupIsPrimaryStructure: boolean;
};

export type RegistryMatrix = ChildMatrixEntry & {
  requiresIzo: boolean;
  requiresRedIzo: boolean;
  requiresIco: boolean;
  requiresDataBox: boolean;
  requiresFounder: boolean;
  requiresTeachingLanguage: boolean;
};

export type CapacityMatrix = ChildMatrixEntry & {
  capacityType: string;
  isRequired: boolean;
};

export type AcademicStructureMatrix = ChildMatrixEntry & {
  usesSubjects: boolean;
  usesFieldOfStudy: boolean;
  subjectIsClassBound: boolean;
  fieldOfStudyIsRequired: boolean;
};

export type AssignmentMatrix = ChildMatrixEntry & {
  allowsClassRoomAssignment: boolean;
  allowsGroupAssignment: boolean;
  allowsSubjectAssignment: boolean;
  studentRequiresClassPlacement: boolean;
  studentRequiresGroupPlacement: boolean;
};

export type ResolvedChildMatrices = {
  matrixId: string;
  schoolType: string;
  code: string;
  structure?: {
    usesGradeLevels: boolean;
    usesClasses: boolean;
    usesGroups: boolean;
    groupIsPrimaryStructure: boolean;
  } | null;
  registry?: {
    requiresIzo: boolean;
    requiresRedIzo: boolean;
    requiresIco: boolean;
    requiresDataBox: boolean;
    requiresFounder: boolean;
    requiresTeachingLanguage: boolean;
  } | null;
  capacity: { capacityType: string; isRequired: boolean }[];
  academic?: {
    usesSubjects: boolean;
    usesFieldOfStudy: boolean;
    subjectIsClassBound: boolean;
    fieldOfStudyIsRequired: boolean;
  } | null;
  assignment?: {
    allowsClassRoomAssignment: boolean;
    allowsGroupAssignment: boolean;
    allowsSubjectAssignment: boolean;
    studentRequiresClassPlacement: boolean;
    studentRequiresGroupPlacement: boolean;
  } | null;
};

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

function normalizeSchool(school: School): School {
  return {
    ...school,
    schoolType: normalizeSchoolType(school.schoolType),
    schoolKind: normalizeSchoolKind(school.schoolKind),
    platformStatus: normalizePlatformStatus(school.platformStatus)
  };
}

export function createOrganizationApi(http: ReturnType<typeof createHttpClient>) {
  return {
    schools: async (query?: { search?: string; schoolType?: string; isActive?: boolean }) => {
      const params = new URLSearchParams();
      if (query?.search) params.set('search', query.search);
      if (query?.schoolType) params.set('schoolType', query.schoolType);
      if (typeof query?.isActive === 'boolean') params.set('isActive', String(query.isActive));
      const result = await http<PagedResult<School>>('organization', `/api/organization/schools${params.size > 0 ? `?${params.toString()}` : ''}`);
      return result.items.map(normalizeSchool);
    },
    school: async (id: string) => normalizeSchool(await http<School>('organization', `/api/organization/schools/${id}`)),
    createSchool: async (payload: SchoolMutation) => normalizeSchool(await http<School>('organization', '/api/organization/schools', { method: 'POST', body: JSON.stringify(payload) })),
    updateSchool: async (id: string, payload: SchoolMutation) => normalizeSchool(await http<School>('organization', `/api/organization/schools/${id}`, { method: 'PUT', body: JSON.stringify(payload) })),
    setSchoolStatus: async (id: string, isActive: boolean) => normalizeSchool(await http<School>('organization', `/api/organization/schools/${id}/status`, { method: 'PUT', body: JSON.stringify({ isActive }) })),
    assignSchoolAdministrator: async (id: string, userProfileId: string) => normalizeSchool(await http<School>('organization', `/api/organization/schools/${id}/school-administrator`, { method: 'PUT', body: JSON.stringify({ userProfileId }) })),
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
    studentContext: async (schoolId: string) => {
      const context = await http<StudentContext>('organization', `/api/organization/student-context?schoolId=${schoolId}`);
      return {
        ...context,
        school: normalizeSchool(context.school)
      };
    },

    // School Places of Education
    schoolPlaces: (schoolId: string) => http<SchoolPlaceOfEducation[]>('organization', `/api/organization/school-places-of-education?schoolId=${schoolId}`),
    createSchoolPlace: (payload: { schoolId: string; name: string; address: Address; description?: string; isPrimary: boolean }) =>
      http<SchoolPlaceOfEducation>('organization', '/api/organization/school-places-of-education', { method: 'POST', body: JSON.stringify(payload) }),
    updateSchoolPlace: (id: string, payload: { name: string; address: Address; description?: string; isPrimary: boolean }) =>
      http<SchoolPlaceOfEducation>('organization', `/api/organization/school-places-of-education/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),

    // School Capacities
    schoolCapacities: (schoolId: string) => http<SchoolCapacity[]>('organization', `/api/organization/school-capacities?schoolId=${schoolId}`),
    createSchoolCapacity: (payload: { schoolId: string; capacityType: string; maxCapacity: number; description?: string }) =>
      http<SchoolCapacity>('organization', '/api/organization/school-capacities', { method: 'POST', body: JSON.stringify(payload) }),
    updateSchoolCapacity: (id: string, payload: { capacityType: string; maxCapacity: number; description?: string }) =>
      http<SchoolCapacity>('organization', `/api/organization/school-capacities/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),

    // Role Definitions (read-only)
    roleDefinitions: () => http<RoleDefinition[]>('organization', '/api/organization/role-definitions'),

    // Child Matrices (read-only)
    childMatricesStructure: () => http<SchoolStructureMatrix[]>('organization', '/api/organization/child-matrices/school-structure'),
    childMatricesRegistry: () => http<RegistryMatrix[]>('organization', '/api/organization/child-matrices/registry'),
    childMatricesCapacity: () => http<CapacityMatrix[]>('organization', '/api/organization/child-matrices/capacity'),
    childMatricesAcademic: () => http<AcademicStructureMatrix[]>('organization', '/api/organization/child-matrices/academic-structure'),
    childMatricesAssignment: () => http<AssignmentMatrix[]>('organization', '/api/organization/child-matrices/assignment'),
    childMatricesBySchoolType: (schoolType: string) => http<ResolvedChildMatrices>('organization', `/api/organization/child-matrices/by-school-type/${schoolType}`)
  };
}
