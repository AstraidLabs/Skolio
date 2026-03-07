import React, { createContext, useContext, useMemo, useState, type ReactNode } from 'react';

export type Locale = 'cs' | 'sk' | 'de' | 'pl' | 'en';

const STORAGE_KEY = 'skolio.locale';

const localeLabelsInternal: Record<Locale, string> = {
  cs: 'CZ',
  sk: 'SK',
  de: 'DE',
  pl: 'PL',
  en: 'EN'
};

const en = {
  appTitle: 'Skolio App Shell',
  platform: 'Platform',
  signIn: 'Sign in',
  signOut: 'Sign out',
  loginTitle: 'Sign in to Skolio',
  loginSubtitle: 'Use your credentials.',
  email: 'Email',
  password: 'Password',
  goToLogin: 'Go to login',
  landingTitle: 'Skolio',
  landingSubtitle: 'Modern platform for schools. Organization, academics, communication, and administration in one place.',
  landingHeroTag: 'Digital platform for schools',
  landingHeroTitle: 'Skolio connects academics, communication and administration in one place',
  landingHeroText: 'From kindergarten to secondary school. Clear workflows for leaders, teachers and parents without switching between disconnected tools.',
  landingStat1: 'Schools managed in one platform',
  landingStat2: 'Fast rollout and onboarding',
  landingStat3: 'Unified sign-in and role model',
  landingModulesTitle: 'Platform modules',
  landingModulesText: 'Each module solves a concrete area of school operations while sharing one identity and one data backbone.',
  landingProcessTitle: 'How it works',
  landingProcess1Title: '1. School setup',
  landingProcess1Text: 'Configure organization, roles, classes and school structure in a guided flow.',
  landingProcess2Title: '2. Daily operations',
  landingProcess2Text: 'Schedule, attendance, grades, homework and announcements in one workspace.',
  landingProcess3Title: '3. Governance',
  landingProcess3Text: 'Administration, audit trail and clear accountability across the whole stack.',
  landingCtaTitle: 'Ready to start?',
  landingCtaText: 'Open the sign-in screen and enter your Skolio workspace.',
  landingFooter: 'Skolio platform for modern schools',
  featureOrganizationTitle: 'Organization',
  featureOrganizationText: 'Schools, classes, roles and organizational structure.',
  featureAcademicsTitle: 'Academics',
  featureAcademicsText: 'Schedule, attendance, grades, assignments and daily agenda.',
  featureCommunicationTitle: 'Communication',
  featureCommunicationText: 'Announcements and communication between school, teachers and parents.',
  routeDashboard: 'Dashboard',
  routeOrganization: 'Organization',
  routeAcademics: 'Academics',
  routeCommunication: 'Communication',
  routeAdministration: 'Administration',
  routeIdentity: 'Identity',
  roleUser: 'User',
  dashboardSuffix: 'dashboard',
  dashboardKindergarten: 'Groups, daily reports, attendance and parent communication.',
  dashboardSecondary: 'Classes, subjects, study programs and broader school agenda.',
  dashboardDefault: 'Classes, subjects, schedule, attendance, grades and homework.',
  loadingOrganization: 'Loading organization view...',
  organizationTitle: 'Organization',
  academicsTitle: 'Academics',
  academicsKindergartenHint: 'Daily report workflow is emphasized.',
  academicsDefaultHint: 'Schedule, lessons, attendance, grades and homework.',
  loadDailyReports: 'Load daily reports',
  communicationTitle: 'Communication',
  connectionState: 'Connection',
  connected: 'connected',
  disconnected: 'disconnected',
  retrying: 'retrying',
  reload: 'Reload',
  administrationTitle: 'Administration',
  systemSettings: 'System settings',
  auditLog: 'Audit log',
  identityTitle: 'Identity',
  unauthorizedAdministration: 'You are not authorized for administration route.',
  unauthorizedIdentity: 'You are not authorized for identity profile details.',
  processingCallback: 'Processing callback...',
  authCompleted: 'Authentication completed. Redirecting...',
  authFailed: 'Authentication failed',
  missingAuthState: 'Missing authorization code state.',
  stateValidationFailed: 'State validation failed.',
  tokenExchangeFailed: 'Token exchange failed with {status}'
} as const;

type TranslationKey = keyof typeof en;

type Translations = Record<TranslationKey, string>;

const cs: Translations = {
  ...en,
  appTitle: 'Skolio Aplikace',
  platform: 'Platforma',
  signIn: 'P\u0159ihl\u00E1sit se',
  signOut: 'Odhl\u00E1sit se',
  loginTitle: 'P\u0159ihl\u00E1\u0161en\u00ED do Skolio',
  loginSubtitle: 'Pou\u017Eijte sv\u00E9 p\u0159ihla\u0161ovac\u00ED \u00FAdaje.',
  goToLogin: 'P\u0159ej\u00EDt na p\u0159ihl\u00E1\u0161en\u00ED',
  landingSubtitle: 'Modern\u00ED platforma pro \u0161koly. Organizace, studijn\u00ED agenda, komunikace a administrace na jednom m\u00EDst\u011B.',
  landingHeroTag: 'Digit\u00E1ln\u00ED platforma pro \u0161koly',
  landingHeroTitle: 'Skolio propojuje v\u00FDuku, komunikaci a administraci na jednom m\u00EDst\u011B',
  landingHeroText: 'Od mate\u0159sk\u00E9 \u0161koly po st\u0159edn\u00ED \u0161kolu. P\u0159ehledn\u00E1 agenda pro veden\u00ED, u\u010Ditele i rodi\u010De, bez zbyte\u010Dn\u00FDch p\u0159ep\u00EDn\u00E1n\u00ED mezi syst\u00E9my.',
  landingStat1: '\u0160koly pod jednou spr\u00E1vou',
  landingStat2: 'Rychl\u00E9 nasazen\u00ED a onboarding',
  landingStat3: 'Jednotn\u00E9 p\u0159ihl\u00E1\u0161en\u00ED a role',
  landingModulesTitle: 'Moduly platformy',
  landingModulesText: 'Ka\u017Ed\u00FD modul \u0159e\u0161\u00ED konkr\u00E9tn\u00ED oblast provozu \u0161koly, ale v\u0161e sd\u00EDl\u00ED jednu identitu a jednotn\u00E1 data.',
  landingProcessTitle: 'Jak to funguje',
  landingProcess1Title: '1. Nastaven\u00ED \u0161koly',
  landingProcess1Text: 'Zalo\u017Een\u00ED organizace, rol\u00ED, t\u0159\u00EDd a struktury \u0161koly b\u011Bhem n\u011Bkolika krok\u016F.',
  landingProcess2Title: '2. Denn\u00ED provoz',
  landingProcess2Text: 'Rozvrh, doch\u00E1zka, zn\u00E1mky, \u00FAkoly i ozn\u00E1men\u00ED v jednom pracovn\u00EDm prostoru.',
  landingProcess3Title: '3. P\u0159ehled a kontrola',
  landingProcess3Text: 'Administrace, auditn\u00ED stopa a jasn\u00E1 odpov\u011Bdnost v r\u00E1mci cel\u00E9ho stacku.',
  landingCtaTitle: 'P\u0159ipraveni za\u010D\u00EDt?',
  landingCtaText: 'P\u0159ejd\u011Bte na p\u0159ihl\u00E1\u0161en\u00ED a otev\u0159ete sv\u00E9 pracovn\u00ED prost\u0159ed\u00ED ve Skolio.',
  landingFooter: 'Skolio platforma pro modern\u00ED \u0161kolu',
  featureOrganizationTitle: 'Organizace',
  featureOrganizationText: '\u0160koly, t\u0159\u00EDdy, role a organiza\u010Dn\u00ED struktura.',
  featureAcademicsTitle: 'Studium',
  featureAcademicsText: 'Rozvrh, doch\u00E1zka, zn\u00E1mky, \u00FAkoly a denn\u00ED agenda.',
  featureCommunicationTitle: 'Komunikace',
  featureCommunicationText: 'Ozn\u00E1men\u00ED a spojen\u00ED mezi \u0161kolou, u\u010Diteli a rodi\u010Di.',
  routeDashboard: 'P\u0159ehled',
  routeAcademics: 'Studium',
  routeIdentity: 'Profil',
  roleUser: 'U\u017Eivatel',
  dashboardSuffix: 'p\u0159ehled',
  dashboardKindergarten: 'Skupiny, denn\u00ED reporty, doch\u00E1zka a komunikace s rodi\u010Di.',
  dashboardSecondary: 'T\u0159\u00EDdy, p\u0159edm\u011Bty, obory a \u0161ir\u0161\u00ED \u0161koln\u00ED agenda.',
  dashboardDefault: 'T\u0159\u00EDdy, p\u0159edm\u011Bty, rozvrh, doch\u00E1zka, zn\u00E1mky a \u00FAkoly.',
  loadingOrganization: 'Na\u010D\u00EDt\u00E1m organiza\u010Dn\u00ED pohled...',
  academicsKindergartenHint: 'Workflow denn\u00EDch report\u016F je zv\u00FDrazn\u011Bn\u00FD.',
  academicsDefaultHint: 'Rozvrh, v\u00FDuka, doch\u00E1zka, zn\u00E1mky a \u00FAkoly.',
  loadDailyReports: 'Na\u010D\u00EDst denn\u00ED reporty',
  connectionState: 'P\u0159ipojen\u00ED',
  connected: 'p\u0159ipojeno',
  disconnected: 'odpojeno',
  administrationTitle: 'Administrace',
  systemSettings: 'Syst\u00E9mov\u00E1 nastaven\u00ED',
  unauthorizedAdministration: 'Nem\u00E1te opr\u00E1vn\u011Bn\u00ED pro administraci.',
  unauthorizedIdentity: 'Nem\u00E1te opr\u00E1vn\u011Bn\u00ED pro detaily profilu.',
  processingCallback: 'Zpracov\u00E1v\u00E1m p\u0159ihl\u00E1\u0161en\u00ED...',
  authCompleted: 'P\u0159ihl\u00E1\u0161en\u00ED dokon\u010Deno. P\u0159esm\u011Brov\u00E1v\u00E1m...',
  authFailed: 'P\u0159ihl\u00E1\u0161en\u00ED selhalo',
  missingAuthState: 'Chyb\u00ED autoriza\u010Dn\u00ED stav.',
  stateValidationFailed: 'Validace stavu selhala.',
  tokenExchangeFailed: 'V\u00FDm\u011Bna tokenu selhala se stavem {status}'
};

const sk: Translations = {
  ...en,
  appTitle: 'Skolio Aplikacia',
  signIn: 'Prihlasit sa',
  signOut: 'Odhlasit sa',
  goToLogin: 'Prejst na prihlasenie'
};

const de: Translations = {
  ...en,
  signIn: 'Anmelden',
  signOut: 'Abmelden',
  goToLogin: 'Zur Anmeldung'
};

const pl: Translations = {
  ...en,
  signIn: 'Zaloguj sie',
  signOut: 'Wyloguj sie',
  goToLogin: 'Przejdz do logowania'
};

const translations: Record<Locale, Translations> = {
  en,
  cs,
  sk,
  de,
  pl
};

type I18nContextValue = {
  locale: Locale;
  setLocale: (locale: Locale) => void;
  t: (key: TranslationKey, params?: Record<string, string | number>) => string;
};

const I18nContext = createContext<I18nContextValue | null>(null);

export function I18nProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(() => {
    const stored = localStorage.getItem(STORAGE_KEY) as Locale | null;
    if (stored && stored in localeLabelsInternal) {
      return stored;
    }

    const browser = (navigator.language ?? 'en').toLowerCase();
    if (browser.startsWith('cs')) return 'cs';
    if (browser.startsWith('sk')) return 'sk';
    if (browser.startsWith('de')) return 'de';
    if (browser.startsWith('pl')) return 'pl';
    return 'en';
  });

  const value = useMemo<I18nContextValue>(() => {
    const t = (key: TranslationKey, params?: Record<string, string | number>) => {
      let text = translations[locale][key] ?? translations.en[key] ?? key;
      if (params) {
        for (const [paramKey, paramValue] of Object.entries(params)) {
          text = text.replaceAll(`{${paramKey}}`, String(paramValue));
        }
      }

      return text;
    };

    return {
      locale,
      setLocale: (nextLocale: Locale) => {
        localStorage.setItem(STORAGE_KEY, nextLocale);
        setLocaleState(nextLocale);
      },
      t
    };
  }, [locale]);

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useI18n() {
  const context = useContext(I18nContext);
  if (!context) {
    throw new Error('useI18n must be used within I18nProvider');
  }

  return context;
}

export const supportedLocales = Object.keys(localeLabelsInternal) as Locale[];
export const localeLabels = localeLabelsInternal;
