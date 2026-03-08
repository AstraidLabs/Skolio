import React from 'react';
import { useI18n } from '../../i18n';

export function LoadingState({ text }: { text?: string }) {
  const { t } = useI18n();
  return <div className="sk-state sk-state-loading">{text ?? t('loading')}</div>;
}

export function EmptyState({ text }: { text?: string }) {
  const { t } = useI18n();
  return <div className="sk-state sk-state-empty">{text ?? t('noData')}</div>;
}

export function ErrorState({ text }: { text: string }) {
  return <div className="sk-state sk-state-error">{text}</div>;
}
