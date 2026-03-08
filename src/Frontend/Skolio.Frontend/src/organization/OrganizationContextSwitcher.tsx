import React, { useEffect, useMemo, useRef, useState } from 'react';
import type { School } from './api';

export function OrganizationContextSwitcher({
  schools,
  activeSchoolId,
  onSelectSchool
}: {
  schools: School[];
  activeSchoolId: string;
  onSelectSchool: (schoolId: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement | null>(null);
  const activeSchool = useMemo(
    () => schools.find((x) => x.id === activeSchoolId) ?? schools[0] ?? null,
    [schools, activeSchoolId]
  );

  useEffect(() => {
    if (!open) return;
    const onMouseDown = (event: MouseEvent) => {
      if (rootRef.current && !rootRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    };

    window.addEventListener('mousedown', onMouseDown);
    return () => window.removeEventListener('mousedown', onMouseDown);
  }, [open]);

  if (!activeSchool) return null;

  return (
    <div className="relative" ref={rootRef}>
      <button
        type="button"
        className="sk-btn sk-btn-secondary w-full justify-between"
        onClick={() => setOpen((value) => !value)}
        aria-expanded={open}
        aria-haspopup="menu"
      >
        <span className="flex min-w-0 flex-col items-start text-left">
          <span className="truncate text-sm font-semibold text-slate-900">{activeSchool.name}</span>
          <span className="text-xs text-slate-500">{activeSchool.schoolType}</span>
        </span>
        <span className="ml-3 text-xs text-slate-500">{open ? '^' : 'v'}</span>
      </button>

      {open ? (
        <OrganizationContextMenu
          activeSchoolId={activeSchool.id}
          schools={schools}
          onSelect={(schoolId) => {
            setOpen(false);
            onSelectSchool(schoolId);
          }}
        />
      ) : null}
    </div>
  );
}

export function OrganizationContextMenu({
  schools,
  activeSchoolId,
  onSelect
}: {
  schools: School[];
  activeSchoolId: string;
  onSelect: (schoolId: string) => void;
}) {
  return (
    <div className="absolute right-0 z-30 mt-2 w-full rounded-lg border border-slate-200 bg-white p-1 shadow-lg" role="menu">
      {schools.map((school) => {
        const isActive = school.id === activeSchoolId;
        return (
          <button
            key={school.id}
            type="button"
            role="menuitem"
            className={`w-full rounded-md px-3 py-2 text-left text-sm transition ${isActive ? 'bg-sky-100 text-sky-800' : 'text-slate-700 hover:bg-slate-100'}`}
            onClick={() => onSelect(school.id)}
          >
            <span className="block truncate font-medium">{school.name}</span>
            <span className="block text-xs text-slate-500">{school.schoolType}</span>
          </button>
        );
      })}
    </div>
  );
}
