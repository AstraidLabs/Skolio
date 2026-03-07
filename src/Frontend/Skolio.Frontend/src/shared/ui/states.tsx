import React from 'react';

export function LoadingState({ text = 'Loading...' }: { text?: string }) {
  return <div className="sk-state sk-state-loading">{text}</div>;
}

export function EmptyState({ text }: { text: string }) {
  return <div className="sk-state sk-state-empty">{text}</div>;
}

export function ErrorState({ text }: { text: string }) {
  return <div className="sk-state sk-state-error">{text}</div>;
}
