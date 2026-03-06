import React from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { loadBootstrapConfig } from './bootstrap';
import './styles.css';

const root = createRoot(document.getElementById('root')!);

loadBootstrapConfig()
  .then((config) => {
    root.render(
      <React.StrictMode>
        <App config={config} />
      </React.StrictMode>
    );
  })
  .catch((error) => {
    root.render(
      <React.StrictMode>
        <main className="p-6 text-red-700">
          Failed to load frontend bootstrap configuration: {error instanceof Error ? error.message : 'unknown error'}
        </main>
      </React.StrictMode>
    );
  });
