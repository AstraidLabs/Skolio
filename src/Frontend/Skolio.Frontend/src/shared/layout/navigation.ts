import type { SchoolType } from '../auth/session';

export type SidebarItem = {
  key: string;
  label: string;
};

export type SidebarLeaf = SidebarItem & {
  route: string;
};

export type SidebarNode = {
  key: string;
  label: string;
  route?: string;
  children?: SidebarLeaf[];
};

export type SidebarSection = {
  key: string;
  label: string;
  nodes: SidebarNode[];
};

export function buildSidebarSections(
  roles: string[],
  schoolType: SchoolType,
  nav: SidebarItem[]
): SidebarSection[] {
  const hasRole = (role: string) => roles.includes(role);
  const hasNav = (key: string) => nav.some((x) => x.key === key);
  const labelFor = (key: string) => nav.find((x) => x.key === key)?.label ?? key;
  const isParentOrStudent = hasRole('Parent') || hasRole('Student');
  const isTeacherOnly = hasRole('Teacher') && !hasRole('SchoolAdministrator') && !hasRole('PlatformAdministrator');

  const sections: SidebarSection[] = [];

  const overviewNodes: SidebarNode[] = [];
  if (hasNav('/dashboard')) overviewNodes.push({ key: '/dashboard', label: labelFor('/dashboard'), route: '/dashboard' });
  if (hasNav('/communication')) overviewNodes.push({ key: '/communication', label: labelFor('/communication'), route: '/communication' });
  if (hasNav('/administration') && (hasRole('PlatformAdministrator') || hasRole('SchoolAdministrator'))) {
    overviewNodes.push({ key: '/administration', label: labelFor('/administration'), route: '/administration' });
  }
  if (overviewNodes.length > 0) sections.push({ key: 'overview', label: 'Overview', nodes: overviewNodes });

  const operationsNodes: SidebarNode[] = [];
  if (!isParentOrStudent) {
    const organizationChildren: SidebarLeaf[] = [];
    if (hasNav('/organization/schools')) organizationChildren.push({ key: '/organization/schools', label: 'Schools', route: '/organization/schools' });
    if (hasNav('/organization/school-years')) organizationChildren.push({ key: '/organization/school-years', label: 'School Years', route: '/organization/school-years' });
    if (hasNav('/organization/grade-levels')) organizationChildren.push({ key: '/organization/grade-levels', label: 'Grade Levels', route: '/organization/grade-levels' });
    if (hasNav('/organization/classes')) organizationChildren.push({ key: '/organization/classes', label: 'Classes', route: '/organization/classes' });
    if (hasNav('/organization/groups')) organizationChildren.push({ key: '/organization/groups', label: 'Groups', route: '/organization/groups' });
    if (hasNav('/organization/subjects')) organizationChildren.push({ key: '/organization/subjects', label: 'Subjects', route: '/organization/subjects' });
    if (hasNav('/organization/fields-of-study') && schoolType === 'SecondarySchool') {
      organizationChildren.push({ key: '/organization/fields-of-study', label: 'Fields of Study', route: '/organization/fields-of-study' });
    }
    if (hasNav('/organization/teacher-assignments')) {
      organizationChildren.push({ key: '/organization/teacher-assignments', label: 'Teacher Assignments', route: '/organization/teacher-assignments' });
    }

    if (hasNav('/organization') || organizationChildren.length > 0) {
      operationsNodes.push({
        key: '/organization',
        label: 'Organization',
        route: hasNav('/organization') ? '/organization' : undefined,
        children: organizationChildren
      });
    }
  }

  const academicsChildren: SidebarLeaf[] = [];
  if (hasNav('/academics/timetable')) academicsChildren.push({ key: '/academics/timetable', label: 'Timetable', route: '/academics/timetable' });
  if (hasNav('/academics/lesson-records')) academicsChildren.push({ key: '/academics/lesson-records', label: 'Lesson Records', route: '/academics/lesson-records' });
  if (hasNav('/academics/attendance')) academicsChildren.push({ key: '/academics/attendance', label: 'Attendance', route: '/academics/attendance' });
  if (hasNav('/academics/excuses')) academicsChildren.push({ key: '/academics/excuses', label: 'Excuses', route: '/academics/excuses' });
  if (hasNav('/academics/grades')) academicsChildren.push({ key: '/academics/grades', label: 'Grades', route: '/academics/grades' });
  if (hasNav('/academics/homework')) academicsChildren.push({ key: '/academics/homework', label: 'Homework', route: '/academics/homework' });
  if (hasNav('/academics/daily-reports')) academicsChildren.push({ key: '/academics/daily-reports', label: 'Daily Reports', route: '/academics/daily-reports' });
  if (!isTeacherOnly && hasNav('/academics') && academicsChildren.length === 0) {
    academicsChildren.push({ key: '/academics', label: labelFor('/academics'), route: '/academics' });
  }

  if (hasNav('/academics') || academicsChildren.length > 0) {
    operationsNodes.push({
      key: '/academics',
      label: 'Academics',
      route: hasNav('/academics') ? '/academics' : undefined,
      children: academicsChildren
    });
  }

  if (operationsNodes.length > 0) {
    const schoolPriority =
      schoolType === 'Kindergarten'
        ? 'Operations (Groups & Daily Reports)'
        : schoolType === 'SecondarySchool'
          ? 'Operations (Classes, Subjects, Study Context)'
          : 'Operations (Classes & Subjects)';

    sections.push({ key: 'operations', label: schoolPriority, nodes: operationsNodes });
  }

  return sections;
}
