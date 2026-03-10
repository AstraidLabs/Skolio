import React, { useEffect, useState } from 'react';
import QRCode from 'qrcode';
import type { createIdentityApi, MfaSetupStart, SecuritySummary } from './api';
import { Card } from '../shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from '../shared/ui/states';
import { useI18n } from '../i18n';
import { extractValidationErrors } from '../shared/http/httpClient';

export function SecuritySelfServicePage({ api }: { api: ReturnType<typeof createIdentityApi> }) {
  const { t } = useI18n();
  const [loading, setLoading] = useState(true);
  const [refreshingSummary, setRefreshingSummary] = useState(false);
  const [pageError, setPageError] = useState('');
  const [feedbackError, setFeedbackError] = useState('');
  const [feedbackSuccess, setFeedbackSuccess] = useState('');

  const [summary, setSummary] = useState<SecuritySummary | null>(null);
  const [mfaSetup, setMfaSetup] = useState<MfaSetupStart | null>(null);
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [mfaQrCodeDataUrl, setMfaQrCodeDataUrl] = useState('');
  const [mfaQrLoading, setMfaQrLoading] = useState(false);
  const [mfaQrError, setMfaQrError] = useState('');

  const [changePasswordDraft, setChangePasswordDraft] = useState({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
  const [changeEmailDraft, setChangeEmailDraft] = useState({ currentPassword: '', newEmail: '' });
  const [mfaConfirmCode, setMfaConfirmCode] = useState('');
  const [mfaDisableDraft, setMfaDisableDraft] = useState({ currentPassword: '', verificationCode: '' });
  const [mfaRegeneratePassword, setMfaRegeneratePassword] = useState('');

  const [changePasswordErrors, setChangePasswordErrors] = useState<Record<string, string>>({});
  const [changeEmailErrors, setChangeEmailErrors] = useState<Record<string, string>>({});
  const [mfaConfirmError, setMfaConfirmError] = useState('');
  const [mfaDisableErrors, setMfaDisableErrors] = useState<Record<string, string>>({});
  const [mfaRegenerateError, setMfaRegenerateError] = useState('');

  const [savingPassword, setSavingPassword] = useState(false);
  const [savingEmail, setSavingEmail] = useState(false);
  const [startingMfa, setStartingMfa] = useState(false);
  const [confirmingMfa, setConfirmingMfa] = useState(false);
  const [disablingMfa, setDisablingMfa] = useState(false);
  const [regeneratingCodes, setRegeneratingCodes] = useState(false);

  const load = (mode: 'initial' | 'refresh' = 'initial') => {
    if (mode === 'initial') {
      setLoading(true);
    } else {
      setRefreshingSummary(true);
    }

    setPageError('');

    void Promise.all([api.securitySummary(), api.mfaStatus()])
      .then(([securitySummary]) => {
        setSummary(securitySummary);
      })
      .catch((e: Error) => setPageError(e.message))
      .finally(() => {
        if (mode === 'initial') {
          setLoading(false);
        } else {
          setRefreshingSummary(false);
        }
      });
  };

  useEffect(() => {
    load('initial');
  }, []);

  useEffect(() => {
    if (!feedbackSuccess) return;
    const timer = window.setTimeout(() => setFeedbackSuccess(''), 5000);
    return () => window.clearTimeout(timer);
  }, [feedbackSuccess]);

  useEffect(() => {
    if (!mfaSetup?.authenticatorUri) {
      setMfaQrCodeDataUrl('');
      setMfaQrError('');
      setMfaQrLoading(false);
      return;
    }

    setMfaQrLoading(true);
    setMfaQrError('');
    void QRCode.toDataURL(mfaSetup.authenticatorUri, {
      width: 224,
      margin: 1,
      errorCorrectionLevel: 'M'
    })
      .then((dataUrl) => setMfaQrCodeDataUrl(dataUrl))
      .catch(() => setMfaQrError(t('securityMfaQrRenderError')))
      .finally(() => setMfaQrLoading(false));
  }, [mfaSetup?.authenticatorUri, t]);

  const resetFeedback = () => {
    setFeedbackError('');
    setFeedbackSuccess('');
  };

  const submitChangePassword = () => {
    resetFeedback();
    const nextErrors: Record<string, string> = {};
    if (!changePasswordDraft.currentPassword.trim()) nextErrors.currentPassword = t('profileFieldRequired');
    if (!changePasswordDraft.newPassword.trim()) nextErrors.newPassword = t('profileFieldRequired');
    if (!changePasswordDraft.confirmNewPassword.trim()) nextErrors.confirmNewPassword = t('profileFieldRequired');
    if (changePasswordDraft.newPassword && changePasswordDraft.confirmNewPassword && changePasswordDraft.newPassword !== changePasswordDraft.confirmNewPassword) {
      nextErrors.confirmNewPassword = t('validationPasswordConfirmationMismatch');
    }

    setChangePasswordErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) {
      setFeedbackError(t('profileSaveErrorValidation'));
      return;
    }

    setSavingPassword(true);
    void api.changePassword(changePasswordDraft)
      .then((response) => {
        setFeedbackSuccess(response.message);
        setChangePasswordDraft({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
      })
      .catch((e: unknown) => setFeedbackError(mapValidationMessage(e, t)))
      .finally(() => setSavingPassword(false));
  };

  const submitChangeEmailRequest = () => {
    resetFeedback();
    const nextErrors: Record<string, string> = {};
    if (!changeEmailDraft.currentPassword.trim()) nextErrors.currentPassword = t('profileFieldRequired');
    if (!changeEmailDraft.newEmail.trim()) nextErrors.newEmail = t('profileFieldRequired');
    if (changeEmailDraft.newEmail && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(changeEmailDraft.newEmail)) {
      nextErrors.newEmail = t('securityInvalidEmailFormat');
    }

    setChangeEmailErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) {
      setFeedbackError(t('profileSaveErrorValidation'));
      return;
    }

    setSavingEmail(true);
    void api.requestEmailChange(changeEmailDraft)
      .then((response) => {
        setFeedbackSuccess(response.message);
        setChangeEmailDraft({ currentPassword: '', newEmail: '' });
      })
      .catch((e: unknown) => setFeedbackError(mapValidationMessage(e, t)))
      .finally(() => setSavingEmail(false));
  };

  const startMfaSetup = () => {
    resetFeedback();
    setStartingMfa(true);
    void api.startMfaSetup()
      .then((response) => {
        setMfaSetup(response);
        setRecoveryCodes([]);
        setMfaConfirmCode('');
        setMfaConfirmError('');
      })
      .catch((e: unknown) => setFeedbackError(mapValidationMessage(e, t)))
      .finally(() => setStartingMfa(false));
  };

  const confirmMfaSetup = () => {
    resetFeedback();
    if (!mfaConfirmCode.trim()) {
      setMfaConfirmError(t('profileFieldRequired'));
      setFeedbackError(t('profileSaveErrorValidation'));
      return;
    }

    setMfaConfirmError('');
    setConfirmingMfa(true);
    void api.confirmMfaSetup({ verificationCode: mfaConfirmCode })
      .then((response) => {
        setRecoveryCodes(response.recoveryCodes);
        setMfaConfirmCode('');
        setMfaSetup(null);
        setFeedbackSuccess(t('securityMfaEnabledSuccess'));
        load('refresh');
      })
      .catch((e: unknown) => setFeedbackError(mapValidationMessage(e, t)))
      .finally(() => setConfirmingMfa(false));
  };

  const disableMfa = () => {
    resetFeedback();
    const nextErrors: Record<string, string> = {};
    if (!mfaDisableDraft.currentPassword.trim()) nextErrors.currentPassword = t('profileFieldRequired');
    if (!mfaDisableDraft.verificationCode.trim()) nextErrors.verificationCode = t('profileFieldRequired');

    setMfaDisableErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) {
      setFeedbackError(t('profileSaveErrorValidation'));
      return;
    }

    setDisablingMfa(true);
    void api.disableMfa(mfaDisableDraft)
      .then((response) => {
        setFeedbackSuccess(response.message);
        setMfaDisableDraft({ currentPassword: '', verificationCode: '' });
        load('refresh');
      })
      .catch((e: unknown) => setFeedbackError(mapValidationMessage(e, t)))
      .finally(() => setDisablingMfa(false));
  };

  const regenerateCodes = () => {
    resetFeedback();
    if (!mfaRegeneratePassword.trim()) {
      setMfaRegenerateError(t('profileFieldRequired'));
      setFeedbackError(t('profileSaveErrorValidation'));
      return;
    }

    setMfaRegenerateError('');
    setRegeneratingCodes(true);
    void api.regenerateRecoveryCodes({ currentPassword: mfaRegeneratePassword })
      .then((response) => {
        setRecoveryCodes(response.recoveryCodes);
        setMfaRegeneratePassword('');
        setFeedbackSuccess(t('securityRecoveryRegeneratedSuccess'));
        load('refresh');
      })
      .catch((e: unknown) => setFeedbackError(mapValidationMessage(e, t)))
      .finally(() => setRegeneratingCodes(false));
  };

  if (loading) return <LoadingState text={t('securityLoading')} />;
  if (pageError) return <ErrorState text={pageError} />;
  if (!summary) return <EmptyState text={t('securitySummaryNotAvailable')} />;

  return (
    <section className="space-y-4">
      {(feedbackSuccess || feedbackError) ? (
        <Card className={feedbackSuccess ? 'border-emerald-200 bg-emerald-50 text-emerald-900' : 'border-red-200 bg-red-50 text-red-900'}>
          <div className="flex items-start justify-between gap-3">
            <p className="text-sm font-medium">{feedbackSuccess || feedbackError}</p>
            <button className="sk-btn sk-btn-secondary" type="button" onClick={resetFeedback}>{t('profileDismiss')}</button>
          </div>
        </Card>
      ) : null}

      <Card>
        <div className="flex items-start justify-between gap-3">
          <div>
            <SectionTitle icon={<SecurityIcon className="h-4 w-4 text-slate-500" />} title={t('securitySummaryTitle')} />
          </div>
          <button className="sk-btn sk-btn-secondary" onClick={() => load('refresh')} type="button" disabled={refreshingSummary}>
            {refreshingSummary ? t('profileSaving') : t('reloadLabel')}
          </button>
        </div>
        <div className="mt-3 grid gap-2 md:grid-cols-2">
          <SummaryLine icon={<EmailIcon className="h-4 w-4 text-slate-500" />} label={t('securityCurrentEmail')} value={summary.currentEmail} />
          <SummaryLine icon={<StatusIcon className="h-4 w-4 text-slate-500" />} label={t('securityEmailConfirmed')} value={summary.emailConfirmed ? t('securityYes') : t('securityNo')} />
          <SummaryLine icon={<MfaIcon className="h-4 w-4 text-slate-500" />} label={t('securityMfaEnabled')} value={summary.mfaEnabled ? t('securityYes') : t('securityNo')} />
          <SummaryLine icon={<RecoveryIcon className="h-4 w-4 text-slate-500" />} label={t('securityRecoveryCodesLeft')} value={String(summary.recoveryCodesLeft)} />
        </div>
      </Card>

      <Card>
        <SectionTitle icon={<PasswordIcon className="h-4 w-4 text-slate-500" />} title={t('securityChangePasswordTitle')} />
        <div className="mt-3 grid gap-2 md:grid-cols-2">
          <InputField
            type="password"
            label={t('securityCurrentPassword')}
            value={changePasswordDraft.currentPassword}
            error={changePasswordErrors.currentPassword}
            onChange={(value) => setChangePasswordDraft((v) => ({ ...v, currentPassword: value }))}
          />
          <InputField
            type="password"
            label={t('securityNewPassword')}
            value={changePasswordDraft.newPassword}
            error={changePasswordErrors.newPassword}
            onChange={(value) => setChangePasswordDraft((v) => ({ ...v, newPassword: value }))}
          />
          <InputField
            type="password"
            label={t('securityConfirmNewPassword')}
            value={changePasswordDraft.confirmNewPassword}
            error={changePasswordErrors.confirmNewPassword}
            onChange={(value) => setChangePasswordDraft((v) => ({ ...v, confirmNewPassword: value }))}
          />
        </div>
        <div className="mt-3">
          <button className="sk-btn sk-btn-primary" type="button" onClick={submitChangePassword} disabled={savingPassword}>
            {savingPassword ? t('profileSaving') : t('securityChangePasswordAction')}
          </button>
        </div>
      </Card>

      <Card>
        <SectionTitle icon={<EmailIcon className="h-4 w-4 text-slate-500" />} title={t('securityChangeEmailTitle')} />
        <div className="mt-3 grid gap-2 md:grid-cols-2">
          <ReadOnlyField label={t('securityCurrentEmail')} value={summary.currentEmail} />
          <InputField
            type="password"
            label={t('securityCurrentPasswordReauth')}
            value={changeEmailDraft.currentPassword}
            error={changeEmailErrors.currentPassword}
            onChange={(value) => setChangeEmailDraft((v) => ({ ...v, currentPassword: value }))}
          />
          <InputField
            type="email"
            label={t('securityNewEmail')}
            value={changeEmailDraft.newEmail}
            error={changeEmailErrors.newEmail}
            onChange={(value) => setChangeEmailDraft((v) => ({ ...v, newEmail: value }))}
          />
        </div>
        <div className="mt-3">
          <button className="sk-btn sk-btn-primary" type="button" onClick={submitChangeEmailRequest} disabled={savingEmail}>
            {savingEmail ? t('profileSaving') : t('securityRequestEmailChangeAction')}
          </button>
        </div>
      </Card>

      <Card>
        <SectionTitle icon={<MfaIcon className="h-4 w-4 text-slate-500" />} title={t('securityMfaManagementTitle')} />
        <div className="mt-3 flex flex-wrap items-center gap-2">
          <span className={`sk-badge ${summary.mfaEnabled ? 'sk-badge-good' : 'sk-badge-warn'}`}>
            {summary.mfaEnabled ? t('securityMfaEnabledBadge') : t('securityMfaDisabledBadge')}
          </span>
        </div>

        <div className="mt-3 space-y-3">
          <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
            <SectionTitle icon={<MfaSetupIcon className="h-4 w-4 text-slate-500" />} title={t('securityStartMfaSetupAction')} />
            <div className="mt-2">
              <button className="sk-btn sk-btn-secondary" type="button" onClick={startMfaSetup} disabled={startingMfa}>
                {startingMfa ? t('profileSaving') : t('security.enableMfa')}
              </button>
            </div>
          </div>

          {mfaSetup ? (
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
              <SectionTitle icon={<StatusIcon className="h-4 w-4 text-slate-500" />} title={t('securityConfirmMfaAction')} />
              <p className="mt-2 text-xs text-slate-600">{t('securityMfaScanQr')}</p>
              <div className="mt-2 flex min-h-56 items-center justify-center rounded-lg border border-slate-200 bg-white p-3">
                {mfaQrLoading ? <p className="text-sm text-slate-600">{t('securityMfaQrLoading')}</p> : null}
                {!mfaQrLoading && mfaQrCodeDataUrl ? (
                  <img src={mfaQrCodeDataUrl} alt={t('securityMfaQrAlt')} className="h-56 w-56 rounded-md border border-slate-100" />
                ) : null}
                {!mfaQrLoading && !mfaQrCodeDataUrl && mfaQrError ? <p className="text-sm text-red-700">{mfaQrError}</p> : null}
              </div>

              <div className="mt-3 grid gap-2 md:grid-cols-2">
                <ReadOnlyField label={t('securityMfaIssuer')} value={mfaSetup.issuer} />
                <ReadOnlyField label={t('securityMfaAccountLabel')} value={mfaSetup.accountLabel} />
              </div>

              <p className="mt-3 text-xs text-slate-600">{t('securityMfaManualEntryHelp')}</p>
              <p className="mt-2 text-xs text-slate-600">{t('securitySharedKey')}</p>
              <code className="mt-1 block rounded bg-white px-2 py-1 text-xs text-slate-900">{mfaSetup.sharedKey}</code>

              <div className="mt-3 grid gap-2 md:grid-cols-2">
                <InputField
                  type="text"
                  label={t('securityVerificationCode')}
                  value={mfaConfirmCode}
                  error={mfaConfirmError}
                  onChange={(value) => setMfaConfirmCode(value)}
                />
              </div>
              <div className="mt-2">
                <button className="sk-btn sk-btn-primary" type="button" onClick={confirmMfaSetup} disabled={confirmingMfa}>
                  {confirmingMfa ? t('profileSaving') : t('securityConfirmMfaAction')}
                </button>
              </div>
            </div>
          ) : null}

          <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
            <SectionTitle icon={<DisableIcon className="h-4 w-4 text-slate-500" />} title={t('securityDisableMfaAction')} />
            <div className="mt-2 grid gap-2 md:grid-cols-2">
              <InputField
                type="password"
                label={t('securityCurrentPassword')}
                value={mfaDisableDraft.currentPassword}
                error={mfaDisableErrors.currentPassword}
                onChange={(value) => setMfaDisableDraft((v) => ({ ...v, currentPassword: value }))}
              />
              <InputField
                type="text"
                label={t('securityMfaVerificationCode')}
                value={mfaDisableDraft.verificationCode}
                error={mfaDisableErrors.verificationCode}
                onChange={(value) => setMfaDisableDraft((v) => ({ ...v, verificationCode: value }))}
              />
            </div>
            <div className="mt-2">
              <button className="sk-btn sk-btn-secondary" type="button" onClick={disableMfa} disabled={disablingMfa}>
                {disablingMfa ? t('profileSaving') : t('securityDisableMfaAction')}
              </button>
            </div>
          </div>

          <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
            <SectionTitle icon={<RefreshIcon className="h-4 w-4 text-slate-500" />} title={t('securityRegenerateRecoveryCodesAction')} />
            <div className="mt-2 grid gap-2 md:grid-cols-2">
              <InputField
                type="password"
                label={t('securityCurrentPassword')}
                value={mfaRegeneratePassword}
                error={mfaRegenerateError}
                onChange={(value) => setMfaRegeneratePassword(value)}
              />
            </div>
            <div className="mt-2">
              <button className="sk-btn sk-btn-secondary" type="button" onClick={regenerateCodes} disabled={regeneratingCodes}>
                {regeneratingCodes ? t('profileSaving') : t('securityRegenerateRecoveryCodesAction')}
              </button>
            </div>
          </div>
        </div>
      </Card>

      <Card>
        <SectionTitle icon={<RecoveryIcon className="h-4 w-4 text-slate-500" />} title={t('securityRecoveryCodesTitle')} />
        <p className="mt-2 text-xs text-slate-600">{t('securityRecoveryCodesEmpty')}</p>
        {recoveryCodes.length === 0 ? (
          <div className="mt-2 rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm text-slate-600">
            {t('securityRecoveryCodesEmpty')}
          </div>
        ) : (
          <div className="mt-2 rounded-lg border border-slate-200 bg-slate-50 p-3">
            <ul className="space-y-1">
              {recoveryCodes.map((code) => (
                <li key={code} className="rounded bg-white px-2 py-1 text-xs font-mono text-slate-900">{code}</li>
              ))}
            </ul>
          </div>
        )}
      </Card>
    </section>
  );
}

function SectionTitle({ icon, title }: { icon: React.ReactNode; title: string }) {
  return (
    <div className="flex items-center gap-2">
      {icon}
      <p className="font-semibold text-sm text-slate-900">{title}</p>
    </div>
  );
}

function SummaryLine({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
      <div className="flex items-center gap-2">
        {icon}
        <p className="text-xs uppercase tracking-wide text-slate-500">{label}</p>
      </div>
      <p className="mt-1 text-sm font-semibold text-slate-900">{value}</p>
    </div>
  );
}

function ReadOnlyField({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label">{label}</label>
      <div className="sk-input bg-slate-50 text-slate-700" aria-readonly="true">{value}</div>
    </div>
  );
}

function InputField({
  label,
  value,
  onChange,
  type,
  error
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type: 'text' | 'email' | 'password';
  error?: string;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label">{label}</label>
      <input
        className={`sk-input ${error ? 'sk-input-invalid' : ''}`}
        type={type}
        value={value}
        aria-invalid={Boolean(error)}
        onChange={(e) => onChange(e.target.value)}
      />
      {error ? <span className="text-xs text-red-700">{error}</span> : null}
    </div>
  );
}

function mapValidationMessage(e: unknown, t: ReturnType<typeof useI18n>['t']) {
  const mapped = extractValidationErrors(e);
  const normalizedFormErrors = mapped.formErrors.map((x) => x.toLowerCase());
  if (normalizedFormErrors.some((x) => x.includes('verification code is invalid'))) return t('securityInvalidVerificationCode');

  const normalizedFieldErrors = Object.values(mapped.fieldErrors).flat().map((x) => x.toLowerCase());
  if (normalizedFieldErrors.some((x) => x.includes('verification code is invalid'))) return t('securityInvalidVerificationCode');

  if (mapped.formErrors.length > 0) return mapped.formErrors[0];
  const firstField = Object.values(mapped.fieldErrors)[0];
  if (firstField && firstField.length > 0) return firstField[0];
  if (e instanceof Error && e.message.toLowerCase().includes('verification code is invalid')) return t('securityInvalidVerificationCode');
  return e instanceof Error ? e.message : t('errorUnexpected');
}

function SecurityIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg aria-hidden="true" className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 3l8 4v5c0 5.3-3.5 8.9-8 10-4.5-1.1-8-4.7-8-10V7l8-4z" />
    </svg>
  );
}

function EmailIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg aria-hidden="true" className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="6" width="18" height="12" rx="2" />
      <path d="M4 8l8 6 8-6" />
    </svg>
  );
}

function StatusIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg aria-hidden="true" className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="9" />
      <path d="M9 12l2 2 4-4" />
    </svg>
  );
}

function PasswordIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg aria-hidden="true" className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="10" width="18" height="10" rx="2" />
      <path d="M8 10V7a4 4 0 118 0v3" />
    </svg>
  );
}

function MfaIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg aria-hidden="true" className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <rect x="7" y="2.5" width="10" height="19" rx="2" />
      <path d="M10 6h4M12 18h.01" />
      <path d="M4 9h2M18 9h2M4 13h2M18 13h2" />
    </svg>
  );
}

function MfaSetupIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg aria-hidden="true" className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="9" />
      <path d="M12 8v8M8 12h8" />
    </svg>
  );
}

function DisableIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg aria-hidden="true" className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="9" />
      <path d="M8 12h8" />
    </svg>
  );
}

function RefreshIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg aria-hidden="true" className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M20 6v5h-5" />
      <path d="M4 18v-5h5" />
      <path d="M19 11a7 7 0 00-12-3l-2 2M5 13a7 7 0 0012 3l2-2" />
    </svg>
  );
}

function RecoveryIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg aria-hidden="true" className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M8 10a4 4 0 117 2l-1 1" />
      <circle cx="7" cy="16" r="2" />
      <path d="M9 16h6l2-2" />
    </svg>
  );
}

export function ForgotPasswordPage({ api }: { api: ReturnType<typeof createIdentityApi> }) {
  const { t } = useI18n();
  const [email, setEmail] = useState('');
  const [done, setDone] = useState('');
  const [error, setError] = useState('');

  const submit = () => {
    setError('');
    setDone('');
    void api.forgotPassword({ email })
      .then((response) => setDone(response.message))
      .catch((e: unknown) => {
        const mapped = extractValidationErrors(e);
        setError(mapped.formErrors[0] ?? Object.values(mapped.fieldErrors)[0]?.[0] ?? (e instanceof Error ? e.message : t('errorUnexpected')));
      });
  };

  return (
    <section className="mx-auto max-w-lg space-y-3 p-4">
      <div>
        <h1 className="text-xl font-semibold text-slate-900">{t('securityForgotPasswordTitle')}</h1>
        <p className="mt-1 text-sm text-slate-600">{t('securityForgotPasswordDescription')}</p>
      </div>
      {error ? <ErrorState text={error} /> : null}
      {done ? <Card className="border-emerald-200 bg-emerald-50 text-emerald-900"><p className="text-sm font-medium">{done}</p></Card> : null}
      <Card>
        <div className="grid gap-2">
          <InputField label={t('email')} type="email" value={email} onChange={setEmail} />
          <button className="sk-btn sk-btn-primary" type="button" onClick={submit}>{t('securityForgotPasswordAction')}</button>
        </div>
      </Card>
    </section>
  );
}

export function ResetPasswordPage({
  api,
  userId,
  token
}: {
  api: ReturnType<typeof createIdentityApi>;
  userId: string;
  token: string;
}) {
  const { t } = useI18n();
  const [draft, setDraft] = useState({ newPassword: '', confirmNewPassword: '' });
  const [done, setDone] = useState('');
  const [error, setError] = useState('');

  const submit = () => {
    setError('');
    setDone('');
    void api.resetPassword({ userId, token, ...draft })
      .then((response) => setDone(response.message))
      .catch((e: unknown) => {
        const mapped = extractValidationErrors(e);
        setError(mapped.formErrors[0] ?? Object.values(mapped.fieldErrors)[0]?.[0] ?? (e instanceof Error ? e.message : t('errorUnexpected')));
      });
  };

  return (
    <section className="mx-auto max-w-lg space-y-3 p-4">
      <div>
        <h1 className="text-xl font-semibold text-slate-900">{t('securityResetPasswordTitle')}</h1>
        <p className="mt-1 text-sm text-slate-600">{t('securityResetPasswordDescription')}</p>
      </div>
      {error ? <ErrorState text={error} /> : null}
      {done ? <Card className="border-emerald-200 bg-emerald-50 text-emerald-900"><p className="text-sm font-medium">{done}</p></Card> : null}
      <Card>
        <div className="grid gap-2">
          <InputField label={t('securityNewPassword')} type="password" value={draft.newPassword} onChange={(value) => setDraft((v) => ({ ...v, newPassword: value }))} />
          <InputField label={t('securityConfirmNewPassword')} type="password" value={draft.confirmNewPassword} onChange={(value) => setDraft((v) => ({ ...v, confirmNewPassword: value }))} />
          <button className="sk-btn sk-btn-primary" type="button" onClick={submit}>{t('securityResetPasswordAction')}</button>
        </div>
      </Card>
    </section>
  );
}

export function ConfirmEmailChangePage({
  api,
  userId,
  token,
  newEmail
}: {
  api: ReturnType<typeof createIdentityApi>;
  userId: string;
  token: string;
  newEmail: string;
}) {
  const { t } = useI18n();
  const [done, setDone] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    setError('');
    setDone('');
    void api.confirmEmailChange({ userId, token, newEmail })
      .then((response) => setDone(response.message))
      .catch((e: unknown) => {
        const mapped = extractValidationErrors(e);
        setError(mapped.formErrors[0] ?? Object.values(mapped.fieldErrors)[0]?.[0] ?? (e instanceof Error ? e.message : t('errorUnexpected')));
      });
  }, [api, newEmail, token, userId]);

  if (error) return <section className="mx-auto max-w-lg p-4"><ErrorState text={error} /></section>;
  if (!done) return <section className="mx-auto max-w-lg p-4"><LoadingState text={t('securityConfirmEmailChangeLoading')} /></section>;
  return <section className="mx-auto max-w-lg p-4"><Card className="border-emerald-200 bg-emerald-50 text-emerald-900"><p className="text-sm font-medium">{done}</p></Card></section>;
}


export function ConfirmActivationPage({
  api,
  userId,
  token
}: {
  api: ReturnType<typeof createIdentityApi>;
  userId: string;
  token: string;
}) {
  const { t } = useI18n();
  const [done, setDone] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    setError('');
    setDone('');
    void api.confirmActivation({ userId, token })
      .then((response) => setDone(response.message))
      .catch((e: unknown) => {
        const mapped = extractValidationErrors(e);
        setError(mapped.formErrors[0] ?? Object.values(mapped.fieldErrors)[0]?.[0] ?? (e instanceof Error ? e.message : t('errorUnexpected')));
      });
  }, [api, token, userId]);

  if (error) return <section className="mx-auto max-w-lg p-4"><ErrorState text={error} /></section>;
  if (!done) return <section className="mx-auto max-w-lg p-4"><LoadingState text={t('securityActivationConfirmLoading')} /></section>;
  return <section className="mx-auto max-w-lg p-4"><Card className="border-emerald-200 bg-emerald-50 text-emerald-900"><p className="text-sm font-medium">{done}</p></Card></section>;
}
