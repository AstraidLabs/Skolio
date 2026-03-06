import { useEffect, useMemo, useState } from 'react';
import type { SkolioBootstrapConfig } from './bootstrap';

type RouterProps = {
  config: SkolioBootstrapConfig;
};

type SessionState = {
  accessToken: string;
  expiresAtUtc: number;
  subject: string;
  roles: string[];
};

const sessionStorageKey = 'skolio.auth.session';
const pkceVerifierStorageKey = 'skolio.auth.pkce';

export function RouterShell({ config }: RouterProps) {
  const [session, setSession] = useState<SessionState | null>(() => loadSession());
  const route = window.location.pathname;

  useEffect(() => {
    if (session && Date.now() > session.expiresAtUtc) {
      clearSession();
      setSession(null);
    }
  }, [session]);

  if (route === '/auth/callback') {
    return <AuthCallbackPage config={config} onSession={setSession} />;
  }

  if (route === '/status') {
    return <TechnicalStatusPage config={config} session={session} />;
  }

  return <HostShellPage config={config} session={session} onSession={setSession} />;
}

function HostShellPage({ config, session, onSession }: RouterProps & { session: SessionState | null; onSession: (state: SessionState | null) => void; }) {
  const hasAdminAccess = !!session?.roles.some((role) => role === 'PlatformAdministrator' || role === 'SchoolAdministrator');

  return (
    <section className="space-y-4">
      <h1 className="text-2xl font-semibold">Skolio Frontend Auth Shell</h1>
      <p className="text-slate-600">Authorization Code + PKCE without refresh tokens (sessionStorage token). </p>
      {!session ? (
        <button className="rounded bg-indigo-600 px-3 py-2 text-white" onClick={() => beginLogin(config)}>
          Sign in
        </button>
      ) : (
        <div className="space-y-3 rounded border border-slate-200 p-3">
          <div className="text-sm">subject: {session.subject}</div>
          <div className="text-sm">roles: {session.roles.join(', ')}</div>
          <div className="flex gap-2">
            <button className="rounded bg-slate-700 px-3 py-2 text-white" onClick={() => beginLogout(config, onSession)}>
              Sign out
            </button>
          </div>
        </div>
      )}
      <ul className="list-disc pl-6 text-sm text-slate-700">
        <li>Identity: {config.identityAuthority}</li>
        <li>Organization API: {config.organizationApi}</li>
        <li>Academics API: {config.academicsApi}</li>
        <li>Communication API: {config.communicationApi}</li>
        <li>Administration API: {config.administrationApi}</li>
      </ul>
      <div className="rounded bg-slate-100 p-3 text-sm">
        Administration route access: {hasAdminAccess ? 'allowed' : 'denied'}
      </div>
    </section>
  );
}

function AuthCallbackPage({ config, onSession }: { config: SkolioBootstrapConfig; onSession: (state: SessionState | null) => void; }) {
  const [status, setStatus] = useState('Processing callback...');

  useEffect(() => {
    void completeAuthorizationCodeFlow(config)
      .then((nextSession) => {
        persistSession(nextSession);
        onSession(nextSession);
        setStatus('Authentication completed. Redirecting...');
        window.location.replace('/');
      })
      .catch((error) => setStatus(`Authentication failed: ${error instanceof Error ? error.message : 'unknown error'}`));
  }, [config, onSession]);

  return <section className="text-sm text-slate-700">{status}</section>;
}

function TechnicalStatusPage({ config, session }: { config: SkolioBootstrapConfig; session: SessionState | null }) {
  const payload = useMemo(() => ({ config, session }), [config, session]);
  return (
    <section className="space-y-4">
      <h1 className="text-2xl font-semibold">Skolio Technical Status</h1>
      <pre className="rounded bg-slate-100 p-4 text-xs text-slate-800">{JSON.stringify(payload, null, 2)}</pre>
    </section>
  );
}

async function beginLogin(config: SkolioBootstrapConfig) {
  const verifier = randomString();
  const state = randomString();
  const challenge = await sha256ToBase64Url(verifier);
  sessionStorage.setItem(pkceVerifierStorageKey, JSON.stringify({ verifier, state }));

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

async function completeAuthorizationCodeFlow(config: SkolioBootstrapConfig): Promise<SessionState> {
  const callback = new URL(window.location.href);
  const code = callback.searchParams.get('code');
  const state = callback.searchParams.get('state');
  const payload = sessionStorage.getItem(pkceVerifierStorageKey);

  if (!code || !payload) {
    throw new Error('Missing authorization code state.');
  }

  const pkce = JSON.parse(payload) as { verifier: string; state: string };
  if (pkce.state !== state) {
    throw new Error('State validation failed.');
  }

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

  if (!response.ok) {
    throw new Error(`Token exchange failed with ${response.status}`);
  }

  const token = (await response.json()) as { access_token: string; expires_in: number };
  const claims = parseJwt(token.access_token);
  sessionStorage.removeItem(pkceVerifierStorageKey);

  return {
    accessToken: token.access_token,
    expiresAtUtc: Date.now() + token.expires_in * 1000,
    subject: claims.sub ?? 'unknown',
    roles: normalizeRoles(claims.role)
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

function loadSession(): SessionState | null {
  const raw = sessionStorage.getItem(sessionStorageKey);
  if (!raw) {
    return null;
  }

  return JSON.parse(raw) as SessionState;
}

function persistSession(session: SessionState) {
  sessionStorage.setItem(sessionStorageKey, JSON.stringify(session));
}

function clearSession() {
  sessionStorage.removeItem(sessionStorageKey);
  sessionStorage.removeItem(pkceVerifierStorageKey);
}

function parseJwt(token: string): Record<string, string | string[]> {
  const [, payload] = token.split('.');
  const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
  return JSON.parse(json) as Record<string, string | string[]>;
}

function normalizeRoles(roleClaim: string | string[] | undefined): string[] {
  if (!roleClaim) return [];
  return Array.isArray(roleClaim) ? roleClaim : [roleClaim];
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
