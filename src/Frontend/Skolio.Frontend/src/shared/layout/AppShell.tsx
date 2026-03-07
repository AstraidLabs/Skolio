import React, { useMemo, useState, type ReactNode } from 'react';
import type { SessionState } from '../auth/session';
import { AppNavbar, type NavbarMenuItem } from './AppNavbar';
import { buildSidebarSections, type SidebarItem } from './navigation';
import { AppFooter } from './AppFooter';
import { ShellLanguageSwitcher } from './ShellLanguageSwitcher';

export type AppRouteLike = '/dashboard' | '/organization' | '/academics' | '/communication' | '/administration' | '/identity' | '/login';

export function AppShell({
  session,
  nav,
  active,
  onNavigate,
  onLogout,
  pageTitle,
  pageSubtitle,
  topSlot,
  footerLanguageSwitcher,
  pageActions,
  children
}: {
  session: SessionState;
  nav: AppRouteLike[];
  active: AppRouteLike;
  onNavigate: (route: AppRouteLike) => void;
  onLogout: () => void;
  pageTitle: string;
  pageSubtitle?: string;
  topSlot?: ReactNode;
  footerLanguageSwitcher?: ReactNode;
  pageActions?: ReactNode;
  children: ReactNode;
}) {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const navItems: SidebarItem[] = useMemo(
    () => nav.map((route) => ({ key: route, label: route })),
    [nav]
  );
  const sections = useMemo(
    () => buildSidebarSections(session.roles, session.schoolType, navItems),
    [session.roles, session.schoolType, navItems]
  );

  const topMenuItems: NavbarMenuItem[] = nav.slice(0, 3).map((route) => ({
    key: route,
    label: routeLabel(route),
    active: active === route,
    onSelect: () => onNavigate(route)
  }));

  return (
    <div className="sk-app-root">
      <AppNavbar
        brand="Skolio"
        menuItems={topMenuItems}
        profileName={session.subject}
        profileContext={`${session.roles.join(', ') || 'User'} | ${session.schoolType}`}
        onProfile={() => onNavigate('/identity')}
        onLogout={onLogout}
        rightSlot={topSlot}
      />
      <div className="sk-shell-content-frame">
        <button
          className="sk-sidebar-open"
          onClick={() => setIsSidebarOpen((value) => !value)}
          type="button"
        >
          Menu
        </button>
        <div className="sk-shell-content">
          <AppSidebar
            active={active}
            isSidebarOpen={isSidebarOpen}
            onClose={() => setIsSidebarOpen(false)}
            sections={sections.map((section) => ({
              ...section,
              items: section.items.map((item) => ({
                ...item,
                label: routeLabel(item.key as AppRouteLike)
              }))
            }))}
            onNavigate={(route) => onNavigate(route as AppRouteLike)}
          />
          <div className="sk-page-region">
            <AppPageHeader title={pageTitle} subtitle={pageSubtitle} actions={pageActions} />
            <main className="sk-page-body">{children}</main>
            <AppFooter languageSwitcher={footerLanguageSwitcher ?? <ShellLanguageSwitcher />} mode={import.meta.env.MODE} />
          </div>
        </div>
      </div>
    </div>
  );
}

function AppSidebar({
  sections,
  active,
  onNavigate,
  isSidebarOpen,
  onClose
}: {
  sections: { key: string; label: string; items: { key: string; label: string }[] }[];
  active: string;
  onNavigate: (route: string) => void;
  isSidebarOpen: boolean;
  onClose: () => void;
}) {
  return (
    <aside className={`sk-sidebar ${isSidebarOpen ? 'open' : ''}`} aria-label="Sidebar navigation">
      <button className="sk-sidebar-close" onClick={onClose} type="button">
        Close
      </button>
      {sections.map((section) => (
        <section key={section.key} className="sk-sidebar-section">
          <h3 className="sk-sidebar-title">{section.label}</h3>
          <nav className="sk-sidebar-items">
            {section.items.map((item) => (
              <button
                key={item.key}
                className={`sk-sidebar-link ${active === item.key ? 'is-active' : ''}`}
                onClick={() => {
                  onNavigate(item.key);
                  onClose();
                }}
                type="button"
              >
                {item.label}
              </button>
            ))}
          </nav>
        </section>
      ))}
    </aside>
  );
}

function AppPageHeader({
  title,
  subtitle,
  actions
}: {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
}) {
  return (
    <header className="sk-page-header">
      <div>
        <h1 className="sk-page-title">{title}</h1>
        {subtitle ? <p className="sk-page-subtitle">{subtitle}</p> : null}
      </div>
      {actions ? <div className="sk-page-actions">{actions}</div> : null}
    </header>
  );
}

function routeLabel(route: AppRouteLike) {
  if (route === '/dashboard') return 'Dashboard';
  if (route === '/organization') return 'Organization';
  if (route === '/academics') return 'Academics';
  if (route === '/communication') return 'Communication';
  if (route === '/administration') return 'Administration';
  if (route === '/identity') return 'Identity';
  return route;
}
