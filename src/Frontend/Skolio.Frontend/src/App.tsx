import React from 'react';
import type { SkolioBootstrapConfig } from './bootstrap';
import { RouterShell } from './router';

type AppProps = {
  config: SkolioBootstrapConfig;
};

export function App({ config }: AppProps) {
  return (
    <main className="min-h-screen bg-slate-50 p-6 text-slate-900">
      <div className="mx-auto max-w-5xl rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        <RouterShell config={config} />
      </div>
    </main>
  );
}
