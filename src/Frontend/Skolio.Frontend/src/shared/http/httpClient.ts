import type { SkolioBootstrapConfig } from '../../bootstrap';
import { clearSession, loadSession } from '../auth/session';

export type ValidationProblem = { errors?: Record<string, string[]>; title?: string; status?: number };

export class SkolioHttpError extends Error {
  constructor(message: string, public status: number, public problem?: ValidationProblem) {
    super(message);
  }
}

export function createHttpClient(config: SkolioBootstrapConfig) {
  const resolveBase = (service: 'identity' | 'organization' | 'academics' | 'communication' | 'administration') => {
    switch (service) {
      case 'identity': return config.identityAuthority;
      case 'organization': return config.organizationApi;
      case 'academics': return config.academicsApi;
      case 'communication': return config.communicationApi;
      default: return config.administrationApi;
    }
  };

  return async function request<T>(service: 'identity' | 'organization' | 'academics' | 'communication' | 'administration', path: string, init?: RequestInit): Promise<T> {
    const session = loadSession();
    const headers = new Headers(init?.headers);
    headers.set('Content-Type', 'application/json');
    if (session) headers.set('Authorization', `Bearer ${session.accessToken}`);

    let response: Response;
    try {
      response = await fetch(`${resolveBase(service)}${path}`, { ...init, headers });
    } catch {
      await new Promise((resolve) => setTimeout(resolve, 300));
      response = await fetch(`${resolveBase(service)}${path}`, { ...init, headers });
    }

    if (response.status === 401) {
      clearSession();
      window.dispatchEvent(new CustomEvent('skolio:auth-expired'));
      window.location.replace('/');
      throw new SkolioHttpError('Session expired', 401);
    }

    if (response.status === 403) {
      throw new SkolioHttpError('Forbidden', 403);
    }

    if (!response.ok) {
      let problem: ValidationProblem | undefined;
      try { problem = (await response.json()) as ValidationProblem; } catch { /* ignore */ }
      throw new SkolioHttpError(problem?.title ?? `Request failed with ${response.status}`, response.status, problem);
    }

    if (response.status === 204) return undefined as T;
    return (await response.json()) as T;
  };
}
