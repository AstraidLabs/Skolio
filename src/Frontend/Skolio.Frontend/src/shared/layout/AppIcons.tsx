import React from 'react';

export function iconForNav(key: string) {
  if (key.includes('/organization')) return <BuildingIcon />;
  if (key.includes('/academics')) return <BookIcon />;
  if (key === '/communication') return <ChatIcon />;
  if (key === '/administration') return <ShieldIcon />;
  if (key === '/identity') return <UserIcon />;
  if (key === '/dashboard') return <DashboardIcon />;
  return <DotIcon />;
}

function IconWrap({ children }: { children: React.ReactNode }) {
  return (
    <span className="inline-flex h-4 w-4 items-center justify-center text-slate-500" aria-hidden="true">
      <svg viewBox="0 0 24 24" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth="1.8">
        {children}
      </svg>
    </span>
  );
}

function DashboardIcon() { return <IconWrap><path d="M4 4h7v7H4zM13 4h7v5h-7zM13 11h7v9h-7zM4 13h7v7H4z" /></IconWrap>; }
function BuildingIcon() { return <IconWrap><path d="M4 20V6l8-3 8 3v14M9 20v-5h6v5M8 9h1M12 9h1M16 9h1" /></IconWrap>; }
function BookIcon() { return <IconWrap><path d="M5 5h9a3 3 0 013 3v11H8a3 3 0 01-3-3V5z" /><path d="M8 8h7" /></IconWrap>; }
function ChatIcon() { return <IconWrap><path d="M4 6h16v9H9l-4 3v-3H4V6z" /></IconWrap>; }
function ShieldIcon() { return <IconWrap><path d="M12 3l7 3v5c0 5-3.2 8.7-7 10-3.8-1.3-7-5-7-10V6l7-3z" /></IconWrap>; }
function UserIcon() { return <IconWrap><circle cx="12" cy="8" r="3" /><path d="M6 19a6 6 0 0112 0" /></IconWrap>; }
function DotIcon() { return <IconWrap><circle cx="12" cy="12" r="2" /></IconWrap>; }
