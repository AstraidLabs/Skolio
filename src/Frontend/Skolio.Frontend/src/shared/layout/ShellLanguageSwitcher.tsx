import React from 'react';
import { localeLabels, supportedLocales, useI18n, type Locale } from '../../i18n';

export function ShellLanguageSwitcher() {
  const { locale, setLocale, t } = useI18n();

  return (
    <label className="sk-footer-language-switch">
      <span className="sk-footer-language-label">{t('language')}</span>
      <select
        aria-label={t('selectLanguage')}
        className="sk-footer-language-select"
        onChange={(event) => setLocale(event.target.value as Locale)}
        value={locale}
      >
        {supportedLocales.map((value) => (
          <option key={value} value={value}>
            {localeLabels[value]}
          </option>
        ))}
      </select>
    </label>
  );
}
