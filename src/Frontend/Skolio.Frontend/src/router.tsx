import type { SkolioBootstrapConfig } from './bootstrap';

type RouterProps = {
  config: SkolioBootstrapConfig;
};

export function RouterShell({ config }: RouterProps) {
  const route = window.location.pathname;

  if (route === '/status') {
    return <TechnicalStatusPage config={config} />;
  }

  return <HostShellPage config={config} />;
}

function HostShellPage({ config }: RouterProps) {
  return (
    <section className="space-y-4">
      <h1 className="text-2xl font-semibold">Skolio Frontend Shell</h1>
      <p className="text-slate-600">Technical bootstrap for Kindergarten, ElementarySchool and SecondarySchool.</p>
      <ul className="list-disc pl-6 text-sm text-slate-700">
        <li>Identity: {config.identityAuthority}</li>
        <li>Organization API: {config.organizationApi}</li>
        <li>Academics API: {config.academicsApi}</li>
        <li>Communication API: {config.communicationApi}</li>
        <li>Administration API: {config.administrationApi}</li>
      </ul>
    </section>
  );
}

function TechnicalStatusPage({ config }: RouterProps) {
  return (
    <section className="space-y-4">
      <h1 className="text-2xl font-semibold">Skolio Technical Status</h1>
      <pre className="rounded bg-slate-100 p-4 text-xs text-slate-800">{JSON.stringify(config, null, 2)}</pre>
    </section>
  );
}
