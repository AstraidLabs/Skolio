# Frontend i18n Complete Coverage

## Zapojeni i18n
- i18n init je v `src/Frontend/Skolio.Frontend/src/i18n.tsx`
- centralni `I18nProvider` drzi aktivni locale a funkci `t(key, params)`
- komponenty pouzivaji `useI18n()` a translation key volani `t(...)`

## Podporovane jazyky
- `cs` (default + fallback)
- `en`
- `sk`
- `de`
- `pl`

Zobrazene nazvy jazyku ve footer switcheru:
- Čeština
- English
- Slovenčina
- Deutsch
- Polski

## Fallback a persistence
- vychozi jazyk: `cs`
- fallback jazyk: `cs`
- perzistence volby: `localStorage` (`skolio.locale`)
- pri neplatne hodnote v ulozisti se pouzije `cs`

## Pokryte oblasti
- shell: navbar, sidebar, profile dropdown, footer, route labels
- navigation: route labels + sekcni popisy
- profile: labels, akce, read-only sekce, stavy
- security: security overview, change password, forgot/reset password, change email, MFA a recovery codes
- stavy: loading, empty, error, success v lokalizovanych obrazovkach

## Translation struktura
- centralni dictionaries v `i18n.tsx`
- stabilni key naming podle domenovych oblasti (`route*`, `profile*`, `security*`, `nav*`, `state*`)
- bez metadata-driven translation engine

## Terminologicka pravidla
- jednotne nazvy pro School, School Year, Grade Level, Class, Group, Subject, Field of Study
- jednotne nazvy pro Timetable, Lesson Record, Attendance, Excuse, Grade, Homework, Daily Report
- jednotne nazvy pro Announcement, Conversation, Notification, Audit Log, System Settings, Feature Toggle
- bez university terminologie (credits, semesters, enrollment, exam period)

## Pridani noveho translation key
1. pridat key do `en` slovniku
2. doplnit preklad v `cs`, `sk`, `de`, `pl`
3. pouzit key v komponentach pres `t('...')`
4. overit `npm run build`

## Mimo scope
- backend i18n engine
- Razor UI localization
- metadata-driven translation platform
- druhy frontend
