import { useEffect, useMemo, useState, type ReactNode } from 'react';
import type { SkolioBootstrapConfig } from './bootstrap';
import { createHttpClient } from './shared/http/httpClient';
import { clearPkce, clearSession, loadPkce, loadSession, normalizeRoles, parseJwt, persistPkce, persistSession, type SchoolType, type SessionState } from './shared/auth/session';
import { createOrganizationApi } from './organization/api';
import { createAcademicsApi } from './academics/api';
import { createCommunicationApi } from './communication/api';
import { createAdministrationApi } from './administration/api';
import { createIdentityApi } from './identity/api';

type RouterProps = { config: SkolioBootstrapConfig };
type AppRoute = '/dashboard' | '/organization' | '/academics' | '/communication' | '/administration' | '/identity';

export function RouterShell({ config }: RouterProps) {
  const [session, setSession] = useState<SessionState | null>(() => loadSession());
  const [route, setRoute] = useState(window.location.pathname as AppRoute | '/auth/callback');

  useEffect(() => {
    const onPop = () => setRoute(window.location.pathname as AppRoute);
    window.addEventListener('popstate', onPop);
    return () => window.removeEventListener('popstate', onPop);
  }, []);

  const http = useMemo(() => createHttpClient(config), [config]);
  const apis = useMemo(() => ({
    organization: createOrganizationApi(http), academics: createAcademicsApi(http), communication: createCommunicationApi(http), administration: createAdministrationApi(http), identity: createIdentityApi(http)
  }), [http]);

  if (route === '/auth/callback') return <AuthCallbackPage config={config} onSession={setSession} />;
  if (!session) return <LoginPage config={config} />;

  if (Date.now() > session.expiresAtUtc) {
    clearSession();
    return <LoginPage config={config} />;
  }

  const nav = navigationFor(session.roles, session.schoolType);
  const active = nav.includes(route as AppRoute) ? (route as AppRoute) : '/dashboard';

  return <AppShell session={session} nav={nav} active={active} onNavigate={setRoute} onLogout={() => beginLogout(config, setSession)}>
    {active === '/dashboard' && <DashboardPage session={session} />}
    {active === '/organization' && <OrganizationPage api={apis.organization} />}
    {active === '/academics' && <AcademicsPage api={apis.academics} schoolType={session.schoolType} />}
    {active === '/communication' && <CommunicationPage api={apis.communication} session={session} config={config} />}
    {active === '/administration' && <AdministrationPage api={apis.administration} />}
    {active === '/identity' && <IdentityPage api={apis.identity} />}
  </AppShell>;
}

function LoginPage({ config }: { config: SkolioBootstrapConfig }) {
  return <section className="space-y-4"><h1 className="text-2xl font-semibold">Skolio</h1><button className="rounded bg-indigo-600 px-3 py-2 text-white" onClick={() => void beginLogin(config)}>Sign in</button></section>;
}

function AppShell({ session, nav, active, onNavigate, onLogout, children }: { session: SessionState; nav: AppRoute[]; active: AppRoute; onNavigate: (r: AppRoute) => void; onLogout: () => void; children: ReactNode }) {
  return <section className="space-y-4"><header className="flex items-center justify-between border-b pb-3"><div><h1 className="text-xl font-semibold">Skolio App Shell</h1><p className="text-xs text-slate-600">{session.roles.join(', ')} · {session.schoolType}</p></div><button className="rounded bg-slate-700 px-3 py-2 text-white" onClick={onLogout}>Sign out</button></header><nav className="flex flex-wrap gap-2">{nav.map((item) => <button key={item} className={`rounded px-3 py-1 ${active === item ? 'bg-indigo-600 text-white' : 'bg-slate-200'}`} onClick={() => { history.pushState({}, '', item); onNavigate(item); }}>{item.replace('/', '')}</button>)}</nav><div>{children}</div></section>;
}

function DashboardPage({ session }: { session: SessionState }) {
  const role = session.roles[0] ?? 'User';
  const summary = session.schoolType === 'Kindergarten' ? 'Skupiny, daily reports, attendance a rodičovská komunikace.' : session.schoolType === 'SecondarySchool' ? 'Třídy, předměty, obory a širší školní agenda.' : 'Třídy, předměty, rozvrh, docházka, známky a úkoly.';
  return <section className="space-y-3"><h2 className="text-lg font-semibold">{role} dashboard</h2><div className="rounded border p-3">{summary}</div></section>;
}

function OrganizationPage({ api }: { api: ReturnType<typeof createOrganizationApi> }) {
  const [schoolId, setSchoolId] = useState('');
  const [schools, setSchools] = useState<any[]>([]);
  const [err, setErr] = useState('');
  useEffect(() => { void api.schools().then(setSchools).catch((e: Error) => setErr(e.message)); }, [api]);
  return <section className="space-y-3"><h2 className="font-semibold">Organization</h2>{err && <p className="text-sm text-red-700">{err}</p>}<ul className="text-sm">{schools.map((s) => <li key={s.id}>{s.name} ({s.schoolType})</li>)}</ul><input className="rounded border p-2" placeholder="SchoolId for detail flows" value={schoolId} onChange={(e) => setSchoolId(e.target.value)} /></section>;
}

function AcademicsPage({ api, schoolType }: { api: ReturnType<typeof createAcademicsApi>; schoolType: SchoolType }) {
  const [schoolId, setSchoolId] = useState('');
  const [dailyReports, setDailyReports] = useState<any[]>([]);
  return <section className="space-y-3"><h2 className="font-semibold">Academics</h2><p className="text-sm text-slate-600">{schoolType === 'Kindergarten' ? 'Daily report workflow je zvýrazněný.' : 'Rozvrh, lessons, attendance, grades a homework.'}</p><input className="rounded border p-2" placeholder="SchoolId" value={schoolId} onChange={(e) => setSchoolId(e.target.value)} /><button className="rounded bg-indigo-600 px-3 py-1 text-white" onClick={() => void api.dailyReports(schoolId).then(setDailyReports)}>Load daily reports</button><ul>{dailyReports.map((x) => <li key={x.id}>{x.reportDate} · {x.summary}</li>)}</ul></section>;
}

function CommunicationPage({ api, session, config }: { api: ReturnType<typeof createCommunicationApi>; session: SessionState; config: SkolioBootstrapConfig }) {
  const [notifications, setNotifications] = useState<any[]>([]);
  const [schoolId, setSchoolId] = useState('');
  const [announcements, setAnnouncements] = useState<any[]>([]);

  useEffect(() => {
    void api.notifications(session.subject).then(setNotifications).catch(() => undefined);
    const hub = new WebSocket(config.communicationApi.replace('http', 'ws') + '/hubs/communication');
    hub.onmessage = () => void api.notifications(session.subject).then(setNotifications).catch(() => undefined);
    return () => hub.close();
  }, [api, config.communicationApi, session.subject]);

  return <section className="space-y-3"><h2 className="font-semibold">Communication</h2><input className="rounded border p-2" placeholder="SchoolId" value={schoolId} onChange={(e) => setSchoolId(e.target.value)} /><button className="rounded bg-indigo-600 px-3 py-1 text-white" onClick={() => void api.announcements(schoolId).then(setAnnouncements)}>Load announcements</button><ul>{announcements.map((a) => <li key={a.id}>{a.title}</li>)}</ul><div className="rounded border p-2"><h3 className="font-medium">Notifications</h3>{notifications.map((n) => <p key={n.id} className="text-sm">{n.title}</p>)}</div></section>;
}

function AdministrationPage({ api }: { api: ReturnType<typeof createAdministrationApi> }) {
  const [settings, setSettings] = useState<any[]>([]);
  const [audit, setAudit] = useState<any[]>([]);
  useEffect(() => { void api.settings().then(setSettings); void api.auditLogs().then(setAudit); }, [api]);
  return <section className="space-y-3"><h2 className="font-semibold">Administration</h2><h3 className="font-medium">System settings</h3><ul>{settings.map((s) => <li key={s.id}>{s.key}</li>)}</ul><h3 className="font-medium">Audit log</h3><ul>{audit.slice(0, 10).map((a) => <li key={a.id}>{a.actionCode}</li>)}</ul></section>;
}

function IdentityPage({ api }: { api: ReturnType<typeof createIdentityApi> }) {
  const [profile, setProfile] = useState<any>();
  const [roles, setRoles] = useState<any[]>([]);
  const [links, setLinks] = useState<any[]>([]);
  const [error, setError] = useState('');
  useEffect(() => {
    void api.myProfile().then(setProfile).catch((e: unknown) => setError((e as Error).message));
    void api.roleAssignments().then(setRoles).catch(() => undefined);
    void api.parentStudentLinks().then(setLinks).catch(() => undefined);
  }, [api]);
  return <section className="space-y-3"><h2 className="font-semibold">Identity</h2>{error && <p className="text-sm text-red-700">{error}</p>}{profile && <div className="rounded border p-3 text-sm">{profile.firstName} {profile.lastName} ({profile.email})</div>}<p className="text-sm">Role assignments: {roles.length}</p><p className="text-sm">Parent-student links: {links.length}</p></section>;
}

function navigationFor(roles: string[], schoolType: SchoolType): AppRoute[] {
  const nav: AppRoute[] = ['/dashboard', '/identity', '/communication'];
  if (roles.some((x) => x === 'SchoolAdministrator' || x === 'Teacher')) nav.push('/organization', '/academics');
  if (roles.some((x) => x === 'PlatformAdministrator' || x === 'SchoolAdministrator')) nav.push('/administration');
  if (schoolType === 'Kindergarten' && !nav.includes('/academics')) nav.push('/academics');
  return nav;
}

function AuthCallbackPage({ config, onSession }: { config: SkolioBootstrapConfig; onSession: (state: SessionState | null) => void }) {
  const [status, setStatus] = useState('Processing callback...');
  useEffect(() => {
    void completeAuthorizationCodeFlow(config).then((nextSession) => { persistSession(nextSession); onSession(nextSession); setStatus('Authentication completed. Redirecting...'); window.location.replace('/dashboard'); }).catch((error) => setStatus(`Authentication failed: ${error instanceof Error ? error.message : 'unknown error'}`));
  }, [config, onSession]);
  return <section className="text-sm text-slate-700">{status}</section>;
}

async function beginLogin(config: SkolioBootstrapConfig) {
  const verifier = randomString(); const state = randomString(); const challenge = await sha256ToBase64Url(verifier); persistPkce(verifier, state);
  const params = new URLSearchParams({ client_id: config.oidcClientId, response_type: 'code', scope: config.oidcScope, redirect_uri: config.oidcRedirectUri, code_challenge: challenge, code_challenge_method: 'S256', state });
  window.location.href = `${config.identityAuthority}/connect/authorize?${params.toString()}`;
}

async function completeAuthorizationCodeFlow(config: SkolioBootstrapConfig): Promise<SessionState> {
  const callback = new URL(window.location.href); const code = callback.searchParams.get('code'); const state = callback.searchParams.get('state'); const pkce = loadPkce();
  if (!code || !pkce) throw new Error('Missing authorization code state.'); if (pkce.state !== state) throw new Error('State validation failed.');
  const body = new URLSearchParams({ grant_type: 'authorization_code', client_id: config.oidcClientId, code, redirect_uri: config.oidcRedirectUri, code_verifier: pkce.verifier });
  const response = await fetch(`${config.identityAuthority}/connect/token`, { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body });
  if (!response.ok) throw new Error(`Token exchange failed with ${response.status}`);
  const token = (await response.json()) as { access_token: string; expires_in: number }; const claims = parseJwt(token.access_token); clearPkce();
  return { accessToken: token.access_token, expiresAtUtc: Date.now() + token.expires_in * 1000, subject: claims.sub ?? 'unknown', roles: normalizeRoles(claims.role), schoolType: (claims['school_type'] as SchoolType) ?? 'ElementarySchool' };
}

function beginLogout(config: SkolioBootstrapConfig, onSession: (state: SessionState | null) => void) {
  clearSession(); onSession(null); const params = new URLSearchParams({ post_logout_redirect_uri: config.oidcPostLogoutRedirectUri, client_id: config.oidcClientId });
  window.location.href = `${config.identityAuthority}/connect/logout?${params.toString()}`;
}

function randomString() { const bytes = crypto.getRandomValues(new Uint8Array(32)); return Array.from(bytes, (value) => value.toString(16).padStart(2, '0')).join(''); }
async function sha256ToBase64Url(value: string) { const encoded = new TextEncoder().encode(value); const digest = await crypto.subtle.digest('SHA-256', encoded); const b64 = btoa(String.fromCharCode(...new Uint8Array(digest))); return b64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, ''); }
