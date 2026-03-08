import React, { type ReactNode } from 'react';
import { useI18n } from '../../i18n';

export function AppFooter({
  languageSwitcher,
  mode
}: {
  languageSwitcher?: ReactNode;
  mode: string;
}) {
  const { t } = useI18n();

  return (
    <footer className="sk-app-footer">
      <div className="sk-app-footer-meta">
        <span>Skolio</span>
        <span>{t('shellApplication')}</span>
        <span>{mode}</span>
      </div>
      <FooterLanguageSwitcher>{languageSwitcher}</FooterLanguageSwitcher>
    </footer>
  );
}

export function FooterLanguageSwitcher({ children }: { children?: ReactNode }) {
  return <div className="sk-footer-language">{children}</div>;
}
