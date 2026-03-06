export type SkolioBootstrapConfig = {
  identityAuthority: string;
  oidcClientId: string;
  oidcRedirectUri: string;
  oidcPostLogoutRedirectUri: string;
  oidcScope: string;
  organizationApi: string;
  academicsApi: string;
  communicationApi: string;
  administrationApi: string;
};

const bootstrapUrl = '/bootstrap-config';

export async function loadBootstrapConfig(): Promise<SkolioBootstrapConfig> {
  const response = await fetch(bootstrapUrl, { credentials: 'same-origin' });
  const contentType = response.headers.get('content-type') ?? '';

  if (!response.ok) {
    const body = await response.text();
    throw new Error(
      `Unable to load bootstrap config from ${bootstrapUrl}: HTTP ${response.status} ${response.statusText}. ` +
      `Content-Type: ${contentType || 'unknown'}. Response body: ${body.slice(0, 400)}`
    );
  }

  if (!contentType.toLowerCase().includes('application/json')) {
    const body = await response.text();
    throw new Error(
      `Unable to load bootstrap config from ${bootstrapUrl}: expected application/json but received ` +
      `'${contentType || 'unknown'}'. Response body: ${body.slice(0, 400)}`
    );
  }

  return (await response.json()) as SkolioBootstrapConfig;
}
