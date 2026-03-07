import React, { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';

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
  onProfile,
  onLogout,
  rightSlot
}: {
  brand: string;
  menuItems: NavbarMenuItem[];
  profileName: string;
  profileContext: string;
  onProfile: () => void;
  onLogout: () => void;
  rightSlot?: ReactNode;
}) {
  return (
    <header className="sk-navbar">
      <div className="sk-navbar-inner">
        <div className="sk-navbar-main">
          <div className="sk-navbar-brand">{brand}</div>
          <NavbarMenu items={menuItems} />
        </div>
        <div className="sk-navbar-right">
          <button className="sk-pill" type="button">Notifications</button>
          {rightSlot ? <div>{rightSlot}</div> : null}
          <NavbarProfileMenu
            displayName={profileName}
            context={profileContext}
            onProfile={onProfile}
            onLogout={onLogout}
          />
        </div>
      </div>
    </header>
  );
}

export function NavbarMenu({ items }: { items: NavbarMenuItem[] }) {
  return (
    <nav aria-label="Main navigation" className="sk-navbar-menu">
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
  onProfile,
  onLogout
}: {
  displayName: string;
  context: string;
  onProfile: () => void;
  onLogout: () => void;
}) {
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

  return (
    <div className="sk-profile" ref={rootRef}>
      <button
        aria-expanded={open}
        aria-haspopup="menu"
        className="sk-profile-trigger"
        onClick={() => setOpen((value) => !value)}
        type="button"
      >
        <span className="sk-profile-avatar" aria-hidden="true">{initials}</span>
        <span className="sk-profile-meta">
          <span className="sk-profile-name">{displayName}</span>
          <span className="sk-profile-context">{context}</span>
        </span>
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
            Profil
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
            Odhlasit
          </button>
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
