import React from 'react';
import ReactDOM from 'react-dom/client';
import './styles.css';

function App() {
  return (
    <main className="min-h-screen bg-slate-100 p-6 text-slate-900">
      <h1 className="text-2xl font-semibold">SchoolPlatform Frontend Shell</h1>
      <p className="mt-2">React + Vite + Tailwind host for role-based dashboards.</p>
    </main>
  );
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
