import React, { type ReactNode } from 'react';

export function AppHeader({
  title,
  subtitle,
  notifications,
  profileLabel,
  onLogout,
  rightSlot
}: {
  title: string;
  subtitle: string;
  notifications?: string;
  profileLabel: string;
  onLogout: () => void;
  rightSlot?: ReactNode;
}) {
  return (
    <header className="sk-topbar">
      <div>
        <h1 className="sk-topbar-title">{title}</h1>
        <p className="sk-topbar-subtitle">{subtitle}</p>
      </div>
      <div className="sk-topbar-actions">
        {rightSlot}
        <button className="sk-pill" type="button">{notifications ?? 'Notifications'}</button>
        <button className="sk-pill" type="button">{profileLabel}</button>
        <button className="sk-btn sk-btn-secondary" onClick={onLogout} type="button">Sign out</button>
      </div>
    </header>
  );
}

export function SectionHeader({
  title,
  description,
  action
}: {
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <div className="sk-section-header">
      <div>
        <h2 className="sk-section-title">{title}</h2>
        {description ? <p className="sk-section-desc">{description}</p> : null}
      </div>
      {action ? <div>{action}</div> : null}
    </div>
  );
}

export function WidgetGrid({ children }: { children: ReactNode }) {
  return <div className="sk-widget-grid">{children}</div>;
}

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <article className={`sk-card ${className}`.trim()}>{children}</article>;
}

export function StatusBadge({ label, tone = 'neutral' }: { label: string; tone?: 'neutral' | 'good' | 'warn' | 'info' }) {
  return <span className={`sk-badge sk-badge-${tone}`}>{label}</span>;
}
