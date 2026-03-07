import type { SchoolType } from '../auth/session';

export type SidebarItem = {
  key: string;
  label: string;
};

export type SidebarSection = {
  key: string;
  label: string;
  items: SidebarItem[];
};

export function buildSidebarSections(
  roles: string[],
  schoolType: SchoolType,
  nav: SidebarItem[]
): SidebarSection[] {
  const hasRole = (role: string) => roles.includes(role);
  const hasNav = (key: string) => nav.some((x) => x.key === key);
  const labelFor = (key: string) => nav.find((x) => x.key === key)?.label ?? key;

  const sections: SidebarSection[] = [];

  const overviewItems: SidebarItem[] = [];
  if (hasNav('/dashboard')) overviewItems.push({ key: '/dashboard', label: labelFor('/dashboard') });
  if (hasNav('/identity')) overviewItems.push({ key: '/identity', label: labelFor('/identity') });
  if (hasNav('/communication')) overviewItems.push({ key: '/communication', label: labelFor('/communication') });
  if (overviewItems.length > 0) sections.push({ key: 'overview', label: 'Overview', items: overviewItems });

  const operationsItems: SidebarItem[] = [];
  if (hasNav('/organization')) operationsItems.push({ key: '/organization', label: labelFor('/organization') });
  if (hasNav('/academics')) operationsItems.push({ key: '/academics', label: labelFor('/academics') });
  if (operationsItems.length > 0) {
    const schoolPriority =
      schoolType === 'Kindergarten'
        ? 'Operations (Groups & Daily Reports)'
        : schoolType === 'SecondarySchool'
          ? 'Operations (Classes, Subjects, Study Context)'
          : 'Operations (Classes & Subjects)';

    sections.push({ key: 'operations', label: schoolPriority, items: operationsItems });
  }

  const adminItems: SidebarItem[] = [];
  if (hasNav('/administration')) adminItems.push({ key: '/administration', label: labelFor('/administration') });
  if (adminItems.length > 0 && (hasRole('PlatformAdministrator') || hasRole('SchoolAdministrator'))) {
    sections.push({ key: 'admin', label: 'Administration', items: adminItems });
  }

  return sections;
}
