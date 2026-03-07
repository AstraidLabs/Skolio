import React, { type ReactNode } from 'react';

export function AppFooter({
  languageSwitcher,
  mode
}: {
  languageSwitcher?: ReactNode;
  mode: string;
}) {
  return (
    <footer className="sk-app-footer">
      <div className="sk-app-footer-meta">
        <span>Skolio</span>
        <span>Application shell</span>
        <span>{mode}</span>
      </div>
      <FooterLanguageSwitcher>{languageSwitcher}</FooterLanguageSwitcher>
    </footer>
  );
}

export function FooterLanguageSwitcher({ children }: { children?: ReactNode }) {
  return <div className="sk-footer-language">{children}</div>;
}
