import React, { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import type { SkolioBootstrapConfig } from './bootstrap';
import { localeLabels, supportedLocales, useI18n } from './i18n';
import { createAdministrationApi } from './administration/api';
import { createAcademicsApi } from './academics/api';
import { createCommunicationApi } from './communication/api';
import { createIdentityApi, type MyProfileSummary } from './identity/api';
import { createOrganizationApi } from './organization/api';
import { clearPkce, clearSession, extractRolesFromClaims, loadPkce, loadSession, parseJwt, persistPkce, persistSession, type SchoolType, type SessionState } from './shared/auth/session';
import { createHttpClient, SkolioHttpError } from './shared/http/httpClient';
import { Card, SectionHeader, StatusBadge, WidgetGrid } from './shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from './shared/ui/states';
import { AppShell as AppLayoutShell } from './shared/layout/AppShell';
import { IdentityParityPage } from './identity/IdentityParityPage';
import { ConfirmEmailChangePage, ForgotPasswordPage, ResetPasswordPage, SecuritySelfServicePage } from './identity/SecuritySelfServicePage';
import { OrganizationParityPage } from './organization/OrganizationParityPage';
import { AcademicsParityPage } from './academics/AcademicsParityPage';
import { CommunicationParityPage } from './communication/CommunicationParityPage';
import { AdministrationParityPage } from './administration/AdministrationParityPage';

type RouterProps = { config: SkolioBootstrapConfig };
type AppRoute =
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
  | '/identity/security'
  | '/security/forgot-password'
  | '/security/reset-password'
  | '/security/confirm-email-change'
  | '/login';

const idleTimeoutMs = 30 * 60 * 1000;
const idleWarningWindowMs = 2 * 60 * 1000;
const idleActivityStorageKey = 'skolio.auth.idle.lastActivityUtc';
const idleLogoutStorageKey = 'skolio.auth.idle.logout';
const logoutReasonStorageKey = 'skolio.auth.logoutReason';

export function RouterShell({ config }: RouterProps) {
  const { t } = useI18n();
  const [session, setSession] = useState<SessionState | null>(() => loadSession());
  const [route, setRoute] = useState(window.location.pathname as AppRoute | '/auth/callback');
  const [profileSummary, setProfileSummary] = useState<MyProfileSummary | null>(null);
  const [idleWarningSecondsLeft, setIdleWarningSecondsLeft] = useState<number | null>(null);
  const idleLogoutStartedRef = useRef(false);

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

  useEffect(() => {
    if (!session) {
      setProfileSummary(null);
      return;
    }

    void apis.identity.myProfileSummary()
      .then(setProfileSummary)
      .catch(() => setProfileSummary(null));
  }, [apis.identity, session?.accessToken]);

  useEffect(() => {
    const onStorage = (event: StorageEvent) => {
      if (event.key !== idleLogoutStorageKey || !event.newValue) return;
      const reason = event.newValue.split(':')[1] ?? 'manual';
      if (reason === 'idle') {
        sessionStorage.setItem(logoutReasonStorageKey, 'idle');
      }

      clearSession();
      setSession(null);
      setRoute('/login');
      window.history.replaceState({}, '', '/login');
    };

    window.addEventListener('storage', onStorage);
    return () => window.removeEventListener('storage', onStorage);
  }, []);

  useEffect(() => {
    if (!session) {
      setIdleWarningSecondsLeft(null);
      idleLogoutStartedRef.current = false;
      return;
    }

    const touchActivity = () => {
      localStorage.setItem(idleActivityStorageKey, Date.now().toString());
      setIdleWarningSecondsLeft(null);
    };

    if (!localStorage.getItem(idleActivityStorageKey)) {
      touchActivity();
    }

    const onActivity = () => touchActivity();
    const onRouteActivity = () => touchActivity();

    const logoutForIdle = () => {
      if (idleLogoutStartedRef.current) return;
      idleLogoutStartedRef.current = true;
      sessionStorage.setItem(logoutReasonStorageKey, 'idle');
      localStorage.setItem(idleLogoutStorageKey, `${Date.now()}:idle`);
      beginLogout(config, setSession, 'idle');
    };

    const checkTimer = window.setInterval(() => {
      const lastActivityRaw = localStorage.getItem(idleActivityStorageKey);
      const lastActivity = Number(lastActivityRaw ?? Date.now());
      const remainingMs = (Number.isFinite(lastActivity) ? lastActivity : Date.now()) + idleTimeoutMs - Date.now();

      if (remainingMs <= 0)
      {
        setIdleWarningSecondsLeft(0);
        logoutForIdle();
        return;
      }

      if (remainingMs <= idleWarningWindowMs)
      {
        setIdleWarningSecondsLeft(Math.ceil(remainingMs / 1000));
        return;
      }

      setIdleWarningSecondsLeft(null);
    }, 1000);

    window.addEventListener('click', onActivity, { passive: true });
    window.addEventListener('keydown', onActivity, { passive: true });
    window.addEventListener('scroll', onActivity, { passive: true });
    window.addEventListener('pointerdown', onActivity, { passive: true });
    window.addEventListener('touchstart', onActivity, { passive: true });
    window.addEventListener('popstate', onRouteActivity);

    return () => {
      window.clearInterval(checkTimer);
      window.removeEventListener('click', onActivity);
      window.removeEventListener('keydown', onActivity);
      window.removeEventListener('scroll', onActivity);
      window.removeEventListener('pointerdown', onActivity);
      window.removeEventListener('touchstart', onActivity);
      window.removeEventListener('popstate', onRouteActivity);
    };
  }, [config, session?.subject]);

  if (route === '/login') {
    return <IdentityLoginPage config={config} />;
  }

  if (route === '/auth/callback') {
    return <AuthCallbackPage config={config} onSession={setSession} />;
  }

  if (route === '/security/forgot-password') {
    return <ForgotPasswordPage api={apis.identity} />;
  }

  if (route === '/security/reset-password') {
    const query = new URLSearchParams(window.location.search);
    const userId = query.get('userId') ?? '';
    const token = query.get('token') ?? '';
    if (!userId || !token) return <ErrorState text="Missing reset password token parameters." />;
    return <ResetPasswordPage api={apis.identity} userId={userId} token={token} />;
  }

  if (route === '/security/confirm-email-change') {
    const query = new URLSearchParams(window.location.search);
    const userId = query.get('userId') ?? '';
    const token = query.get('token') ?? '';
    const newEmail = query.get('newEmail') ?? '';
    if (!userId || !token || !newEmail) return <ErrorState text="Missing email confirmation parameters." />;
    return <ConfirmEmailChangePage api={apis.identity} userId={userId} token={token} newEmail={newEmail} />;
  }

  if (!session) {
    return <LandingPage onSignIn={() => { void beginLogin(config); }} />;
  }

  if (Date.now() > session.expiresAtUtc) {
    clearSession();
    return <LandingPage onSignIn={() => { void beginLogin(config); }} />;
  }

  const nav = navigationFor(session.roles, session.schoolType);
  const active =
    nav.includes(route as AppRoute) || route === '/identity' || route === '/identity/security'
      ? (route as AppRoute)
      : '/dashboard';
  const profileName = (profileSummary?.profile.preferredDisplayName?.trim()
    || `${profileSummary?.profile.firstName ?? ''} ${profileSummary?.profile.lastName ?? ''}`.trim()
    || session.subject);
  const profileContext = `${session.roles.join(', ') || 'User'} | ${session.schoolType}`;

  return (
    <AppLayoutShell
      session={session}
      nav={nav}
      active={active}
      onNavigate={(nextRoute) => navigateTo(nextRoute, setRoute)}
      onLogout={() => beginLogout(config, setSession, 'manual')}
      profileDisplayName={profileName}
      profileContext={profileContext}
      pageTitle={labelForRoute(active, t)}
      pageSubtitle={active === '/identity' || active === '/identity/security' ? undefined : t('shellSubtitle')}
      footerLanguageSwitcher={<LanguageSwitcher />}
    >
      {active === '/dashboard' && <DashboardPage session={session} profileSummary={profileSummary} apis={apis} />}
      {active.startsWith('/organization') && (
        <OrganizationParityPage
          api={apis.organization}
          session={session}
          initialView={organizationViewForRoute(active)}
        />
      )}
      {active.startsWith('/academics') && (
        <AcademicsParityPage
          api={apis.academics}
          administrationApi={apis.administration}
          session={session}
          initialView={academicsViewForRoute(active)}
        />
      )}
      {active === '/communication' && <CommunicationParityPage api={apis.communication} session={session} />}
      {active === '/administration' && <AdministrationParityPage api={apis.administration} session={session} />}
      {active === '/identity' && <IdentityParityPage api={apis.identity} session={session} />}
      {active === '/identity/security' && <SecuritySelfServicePage api={apis.identity} />}
      {!nav.includes(active) && active !== '/identity' && active !== '/identity/security' && <p className="text-sm text-red-700">{t('authFailed')}</p>}
      {idleWarningSecondsLeft !== null && idleWarningSecondsLeft > 0 ? (
        <IdleTimeoutWarning
          secondsLeft={idleWarningSecondsLeft}
          onContinue={() => {
            localStorage.setItem(idleActivityStorageKey, Date.now().toString());
            setIdleWarningSecondsLeft(null);
          }}
          onLogout={() => beginLogout(config, setSession, 'manual')}
        />
      ) : null}
    </AppLayoutShell>
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

function isPlatformAdministrator(session: SessionState) {
  return session.roles.includes('PlatformAdministrator');
}

function isTeacher(session: SessionState) {
  return session.roles.includes('Teacher') && !session.roles.includes('SchoolAdministrator') && !session.roles.includes('PlatformAdministrator');
}

function isStudent(session: SessionState) {
  return session.roles.includes('Student') && !session.roles.includes('SchoolAdministrator') && !session.roles.includes('PlatformAdministrator') && !session.roles.includes('Teacher') && !session.roles.includes('Parent');
}

function DashboardPage({
  session,
  profileSummary,
  apis
}: {
  session: SessionState;
  profileSummary: MyProfileSummary | null;
  apis: {
    organization: ReturnType<typeof createOrganizationApi>;
    identity: ReturnType<typeof createIdentityApi>;
    administration: ReturnType<typeof createAdministrationApi>;
    communication: ReturnType<typeof createCommunicationApi>;
    academics: ReturnType<typeof createAcademicsApi>;
  };
}) {
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
  const [studentSnapshot, setStudentSnapshot] = useState<{ nearestSchedule: number; attendanceRecords: number; grades: number; homework: number; dailyReports: number; notifications: number; latestAnnouncement: string; lifecycleHints: number; kindergartenLimited: boolean }>({ nearestSchedule: 0, attendanceRecords: 0, grades: 0, homework: 0, dailyReports: 0, notifications: 0, latestAnnouncement: '-', lifecycleHints: 0, kindergartenLimited: false });

  const platformAdminAuthorized = profileSummary?.isPlatformAdministrator ?? false;

  useEffect(() => {
    if (!platformAdminAuthorized) return;

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
  }, [apis, platformAdminAuthorized]);

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

  useEffect(() => {
    if (!isStudent(session)) return;

    setLoading(true);
    setError('');
    const schoolId = selectedSchoolId(session);

    void Promise.all([
      apis.organization.studentContext(schoolId),
      apis.academics.timetable(schoolId, session.subject),
      apis.academics.attendance(schoolId, undefined, session.subject),
      apis.academics.homework(schoolId, undefined, session.subject),
      apis.academics.dailyReports(schoolId, undefined, session.subject),
      apis.communication.announcements(schoolId, true),
      apis.communication.notifications(session.subject),
      apis.administration.studentContext()
    ])
      .then(async ([context, timetable, attendance, homework, dailyReports, announcements, notifications, adminContext]) => {
        const gradesBySubject = await Promise.all(context.subjects.map((x) => apis.academics.grades(schoolId, session.subject, x.id).catch(() => [])));
        const grades = gradesBySubject.flat();

        setStudentSnapshot({
          nearestSchedule: timetable.length,
          attendanceRecords: attendance.length,
          grades: grades.length,
          homework: homework.length,
          dailyReports: dailyReports.length,
          notifications: notifications.length,
          latestAnnouncement: announcements[0]?.title ?? '-',
          lifecycleHints: adminContext.schoolLifecyclePolicySummaries.length,
          kindergartenLimited: session.schoolType === 'Kindergarten'
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
          <div className="sk-hero">
            <h2 className="text-xl font-semibold">Teacher Dashboard</h2>
            <p className="mt-2 text-sm text-blue-100">{schoolTypeHint}</p>
          </div>
          {loading && <LoadingState text="Loading teacher snapshot..." />}
          {error && <ErrorState text={error} />}
          <WidgetGrid>
            <Card><p className="sk-metric-label">Teacher assignments</p><p className="sk-metric-value">{teacherSnapshot.assignments}</p></Card>
            <Card><p className="sk-metric-label">Upcoming timetable</p><p className="sk-metric-value">{teacherSnapshot.nextLessons}</p></Card>
            <Card><p className="sk-metric-label">Pending attendance</p><p className="sk-metric-value">{teacherSnapshot.pendingAttendance}</p></Card>
            <Card><p className="sk-metric-label">Pending lesson records</p><p className="sk-metric-value">{teacherSnapshot.pendingLessonRecords}</p></Card>
          </WidgetGrid>
          <div className="grid gap-3 lg:grid-cols-2">
            <Card className="sk-card-muted"><p className="font-semibold text-sm">Quick actions</p><div className="mt-3 flex flex-wrap gap-2"><StatusBadge label="Attendance" tone="info" /><StatusBadge label="Lesson records" tone="info" /><StatusBadge label="Grades" tone="good" /><StatusBadge label="Homework" tone="warn" /><StatusBadge label="Daily reports" tone="neutral" /></div></Card>
            <Card><p className="font-semibold text-sm">Latest announcement</p><p className="mt-2 text-sm">{teacherSnapshot.latestAnnouncement}</p><p className="mt-3 text-xs text-slate-500">Teacher audit impact entries: {teacherSnapshot.recentTeacherAudit}</p></Card>
          </div>
        </section>
      );
    }

    if (isStudent(session)) {
      return (
        <section className="space-y-4">
          <div className="sk-hero">
            <h2 className="text-xl font-semibold">Student Dashboard</h2>
            <p className="mt-2 text-sm text-blue-100">{studentSnapshot.kindergartenLimited ? 'Kindergarten student self-service je konzervativně omezený.' : summary}</p>
          </div>
          {loading && <LoadingState text="Loading student snapshot..." />}
          {error && <ErrorState text={error} />}
          <WidgetGrid>
            <Card><p className="sk-metric-label">Nearest timetable</p><p className="sk-metric-value">{studentSnapshot.nearestSchedule}</p></Card>
            <Card><p className="sk-metric-label">Attendance summary</p><p className="sk-metric-value">{studentSnapshot.attendanceRecords}</p></Card>
            <Card><p className="sk-metric-label">Grades summary</p><p className="sk-metric-value">{studentSnapshot.grades}</p></Card>
            <Card><p className="sk-metric-label">Homework summary</p><p className="sk-metric-value">{studentSnapshot.homework}</p></Card>
          </WidgetGrid>
          <div className="grid gap-3 lg:grid-cols-2">
            <Card><p className="font-semibold text-sm">Announcements</p><p className="mt-2 text-sm">{studentSnapshot.latestAnnouncement}</p><p className="mt-2 text-xs text-slate-500">Notifications: {studentSnapshot.notifications}</p></Card>
            <Card className="sk-card-muted"><p className="font-semibold text-sm">Quick links</p><div className="mt-3 flex flex-wrap gap-2"><StatusBadge label="Timetable" tone="info" /><StatusBadge label="Grades" tone="good" /><StatusBadge label="Homework" tone="warn" /><StatusBadge label="Communication" tone="neutral" /></div></Card>
          </div>
        </section>
      );
    }

    if (session.roles.includes('Parent')) {
      return (
        <section className="space-y-4">
          <div className="sk-hero">
            <h2 className="text-xl font-semibold">Parent Dashboard</h2>
            <p className="mt-2 text-sm text-blue-100">{session.schoolType === 'Kindergarten' ? 'Skupina, attendance, daily reports a provozní komunikace.' : session.schoolType === 'SecondarySchool' ? 'Širší studijní přehled navázaného studenta bez university modelu.' : 'Attendance, grades, homework a školní oznámení.'}</p>
          </div>
          <WidgetGrid>
            <Card><p className="sk-metric-label">Linked students</p><p className="sk-metric-value">{session.linkedStudentIds.length}</p></Card>
            <Card><p className="sk-metric-label">Attendance summary</p><p className="sk-metric-value">{session.linkedStudentIds.length > 0 ? 'Ready' : 'No links'}</p></Card>
            <Card><p className="sk-metric-label">Excuse status</p><p className="sk-metric-value">Active</p></Card>
            <Card><p className="sk-metric-label">Parent communication</p><p className="sk-metric-value">Enabled</p></Card>
          </WidgetGrid>
          <Card><p className="font-semibold text-sm">Quick actions</p><div className="mt-3 flex flex-wrap gap-2"><StatusBadge label="Create excuse" tone="warn" /><StatusBadge label="Check communication" tone="info" /><StatusBadge label="Announcements" tone="good" /></div></Card>
        </section>
      );
    }

    if (session.roles.includes('SchoolAdministrator')) {
      return (
        <section className="space-y-4">
          <div className="sk-hero">
            <h2 className="text-xl font-semibold">SchoolAdministrator Dashboard</h2>
            <p className="mt-2 text-sm text-blue-100">{session.schoolType === 'Kindergarten' ? 'Skupiny a daily reports mají prioritu.' : session.schoolType === 'SecondarySchool' ? 'Obory a ročníky jsou zvýrazněné v přehledech.' : 'Třídy, předměty, attendance a grades jsou hlavní provozní osa.'}</p>
          </div>
          <WidgetGrid>
            <Card><p className="sk-metric-label">School year status</p><p className="sk-metric-value">Open</p></Card>
            <Card><p className="sk-metric-label">{session.schoolType === 'Kindergarten' ? 'Groups' : 'Classes'}</p><p className="sk-metric-value">Operational</p></Card>
            <Card><p className="sk-metric-label">Active teachers</p><p className="sk-metric-value">Managed</p></Card>
            <Card><p className="sk-metric-label">Active students/children</p><p className="sk-metric-value">Tracked</p></Card>
          </WidgetGrid>
          <div className="grid gap-3 lg:grid-cols-2">
            <Card><p className="font-semibold text-sm">Quick actions</p><div className="mt-3 flex flex-wrap gap-2"><StatusBadge label="School years" tone="info" /><StatusBadge label="Classes/Groups" tone="good" /><StatusBadge label="Subjects" tone="neutral" /><StatusBadge label="Teacher assignments" tone="warn" /></div></Card>
            <Card><p className="font-semibold text-sm">Pending operational tasks</p><ul className="sk-list"><li className="sk-list-item"><span>School announcement review</span><StatusBadge label="Pending" tone="warn" /></li><li className="sk-list-item"><span>Role assignment check</span><StatusBadge label="Open" tone="info" /></li></ul></Card>
          </div>
        </section>
      );
    }

    return (
      <section className="space-y-3">
        <Card><h2 className="text-lg font-semibold">{role} {t('dashboardSuffix')}</h2><p className="mt-2 text-sm text-slate-600">{summary}</p></Card>
      </section>
    );
  }

  return (
    <section className="space-y-5">
      <div className="sk-hero">
        <h2 className="text-xl font-semibold">PlatformAdministrator Dashboard</h2>
        <p className="mt-2 text-sm text-blue-100">Globální governance, audit a provozní platformové summary.</p>
      </div>
      {loading && <LoadingState text="Loading platform metrics..." />}
      {error && <ErrorState text={error} />}
      <WidgetGrid>
        <Card><p className="sk-metric-label">Active schools</p><p className="sk-metric-value">{metrics.activeSchools}</p></Card>
        <Card><p className="sk-metric-label">Active school administrators</p><p className="sk-metric-value">{metrics.activeSchoolAdmins}</p></Card>
        <Card><p className="sk-metric-label">Enabled feature toggles</p><p className="sk-metric-value">{metrics.enabledToggles}</p></Card>
        <Card><p className="sk-metric-label">Recent audit (7d)</p><p className="sk-metric-value">{metrics.recentAudit}</p></Card>
      </WidgetGrid>
      <div className="grid gap-3 lg:grid-cols-2">
        <Card>
          <h3 className="font-semibold">School type distribution</h3>
          <ul className="sk-list">
          {Object.entries(metrics.schoolTypes).map(([schoolType, count]) => (
            <li className="sk-list-item" key={schoolType}><span>{schoolType}</span><StatusBadge label={`${count}`} tone="info" /></li>
          ))}
          </ul>
        </Card>
        <Card className="sk-card-muted">
          <h3 className="font-semibold">Quick links</h3>
          <div className="mt-3 flex flex-wrap gap-2">
            <StatusBadge label="Schools" tone="neutral" />
            <StatusBadge label="Settings" tone="info" />
            <StatusBadge label="Feature toggles" tone="good" />
            <StatusBadge label="Audit" tone="warn" />
            <StatusBadge label="Lifecycle policies" tone="info" />
          </div>
        </Card>
      </div>
    </section>
  );
}

function OrganizationPage({ api, session }: { api: ReturnType<typeof createOrganizationApi>; session: SessionState }) {
  const { t } = useI18n();
  const [schools, setSchools] = useState<any[]>([]);
  const [studentContext, setStudentContext] = useState<any>();
  const [state, setState] = useState<'loading' | 'ready' | 'error'>('loading');
  const [err, setErr] = useState('');

  const load = () => {
    setState('loading');
    if (isStudent(session)) {
      void api.studentContext(selectedSchoolId(session)).then((result) => {
        setStudentContext(result);
        setState('ready');
      }).catch((e: Error) => {
        setErr(e.message);
        setState('error');
      });
      return;
    }

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

  if (state === 'loading') return <LoadingState text={t('loadingOrganization')} />;
  if (state === 'error') return <ErrorState text={err} />;

  if (isStudent(session) && studentContext) {
    return <section className="space-y-3"><SectionHeader title="Student Organization Context" /><Card><p className="text-sm">{studentContext.school.name} ({studentContext.school.schoolType})</p></Card><div className="grid gap-3 md:grid-cols-2"><Card><p className="font-semibold text-sm">School years</p><ul className="sk-list">{studentContext.schoolYears.map((x: any) => <li className="sk-list-item" key={x.id}>{x.label}</li>)}</ul></Card><Card><p className="font-semibold text-sm">Classes</p><ul className="sk-list">{studentContext.classRooms.map((x: any) => <li className="sk-list-item" key={x.id}>{x.displayName}</li>)}</ul></Card><Card><p className="font-semibold text-sm">Groups</p><ul className="sk-list">{studentContext.teachingGroups.map((x: any) => <li className="sk-list-item" key={x.id}>{x.name}</li>)}</ul></Card><Card><p className="font-semibold text-sm">Subjects</p><ul className="sk-list">{studentContext.subjects.map((x: any) => <li className="sk-list-item" key={x.id}>{x.name}</li>)}</ul></Card>{session.schoolType === 'SecondarySchool' && <Card className="md:col-span-2"><p className="font-semibold text-sm">Secondary context</p><ul className="sk-list">{studentContext.secondaryFieldsOfStudy.map((x: any) => <li className="sk-list-item" key={x.id}>{x.code} - {x.name}</li>)}</ul></Card>}</div></section>;
  }

  return (
    <section className="space-y-3">
      <SectionHeader title={isPlatformAdministrator(session) ? 'Platform School Governance' : t('organizationTitle')} />
      <ul className="sk-list">
        {schools.map((s) => (
          <li key={s.id} className="sk-list-item">
            <span>{s.name} ({s.schoolType}) {s.isActive ? 'Active' : 'Inactive'}</span>
            {isPlatformAdministrator(session) && (
              <button className="sk-btn sk-btn-secondary" onClick={() => toggleSchoolStatus(s.id, s.isActive)}>
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
  const [attendance, setAttendance] = useState<any[]>([]);
  const [homework, setHomework] = useState<any[]>([]);
  const [overrides, setOverrides] = useState<any[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!isPlatformAdministrator(session)) return;
    void administrationApi.auditLogs({ actionCode: 'academics.daily-report.override' }).then(setOverrides).catch((e: Error) => setError(e.message));
  }, [administrationApi, session]);

  useEffect(() => {
    if (!isStudent(session)) return;
    const schoolId = selectedSchoolId(session);
    void Promise.all([
      api.attendance(schoolId, undefined, session.subject),
      api.homework(schoolId, undefined, session.subject),
      api.dailyReports(schoolId, undefined, session.subject)
    ]).then(([attendanceResult, homeworkResult, reports]) => {
      setAttendance(attendanceResult);
      setHomework(homeworkResult);
      setDailyReports(reports);
    }).catch((e: Error) => setError(e.message));
  }, [api, session]);

  if (isStudent(session)) {
    return <section className="space-y-3"><SectionHeader title="Student Academics" description={schoolType === 'Kindergarten' ? 'Kindergarten student view je omezený na konzervativní read-only přehled.' : t('academicsDefaultHint')} />{error && <ErrorState text={error} />}<WidgetGrid><Card><p className="sk-metric-label">Attendance records</p><p className="sk-metric-value">{attendance.length}</p></Card><Card><p className="sk-metric-label">Homework</p><p className="sk-metric-value">{homework.length}</p></Card><Card><p className="sk-metric-label">Daily reports</p><p className="sk-metric-value">{dailyReports.length}</p></Card></WidgetGrid></section>;
  }

  return <section className="space-y-3"><SectionHeader title={t('academicsTitle')} description={isPlatformAdministrator(session) ? 'PlatformAdministrator can review and execute only audited corrective overrides. Daily teacher workflows remain out of primary scope.' : schoolType === 'Kindergarten' ? t('academicsKindergartenHint') : t('academicsDefaultHint')} action={<button className="sk-btn sk-btn-primary" onClick={() => void api.dailyReports(selectedSchoolId(session), undefined, session.roles.includes('Parent') ? session.linkedStudentIds[0] : undefined).then(setDailyReports).catch((e: Error) => setError(e.message))}>{t('loadDailyReports')}</button>} />{error && <ErrorState text={error} />}{dailyReports.length === 0 ? <EmptyState text="No daily reports in current scope." /> : <Card><ul className="sk-list">{dailyReports.map((r) => <li className="sk-list-item" key={r.id}>{r.title ?? r.id}</li>)}</ul></Card>}{isPlatformAdministrator(session) && <Card><h3 className="font-semibold text-sm">Recent override operations</h3><ul className="sk-list">{overrides.slice(0, 8).map((x) => <li className="sk-list-item" key={x.id}>{x.actionCode}</li>)}</ul></Card>}</section>;
}

function CommunicationPage({ api, session }: { api: ReturnType<typeof createCommunicationApi>; session: SessionState }) {
  const { t } = useI18n();
  const [announcements, setAnnouncements] = useState<any[]>([]);
  const [notifications, setNotifications] = useState<any[]>([]);
  const [connectionState, setConnectionState] = useState<'connected' | 'disconnected' | 'retrying'>('connected');
  const [retryCount, setRetryCount] = useState(0);

  const load = () => void Promise.all([
    api.announcements(selectedSchoolId(session)),
    api.notifications(session.subject)
  ])
    .then(([announcementResult, notificationResult]) => {
      setAnnouncements(announcementResult);
      setNotifications(notificationResult);
    })
    .catch(() => {
      setConnectionState('disconnected');
      setTimeout(() => {
        setRetryCount((v) => v + 1);
        setConnectionState('retrying');
      }, 1500);
    });

  useEffect(load, [retryCount]);

  return <section className="space-y-3"><SectionHeader title={t('communicationTitle')} description={`${t('connectionState')}: ${t(connectionState)}`} action={<button className="sk-btn sk-btn-secondary" onClick={load}>{t('reload')}</button>} />{announcements.length === 0 ? <EmptyState text="No announcements in current scope." /> : <Card><ul className="sk-list">{announcements.map((a) => <li className="sk-list-item" key={a.id}><span>{a.title}</span><StatusBadge label={a.isActive ? 'Active' : 'Inactive'} tone={a.isActive ? 'good' : 'warn'} /></li>)}</ul></Card>}<Card className="sk-card-muted"><p className="font-semibold text-sm">Notifications panel</p><p className="mt-1 text-sm">Total notifications: {notifications.length}</p></Card>{isPlatformAdministrator(session) && <Card><p className="text-xs text-slate-600">Platform announcements and support overrides are available through this module. PlatformAdministrator is not a daily school chat role.</p></Card>}</section>;
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

  return <section className="space-y-3"><SectionHeader title={t('administrationTitle')} />{error && <ErrorState text={error} />}<div className="grid gap-3 lg:grid-cols-2"><Card><p className="font-semibold text-sm">{t('systemSettings')}</p><ul className="sk-list">{settings.map((s) => <li className="sk-list-item" key={s.id}>{s.key}</li>)}</ul></Card><Card><p className="font-semibold text-sm">Feature toggles</p><ul className="sk-list">{toggles.map((f) => <li className="sk-list-item" key={f.id}><span>{f.featureCode}</span><StatusBadge label={f.isEnabled ? 'ON' : 'OFF'} tone={f.isEnabled ? 'good' : 'warn'} /></li>)}</ul></Card><Card><p className="font-semibold text-sm">Lifecycle policies</p><ul className="sk-list">{lifecycle.map((p) => <li className="sk-list-item" key={p.id}>{p.policyName} ({p.status})</li>)}</ul></Card><Card><p className="font-semibold text-sm">Housekeeping policies</p><ul className="sk-list">{housekeeping.map((p) => <li className="sk-list-item" key={p.id}>{p.policyName} ({p.status})</li>)}</ul></Card></div><Card><p className="font-semibold text-sm">{t('auditLog')}</p><ul className="sk-list">{audit.slice(0, 10).map((a) => <li className="sk-list-item" key={a.id}>{a.actionCode}</li>)}</ul></Card>{summary && isPlatformAdministrator(session) && <Card className="sk-card-muted"><p className="text-sm">Recent audit (7d): {summary.recentAuditCount}</p><p className="text-sm">Enabled toggles: {summary.enabledFeatureToggles}</p></Card>}</section>;
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

    if (isStudent(session)) {
      void api.studentContext()
        .then((result) => {
          setProfile(result.profile);
          setRoleAssignments(result.roleAssignments);
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
        <SectionHeader title="Identity Administration" />
        {error && <ErrorState text={error} />}
        <WidgetGrid>
          <Card><p className="sk-metric-label">User profiles</p><p className="sk-metric-value">{users.length}</p></Card>
          <Card><p className="sk-metric-label">Role assignments</p><p className="sk-metric-value">{roleAssignments.length}</p></Card>
          <Card><p className="sk-metric-label">Parent-student links</p><p className="sk-metric-value">{links.length}</p></Card>
        </WidgetGrid>
      </section>
    );
  }

  if (session.roles.includes('Parent')) {
    return <section className="space-y-3"><SectionHeader title={t('identityTitle')} />{error && <ErrorState text={error} />}{profile && <Card><p className="text-sm">{profile.firstName} {profile.lastName} ({profile.email})</p></Card>}<Card><p className="font-semibold text-sm">Linked students</p><ul className="sk-list">{linkedStudents.map((x) => <li className="sk-list-item" key={x.id}>{x.firstName} {x.lastName}</li>)}</ul></Card><Card><p className="font-semibold text-sm">Parent-student links</p><ul className="sk-list">{links.map((x) => <li className="sk-list-item" key={x.id}>{x.relationship}</li>)}</ul></Card></section>;
  }

  if (isStudent(session)) {
    return <section className="space-y-3"><SectionHeader title={t('identityTitle')} />{error && <ErrorState text={error} />}{profile && <Card><p className="text-sm">{profile.firstName} {profile.lastName} ({profile.email})</p></Card>}<Card><p className="font-semibold text-sm">Student roles</p><ul className="sk-list">{roleAssignments.map((x) => <li className="sk-list-item" key={x.id}>{x.roleCode}</li>)}</ul></Card></section>;
  }

  return <section className="space-y-3"><SectionHeader title={t('identityTitle')} />{error && <ErrorState text={error} />}{profile ? <Card><p className="text-sm">{profile.firstName} {profile.lastName} ({profile.email})</p></Card> : <EmptyState text="No identity profile available in current scope." />}</section>;
}

function IdentityLoginPage({ config }: { config: SkolioBootstrapConfig }) {
  const { t } = useI18n();
  const query = new URLSearchParams(window.location.search);
  const returnUrl = query.get('returnUrl');
  const mfaRequired = query.get('mfa') === 'required';
  const challengeId = query.get('challengeId') ?? '';
  const loginError = query.get('error') ?? '';
  const [useRecoveryCode, setUseRecoveryCode] = useState(loginError === 'login_mfa_invalid_recovery_code');
  const [busy, setBusy] = useState(false);
  const errorText = mapLoginError(loginError, t);
  const [idleLogoutInfo, setIdleLogoutInfo] = useState(() => {
    const queryReason = query.get('logoutReason');
    if (queryReason === 'idle') return true;
    return sessionStorage.getItem(logoutReasonStorageKey) === 'idle';
  });

  useEffect(() => {
    if (!returnUrl) {
      void beginLogin(config);
    }
  }, [config, returnUrl]);

  useEffect(() => {
    if (idleLogoutInfo) {
      sessionStorage.removeItem(logoutReasonStorageKey);
    }
  }, [idleLogoutInfo]);

  useEffect(() => {
    if (loginError === 'login_mfa_invalid_recovery_code') {
      setUseRecoveryCode(true);
      return;
    }

    if (loginError === 'login_mfa_invalid_code') {
      setUseRecoveryCode(false);
    }
  }, [loginError]);

  if (!returnUrl) {
    return (
      <section className="min-h-[50vh] grid place-items-center px-4">
        <p className="text-sm text-slate-600">{t('processingCallback')}</p>
      </section>
    );
  }

  return (
    <section className="min-h-[70vh] grid place-items-center px-4">
      <div className="w-full max-w-md sk-panel">
        <div className="flex items-center justify-between gap-3">
          <h1 className="text-2xl font-bold text-slate-900">{mfaRequired ? t('loginMfaTitle') : t('loginTitle')}</h1>
          <LanguageSwitcher />
        </div>
        <p className="mt-2 text-sm text-slate-600">{mfaRequired ? t('loginMfaSubtitle') : t('loginSubtitle')}</p>

        {idleLogoutInfo ? (
          <div className="mt-4 rounded border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900">
            {t('idleLogoutInfo')}
          </div>
        ) : null}

        {errorText ? (
          <div className="mt-4 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
            {errorText}
          </div>
        ) : null}

        {!mfaRequired ? (
          <form className="sk-form mt-6" method="post" action={`${config.identityAuthority}/account/login`} onSubmit={() => setBusy(true)}>
            <input type="hidden" name="returnUrl" value={returnUrl} />
            <div className="sk-field">
              <label className="sk-label" htmlFor="username">{t('email')}</label>
              <input id="username" name="username" type="text" autoComplete="username" required className="sk-input" />
            </div>
            <div className="sk-field">
              <label className="sk-label" htmlFor="password">{t('password')}</label>
              <input id="password" name="password" type="password" autoComplete="current-password" required className="sk-input" />
            </div>
            <button type="submit" className="sk-btn sk-btn-primary w-full" disabled={busy}>
              {busy ? t('loginMfaBusy') : t('signIn')}
            </button>
          </form>
        ) : (
          <form className="sk-form mt-6" method="post" action={`${config.identityAuthority}/account/login/mfa/verify`} onSubmit={() => setBusy(true)}>
            <input type="hidden" name="returnUrl" value={returnUrl} />
            <input type="hidden" name="challengeId" value={challengeId} />
            <input type="hidden" name="useRecoveryCode" value={useRecoveryCode ? 'true' : 'false'} />

            <div className="sk-field">
              <label className="sk-label" htmlFor="mfa-code">{useRecoveryCode ? t('loginMfaRecoveryCodeLabel') : t('loginMfaCodeLabel')}</label>
              <input id="mfa-code" name="code" type="text" autoComplete="one-time-code" required className="sk-input" />
            </div>

            <button type="submit" className="sk-btn sk-btn-primary w-full" disabled={busy}>
              {busy ? t('loginMfaBusy') : t('loginMfaConfirmAction')}
            </button>

            <div className="mt-3 flex flex-wrap gap-2">
              <button className="sk-btn sk-btn-secondary" type="button" onClick={() => setUseRecoveryCode((v) => !v)} disabled={busy}>
                {useRecoveryCode ? t('loginMfaUseAuthenticator') : t('loginMfaUseRecovery')}
              </button>
              <a className="sk-btn sk-btn-secondary" href={`/login?returnUrl=${encodeURIComponent(returnUrl)}`}>
                {t('loginMfaRestart')}
              </a>
            </div>
          </form>
        )}
      </div>
    </section>
  );
}

function mapLoginError(code: string, t: ReturnType<typeof useI18n>['t']) {
  if (!code) return '';
  if (code === 'login_invalid_credentials') return t('loginErrorInvalidCredentials');
  if (code === 'login_mfa_invalid_code') return t('loginErrorMfaInvalidCode');
  if (code === 'login_mfa_invalid_recovery_code') return t('loginErrorMfaInvalidRecoveryCode');
  if (code === 'login_mfa_challenge_expired') return t('loginErrorMfaChallengeExpired');
  if (code === 'login_mfa_blocked') return t('loginErrorMfaBlocked');
  return t('loginErrorGeneric');
}

function IdleTimeoutWarning({
  secondsLeft,
  onContinue,
  onLogout
}: {
  secondsLeft: number;
  onContinue: () => void;
  onLogout: () => void;
}) {
  const { t } = useI18n();
  const minutes = Math.max(0, Math.ceil(secondsLeft / 60));

  return (
    <div className="fixed bottom-4 right-4 z-50 w-full max-w-sm rounded-lg border border-amber-300 bg-amber-50 p-4 shadow-lg">
      <p className="text-sm font-semibold text-amber-900">{t('idleWarningTitle')}</p>
      <p className="mt-1 text-sm text-amber-900">{t('idleWarningText', { minutes })}</p>
      <div className="mt-3 flex gap-2">
        <button className="sk-btn sk-btn-primary" type="button" onClick={onContinue}>{t('idleContinueSession')}</button>
        <button className="sk-btn sk-btn-secondary" type="button" onClick={onLogout}>{t('idleLogoutNow')}</button>
      </div>
    </div>
  );
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

function organizationViewForRoute(route: AppRoute) {
  if (route === '/organization/schools') return 'schools' as const;
  if (route === '/organization/school-years') return 'school-years' as const;
  if (route === '/organization/grade-levels') return 'grade-levels' as const;
  if (route === '/organization/classes') return 'class-rooms' as const;
  if (route === '/organization/groups') return 'teaching-groups' as const;
  if (route === '/organization/subjects') return 'subjects' as const;
  if (route === '/organization/fields-of-study') return 'secondary-fields' as const;
  if (route === '/organization/teacher-assignments') return 'teacher-assignments' as const;
  return 'overview' as const;
}

function academicsViewForRoute(route: AppRoute) {
  if (route === '/academics/timetable') return 'timetable' as const;
  if (route === '/academics/lesson-records') return 'lesson-records' as const;
  if (route === '/academics/attendance') return 'attendance' as const;
  if (route === '/academics/excuses') return 'excuses' as const;
  if (route === '/academics/grades') return 'grades' as const;
  if (route === '/academics/homework') return 'homework' as const;
  if (route === '/academics/daily-reports') return 'daily-reports' as const;
  return 'overview' as const;
}

function navigationFor(roles: string[], schoolType: SchoolType): AppRoute[] {
  if (roles.includes('PlatformAdministrator')) {
    return [
      '/dashboard',
      '/communication',
      '/administration',
      '/organization',
      '/organization/schools',
      '/organization/school-years',
      '/organization/grade-levels',
      '/organization/classes',
      '/organization/groups',
      '/organization/subjects',
      '/organization/teacher-assignments',
      ...(schoolType === 'SecondarySchool' ? (['/organization/fields-of-study'] as const) : []),
      '/academics',
      '/academics/timetable',
      '/academics/lesson-records',
      '/academics/attendance',
      '/academics/excuses',
      '/academics/grades',
      '/academics/homework',
      '/academics/daily-reports'
    ];
  }

  if (roles.includes('Parent')) {
    return [
      '/dashboard',
      '/communication',
      '/academics',
      '/academics/attendance',
      '/academics/excuses',
      '/academics/grades',
      '/academics/homework',
      '/academics/daily-reports'
    ];
  }

  if (roles.includes('Student')) {
    return [
      '/dashboard',
      '/communication',
      '/academics',
      '/academics/timetable',
      '/academics/attendance',
      '/academics/grades',
      '/academics/homework',
      '/academics/daily-reports'
    ];
  }

  const nav: AppRoute[] = ['/dashboard', '/communication'];
  if (roles.some((x) => x === 'SchoolAdministrator' || x === 'Teacher')) {
    nav.push(
      '/organization',
      '/organization/school-years',
      '/organization/classes',
      '/organization/groups',
      '/organization/subjects',
      '/organization/teacher-assignments',
      '/academics',
      '/academics/timetable',
      '/academics/lesson-records',
      '/academics/attendance',
      '/academics/excuses',
      '/academics/grades',
      '/academics/homework',
      '/academics/daily-reports'
    );
  }
  if (roles.includes('SchoolAdministrator')) {
    nav.push('/organization/grade-levels');
    if (schoolType === 'SecondarySchool') {
      nav.push('/organization/fields-of-study');
    }
    nav.push('/administration');
  }
  if (roles.includes('Teacher')) {
    nav.push('/organization/groups', '/organization/subjects');
  }
  if (schoolType === 'Kindergarten' && !nav.includes('/academics')) {
    nav.push('/academics', '/academics/attendance', '/academics/daily-reports');
  }
  return nav;
}

function labelForRoute(route: AppRoute, t: ReturnType<typeof useI18n>['t']) {
  if (route === '/dashboard') return t('routeDashboard');
  if (route === '/organization') return t('routeOrganization');
  if (route === '/organization/schools') return `${t('routeOrganization')} / ${t('navSchools')}`;
  if (route === '/organization/school-years') return `${t('routeOrganization')} / ${t('navSchoolYears')}`;
  if (route === '/organization/grade-levels') return `${t('routeOrganization')} / ${t('navGradeLevels')}`;
  if (route === '/organization/classes') return `${t('routeOrganization')} / ${t('navClasses')}`;
  if (route === '/organization/groups') return `${t('routeOrganization')} / ${t('navGroups')}`;
  if (route === '/organization/subjects') return `${t('routeOrganization')} / ${t('navSubjects')}`;
  if (route === '/organization/fields-of-study') return `${t('routeOrganization')} / ${t('navFieldsOfStudy')}`;
  if (route === '/organization/teacher-assignments') return `${t('routeOrganization')} / ${t('navTeacherAssignments')}`;
  if (route === '/academics') return t('routeAcademics');
  if (route === '/academics/timetable') return `${t('routeAcademics')} / ${t('navTimetable')}`;
  if (route === '/academics/lesson-records') return `${t('routeAcademics')} / ${t('navLessonRecords')}`;
  if (route === '/academics/attendance') return `${t('routeAcademics')} / ${t('navAttendance')}`;
  if (route === '/academics/excuses') return `${t('routeAcademics')} / ${t('navExcuses')}`;
  if (route === '/academics/grades') return `${t('routeAcademics')} / ${t('navGrades')}`;
  if (route === '/academics/homework') return `${t('routeAcademics')} / ${t('navHomework')}`;
  if (route === '/academics/daily-reports') return `${t('routeAcademics')} / ${t('navDailyReports')}`;
  if (route === '/communication') return t('routeCommunication');
  if (route === '/administration') return t('routeAdministration');
  if (route === '/identity') return t('myProfile.title');
  if (route === '/identity/security') return t('security.title');
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
    roles: extractRolesFromClaims(claims),
    schoolType: (claims['school_type'] as SchoolType) ?? 'ElementarySchool',
    schoolIds: Array.isArray(claims['school_id']) ? claims['school_id'] as string[] : claims['school_id'] ? [claims['school_id'] as string] : [],
    linkedStudentIds: Array.isArray(claims['linked_student_id']) ? claims['linked_student_id'] as string[] : claims['linked_student_id'] ? [claims['linked_student_id'] as string] : []
  };
}

function beginLogout(config: SkolioBootstrapConfig, onSession: (state: SessionState | null) => void, reason: 'manual' | 'idle' = 'manual') {
  if (reason === 'idle') {
    sessionStorage.setItem(logoutReasonStorageKey, 'idle');
  }

  localStorage.setItem(idleLogoutStorageKey, `${Date.now()}:${reason}`);
  clearSession();
  onSession(null);

  const params = new URLSearchParams({
    post_logout_redirect_uri: config.oidcPostLogoutRedirectUri,
    client_id: config.oidcClientId,
    logoutReason: reason
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
















