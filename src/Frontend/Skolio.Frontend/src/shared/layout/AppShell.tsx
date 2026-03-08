import React, { useMemo, useState, type ReactNode } from 'react';
import { useI18n, type Locale } from '../../i18n';
import type { SessionState } from '../auth/session';
import { AppNavbar, type NavbarMenuItem } from './AppNavbar';
import { buildSidebarSections, type SidebarItem, type SidebarNode } from './navigation';
import { AppFooter } from './AppFooter';
import { ShellLanguageSwitcher } from './ShellLanguageSwitcher';
import { iconForNav } from './AppIcons';

export type AppRouteLike =
  | '/dashboard'
  | '/organization'
  | '/organization/schools'
  | '/organization/school-years'
  | '/organization/grade-levels'
  | '/organization/classes'
  | '/organization/groups'
  | '/organization/subjects'
  | '/organization/fields-of-study'
  | '/organization/teacher-assignments'
  | '/academics'
  | '/academics/timetable'
  | '/academics/lesson-records'
  | '/academics/attendance'
  | '/academics/excuses'
  | '/academics/grades'
  | '/academics/homework'
  | '/academics/daily-reports'
  | '/communication'
  | '/administration'
  | '/identity'
  | '/login';

export function AppShell({
  session,
  nav,
  active,
  onNavigate,
  onLogout,
  profileDisplayName,
  profileContext,
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
  profileDisplayName?: string;
  profileContext?: string;
  pageTitle: string;
  pageSubtitle?: string;
  topSlot?: ReactNode;
  footerLanguageSwitcher?: ReactNode;
  pageActions?: ReactNode;
  children: ReactNode;
}) {
  const { t } = useI18n();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const navItems: SidebarItem[] = useMemo(
    () => nav.map((route) => ({ key: route, label: routeLabel(route, t) })),
    [nav, t]
  );
  const sections = useMemo(
    () => buildSidebarSections(session.roles, session.schoolType, navItems),
    [session.roles, session.schoolType, navItems]
  );

  const topMenuItems: NavbarMenuItem[] = nav
    .filter((route) => route === '/dashboard' || route === '/communication')
    .map((route) => ({
      key: route,
      label: routeLabel(route, t),
      active: active === route,
      onSelect: () => onNavigate(route)
    }));

  return (
    <div className="sk-app-root">
      <AppNavbar
        brand="Skolio"
        menuItems={topMenuItems}
        profileName={profileDisplayName ?? session.subject}
        profileContext={profileContext ?? `${session.roles.join(', ') || 'User'} | ${session.schoolType}`}
        myProfileLabel={t('myProfile')}
        signOutLabel={t('signOutMenu')}
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
          {t('menu')}
        </button>
        <div className="sk-shell-content">
          <AppSidebar
            active={active}
            isSidebarOpen={isSidebarOpen}
            onClose={() => setIsSidebarOpen(false)}
            sections={sections.map((section) => ({
              ...section,
              label: sectionLabel(section.key, session.schoolType, t),
              nodes: section.nodes.map((node) => mapSidebarNodeLabels(node, t))
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
  sections: { key: string; label: string; nodes: SidebarNode[] }[];
  active: string;
  onNavigate: (route: string) => void;
  isSidebarOpen: boolean;
  onClose: () => void;
}) {
  const { t } = useI18n();
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});

  const isNodeActive = (node: SidebarNode) => {
    if (node.route && active === node.route) return true;
    if (!node.children) return false;
    return node.children.some((child) => child.route === active);
  };

  const isOpen = (node: SidebarNode) => {
    if (!node.children || node.children.length === 0) return true;
    if (expanded[node.key] !== undefined) return expanded[node.key];
    return isNodeActive(node);
  };

  return (
    <aside className={`sk-sidebar ${isSidebarOpen ? 'open' : ''}`} aria-label={t('mainNavigation')}>
      <button className="sk-sidebar-close" onClick={onClose} type="button">
        {t('close')}
      </button>
      {sections.map((section) => (
        <section key={section.key} className="sk-sidebar-section">
          <h3 className="sk-sidebar-title">{section.label}</h3>
          <nav className="sk-sidebar-items">
            {section.nodes.map((node) => {
              const open = isOpen(node);
              const activeParent = isNodeActive(node);
              const hasChildren = !!node.children?.length;

              return (
                <div key={node.key} className="sk-sidebar-node">
                  <div className="sk-sidebar-parent-row">
                    <button
                      className={`sk-sidebar-link ${activeParent ? 'is-active' : ''}`}
                      onClick={() => {
                        if (node.route) {
                          onNavigate(node.route);
                          onClose();
                          return;
                        }
                        if (hasChildren) {
                          setExpanded((current) => ({ ...current, [node.key]: !open }));
                        }
                      }}
                      type="button"
                    >
                      {iconForNav(node.key)}
                      {node.label}
                    </button>
                    {hasChildren ? (
                      <button
                        className={`sk-sidebar-toggle ${open ? 'is-open' : ''}`}
                        type="button"
                        onClick={() => setExpanded((current) => ({ ...current, [node.key]: !open }))}
                        aria-label={open ? `${t('close')} ${node.label}` : `${t('open')} ${node.label}`}
                      >
                        v
                      </button>
                    ) : null}
                  </div>
                  {hasChildren && open ? (
                    <div className="sk-sidebar-children">
                      {node.children?.map((child) => (
                        <button
                          key={child.key}
                          className={`sk-sidebar-link sk-sidebar-child ${active === child.route ? 'is-active' : ''}`}
                          onClick={() => {
                            onNavigate(child.route);
                            onClose();
                          }}
                          type="button"
                        >
                          {iconForNav(child.key)}
                          {child.label}
                        </button>
                      ))}
                    </div>
                  ) : null}
                </div>
              );
            })}
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

function routeLabel(route: AppRouteLike, t: ReturnType<typeof useI18n>['t']) {
  if (route === '/dashboard') return t('routeDashboard');
  if (route === '/organization') return t('routeOrganization');
  if (route === '/organization/schools') return t('navSchools');
  if (route === '/organization/school-years') return t('navSchoolYears');
  if (route === '/organization/grade-levels') return t('navGradeLevels');
  if (route === '/organization/classes') return t('navClasses');
  if (route === '/organization/groups') return t('navGroups');
  if (route === '/organization/subjects') return t('navSubjects');
  if (route === '/organization/fields-of-study') return t('navFieldsOfStudy');
  if (route === '/organization/teacher-assignments') return t('navTeacherAssignments');
  if (route === '/academics') return t('routeAcademics');
  if (route === '/academics/timetable') return t('navTimetable');
  if (route === '/academics/lesson-records') return t('navLessonRecords');
  if (route === '/academics/attendance') return t('navAttendance');
  if (route === '/academics/excuses') return t('navExcuses');
  if (route === '/academics/grades') return t('navGrades');
  if (route === '/academics/homework') return t('navHomework');
  if (route === '/academics/daily-reports') return t('navDailyReports');
  if (route === '/communication') return t('routeCommunication');
  if (route === '/administration') return t('routeAdministration');
  if (route === '/identity') return t('routeIdentity');
  return route;
}

function sectionLabel(
  sectionKey: string,
  schoolType: SessionState['schoolType'],
  t: ReturnType<typeof useI18n>['t']
) {
  if (sectionKey === 'overview') return t('overview');
  if (sectionKey === 'operations') {
    if (schoolType === 'Kindergarten') return t('operationsKindergarten');
    if (schoolType === 'SecondarySchool') return t('operationsSecondary');
    return t('operationsElementary');
  }

  return sectionKey;
}

function mapSidebarNodeLabels(node: SidebarNode, t: ReturnType<typeof useI18n>['t']): SidebarNode {
  return {
    ...node,
    label: node.route ? routeLabel(node.route as AppRouteLike, t) : node.label,
    children: node.children?.map((child) => ({
      ...child,
      label: routeLabel(child.route as AppRouteLike, t)
    }))
  };
}
