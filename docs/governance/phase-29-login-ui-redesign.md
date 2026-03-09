# Phase 29: Login UI Redesign

## Decision
Login page in `Skolio.Frontend` was redesigned as a focused auth card, not a marketing landing page.
The language switcher is placed below the primary login form to keep title/form hierarchy clear.

## Implemented UI changes
- Centered modern login card with clean background and subtle depth.
- Added `Remember me` checkbox in primary login form.
- Added consistent inline SVG icons for username/email, password and MFA code fields.
- Added subtle UI motion: auth-card enter animation and feedback fade-in.
- Added reduced-motion handling for auth animations.

## Language switcher placement
- Moved below form and primary CTA into a separate footer section inside the same card.
- Kept lightweight visual style to avoid competing with core form actions.

## Remember me behavior
- `rememberMe` is posted from login form to existing backend login endpoint.
- Backend sign-in persistence now respects `rememberMe`.
- MFA challenge keeps `rememberMe` state and applies it after successful MFA verification.
- Password is never persisted in frontend.

## i18n coverage
All new login UI texts were added via existing i18n structure (`cs/en/sk/de/pl`), including:
- wordmark descriptor
- username/email label
- password label
- remember me label
- busy sign-in label
- language section label

## Out of scope
- No new auth framework
- No backend auth architecture rewrite
- No new business module
- No marketing/public website redesign
- No second frontend
