# Fáze 20 - Lokalizace a ikonografie (Skolio.Frontend)

## Jazykový model
- Výchozí jazyk: `cs`.
- Podporované jazyky: `cs`, `en`, `sk`, `de`, `pl`.
- Volba jazyka se ukládá do `localStorage` (`skolio.locale`).
- Language switcher zůstává pouze ve footeru.

## Translation struktura
- Centrální slovníky jsou v `src/i18n.tsx`.
- Používá se konzervativní dictionary model přes `t(key)`.
- Klíče jsou stabilní pro shell, navigaci, stavy a common akce.
- Rozšířené pokrytí: shell texty, navigační položky, route titulky, common UI stavy.

## Pokrytí jazyků
- `cs`: plné pokrytí shell/navigation/common keys.
- `en`: plné pokrytí shell/navigation/common keys.
- `sk`: pokrytí shell/navigation/common keys.
- `de`: pokrytí shell/navigation/common keys.
- `pl`: pokrytí shell/navigation/common keys.

## Ikonografie
- Zvolený styl: jednotné inline SVG ikony.
- Centrální mapování ikon: `src/shared/layout/AppIcons.tsx`.
- Ikony se používají v hierarchickém sidebaru (parent/child položky).
- Profilový dropdown v navbaru má konzistentní ikonografii (`My Profile`, `Sign Out`).

## Kde se ikony záměrně nepoužívají
- Nepřidáno nahodile ke každému textu a každému buttonu.
- Bez icon overload v tabulkách a hustých formulářích.
- Primární orientace: navigace a klíčové shell prvky.

## Footer language switcher
- Zobrazuje čitelné názvy jazyků: `čeština`, `English`, `slovenčina`, `Deutsch`, `polski`.
- Přepnutí jazyka se propisuje do shellu a route titulků.
- Switcher není duplikovaný v navbaru ani sidebaru.

## Scope guard
- Nezavádí tests, quizzes, assessment, exams, online testing, question bank, automated grading, scoring, evaluation workflows, exam workflow, assessment engine.
- Nezavádí university model, credits, semesters, subject enrollment.
- Nezavádí analytics engine, reporting engine, BFF, GraphQL ani druhý frontend.
