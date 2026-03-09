# Phase 25: Security UI Redesign (My Profile Pattern)

## Decision
Security page UI in `Skolio.Frontend` was redesigned to match `Mùj profil` page composition and hierarchy.
Backend security business flows were not changed.

## Sections on redesigned Security page
- Security summary overview
- Change password
- Change email
- Two-factor authentication
- Recovery codes

## Main UX changes
- Card-based section layout aligned with `Mùj profil`.
- Clear summary card separated from action forms.
- Form labels moved to explicit field labels (not placeholder-only).
- Per-section primary/secondary CTA hierarchy and busy states.
- MFA actions split into distinct sub-blocks (setup start, setup confirm, disable, regenerate codes).

## Feedback model
- Unified page-level success/error feedback banner.
- Inline field validation messages.
- Form-level validation summary through feedback banner.
- Loading and saving states retained and made consistent with profile page rhythm.

## i18n support
- Existing i18n keys are reused for titles, labels, actions, validation and status texts.
- No hardcoded UI copy was added in redesigned Security page.
- Locales remain supported through existing `cs/en/sk/de/pl` translation structure.

## Backend unchanged
- No change to password change flow logic.
- No change to email change flow logic.
- No change to MFA enable/confirm/disable/regenerate flow logic.
- No change to Email Gateway integration.
