const SCHOOL_TYPE_MAP: Record<string, string> = {
  '1': 'Kindergarten',
  '2': 'ElementarySchool',
  '3': 'SecondarySchool',
  Kindergarten: 'Kindergarten',
  ElementarySchool: 'ElementarySchool',
  SecondarySchool: 'SecondarySchool'
};

const SCHOOL_KIND_MAP: Record<string, string> = {
  '1': 'General',
  '2': 'Specialized',
  General: 'General',
  Specialized: 'Specialized'
};

const PLATFORM_STATUS_MAP: Record<string, string> = {
  '1': 'Draft',
  '2': 'Active',
  '3': 'Suspended',
  '4': 'Archived',
  Draft: 'Draft',
  Active: 'Active',
  Suspended: 'Suspended',
  Archived: 'Archived'
};

function normalize(value: string | number | null | undefined, map: Record<string, string>, fallback: string): string {
  if (value === null || value === undefined || value === '') {
    return fallback;
  }

  return map[String(value)] ?? String(value);
}

export function normalizeSchoolType(value: string | number | null | undefined): string {
  return normalize(value, SCHOOL_TYPE_MAP, 'ElementarySchool');
}

export function normalizeSchoolKind(value: string | number | null | undefined): string {
  return normalize(value, SCHOOL_KIND_MAP, 'General');
}

export function normalizePlatformStatus(value: string | number | null | undefined): string {
  return normalize(value, PLATFORM_STATUS_MAP, 'Active');
}

export function getSchoolTypeLabel(
  t: (key: string, params?: Record<string, string>) => string,
  value: string | number | null | undefined
): string {
  return t(`orgSchoolType${normalizeSchoolType(value)}` as never);
}

export function getSchoolKindLabel(
  t: (key: string, params?: Record<string, string>) => string,
  value: string | number | null | undefined
): string {
  return t(`orgSchoolKind${normalizeSchoolKind(value)}` as never);
}

export function getPlatformStatusLabel(
  t: (key: string, params?: Record<string, string>) => string,
  value: string | number | null | undefined
): string {
  return t(`orgPlatformStatus${normalizePlatformStatus(value)}` as never);
}
