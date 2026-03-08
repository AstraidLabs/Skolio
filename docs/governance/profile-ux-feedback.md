# Profile UX Feedback (Focused Update)

## Feedback model
- `IdentityParityPage` uses page-level error only for initial load failures.
- Self-profile form uses local inline feedback banners for success and save errors.
- No global notification engine was introduced.

## Success feedback
- On successful self-profile save and admin profile save, an inline success banner is shown.
- Success banner uses localized text and auto-hides after 4 seconds.
- User can manually dismiss the banner.

## Error feedback
- Save and validation errors are shown in an inline error banner inside the profile edit card.
- Required name fields use field-level validation messages.
- Raw backend text is mapped to safe localized messages.

## Loading and saving states
- Profile loading keeps existing loading state.
- Save button now has busy state, disabled state, and localized saving label.
- Repeated save clicks are blocked while save is in progress.

## Motion and accessibility
- Only lightweight transitions and fade-in feedback animation were added.
- Focus states remain visible and stronger than neutral input state.
- `prefers-reduced-motion: reduce` disables new feedback animations.
- Alerts remain readable without motion.

## Allowed animation scope
- Inline feedback banner fade-in.
- Subtle input and button state transitions.

## Explicitly not introduced
- Global notification framework.
- Heavy animation library.
- Marketing/decorative motion effects.
- Analytics engine, reporting engine, tests, quizzes, assessment, exams, automated grading, university model.
