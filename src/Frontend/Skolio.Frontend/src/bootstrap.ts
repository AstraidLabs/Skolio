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

export async function loadBootstrapConfig(): Promise<SkolioBootstrapConfig> {
  const response = await fetch('/bootstrap-config', { credentials: 'same-origin' });

  if (!response.ok) {
    throw new Error(`Unable to load bootstrap config: ${response.status}`);
  }

  return (await response.json()) as SkolioBootstrapConfig;
}
