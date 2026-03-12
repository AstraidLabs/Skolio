import React, { type ReactNode } from 'react';

export function OrganizationEntityModal({
  open,
  title,
  description,
  onClose,
  closeLabel = 'Close',
  children,
  className = ''
}: {
  open: boolean;
  title: string;
  description?: string;
  onClose: () => void;
  closeLabel?: string;
  children: ReactNode;
  className?: string;
}) {
  if (!open) {
    return null;
  }

  return (
    <div className="sk-modal-overlay" role="presentation" onClick={onClose}>
      <div
        className={`sk-modal ${className}`.trim()}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        onClick={(event) => event.stopPropagation()}
      >
        <div className="sk-modal-header">
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold text-slate-900">{title}</p>
            {description ? <p className="mt-1 text-xs text-slate-500">{description}</p> : null}
          </div>
          <button type="button" className="sk-btn sk-btn-secondary" onClick={onClose}>
            {closeLabel}
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}
