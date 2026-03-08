import React, { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { useI18n } from '../../i18n';

export type NavbarMenuItem = {
  key: string;
  label: string;
  active: boolean;
  onSelect: () => void;
};

export function AppNavbar({
  brand,
  menuItems,
  profileName,
  profileContext,
  myProfileLabel,
  signOutLabel,
  onProfile,
  onLogout,
  rightSlot
}: {
  brand: string;
  menuItems: NavbarMenuItem[];
  profileName: string;
  profileContext: string;
  myProfileLabel?: string;
  signOutLabel?: string;
  onProfile: () => void;
  onLogout: () => void;
  rightSlot?: ReactNode;
}) {
  const { t } = useI18n();

  return (
    <header className="sk-navbar">
      <div className="sk-navbar-inner">
        <div className="sk-navbar-main">
          <div className="sk-navbar-brand">{brand}</div>
          <NavbarMenu items={menuItems} />
        </div>
        <div className="sk-navbar-right">
          {rightSlot ? <div>{rightSlot}</div> : null}
          <NavbarProfileMenu
            displayName={profileName}
            context={profileContext}
            myProfileLabel={myProfileLabel ?? t('myProfile')}
            signOutLabel={signOutLabel ?? t('signOutMenu')}
            onProfile={onProfile}
            onLogout={onLogout}
          />
          <NavbarNotificationsMenu label={t('notifications')} />
        </div>
      </div>
    </header>
  );
}

export function NavbarMenu({ items }: { items: NavbarMenuItem[] }) {
  const { t } = useI18n();

  return (
    <nav aria-label={t('mainNavigation')} className="sk-navbar-menu">
      {items.map((item) => (
        <button
          key={item.key}
          className={`sk-navbar-link ${item.active ? 'is-active' : ''}`}
          onClick={item.onSelect}
          type="button"
        >
          {item.label}
        </button>
      ))}
    </nav>
  );
}

export function NavbarProfileMenu({
  displayName,
  context,
  myProfileLabel,
  signOutLabel,
  onProfile,
  onLogout
}: {
  displayName: string;
  context: string;
  myProfileLabel: string;
  signOutLabel: string;
  onProfile: () => void;
  onLogout: () => void;
}) {
  const { t } = useI18n();
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement | null>(null);
  const initials = useMemo(() => toInitials(displayName), [displayName]);

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

  useEffect(() => {
    if (!open) return;
    const onEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false);
      }
    };

    window.addEventListener('keydown', onEscape);
    return () => window.removeEventListener('keydown', onEscape);
  }, [open]);

  return (
    <div className="sk-profile" ref={rootRef}>
      <button
        aria-expanded={open}
        aria-haspopup="menu"
        aria-label={t('openProfileMenu')}
        className="sk-profile-trigger"
        onClick={() => setOpen((value) => !value)}
        type="button"
      >
        <span className="sk-profile-avatar" aria-hidden="true">
          <span className="relative inline-flex h-full w-full items-center justify-center">
            <ProfileUserIcon />
            <span className="absolute -bottom-1 -right-1 rounded-full border border-white bg-slate-900 px-1 text-[9px] font-semibold leading-4 text-white">{initials}</span>
          </span>
        </span>
        <span className="sk-profile-meta">
          <span className="sk-profile-name">{displayName}</span>
          <span className="sk-profile-context">{context}</span>
        </span>
        <ChevronDownIcon />
      </button>
      {open ? (
        <div className="sk-profile-menu" role="menu">
          <button
            className="sk-profile-menu-item"
            onClick={() => {
              setOpen(false);
              onProfile();
            }}
            role="menuitem"
            type="button"
          >
            <ProfileCardIcon className="h-4 w-4 shrink-0" />
            <span>{myProfileLabel}</span>
          </button>
          <button
            className="sk-profile-menu-item danger"
            onClick={() => {
              setOpen(false);
              onLogout();
            }}
            role="menuitem"
            type="button"
          >
            <SignOutIcon className="h-4 w-4 shrink-0" />
            <span>{signOutLabel}</span>
          </button>
        </div>
      ) : null}
    </div>
  );
}

function NavbarNotificationsMenu({ label }: { label: string }) {
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement | null>(null);

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

  useEffect(() => {
    if (!open) return;
    const onEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false);
      }
    };

    window.addEventListener('keydown', onEscape);
    return () => window.removeEventListener('keydown', onEscape);
  }, [open]);

  return (
    <div className="sk-profile" ref={rootRef}>
      <button
        aria-expanded={open}
        aria-haspopup="menu"
        aria-label={label}
        className="sk-profile-trigger sk-notification-trigger"
        onClick={() => setOpen((value) => !value)}
        type="button"
      >
        <span className="sk-profile-avatar" aria-hidden="true">
          <NotificationBellIcon className="h-4 w-4" />
        </span>
        <ChevronDownIcon />
      </button>
      {open ? (
        <div className="sk-profile-menu" role="menu">
          <div className="sk-profile-menu-heading inline-flex items-center gap-2">
            <InfoIcon className="h-4 w-4 shrink-0 text-slate-500" />
            <span>{label}</span>
          </div>
          <div className="px-3 py-2 text-sm text-slate-500">Není žádné oznámení</div>
        </div>
      ) : null}
    </div>
  );
}

function toInitials(value: string) {
  const parts = value
    .split(/[\s._-]+/)
    .map((x) => x.trim())
    .filter((x) => x.length > 0);

  if (parts.length === 0) return 'SK';
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
}

function ProfileUserIcon({ className = 'h-4 w-4 shrink-0' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z" stroke="currentColor" strokeWidth="1.8" />
      <path d="M5 20a7 7 0 1 1 14 0" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ProfileCardIcon({ className = 'h-4 w-4 shrink-0' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <rect x="4" y="4" width="16" height="16" rx="3" stroke="currentColor" strokeWidth="1.8" />
      <circle cx="12" cy="10" r="2.5" stroke="currentColor" strokeWidth="1.8" />
      <path d="M8.5 16a3.5 3.5 0 0 1 7 0" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function SignOutIcon({ className = 'h-4 w-4 shrink-0' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M10 5H6a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h4" stroke="currentColor" strokeWidth="1.8" />
      <path d="M14 16l4-4-4-4" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M18 12H9" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function ChevronDownIcon() {
  return (
    <svg viewBox="0 0 20 20" className="h-3 w-3 text-slate-500" fill="none" aria-hidden="true">
      <path d="M5 7.5 10 12.5 15 7.5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function NotificationBellIcon({ className = 'h-4 w-4 shrink-0' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M15 18H6a2 2 0 0 1-2-2v-1c1.5-1 2-2.8 2-4.8V9a6 6 0 1 1 12 0v1.2c0 2 .5 3.8 2 4.8v1a2 2 0 0 1-2 2h-3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <path d="M9.5 18a2.5 2.5 0 0 0 5 0" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function InfoIcon({ className = 'h-4 w-4 shrink-0' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="1.8" />
      <path d="M12 10.5v6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <circle cx="12" cy="7.5" r="1" fill="currentColor" />
    </svg>
  );
}
