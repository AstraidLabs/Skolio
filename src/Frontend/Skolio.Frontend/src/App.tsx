import React from 'react';
import type { SkolioBootstrapConfig } from './bootstrap';
import { RouterShell } from './router';

type AppProps = {
  config: SkolioBootstrapConfig;
};

export function App({ config }: AppProps) {
  return (
    <main className="min-h-screen bg-slate-50 text-slate-900">
      <RouterShell config={config} />
    </main>
  );
}
