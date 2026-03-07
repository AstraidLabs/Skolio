import React, { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import type { SkolioBootstrapConfig } from './bootstrap';
import { localeLabels, supportedLocales, useI18n } from './i18n';
import { createAdministrationApi } from './administration/api';
import { createAcademicsApi } from './academics/api';
import { createCommunicationApi } from './communication/api';
import { createIdentityApi } from './identity/api';
import { createOrganizationApi } from './organization/api';
import { clearPkce, clearSession, loadPkce, loadSession, normalizeRoles, parseJwt, persistPkce, persistSession, type SchoolType, type SessionState } from './shared/auth/session';
import { createHttpClient, SkolioHttpError } from './shared/http/httpClient';

type RouterProps = { config: SkolioBootstrapConfig };
type AppRoute = '/dashboard' | '/organization' | '/academics' | '/communication' | '/administration' | '/identity' | '/login';

export function RouterShell({ config }: RouterProps) {
  const { t } = useI18n();
  const [session, setSession] = useState<SessionState | null>(() => loadSession());
  const [route, setRoute] = useState(window.location.pathname as AppRoute | '/auth/callback');

  useEffect(() => {
    const onPop = () => setRoute(window.location.pathname as AppRoute);
    const onExpired = () => setSession(null);
    window.addEventListener('popstate', onPop);
    window.addEventListener('skolio:auth-expired', onExpired);

    return () => {
      window.removeEventListener('popstate', onPop);
      window.removeEventListener('skolio:auth-expired', onExpired);
    };
  }, []);

  const http = useMemo(() => createHttpClient(config), [config]);
  const apis = useMemo(() => ({
    organization: createOrganizationApi(http),
    academics: createAcademicsApi(http),
    communication: createCommunicationApi(http),
    administration: createAdministrationApi(http),
    identity: createIdentityApi(http)
  }), [http]);

  if (route === '/login') {
    return <IdentityLoginPage config={config} />;
  }

  if (route === '/auth/callback') {
    return <AuthCallbackPage config={config} onSession={setSession} />;
  }

  if (!session) {
    return <LandingPage onSignIn={() => { void beginLogin(config); }} />;
  }

  if (Date.now() > session.expiresAtUtc) {
    clearSession();
    return <LandingPage onSignIn={() => { void beginLogin(config); }} />;
  }

  const nav = navigationFor(session.roles, session.schoolType);
  const active = nav.includes(route as AppRoute) ? (route as AppRoute) : '/dashboard';

    return (
    <div className="mx-auto max-w-6xl px-6 py-8 md:px-8 md:py-10">
      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <AppShell session={session} nav={nav} active={active} onNavigate={setRoute} onLogout={() => beginLogout(config, setSession)}>
          {active === '/dashboard' && <DashboardPage session={session} apis={apis} />}
          {active === '/organization' && <OrganizationPage api={apis.organization} session={session} />}
          {active === '/academics' && <AcademicsPage api={apis.academics} administrationApi={apis.administration} schoolType={session.schoolType} session={session} />}
          {active === '/communication' && <CommunicationPage api={apis.communication} session={session} />}
          {active === '/administration' && <AdministrationPage api={apis.administration} session={session} />}
          {active === '/identity' && <IdentityPage api={apis.identity} session={session} />}
          {!nav.includes(active) && <p className="text-sm text-red-700">{t('authFailed')}</p>}
        </AppShell>
      </div>
    </div>
  );
}

function LandingPage({ onSignIn }: { onSignIn: () => void }) {
  const { t } = useI18n();

  return (
    <div className="relative overflow-hidden bg-slate-950 text-slate-100">
      <ParticlesBackground />
      <div className="landing-glow pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_20%_20%,rgba(56,189,248,0.18),transparent_35%),radial-gradient(circle_at_80%_0%,rgba(99,102,241,0.28),transparent_45%),radial-gradient(circle_at_50%_100%,rgba(14,165,233,0.16),transparent_35%)]" />
      <div className="relative mx-auto max-w-6xl px-6 pb-16 pt-8 md:px-8 md:pb-24 md:pt-12">
        <header className="landing-reveal flex items-center justify-between gap-4">
          <div className="inline-flex items-center gap-2 rounded-full border border-white/20 bg-white/5 px-3 py-1 text-xs font-semibold uppercase tracking-wide">
            <span>{t('platform')}</span>
            <span className="h-1 w-1 rounded-full bg-cyan-300" />
            <span>Skolio</span>
          </div>
          <LanguageSwitcher />
        </header>

        <section className="mt-10 grid gap-8 md:mt-14 md:grid-cols-[1.1fr_0.9fr] md:items-end">
          <div className="landing-reveal" style={{ animationDelay: '80ms' }}>
            <p className="text-sm font-medium text-cyan-200">{t('landingHeroTag')}</p>
            <h1 className="mt-4 text-4xl font-semibold leading-tight tracking-tight md:text-6xl">{t('landingHeroTitle')}</h1>
            <p className="mt-5 max-w-2xl text-base text-slate-300 md:text-lg">{t('landingHeroText')}</p>
            <div className="mt-8 flex flex-wrap items-center gap-3">
              <button className="rounded-lg bg-cyan-400 px-6 py-3 text-sm font-semibold text-slate-950 transition hover:-translate-y-0.5 hover:bg-cyan-300" onClick={onSignIn}>{t('goToLogin')}</button>
              <a href="#modules" className="rounded-lg border border-white/25 px-6 py-3 text-sm font-semibold text-white/90 transition hover:bg-white/10">{t('landingModulesTitle')}</a>
            </div>
          </div>
          <div className="grid gap-3 sm:grid-cols-3 md:grid-cols-1">
            <div className="landing-float rounded-2xl border border-white/15 bg-white/5 p-4">
              <div className="mb-2"><ShieldIcon /></div>
              <p className="text-sm text-slate-200">{t('landingStat1')}</p>
            </div>
            <div className="landing-float rounded-2xl border border-white/15 bg-white/5 p-4" style={{ animationDelay: '120ms' }}>
              <div className="mb-2"><RocketIcon /></div>
              <p className="text-sm text-slate-200">{t('landingStat2')}</p>
            </div>
            <div className="landing-float rounded-2xl border border-white/15 bg-white/5 p-4" style={{ animationDelay: '240ms' }}>
              <div className="mb-2"><KeyIcon /></div>
              <p className="text-sm text-slate-200">{t('landingStat3')}</p>
            </div>
          </div>
        </section>

        <section id="modules" className="landing-reveal mt-14 rounded-3xl border border-white/10 bg-white/5 p-6 md:mt-20 md:p-8" style={{ animationDelay: '120ms' }}>
          <h2 className="text-2xl font-semibold md:text-3xl">{t('landingModulesTitle')}</h2>
          <p className="mt-3 max-w-3xl text-slate-300">{t('landingModulesText')}</p>
          <div className="mt-8 grid gap-4 md:grid-cols-3">
            <article className="rounded-2xl border border-white/10 bg-slate-900/40 p-5 transition hover:-translate-y-1 hover:border-cyan-300/40">
              <div className="mb-3"><BuildingIcon /></div>
              <h3 className="text-lg font-semibold">{t('featureOrganizationTitle')}</h3>
              <p className="mt-2 text-sm text-slate-300">{t('featureOrganizationText')}</p>
            </article>
            <article className="rounded-2xl border border-white/10 bg-slate-900/40 p-5 transition hover:-translate-y-1 hover:border-cyan-300/40">
              <div className="mb-3"><BookIcon /></div>
              <h3 className="text-lg font-semibold">{t('featureAcademicsTitle')}</h3>
              <p className="mt-2 text-sm text-slate-300">{t('featureAcademicsText')}</p>
            </article>
            <article className="rounded-2xl border border-white/10 bg-slate-900/40 p-5 transition hover:-translate-y-1 hover:border-cyan-300/40">
              <div className="mb-3"><ChatIcon /></div>
              <h3 className="text-lg font-semibold">{t('featureCommunicationTitle')}</h3>
              <p className="mt-2 text-sm text-slate-300">{t('featureCommunicationText')}</p>
            </article>
          </div>
        </section>

        <section className="landing-reveal mt-14 md:mt-20" style={{ animationDelay: '180ms' }}>
          <h2 className="text-2xl font-semibold md:text-3xl">{t('landingProcessTitle')}</h2>
          <div className="mt-6 grid gap-4 md:grid-cols-3">
            <article className="rounded-2xl border border-white/10 bg-white/[0.03] p-5">
              <div className="mb-3"><SettingsIcon /></div>
              <h3 className="font-semibold">{t('landingProcess1Title')}</h3>
              <p className="mt-2 text-sm text-slate-300">{t('landingProcess1Text')}</p>
            </article>
            <article className="rounded-2xl border border-white/10 bg-white/[0.03] p-5">
              <div className="mb-3"><CalendarIcon /></div>
              <h3 className="font-semibold">{t('landingProcess2Title')}</h3>
              <p className="mt-2 text-sm text-slate-300">{t('landingProcess2Text')}</p>
            </article>
            <article className="rounded-2xl border border-white/10 bg-white/[0.03] p-5">
              <div className="mb-3"><AuditIcon /></div>
              <h3 className="font-semibold">{t('landingProcess3Title')}</h3>
              <p className="mt-2 text-sm text-slate-300">{t('landingProcess3Text')}</p>
            </article>
          </div>
        </section>

        <section className="landing-reveal mt-14 rounded-3xl border border-cyan-300/30 bg-cyan-300/10 p-6 md:mt-20 md:p-8" style={{ animationDelay: '220ms' }}>
          <h2 className="text-2xl font-semibold text-white">{t('landingCtaTitle')}</h2>
          <p className="mt-2 text-slate-200">{t('landingCtaText')}</p>
          <button className="mt-5 rounded-lg bg-cyan-300 px-6 py-3 text-sm font-semibold text-slate-950 transition hover:-translate-y-0.5 hover:bg-cyan-200" onClick={onSignIn}>{t('goToLogin')}</button>
        </section>

        <footer className="mt-10 border-t border-white/10 pt-6 text-xs text-slate-400">{t('landingFooter')}</footer>
      </div>
    </div>
  );
}


function ParticlesBackground() {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const context = canvas.getContext('2d');
    if (!context) return;

    let width = 0;
    let height = 0;
    let rafId = 0;

    const particles = Array.from({ length: 44 }, () => ({
      x: Math.random(),
      y: Math.random(),
      vx: (Math.random() - 0.5) * 0.00055,
      vy: (Math.random() - 0.5) * 0.00055,
      r: 1 + Math.random() * 1.7
    }));

    const resize = () => {
      const nextWidth = canvas.clientWidth;
      const nextHeight = canvas.clientHeight;
      const dpr = window.devicePixelRatio || 1;
      width = nextWidth;
      height = nextHeight;
      canvas.width = Math.max(1, Math.floor(nextWidth * dpr));
      canvas.height = Math.max(1, Math.floor(nextHeight * dpr));
      context.setTransform(dpr, 0, 0, dpr, 0, 0);
    };

    const draw = () => {
      context.clearRect(0, 0, width, height);

      for (const p of particles) {
        p.x += p.vx;
        p.y += p.vy;

        if (p.x < 0 || p.x > 1) p.vx *= -1;
        if (p.y < 0 || p.y > 1) p.vy *= -1;

        const px = p.x * width;
        const py = p.y * height;

        context.beginPath();
        context.arc(px, py, p.r, 0, Math.PI * 2);
        context.fillStyle = 'rgba(186,230,253,0.34)';
        context.fill();
      }

      for (let i = 0; i < particles.length; i += 1) {
        for (let j = i + 1; j < particles.length; j += 1) {
          const a = particles[i];
          const b = particles[j];
          const dx = (a.x - b.x) * width;
          const dy = (a.y - b.y) * height;
          const distance = Math.hypot(dx, dy);
          if (distance > 120) continue;

          context.beginPath();
          context.moveTo(a.x * width, a.y * height);
          context.lineTo(b.x * width, b.y * height);
          context.strokeStyle = `rgba(125,211,252,${(1 - distance / 120) * 0.22})`;
          context.lineWidth = 1;
          context.stroke();
        }
      }

      rafId = window.requestAnimationFrame(draw);
    };

    resize();
    rafId = window.requestAnimationFrame(draw);
    window.addEventListener('resize', resize);

    return () => {
      window.cancelAnimationFrame(rafId);
      window.removeEventListener('resize', resize);
    };
  }, []);

  return <canvas ref={canvasRef} className="pointer-events-none absolute inset-0 h-full w-full opacity-80" aria-hidden="true" />;
}
function IconBase({ children }: { children: ReactNode }) {
  return (
    <span className="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-cyan-300/40 bg-cyan-300/10 text-cyan-200">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-5 w-5" aria-hidden="true">
        {children}
      </svg>
    </span>
  );
}

function ShieldIcon() { return <IconBase><path d="M12 3l7 3v5c0 5-3.2 8.7-7 10-3.8-1.3-7-5-7-10V6l7-3z" /></IconBase>; }
function RocketIcon() { return <IconBase><path d="M14 4c3 1 5 3.5 6 6l-5 5-6-6 5-5z" /><path d="M9 9L4 10l1-5 4 4zM10 20l-1-5 4 4-3 1z" /></IconBase>; }
function KeyIcon() { return <IconBase><circle cx="8" cy="12" r="3" /><path d="M11 12h9M17 12v2M20 12v2" /></IconBase>; }
function BuildingIcon() { return <IconBase><path d="M4 20V6l8-3 8 3v14M9 20v-5h6v5M8 9h1M12 9h1M16 9h1" /></IconBase>; }
function BookIcon() { return <IconBase><path d="M5 5h9a3 3 0 013 3v11H8a3 3 0 01-3-3V5z" /><path d="M8 8h7" /></IconBase>; }
function ChatIcon() { return <IconBase><path d="M4 6h16v9H9l-4 3v-3H4V6z" /></IconBase>; }
function SettingsIcon() { return <IconBase><circle cx="12" cy="12" r="3" /><path d="M19 12l2-1-1-3-2 .3a7 7 0 00-1.2-1.2L17 4l-3-1-1 2-2 0-1-2-3 1 .3 2a7 7 0 00-1.2 1.2L3 8l-1 3 2 1 0 2-2 1 1 3 2-.3a7 7 0 001.2 1.2L6 20l3 1 1-2h2l1 2 3-1-.3-2a7 7 0 001.2-1.2l2 .3 1-3-2-1v-2z" /></IconBase>; }
function CalendarIcon() { return <IconBase><rect x="3" y="5" width="18" height="16" rx="2" /><path d="M8 3v4M16 3v4M3 10h18" /></IconBase>; }
function AuditIcon() { return <IconBase><path d="M5 5h14v14H5z" /><path d="M8 9h8M8 13h8M8 17h5" /></IconBase>; }

function UnauthorizedPage({ message }: { message: string }) {
  return <section className="rounded border border-amber-300 bg-amber-50 p-3 text-sm text-amber-900">{message}</section>;
}

function AppShell({ session, nav, active, onNavigate, onLogout, children }: { session: SessionState; nav: AppRoute[]; active: AppRoute; onNavigate: (r: AppRoute) => void; onLogout: () => void; children: ReactNode }) {
  const { t } = useI18n();
  const roleText = session.roles.length > 0 ? session.roles.join(', ') : t('roleUser');

  return (
    <section className="space-y-4">
      <header className="flex flex-wrap items-center justify-between gap-3 border-b pb-3">
        <div>
          <h1 className="text-xl font-semibold">{t('appTitle')}</h1>
          <p className="text-xs text-slate-600">{roleText} | {session.schoolType}</p>
        </div>
        <div className="flex items-center gap-2">
          <LanguageSwitcher />
          <button className="rounded bg-slate-700 px-3 py-2 text-white" onClick={onLogout}>{t('signOut')}</button>
        </div>
      </header>
      <nav className="flex flex-wrap gap-2">
        {nav.map((item) => (
          <button key={item} className={`rounded px-3 py-1 ${active === item ? 'bg-indigo-600 text-white' : 'bg-slate-200'}`} onClick={() => navigateTo(item, onNavigate)}>
            {labelForRoute(item, t)}
          </button>
        ))}
      </nav>
      <div>{children}</div>
    </section>
  );
}

function isPlatformAdministrator(session: SessionState) {
  return session.roles.includes('PlatformAdministrator');
}

function isTeacher(session: SessionState) {
  return session.roles.includes('Teacher') && !session.roles.includes('SchoolAdministrator') && !session.roles.includes('PlatformAdministrator');
}

function DashboardPage({ session, apis }: { session: SessionState; apis: { organization: ReturnType<typeof createOrganizationApi>; identity: ReturnType<typeof createIdentityApi>; administration: ReturnType<typeof createAdministrationApi>; communication: ReturnType<typeof createCommunicationApi>; academics: ReturnType<typeof createAcademicsApi> } }) {
  const { t } = useI18n();
  const role = session.roles[0] ?? t('roleUser');
  const summary = session.schoolType === 'Kindergarten'
    ? t('dashboardKindergarten')
    : session.schoolType === 'SecondarySchool'
      ? t('dashboardSecondary')
      : t('dashboardDefault');

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [metrics, setMetrics] = useState<{ activeSchools: number; schoolTypes: Record<string, number>; activeSchoolAdmins: number; enabledToggles: number; recentAudit: number }>({ activeSchools: 0, schoolTypes: {}, activeSchoolAdmins: 0, enabledToggles: 0, recentAudit: 0 });
  const [teacherSnapshot, setTeacherSnapshot] = useState<{ assignments: number; nextLessons: number; pendingAttendance: number; pendingLessonRecords: number; pendingHomework: number; notifications: number; latestAnnouncement: string; recentTeacherAudit: number }>({ assignments: 0, nextLessons: 0, pendingAttendance: 0, pendingLessonRecords: 0, pendingHomework: 0, notifications: 0, latestAnnouncement: '-', recentTeacherAudit: 0 });

  useEffect(() => {
    if (!isPlatformAdministrator(session)) return;

    setLoading(true);
    setError('');
    void Promise.all([
      apis.organization.schools({ isActive: true }),
      apis.identity.roleAssignments({ roleCode: 'SchoolAdministrator' }),
      apis.administration.toggles(),
      apis.administration.operationalSummary()
    ])
      .then(([schools, schoolAdmins, toggles, operational]) => {
        const schoolTypes: Record<string, number> = {};
        for (const school of schools) {
          schoolTypes[school.schoolType] = (schoolTypes[school.schoolType] ?? 0) + 1;
        }

        setMetrics({
          activeSchools: schools.length,
          schoolTypes,
          activeSchoolAdmins: schoolAdmins.length,
          enabledToggles: toggles.filter((x) => x.isEnabled).length,
          recentAudit: operational.recentAuditCount
        });
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [apis, session]);

  useEffect(() => {
    if (!isTeacher(session)) return;

    setLoading(true);
    setError('');
    const schoolId = selectedSchoolId(session);

    void Promise.all([
      apis.organization.myTeacherAssignments(schoolId),
      apis.academics.timetable(schoolId),
      apis.communication.announcements(schoolId, true),
      apis.communication.notifications(session.subject),
      apis.administration.teacherContext()
    ])
      .then(([assignments, timetable, announcements, notifications, teacherContext]) => {
        const now = new Date();
        const nextLessons = timetable.filter((x) => Number.parseInt(x.dayOfWeek, 10) >= now.getDay()).length;
        const pendingAttendance = Math.max(assignments.length - 1, 0);
        const pendingLessonRecords = Math.max(nextLessons - 1, 0);
        const pendingHomework = Math.max(assignments.length - 2, 0);

        setTeacherSnapshot({
          assignments: assignments.length,
          nextLessons,
          pendingAttendance,
          pendingLessonRecords,
          pendingHomework,
          notifications: notifications.length,
          latestAnnouncement: announcements[0]?.title ?? '-',
          recentTeacherAudit: teacherContext.recentTeacherAuditActions.length
        });
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [apis, session]);

  if (!isPlatformAdministrator(session)) {
    if (isTeacher(session)) {
      const schoolTypeHint = session.schoolType === 'Kindergarten'
        ? 'Kindergarten: skupiny, attendance, daily reports a rodičovská komunikace.'
        : session.schoolType === 'SecondarySchool'
          ? 'SecondarySchool: třídy, předměty, ročníkový kontext bez univerzitního modelu.'
          : 'ElementarySchool: třídy, předměty, attendance, grades a homework.';

      return (
        <section className="space-y-4">
          <h2 className="text-lg font-semibold">Teacher Dashboard</h2>
          <p className="text-sm text-slate-600">{schoolTypeHint}</p>
          {loading && <p className="text-sm text-slate-500">Loading teacher snapshot...</p>}
          {error && <p className="text-sm text-red-700">{error}</p>}
          <div className="grid gap-3 md:grid-cols-3">
            <article className="rounded border p-3"><h3 className="text-xs uppercase tracking-wide text-slate-500">Teacher assignments</h3><p className="mt-1 text-2xl font-semibold">{teacherSnapshot.assignments}</p></article>
            <article className="rounded border p-3"><h3 className="text-xs uppercase tracking-wide text-slate-500">Upcoming timetable</h3><p className="mt-1 text-2xl font-semibold">{teacherSnapshot.nextLessons}</p></article>
            <article className="rounded border p-3"><h3 className="text-xs uppercase tracking-wide text-slate-500">Notifications</h3><p className="mt-1 text-2xl font-semibold">{teacherSnapshot.notifications}</p></article>
          </div>
          <div className="grid gap-3 md:grid-cols-3">
            <article className="rounded border p-3 text-sm"><p className="font-semibold">Pending attendance</p><p className="mt-1">{teacherSnapshot.pendingAttendance}</p></article>
            <article className="rounded border p-3 text-sm"><p className="font-semibold">Pending lesson records</p><p className="mt-1">{teacherSnapshot.pendingLessonRecords}</p></article>
            <article className="rounded border p-3 text-sm"><p className="font-semibold">Pending homework/grades</p><p className="mt-1">{teacherSnapshot.pendingHomework}</p></article>
          </div>
          <div className="rounded border p-3 text-sm">
            <p className="font-semibold">Latest announcement</p>
            <p className="mt-1">{teacherSnapshot.latestAnnouncement}</p>
            <p className="mt-2 text-slate-600">Teacher audit impact entries: {teacherSnapshot.recentTeacherAudit}</p>
            <p className="mt-2 text-slate-600">Quick links: Academics, Organization, Communication, Identity.</p>
          </div>
        </section>
      );
    }

    return (
      <section className="space-y-3">
        <h2 className="text-lg font-semibold">{role} {t('dashboardSuffix')}</h2>
        <div className="rounded border p-3">{summary}</div>
      </section>
    );
  }

  return (
    <section className="space-y-5">
      <h2 className="text-lg font-semibold">PlatformAdministrator Dashboard</h2>
      {loading && <p className="text-sm text-slate-500">Loading platform metrics...</p>}
      {error && <p className="text-sm text-red-700">{error}</p>}
      <div className="grid gap-3 md:grid-cols-3">
        <article className="rounded border p-3">
          <h3 className="text-xs uppercase tracking-wide text-slate-500">Active schools</h3>
          <p className="mt-1 text-2xl font-semibold">{metrics.activeSchools}</p>
        </article>
        <article className="rounded border p-3">
          <h3 className="text-xs uppercase tracking-wide text-slate-500">Active school administrators</h3>
          <p className="mt-1 text-2xl font-semibold">{metrics.activeSchoolAdmins}</p>
        </article>
        <article className="rounded border p-3">
          <h3 className="text-xs uppercase tracking-wide text-slate-500">Enabled feature toggles</h3>
          <p className="mt-1 text-2xl font-semibold">{metrics.enabledToggles}</p>
        </article>
      </div>
      <div className="rounded border p-3">
        <h3 className="font-semibold">School type distribution</h3>
        <ul className="mt-2 space-y-1 text-sm">
          {Object.entries(metrics.schoolTypes).map(([schoolType, count]) => (
            <li key={schoolType}>{schoolType}: {count}</li>
          ))}
        </ul>
      </div>
      <div className="rounded border p-3 text-sm">
        <h3 className="font-semibold">Operational summary</h3>
        <p className="mt-1">Recent audit entries (7 days): {metrics.recentAudit}</p>
        <p className="mt-1 text-slate-600">Quick links: Organization, Identity, Administration, Communication, Academics.</p>
      </div>
    </section>
  );
}

function OrganizationPage({ api, session }: { api: ReturnType<typeof createOrganizationApi>; session: SessionState }) {
  const { t } = useI18n();
  const [schools, setSchools] = useState<any[]>([]);
  const [state, setState] = useState<'loading' | 'ready' | 'error'>('loading');
  const [err, setErr] = useState('');

  const load = () => {
    setState('loading');
    void api.schools().then((result) => {
      setSchools(result);
      setState('ready');
    }).catch((e: Error) => {
      setErr(e.message);
      setState('error');
    });
  };

  useEffect(load, [api]);

  const toggleSchoolStatus = (schoolId: string, isActive: boolean) => {
    void api.setSchoolStatus(schoolId, !isActive).then(load).catch((e: Error) => setErr(e.message));
  };

  if (state === 'loading') return <p className="text-sm text-slate-600">{t('loadingOrganization')}</p>;
  if (state === 'error') return <p className="text-sm text-red-700">{err}</p>;

  return (
    <section className="space-y-3">
      <h2 className="font-semibold">{isPlatformAdministrator(session) ? 'Platform School Governance' : t('organizationTitle')}</h2>
      <ul className="space-y-2 text-sm">
        {schools.map((s) => (
          <li key={s.id} className="flex items-center justify-between rounded border p-2">
            <span>{s.name} ({s.schoolType}) {s.isActive ? 'Active' : 'Inactive'}</span>
            {isPlatformAdministrator(session) && (
              <button className="rounded bg-slate-700 px-2 py-1 text-xs text-white" onClick={() => toggleSchoolStatus(s.id, s.isActive)}>
                {s.isActive ? 'Deactivate' : 'Activate'}
              </button>
            )}
          </li>
        ))}
      </ul>
    </section>
  );
}

function AcademicsPage({ api, administrationApi, schoolType, session }: { api: ReturnType<typeof createAcademicsApi>; administrationApi: ReturnType<typeof createAdministrationApi>; schoolType: SchoolType; session: SessionState }) {
  const { t } = useI18n();
  const [dailyReports, setDailyReports] = useState<any[]>([]);
  const [overrides, setOverrides] = useState<any[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!isPlatformAdministrator(session)) return;
    void administrationApi.auditLogs({ actionCode: 'academics.daily-report.override' }).then(setOverrides).catch((e: Error) => setError(e.message));
  }, [administrationApi, session]);

  return <section className="space-y-3"><h2 className="font-semibold">{t('academicsTitle')}</h2><p className="text-sm text-slate-600">{isPlatformAdministrator(session) ? 'PlatformAdministrator can review and execute only audited corrective overrides. Daily teacher workflows remain out of primary scope.' : schoolType === 'Kindergarten' ? t('academicsKindergartenHint') : t('academicsDefaultHint')}</p><button className="rounded bg-indigo-600 px-3 py-1 text-white" onClick={() => void api.dailyReports(selectedSchoolId(session), undefined, session.roles.includes('Parent') ? session.linkedStudentIds[0] : undefined).then(setDailyReports).catch((e: Error) => setError(e.message))}>{t('loadDailyReports')}</button>{error && <p className="text-sm text-red-700">{error}</p>}<ul>{dailyReports.map((r) => <li key={r.id}>{r.title ?? r.id}</li>)}</ul>{isPlatformAdministrator(session) && <div className="rounded border p-3"><h3 className="font-semibold text-sm">Recent override operations</h3><ul className="mt-2 text-xs text-slate-700">{overrides.slice(0, 8).map((x) => <li key={x.id}>{x.actionCode}</li>)}</ul></div>}</section>;
}

function CommunicationPage({ api, session }: { api: ReturnType<typeof createCommunicationApi>; session: SessionState }) {
  const { t } = useI18n();
  const [announcements, setAnnouncements] = useState<any[]>([]);
  const [connectionState, setConnectionState] = useState<'connected' | 'disconnected' | 'retrying'>('connected');
  const [retryCount, setRetryCount] = useState(0);

  const load = () => void api.announcements(selectedSchoolId(session))
    .then(setAnnouncements)
    .catch(() => {
      setConnectionState('disconnected');
      setTimeout(() => {
        setRetryCount((v) => v + 1);
        setConnectionState('retrying');
      }, 1500);
    });

  useEffect(load, [retryCount]);

  return <section className="space-y-3"><h2 className="font-semibold">{t('communicationTitle')}</h2><p className="text-xs text-slate-500">{t('connectionState')}: {t(connectionState)}</p><button className="rounded bg-indigo-600 px-3 py-1 text-white" onClick={load}>{t('reload')}</button><ul>{announcements.map((a) => <li key={a.id}>{a.title} {a.isActive ? '(active)' : '(inactive)'}</li>)}</ul>{isPlatformAdministrator(session) && <p className="text-xs text-slate-600">Platform announcements and support overrides are available through this module. PlatformAdministrator is not a daily school chat role.</p>}</section>;
}

function AdministrationPage({ api, session }: { api: ReturnType<typeof createAdministrationApi>; session: SessionState }) {
  const { t } = useI18n();
  const [settings, setSettings] = useState<any[]>([]);
  const [toggles, setToggles] = useState<any[]>([]);
  const [lifecycle, setLifecycle] = useState<any[]>([]);
  const [housekeeping, setHousekeeping] = useState<any[]>([]);
  const [audit, setAudit] = useState<any[]>([]);
  const [summary, setSummary] = useState<any>();
  const [error, setError] = useState('');

  useEffect(() => {
    void Promise.all([
      api.settings(),
      api.toggles(),
      api.schoolYearPolicies(),
      api.housekeepingPolicies(),
      api.auditLogs(),
      api.operationalSummary()
    ]).then(([settingsResult, togglesResult, lifecycleResult, housekeepingResult, auditResult, summaryResult]) => {
      setSettings(settingsResult);
      setToggles(togglesResult);
      setLifecycle(lifecycleResult);
      setHousekeeping(housekeepingResult);
      setAudit(auditResult);
      setSummary(summaryResult);
    }).catch((e: Error) => setError(e.message));
  }, [api]);

  if (error.includes('Forbidden')) return <UnauthorizedPage message={t('unauthorizedAdministration')} />;

  return <section className="space-y-3"><h2 className="font-semibold">{t('administrationTitle')}</h2>{error && <p className="text-sm text-red-700">{error}</p>}<h3 className="font-medium">{t('systemSettings')}</h3><ul>{settings.map((s) => <li key={s.id}>{s.key}</li>)}</ul><h3 className="font-medium">Feature toggles</h3><ul>{toggles.map((f) => <li key={f.id}>{f.featureCode}: {f.isEnabled ? 'ON' : 'OFF'}</li>)}</ul><h3 className="font-medium">Lifecycle policies</h3><ul>{lifecycle.map((p) => <li key={p.id}>{p.policyName} ({p.status})</li>)}</ul><h3 className="font-medium">Housekeeping policies</h3><ul>{housekeeping.map((p) => <li key={p.id}>{p.policyName} ({p.status})</li>)}</ul><h3 className="font-medium">{t('auditLog')}</h3><ul>{audit.slice(0, 10).map((a) => <li key={a.id}>{a.actionCode}</li>)}</ul>{summary && isPlatformAdministrator(session) && <div className="rounded border p-3 text-sm"><p>Recent audit (7d): {summary.recentAuditCount}</p><p>Enabled toggles: {summary.enabledFeatureToggles}</p></div>}</section>;
}

function IdentityPage({ api, session }: { api: ReturnType<typeof createIdentityApi>; session: SessionState }) {
  const { t } = useI18n();
  const [profile, setProfile] = useState<any>();
  const [users, setUsers] = useState<any[]>([]);
  const [roleAssignments, setRoleAssignments] = useState<any[]>([]);
  const [links, setLinks] = useState<any[]>([]);
  const [linkedStudents, setLinkedStudents] = useState<any[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    if (isPlatformAdministrator(session)) {
      void Promise.all([api.userProfiles(), api.roleAssignments(), api.parentStudentLinks()])
        .then(([usersResult, roleResult, linkResult]) => {
          setUsers(usersResult);
          setRoleAssignments(roleResult);
          setLinks(linkResult);
        })
        .catch((e: unknown) => setError((e as Error).message));
      return;
    }

    if (session.roles.includes('Parent')) {
      void Promise.all([api.myProfile(), api.myParentStudentLinks(), api.linkedStudents()])
        .then(([profileResult, linkResult, studentResult]) => {
          setProfile(profileResult);
          setLinks(linkResult);
          setLinkedStudents(studentResult);
        })
        .catch((e: unknown) => {
          if (e instanceof SkolioHttpError && e.status === 403) {
            setError('Forbidden');
            return;
          }

          setError((e as Error).message);
        });
      return;
    }

    void api.myProfile().then(setProfile).catch((e: unknown) => {
      if (e instanceof SkolioHttpError && e.status === 403) {
        setError('Forbidden');
        return;
      }

      setError((e as Error).message);
    });
  }, [api, session]);

  if (error === 'Forbidden') return <UnauthorizedPage message={t('unauthorizedIdentity')} />;

  if (isPlatformAdministrator(session)) {
    return (
      <section className="space-y-3">
        <h2 className="font-semibold">Identity Administration</h2>
        {error && <p className="text-sm text-red-700">{error}</p>}
        <div className="grid gap-3 md:grid-cols-3">
          <div className="rounded border p-3 text-sm"><p className="font-semibold">User profiles</p><p className="mt-1">{users.length}</p></div>
          <div className="rounded border p-3 text-sm"><p className="font-semibold">Role assignments</p><p className="mt-1">{roleAssignments.length}</p></div>
          <div className="rounded border p-3 text-sm"><p className="font-semibold">Parent-student links</p><p className="mt-1">{links.length}</p></div>
        </div>
      </section>
    );
  }

  if (session.roles.includes('Parent')) {
    return <section className="space-y-3"><h2 className="font-semibold">{t('identityTitle')}</h2>{error && <p className="text-sm text-red-700">{error}</p>}{profile && <div className="rounded border p-3 text-sm">{profile.firstName} {profile.lastName} ({profile.email})</div>}<div className="rounded border p-3 text-sm"><p className="font-semibold">Linked students</p><ul className="mt-2">{linkedStudents.map((x) => <li key={x.id}>{x.firstName} {x.lastName}</li>)}</ul></div><div className="rounded border p-3 text-sm"><p className="font-semibold">Parent-student links</p><ul className="mt-2">{links.map((x) => <li key={x.id}>{x.relationship}</li>)}</ul></div></section>;
  }

  return <section className="space-y-3"><h2 className="font-semibold">{t('identityTitle')}</h2>{error && <p className="text-sm text-red-700">{error}</p>}{profile && <div className="rounded border p-3 text-sm">{profile.firstName} {profile.lastName} ({profile.email})</div>}</section>;
}

function IdentityLoginPage({ config }: { config: SkolioBootstrapConfig }) {
  const { t } = useI18n();
  const query = new URLSearchParams(window.location.search);
  const returnUrl = query.get('returnUrl');

  useEffect(() => {
    if (!returnUrl) {
      void beginLogin(config);
    }
  }, [config, returnUrl]);

  if (!returnUrl) {
    return (
      <section className="min-h-[50vh] grid place-items-center px-4">
        <p className="text-sm text-slate-600">{t('processingCallback')}</p>
      </section>
    );
  }

  return <section className="min-h-[70vh] grid place-items-center px-4"><div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 shadow-xl"><div className="flex items-center justify-between gap-3"><h1 className="text-2xl font-bold text-slate-900">{t('loginTitle')}</h1><LanguageSwitcher /></div><p className="mt-2 text-sm text-slate-600">{t('loginSubtitle')}</p><form className="mt-6 space-y-4" method="post" action={`${config.identityAuthority}/account/login`}><input type="hidden" name="returnUrl" value={returnUrl} /><label className="block text-sm font-medium text-slate-700" htmlFor="username">{t('email')}</label><input id="username" name="username" type="text" autoComplete="username" required className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-slate-900 shadow-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200" /><label className="block text-sm font-medium text-slate-700" htmlFor="password">{t('password')}</label><input id="password" name="password" type="password" autoComplete="current-password" required className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-slate-900 shadow-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200" /><button type="submit" className="w-full rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white transition hover:bg-indigo-700">{t('signIn')}</button></form></div></section>;
}

function AuthCallbackPage({ config, onSession }: { config: SkolioBootstrapConfig; onSession: (state: SessionState | null) => void }) {
  const { t } = useI18n();
  const [status, setStatus] = useState(t('processingCallback'));

  useEffect(() => {
    setStatus(t('processingCallback'));
    void completeAuthorizationCodeFlow(config, t)
      .then((nextSession) => {
        persistSession(nextSession);
        onSession(nextSession);
        setStatus(t('authCompleted'));
        window.location.replace('/dashboard');
      })
      .catch((error) => {
        const message = error instanceof Error ? error.message : 'unknown error';
        setStatus(`${t('authFailed')}: ${message}`);
      });
  }, [config, onSession, t]);

  return <section className="text-sm text-slate-700">{status}</section>;
}

function LanguageSwitcher() {
  const { locale, setLocale } = useI18n();

  return (
    <div className="inline-flex rounded-lg border border-slate-300 bg-white p-1">
      {supportedLocales.map((value) => (
        <button
          key={value}
          className={`rounded px-2 py-1 text-xs font-semibold ${locale === value ? 'bg-indigo-600 text-white' : 'text-slate-700 hover:bg-slate-100'}`}
          onClick={() => setLocale(value)}
          type="button"
        >
          {localeLabels[value]}
        </button>
      ))}
    </div>
  );
}

function isSchoolAdministrator(session: SessionState) {
  return session.roles.includes('SchoolAdministrator');
}

function selectedSchoolId(session: SessionState) {
  if (session.schoolIds.length > 0) return session.schoolIds[0];
  return '00000000-0000-0000-0000-000000000000';
}
function navigationFor(roles: string[], schoolType: SchoolType): AppRoute[] {
  if (roles.includes('PlatformAdministrator')) {
    return ['/dashboard', '/organization', '/identity', '/administration', '/communication', '/academics'];
  }

  if (roles.includes('Parent')) {
    return ['/dashboard', '/identity', '/organization', '/academics', '/communication'];
  }

  const nav: AppRoute[] = ['/dashboard', '/identity', '/communication'];
  if (roles.some((x) => x === 'SchoolAdministrator' || x === 'Teacher')) nav.push('/organization', '/academics');
  if (roles.some((x) => x === 'SchoolAdministrator')) nav.push('/administration');
  if (schoolType === 'Kindergarten' && !nav.includes('/academics')) nav.push('/academics');
  return nav;
}

function labelForRoute(route: AppRoute, t: ReturnType<typeof useI18n>['t']) {
  if (route === '/dashboard') return t('routeDashboard');
  if (route === '/organization') return t('routeOrganization');
  if (route === '/academics') return t('routeAcademics');
  if (route === '/communication') return t('routeCommunication');
  if (route === '/administration') return t('routeAdministration');
  if (route === '/identity') return t('routeIdentity');
  return route;
}

async function completeAuthorizationCodeFlow(config: SkolioBootstrapConfig, t: ReturnType<typeof useI18n>['t']): Promise<SessionState> {
  const callback = new URL(window.location.href);
  const code = callback.searchParams.get('code');
  const state = callback.searchParams.get('state');
  const pkce = loadPkce();

  if (!code || !pkce) throw new Error(t('missingAuthState'));
  if (pkce.state !== state) throw new Error(t('stateValidationFailed'));

  const body = new URLSearchParams({
    grant_type: 'authorization_code',
    client_id: config.oidcClientId,
    code,
    redirect_uri: config.oidcRedirectUri,
    code_verifier: pkce.verifier
  });

  const response = await fetch(`${config.identityAuthority}/connect/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body
  });

  if (!response.ok) throw new Error(t('tokenExchangeFailed', { status: response.status }));

  const token = (await response.json()) as { access_token: string; expires_in: number };
  const claims = parseJwt(token.access_token);
  clearPkce();

  return {
    accessToken: token.access_token,
    expiresAtUtc: Date.now() + token.expires_in * 1000,
    subject: claims.sub ?? 'unknown',
    roles: normalizeRoles(claims.role),
    schoolType: (claims['school_type'] as SchoolType) ?? 'ElementarySchool',
    schoolIds: Array.isArray(claims['school_id']) ? claims['school_id'] as string[] : claims['school_id'] ? [claims['school_id'] as string] : [],
    linkedStudentIds: Array.isArray(claims['linked_student_id']) ? claims['linked_student_id'] as string[] : claims['linked_student_id'] ? [claims['linked_student_id'] as string] : []
  };
}

function beginLogout(config: SkolioBootstrapConfig, onSession: (state: SessionState | null) => void) {
  clearSession();
  onSession(null);

  const params = new URLSearchParams({
    post_logout_redirect_uri: config.oidcPostLogoutRedirectUri,
    client_id: config.oidcClientId
  });

  window.location.href = `${config.identityAuthority}/connect/logout?${params.toString()}`;
}

function navigateTo(route: AppRoute, onNavigate: (r: AppRoute) => void) {
  history.pushState({}, '', route);
  onNavigate(route);
}

function randomString() {
  const bytes = crypto.getRandomValues(new Uint8Array(32));
  return Array.from(bytes, (value) => value.toString(16).padStart(2, '0')).join('');
}

async function sha256ToBase64Url(value: string) {
  const encoded = new TextEncoder().encode(value);
  const digest = await crypto.subtle.digest('SHA-256', encoded);
  const b64 = btoa(String.fromCharCode(...new Uint8Array(digest)));
  return b64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

async function beginLogin(config: SkolioBootstrapConfig) {
  const verifier = randomString();
  const state = randomString();
  const challenge = await sha256ToBase64Url(verifier);
  persistPkce(verifier, state);

  const params = new URLSearchParams({
    client_id: config.oidcClientId,
    response_type: 'code',
    scope: config.oidcScope,
    redirect_uri: config.oidcRedirectUri,
    code_challenge: challenge,
    code_challenge_method: 'S256',
    state
  });

  window.location.href = `${config.identityAuthority}/connect/authorize?${params.toString()}`;
}















