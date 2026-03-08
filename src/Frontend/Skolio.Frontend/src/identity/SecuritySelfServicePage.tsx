import React, { useEffect, useState } from 'react';
import type { createIdentityApi, MfaSetupStart, SecuritySummary } from './api';
import { Card, SectionHeader, StatusBadge } from '../shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from '../shared/ui/states';
import { useI18n } from '../i18n';
import { extractValidationErrors } from '../shared/http/httpClient';

export function SecuritySelfServicePage({ api }: { api: ReturnType<typeof createIdentityApi> }) {
  const { t } = useI18n();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const [summary, setSummary] = useState<SecuritySummary | null>(null);
  const [mfaSetup, setMfaSetup] = useState<MfaSetupStart | null>(null);
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);

  const [changePasswordDraft, setChangePasswordDraft] = useState({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
  const [changeEmailDraft, setChangeEmailDraft] = useState({ currentPassword: '', newEmail: '' });
  const [mfaConfirmCode, setMfaConfirmCode] = useState('');
  const [mfaDisableDraft, setMfaDisableDraft] = useState({ currentPassword: '', verificationCode: '' });
  const [mfaRegeneratePassword, setMfaRegeneratePassword] = useState('');

  const load = () => {
    setLoading(true);
    setError('');
    setSuccess('');
    void Promise.all([api.securitySummary(), api.mfaStatus()])
      .then(([securitySummary]) => {
        setSummary(securitySummary);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const submitChangePassword = () => {
    setError('');
    setSuccess('');
    if (!changePasswordDraft.currentPassword || !changePasswordDraft.newPassword || !changePasswordDraft.confirmNewPassword) {
      setError(t('profileFieldRequired'));
      return;
    }
    if (changePasswordDraft.newPassword !== changePasswordDraft.confirmNewPassword) {
      setError(t('validationPasswordConfirmationMismatch'));
      return;
    }
    void api.changePassword(changePasswordDraft)
      .then((response) => {
        setSuccess(response.message);
        setChangePasswordDraft({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
      })
      .catch((e: unknown) => setError(mapValidationMessage(e)));
  };

  const submitChangeEmailRequest = () => {
    setError('');
    setSuccess('');
    if (!changeEmailDraft.currentPassword || !changeEmailDraft.newEmail) {
      setError(t('profileFieldRequired'));
      return;
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(changeEmailDraft.newEmail)) {
      setError(t('securityInvalidEmailFormat'));
      return;
    }
    void api.requestEmailChange(changeEmailDraft)
      .then((response) => {
        setSuccess(response.message);
        setChangeEmailDraft({ currentPassword: '', newEmail: '' });
      })
      .catch((e: unknown) => setError(mapValidationMessage(e)));
  };

  const startMfaSetup = () => {
    setError('');
    setSuccess('');
    void api.startMfaSetup()
      .then((response) => {
        setMfaSetup(response);
        setRecoveryCodes([]);
      })
      .catch((e: unknown) => setError(mapValidationMessage(e)));
  };

  const confirmMfaSetup = () => {
    setError('');
    setSuccess('');
    void api.confirmMfaSetup({ verificationCode: mfaConfirmCode })
      .then((response) => {
        setRecoveryCodes(response.recoveryCodes);
        setMfaConfirmCode('');
        setMfaSetup(null);
        setSuccess(t('securityMfaEnabledSuccess'));
        load();
      })
      .catch((e: unknown) => setError(mapValidationMessage(e)));
  };

  const disableMfa = () => {
    setError('');
    setSuccess('');
    void api.disableMfa(mfaDisableDraft)
      .then((response) => {
        setSuccess(response.message);
        setMfaDisableDraft({ currentPassword: '', verificationCode: '' });
        load();
      })
      .catch((e: unknown) => setError(mapValidationMessage(e)));
  };

  const regenerateCodes = () => {
    setError('');
    setSuccess('');
    void api.regenerateRecoveryCodes({ currentPassword: mfaRegeneratePassword })
      .then((response) => {
        setRecoveryCodes(response.recoveryCodes);
        setMfaRegeneratePassword('');
        setSuccess(t('securityRecoveryRegeneratedSuccess'));
        load();
      })
      .catch((e: unknown) => setError(mapValidationMessage(e)));
  };

  const mapValidationMessage = (e: unknown) => {
    const mapped = extractValidationErrors(e);
    if (mapped.formErrors.length > 0) return mapped.formErrors[0];
    const firstField = Object.values(mapped.fieldErrors)[0];
    if (firstField && firstField.length > 0) return firstField[0];
    return e instanceof Error ? e.message : t('errorUnexpected');
  };

  if (loading) return <LoadingState text={t('securityLoading')} />;
  if (error) return <ErrorState text={error} />;
  if (!summary) return <EmptyState text={t('securitySummaryNotAvailable')} />;

  return (
    <section className="space-y-4">
      <div className="flex justify-end">
        <button className="sk-btn sk-btn-secondary" onClick={load} type="button">{t('reloadLabel')}</button>
      </div>

      {success ? (
        <Card className="border-emerald-200 bg-emerald-50 text-emerald-900">
          <p className="text-sm font-medium">{success}</p>
        </Card>
      ) : null}

      <Card>
        <p className="font-semibold text-sm">{t('security.overview')}</p>
        <div className="mt-2 grid gap-2 md:grid-cols-2">
          <p className="text-sm">{t('security.currentEmail')}: {summary.currentEmail}</p>
          <p className="text-sm">{t('securityEmailConfirmed')}: {summary.emailConfirmed ? t('securityYes') : t('securityNo')}</p>
          <p className="text-sm">{t('securityMfaEnabled')}: {summary.mfaEnabled ? t('securityYes') : t('securityNo')}</p>
          <p className="text-sm">{t('securityRecoveryCodesLeft')}: {summary.recoveryCodesLeft}</p>
        </div>
      </Card>

      <Card>
        <p className="font-semibold text-sm">{t('security.changePassword')}</p>
        <div className="mt-2 grid gap-2 md:grid-cols-3">
          <input className="sk-input" type="password" placeholder={t('security.currentPassword')} value={changePasswordDraft.currentPassword} onChange={(e) => setChangePasswordDraft((v) => ({ ...v, currentPassword: e.target.value }))} />
          <input className="sk-input" type="password" placeholder={t('security.newPassword')} value={changePasswordDraft.newPassword} onChange={(e) => setChangePasswordDraft((v) => ({ ...v, newPassword: e.target.value }))} />
          <input className="sk-input" type="password" placeholder={t('security.confirmPassword')} value={changePasswordDraft.confirmNewPassword} onChange={(e) => setChangePasswordDraft((v) => ({ ...v, confirmNewPassword: e.target.value }))} />
        </div>
        <div className="mt-2">
          <button className="sk-btn sk-btn-primary" type="button" onClick={submitChangePassword}>{t('securityChangePasswordAction')}</button>
        </div>
      </Card>

      <Card>
        <p className="font-semibold text-sm">{t('security.changeEmail')}</p>
        <div className="mt-2 grid gap-2 md:grid-cols-2">
          <input className="sk-input" type="password" placeholder={t('security.currentPassword')} value={changeEmailDraft.currentPassword} onChange={(e) => setChangeEmailDraft((v) => ({ ...v, currentPassword: e.target.value }))} />
          <input className="sk-input" type="email" placeholder={t('security.newEmail')} value={changeEmailDraft.newEmail} onChange={(e) => setChangeEmailDraft((v) => ({ ...v, newEmail: e.target.value }))} />
        </div>
        <div className="mt-2">
          <button className="sk-btn sk-btn-primary" type="button" onClick={submitChangeEmailRequest}>{t('securityRequestEmailChangeAction')}</button>
        </div>
      </Card>

      <Card>
        <p className="font-semibold text-sm">{t('security.mfa')}</p>
        <div className="mt-2 flex flex-wrap gap-2">
          <StatusBadge label={summary.mfaEnabled ? t('securityMfaEnabledBadge') : t('securityMfaDisabledBadge')} tone={summary.mfaEnabled ? 'good' : 'warn'} />
          <button className="sk-btn sk-btn-secondary" type="button" onClick={startMfaSetup}>{t('security.enableMfa')}</button>
        </div>

        {mfaSetup ? (
          <div className="mt-3 space-y-2 rounded-md border border-slate-200 bg-slate-50 p-3">
            <p className="text-xs text-slate-600">{t('securitySharedKey')}</p>
            <code className="block text-sm">{mfaSetup.sharedKey}</code>
            <p className="text-xs text-slate-600">{t('securityAuthenticatorUri')}</p>
            <code className="block break-all text-xs">{mfaSetup.authenticatorUri}</code>
            <div className="flex gap-2">
              <input className="sk-input" placeholder={t('securityVerificationCode')} value={mfaConfirmCode} onChange={(e) => setMfaConfirmCode(e.target.value)} />
              <button className="sk-btn sk-btn-primary" type="button" onClick={confirmMfaSetup}>{t('securityConfirmMfaAction')}</button>
            </div>
          </div>
        ) : null}

        <div className="mt-3 grid gap-2 md:grid-cols-3">
          <input className="sk-input" type="password" placeholder={t('security.currentPassword')} value={mfaDisableDraft.currentPassword} onChange={(e) => setMfaDisableDraft((v) => ({ ...v, currentPassword: e.target.value }))} />
          <input className="sk-input" placeholder={t('securityMfaVerificationCode')} value={mfaDisableDraft.verificationCode} onChange={(e) => setMfaDisableDraft((v) => ({ ...v, verificationCode: e.target.value }))} />
          <button className="sk-btn sk-btn-secondary" type="button" onClick={disableMfa}>{t('security.disableMfa')}</button>
        </div>

        <div className="mt-3 grid gap-2 md:grid-cols-2">
          <input className="sk-input" type="password" placeholder={t('security.currentPassword')} value={mfaRegeneratePassword} onChange={(e) => setMfaRegeneratePassword(e.target.value)} />
          <button className="sk-btn sk-btn-secondary" type="button" onClick={regenerateCodes}>{t('security.regenerateRecoveryCodes')}</button>
        </div>
      </Card>

      <Card>
        <p className="font-semibold text-sm">{t('security.recoveryCodes')}</p>
        {recoveryCodes.length === 0 ? <EmptyState text={t('securityRecoveryCodesEmpty')} /> : (
          <ul className="sk-list">
            {recoveryCodes.map((code) => <li className="sk-list-item" key={code}><code>{code}</code></li>)}
          </ul>
        )}
      </Card>
    </section>
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
      <SectionHeader title={t('securityForgotPasswordTitle')} description={t('securityForgotPasswordDescription')} />
      {error ? <ErrorState text={error} /> : null}
      {done ? <Card className="border-emerald-200 bg-emerald-50 text-emerald-900"><p className="text-sm font-medium">{done}</p></Card> : null}
      <Card>
        <div className="grid gap-2">
          <input className="sk-input" type="email" placeholder={t('email')} value={email} onChange={(e) => setEmail(e.target.value)} />
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
      <SectionHeader title={t('securityResetPasswordTitle')} description={t('securityResetPasswordDescription')} />
      {error ? <ErrorState text={error} /> : null}
      {done ? <Card className="border-emerald-200 bg-emerald-50 text-emerald-900"><p className="text-sm font-medium">{done}</p></Card> : null}
      <Card>
        <div className="grid gap-2">
          <input className="sk-input" type="password" placeholder={t('securityNewPassword')} value={draft.newPassword} onChange={(e) => setDraft((v) => ({ ...v, newPassword: e.target.value }))} />
          <input className="sk-input" type="password" placeholder={t('securityConfirmNewPassword')} value={draft.confirmNewPassword} onChange={(e) => setDraft((v) => ({ ...v, confirmNewPassword: e.target.value }))} />
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
