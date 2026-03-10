import React from 'react';

export function iconForNav(key: string) {
  // Specific child routes first (more specific matches before prefix matches)
  if (key === '/dashboard') return <DashboardIcon />;

  // Organization children
  if (key === '/organization/schools') return <SchoolBuildingIcon />;
  if (key === '/organization/school-years') return <CalendarIcon />;
  if (key === '/organization/grade-levels') return <LayersIcon />;
  if (key === '/organization/classes') return <UsersGroupIcon />;
  if (key === '/organization/groups') return <GroupCircleIcon />;
  if (key === '/organization/subjects') return <BookOpenIcon />;
  if (key === '/organization/fields-of-study') return <GraduationCapIcon />;
  if (key === '/organization/teacher-assignments') return <ClipboardCheckIcon />;

  // Academics children
  if (key === '/academics/timetable') return <ClockIcon />;
  if (key === '/academics/lesson-records') return <FileTextIcon />;
  if (key === '/academics/attendance') return <CheckListIcon />;
  if (key === '/academics/excuses') return <MessageSquareIcon />;
  if (key === '/academics/grades') return <StarIcon />;
  if (key === '/academics/homework') return <PencilIcon />;
  if (key === '/academics/daily-reports') return <FileReportIcon />;

  // Administration children
  if (key === '/administration/user-management') return <UsersSettingsIcon />;

  // Parent-level prefixes
  if (key.includes('/organization')) return <BuildingIcon />;
  if (key.includes('/academics')) return <BookIcon />;
  if (key === '/communication') return <ChatIcon />;
  if (key.includes('/administration')) return <ShieldIcon />;
  if (key === '/identity') return <UserIcon />;
  if (key === '/identity/security') return <LockIcon />;

  return <DotIcon />;
}

export function ChevronIcon({ open }: { open: boolean }) {
  return (
    <span className="inline-flex h-4 w-4 items-center justify-center text-slate-400" aria-hidden="true">
      <svg
        viewBox="0 0 24 24"
        className="h-3 w-3 transition-transform duration-150"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.2"
        strokeLinecap="round"
        strokeLinejoin="round"
        style={{ transform: open ? 'rotate(180deg)' : 'rotate(0deg)' }}
      >
        <path d="M6 9l6 6 6-6" />
      </svg>
    </span>
  );
}

function IconWrap({ children }: { children: React.ReactNode }) {
  return (
    <span className="inline-flex h-4 w-4 shrink-0 items-center justify-center text-slate-500" aria-hidden="true">
      <svg viewBox="0 0 24 24" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        {children}
      </svg>
    </span>
  );
}

// === Parent-level icons ===
function DashboardIcon() { return <IconWrap><path d="M4 4h7v7H4zM13 4h7v5h-7zM13 11h7v9h-7zM4 13h7v7H4z" /></IconWrap>; }
function BuildingIcon() { return <IconWrap><path d="M4 20V6l8-3 8 3v14M9 20v-5h6v5M8 9h1M12 9h1M16 9h1" /></IconWrap>; }
function BookIcon() { return <IconWrap><path d="M5 5h9a3 3 0 013 3v11H8a3 3 0 01-3-3V5z" /><path d="M8 8h7" /></IconWrap>; }
function ChatIcon() { return <IconWrap><path d="M4 6h16v9H9l-4 3v-3H4V6z" /></IconWrap>; }
function ShieldIcon() { return <IconWrap><path d="M12 3l7 3v5c0 5-3.2 8.7-7 10-3.8-1.3-7-5-7-10V6l7-3z" /></IconWrap>; }
function UserIcon() { return <IconWrap><circle cx="12" cy="8" r="3" /><path d="M6 19a6 6 0 0112 0" /></IconWrap>; }
function DotIcon() { return <IconWrap><circle cx="12" cy="12" r="2" /></IconWrap>; }

// === Organization child icons ===
function SchoolBuildingIcon() { return <IconWrap><path d="M3 21V10l9-7 9 7v11" /><path d="M9 21v-6h6v6" /><path d="M12 3v4" /></IconWrap>; }
function CalendarIcon() { return <IconWrap><rect x="4" y="5" width="16" height="16" rx="2" /><path d="M16 3v4M8 3v4M4 11h16" /><path d="M8 15h2M14 15h2" /></IconWrap>; }
function LayersIcon() { return <IconWrap><path d="M12 4l8 4-8 4-8-4z" /><path d="M4 12l8 4 8-4" /><path d="M4 16l8 4 8-4" /></IconWrap>; }
function UsersGroupIcon() { return <IconWrap><circle cx="9" cy="7" r="2.5" /><path d="M3 18a6 6 0 0112 0" /><circle cx="17" cy="8" r="2" /><path d="M21 18a4 4 0 00-6-3.5" /></IconWrap>; }
function GroupCircleIcon() { return <IconWrap><circle cx="12" cy="12" r="9" /><circle cx="9" cy="10" r="2" /><circle cx="15" cy="10" r="2" /><path d="M7 16a3 3 0 015-2M12 14a3 3 0 015 2" /></IconWrap>; }
function BookOpenIcon() { return <IconWrap><path d="M2 5a2 2 0 012-2h5a3 3 0 013 3v14l-1-1H4a2 2 0 01-2-2V5z" /><path d="M22 5a2 2 0 00-2-2h-5a3 3 0 00-3 3v14l1-1h6a2 2 0 002-2V5z" /></IconWrap>; }
function GraduationCapIcon() { return <IconWrap><path d="M2 10l10-5 10 5-10 5z" /><path d="M6 12v5a6 6 0 0012 0v-5" /><path d="M22 10v6" /></IconWrap>; }
function ClipboardCheckIcon() { return <IconWrap><rect x="6" y="4" width="12" height="17" rx="2" /><path d="M9 2h6v3H9z" /><path d="M9 12l2 2 4-4" /></IconWrap>; }

// === Academics child icons ===
function ClockIcon() { return <IconWrap><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></IconWrap>; }
function FileTextIcon() { return <IconWrap><path d="M6 3h8l5 5v11a2 2 0 01-2 2H6a2 2 0 01-2-2V5a2 2 0 012-2z" /><path d="M14 3v5h5" /><path d="M8 13h8M8 17h5" /></IconWrap>; }
function CheckListIcon() { return <IconWrap><rect x="4" y="3" width="16" height="18" rx="2" /><path d="M8 8l1.5 1.5L13 6" /><path d="M8 14l1.5 1.5L13 12" /><path d="M15 8h2M15 14h2" /></IconWrap>; }
function MessageSquareIcon() { return <IconWrap><path d="M4 4h16v12H8l-4 4V4z" /><path d="M8 8h8M8 12h5" /></IconWrap>; }
function StarIcon() { return <IconWrap><path d="M12 3l2.5 5.5L20 9.5l-4 4 1 5.5L12 16.5 7 19l1-5.5-4-4 5.5-1z" /></IconWrap>; }
function PencilIcon() { return <IconWrap><path d="M4 20h16" /><path d="M5 16l1-4 9-9 3 3-9 9-4 1z" /><path d="M13.5 5.5l3 3" /></IconWrap>; }
function FileReportIcon() { return <IconWrap><path d="M6 3h8l5 5v11a2 2 0 01-2 2H6a2 2 0 01-2-2V5a2 2 0 012-2z" /><path d="M14 3v5h5" /><path d="M8 15v-3M12 15v-5M16 15v-2" /></IconWrap>; }

// === Administration child icons ===
function UsersSettingsIcon() { return <IconWrap><circle cx="9" cy="7" r="3" /><path d="M3 19a6 6 0 019-5.2" /><circle cx="18" cy="15" r="2.5" /><path d="M18 11v1.5M18 17.5V19M14.5 13l1.3.75M20.2 16.25L21.5 17M14.5 17l1.3-.75M20.2 13.75L21.5 13" /></IconWrap>; }
function LockIcon() { return <IconWrap><rect x="5" y="11" width="14" height="9" rx="2" /><path d="M8 11V8a4 4 0 018 0v3" /></IconWrap>; }
