import React, { useEffect, useMemo, useState } from 'react';
import { useI18n } from '../i18n';
import type { School, SchoolMutation } from './api';
import { getPlatformStatusLabel, getSchoolTypeLabel, normalizePlatformStatus, normalizeSchoolKind, normalizeSchoolType } from './schoolLabels';
import { Card, StatusBadge } from '../shared/ui/primitives';

const schoolKinds = ['General', 'Specialized'] as const;
const platformStatuses = ['Draft', 'Active', 'Suspended', 'Archived'] as const;
const founderTypes = ['State', 'Region', 'Municipality', 'AssociationOfMunicipalities', 'Church', 'PrivateLegalEntity', 'NaturalPerson'] as const;
const founderCategories = ['Public', 'Church', 'Private'] as const;
const legalForms = ['PublicInstitution', 'Municipality', 'Region', 'Association', 'ChurchLegalEntity', 'LimitedLiabilityCompany', 'JointStockCompany', 'NonProfitOrganization', 'NaturalPersonEntrepreneur'] as const;
const defaultAddress = { street: '', city: '', postalCode: '', country: 'CZ' };

type DetailTab = 'school' | 'operator' | 'founder';

function toMutation(school: School): SchoolMutation {
  return {
    name: school.name,
    schoolType: normalizeSchoolType(school.schoolType),
    schoolKind: normalizeSchoolKind(school.schoolKind),
    schoolIzo: school.schoolIzo,
    schoolEmail: school.schoolEmail,
    schoolPhone: school.schoolPhone,
    schoolWebsite: school.schoolWebsite,
    mainAddress: school.mainAddress ?? defaultAddress,
    educationLocationsSummary: school.educationLocationsSummary,
    registryEntryDate: school.registryEntryDate,
    educationStartDate: school.educationStartDate,
    maxStudentCapacity: school.maxStudentCapacity,
    teachingLanguage: school.teachingLanguage,
    platformStatus: normalizePlatformStatus(school.platformStatus),
    schoolOperator: {
      legalEntityName: school.schoolOperator?.legalEntityName ?? school.name,
      legalForm: school.schoolOperator?.legalForm ?? 'PublicInstitution',
      companyNumberIco: school.schoolOperator?.companyNumberIco,
      redIzo: school.schoolOperator?.redIzo,
      registeredOfficeAddress: school.schoolOperator?.registeredOfficeAddress ?? school.mainAddress ?? defaultAddress,
      operatorEmail: school.schoolOperator?.operatorEmail,
      dataBox: school.schoolOperator?.dataBox,
      resortIdentifier: school.schoolOperator?.resortIdentifier,
      directorSummary: school.schoolOperator?.directorSummary,
      statutoryBodySummary: school.schoolOperator?.statutoryBodySummary
    },
    founder: {
      founderType: school.founder?.founderType ?? 'Municipality',
      founderCategory: school.founder?.founderCategory ?? 'Public',
      founderName: school.founder?.founderName ?? '',
      founderLegalForm: school.founder?.founderLegalForm ?? 'Municipality',
      founderIco: school.founder?.founderIco,
      founderAddress: school.founder?.founderAddress ?? school.mainAddress ?? defaultAddress,
      founderEmail: school.founder?.founderEmail,
      founderDataBox: school.founder?.founderDataBox
    }
  };
}

function isValid(form: SchoolMutation): boolean {
  return Boolean(
    form.name.trim() &&
    form.mainAddress.street.trim() &&
    form.mainAddress.city.trim() &&
    form.mainAddress.postalCode.trim() &&
    form.mainAddress.country.trim() &&
    form.schoolOperator.legalEntityName.trim() &&
    form.schoolOperator.registeredOfficeAddress.street.trim() &&
    form.schoolOperator.registeredOfficeAddress.city.trim() &&
    form.schoolOperator.registeredOfficeAddress.postalCode.trim() &&
    form.schoolOperator.registeredOfficeAddress.country.trim() &&
    form.founder.founderName.trim() &&
    form.founder.founderAddress.street.trim() &&
    form.founder.founderAddress.city.trim() &&
    form.founder.founderAddress.postalCode.trim() &&
    form.founder.founderAddress.country.trim()
  );
}

// ── Icon helpers ──────────────────────────────────────────────────────────────

function Ico({ children }: { children: React.ReactNode }) {
  return (
    <svg className="h-3.5 w-3.5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      {children}
    </svg>
  );
}

const IcoSchool    = <Ico><path d="M3 21V10l9-7 9 7v11"/><path d="M9 21v-6h6v6"/></Ico>;
const IcoMail      = <Ico><rect x="3" y="5" width="18" height="14" rx="2"/><path d="M3 5l9 9 9-9"/></Ico>;
const IcoPhone     = <Ico><path d="M5 4h4l2 5-2.5 1.5a11 11 0 005 5L15 13l5 2v4a2 2 0 01-2 2A16 16 0 013 6a2 2 0 012-2z"/></Ico>;
const IcoGlobe     = <Ico><circle cx="12" cy="12" r="9"/><path d="M2 12h20M12 3a15 15 0 010 18M12 3a15 15 0 000 18"/></Ico>;
const IcoPin       = <Ico><path d="M12 2a6 6 0 016 6c0 4-6 12-6 12S6 12 6 8a6 6 0 016-6z"/><circle cx="12" cy="8" r="2"/></Ico>;
const IcoCity      = <Ico><path d="M4 20V8l6-4 6 4v12"/><path d="M8 20v-4h4v4"/><path d="M14 10h2M14 14h2M8 10h2M8 14h2"/></Ico>;
const IcoPostal    = <Ico><rect x="3" y="5" width="18" height="14" rx="2"/><path d="M3 9h18M9 13h2M13 13h2"/></Ico>;
const IcoCalendar  = <Ico><rect x="4" y="5" width="16" height="16" rx="2"/><path d="M16 3v4M8 3v4M4 11h16"/></Ico>;
const IcoHash      = <Ico><path d="M5 9h14M5 15h14M9 3l-2 18M13 3l-2 18"/></Ico>;
const IcoUsers     = <Ico><circle cx="9" cy="7" r="3"/><path d="M3 20a9 9 0 0118 0"/></Ico>;
const IcoBuilding  = <Ico><path d="M4 20V6l8-3 8 3v14"/><path d="M8 20v-5h3v5M13 20v-5h3v5"/></Ico>;
const IcoTag       = <Ico><path d="M4 4h7l9 9-7 7-9-9V4z"/><circle cx="9" cy="9" r="1.5"/></Ico>;
const IcoClipboard = <Ico><rect x="6" y="4" width="12" height="17" rx="2"/><path d="M9 2h6v3H9z"/></Ico>;
const IcoInbox     = <Ico><path d="M3 4h18v10H3z"/><path d="M3 14h5l2 3h4l2-3h5"/></Ico>;
const IcoGear      = <Ico><circle cx="12" cy="12" r="3"/><path d="M12 2v2M12 20v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M2 12h2M20 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4"/></Ico>;
const IcoUser      = <Ico><circle cx="12" cy="8" r="3"/><path d="M6 19a6 6 0 0112 0"/></Ico>;
const IcoFolder    = <Ico><path d="M3 7h5l2-3h11v13H3z"/></Ico>;
const IcoLink      = <Ico><path d="M10 13a5 5 0 007.5.5l2-2a5 5 0 00-7-7l-1.5 1.5"/><path d="M14 11a5 5 0 00-7.5-.5l-2 2a5 5 0 007 7l1.5-1.5"/></Ico>;
const IcoCapacity  = <Ico><circle cx="8" cy="7" r="2.5"/><circle cx="16" cy="7" r="2.5"/><path d="M2 19a6 6 0 0112 0"/><path d="M14 15a6 6 0 018 4"/></Ico>;
const IcoText      = <Ico><path d="M4 6h16M4 10h16M4 14h10"/></Ico>;

// ── Main component ─────────────────────────────────────────────────────────────

export function OrganizationSchoolIdentityCard({
  school,
  editable,
  onSave
}: {
  school: School;
  editable: boolean;
  onSave: (schoolId: string, payload: SchoolMutation) => Promise<void>;
}) {
  const { t } = useI18n();
  const [form, setForm] = useState<SchoolMutation>(() => toMutation(school));
  const [saving, setSaving] = useState(false);
  const [activeTab, setActiveTab] = useState<DetailTab>('school');

  useEffect(() => {
    setForm(toMutation(school));
    setActiveTab('school');
  }, [school]);

  const canSave = useMemo(() => editable && isValid(form) && !saving, [editable, form, saving]);
  const locationLabel = [school.mainAddress.street, school.mainAddress.city].filter(Boolean).join(', ');

  const save = () => {
    if (!canSave) {
      return;
    }

    setSaving(true);
    void onSave(school.id, form).finally(() => setSaving(false));
  };

  return (
    <div className="space-y-4">
      <Card className="border-slate-200 bg-white">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="space-y-3">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">{t('orgSchoolDetail')}</p>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <StatusBadge label={getSchoolTypeLabel(t, school.schoolType)} tone="info" />
              <StatusBadge label={school.isActive ? t('orgSchoolActive') : t('orgSchoolInactive')} tone={school.isActive ? 'good' : 'warn'} />
            </div>
            <div>
              <h3 className="text-lg font-semibold text-slate-900">{school.name}</h3>
              <p className="mt-1 text-sm text-slate-600">{locationLabel || t('orgSchoolDetailEmpty')}</p>
            </div>
          </div>
          <div className="grid min-w-[260px] gap-2 rounded-xl border border-slate-100 bg-slate-50/80 p-3 text-sm text-slate-700">
            <ReadOnlyLine label={t('orgSchoolPlatformStatusLabel')} value={getPlatformStatusLabel(t, school.platformStatus)} />
            <ReadOnlyLine label={t('orgSchoolIzo')} value={school.schoolIzo} />
            <ReadOnlyLine label={t('orgSchoolAdministratorProfileId')} value={school.schoolAdministratorUserProfileId} />
            <ReadOnlyLine label={t('orgSchoolMaxStudentCapacity')} value={school.maxStudentCapacity?.toString()} />
            <ReadOnlyLine label={t('orgTeachingLanguage')} value={school.teachingLanguage} />
          </div>
        </div>
      </Card>

      <Card>
        <div className="flex flex-wrap gap-2">
          {([
            { key: 'school', label: t('orgTabSchool') },
            { key: 'operator', label: t('orgTabOperator') },
            { key: 'founder', label: t('orgTabFounder') }
          ] as { key: DetailTab; label: string }[]).map((tab) => (
            <button
              key={tab.key}
              type="button"
              className={`sk-btn ${activeTab === tab.key ? 'sk-btn-primary' : 'sk-btn-secondary'}`}
              onClick={() => setActiveTab(tab.key)}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {activeTab === 'school' ? (
          <div className="mt-4 space-y-4">
            <SectionTitle title={t('orgSchoolIdentityTitle')} />
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
              <InputField icon={IcoSchool} label={t('orgSchoolName')} value={form.name} disabled={!editable} placeholder="Základní škola, Praha 1" onChange={(value) => setForm((v) => ({ ...v, name: value }))} />
              <SelectField icon={IcoTag} label={t('orgSchoolTypeLabel')} value={form.schoolType} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolType: value }))} options={[
                { value: 'Kindergarten', label: t('orgSchoolTypeKindergarten') },
                { value: 'ElementarySchool', label: t('orgSchoolTypeElementarySchool') },
                { value: 'SecondarySchool', label: t('orgSchoolTypeSecondarySchool') }
              ]} />
              <SelectField icon={IcoFolder} label={t('orgSchoolKindLabel')} value={form.schoolKind} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolKind: value }))} options={schoolKinds.map((option) => ({ value: option, label: t(`orgSchoolKind${option}` as never) }))} />
              <InputField icon={IcoHash} label={t('orgSchoolIzo')} value={form.schoolIzo ?? ''} disabled={!editable} placeholder="600000000" onChange={(value) => setForm((v) => ({ ...v, schoolIzo: value }))} />
              <InputField icon={IcoMail} label={t('orgSchoolEmail')} type="email" value={form.schoolEmail ?? ''} disabled={!editable} placeholder="info@skola.cz" onChange={(value) => setForm((v) => ({ ...v, schoolEmail: value }))} />
              <InputField icon={IcoPhone} label={t('orgSchoolPhone')} value={form.schoolPhone ?? ''} disabled={!editable} placeholder="+420 123 456 789" onChange={(value) => setForm((v) => ({ ...v, schoolPhone: value }))} />
              <InputField icon={IcoLink} label={t('orgSchoolWebsite')} value={form.schoolWebsite ?? ''} disabled={!editable} placeholder="https://www.skola.cz" onChange={(value) => setForm((v) => ({ ...v, schoolWebsite: value }))} />
              <InputField icon={IcoPin} label={t('orgAddressStreet')} value={form.mainAddress.street} disabled={!editable} placeholder="Školní 1" onChange={(value) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, street: value } }))} />
              <InputField icon={IcoCity} label={t('orgAddressCity')} value={form.mainAddress.city} disabled={!editable} placeholder="Praha" onChange={(value) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, city: value } }))} />
              <InputField icon={IcoPostal} label={t('orgAddressPostalCode')} value={form.mainAddress.postalCode} disabled={!editable} placeholder="110 00" onChange={(value) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, postalCode: value } }))} />
              <InputField icon={IcoGlobe} label={t('orgAddressCountry')} value={form.mainAddress.country} disabled={!editable} placeholder="CZ" onChange={(value) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, country: value } }))} />
            </div>

            <SectionTitle title={t('orgSchoolOperationsTitle')} />
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
              <InputField icon={IcoCalendar} label={t('orgRegistryEntryDate')} type="date" value={form.registryEntryDate ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, registryEntryDate: value || undefined }))} />
              <InputField icon={IcoCalendar} label={t('orgEducationStartDate')} type="date" value={form.educationStartDate ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, educationStartDate: value || undefined }))} />
              <InputField icon={IcoCapacity} label={t('orgSchoolMaxStudentCapacity')} type="number" value={form.maxStudentCapacity?.toString() ?? ''} disabled={!editable} placeholder="500" onChange={(value) => setForm((v) => ({ ...v, maxStudentCapacity: value ? Number(value) : undefined }))} />
              <InputField icon={IcoGlobe} label={t('orgTeachingLanguage')} value={form.teachingLanguage ?? ''} disabled={!editable} placeholder="cs" onChange={(value) => setForm((v) => ({ ...v, teachingLanguage: value }))} />
              <SelectField icon={IcoGear} label={t('orgSchoolPlatformStatusLabel')} value={form.platformStatus} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, platformStatus: value }))} options={platformStatuses.map((option) => ({ value: option, label: t(`orgPlatformStatus${option}` as never) }))} />
            </div>
            <TextAreaField icon={IcoText} label={t('orgSchoolEducationLocationsSummary')} value={form.educationLocationsSummary ?? ''} disabled={!editable} placeholder="Stručný popis míst vzdělávání..." onChange={(value) => setForm((v) => ({ ...v, educationLocationsSummary: value }))} rows={3} />
          </div>
        ) : null}

        {activeTab === 'operator' ? (
          <div className="mt-4 space-y-4">
            <SectionTitle title={t('orgSchoolOperatorTitle')} />
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
              <InputField icon={IcoBuilding} label={t('orgLegalEntityName')} value={form.schoolOperator.legalEntityName} disabled={!editable} placeholder="Název právnické osoby" onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, legalEntityName: value } }))} />
              <SelectField icon={IcoClipboard} label={t('orgLegalForm')} value={form.schoolOperator.legalForm} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, legalForm: value } }))} options={legalForms.map((option) => ({ value: option, label: t(`orgLegalForm${option}` as never) }))} />
              <InputField icon={IcoHash} label={t('orgCompanyNumberIco')} value={form.schoolOperator.companyNumberIco ?? ''} disabled={!editable} placeholder="12345678" onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, companyNumberIco: value } }))} />
              <InputField icon={IcoHash} label={t('orgSchoolOperatorRedIzo')} value={form.schoolOperator.redIzo ?? ''} disabled={!editable} placeholder="600000000" onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, redIzo: value } }))} />
              <InputField icon={IcoMail} label={t('orgSchoolOperatorEmail')} type="email" value={form.schoolOperator.operatorEmail ?? ''} disabled={!editable} placeholder="reditel@skola.cz" onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, operatorEmail: value } }))} />
              <InputField icon={IcoInbox} label={t('orgSchoolOperatorDataBox')} value={form.schoolOperator.dataBox ?? ''} disabled={!editable} placeholder="abc1234" onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, dataBox: value } }))} />
              <InputField icon={IcoTag} label={t('orgSchoolOperatorResortIdentifier')} value={form.schoolOperator.resortIdentifier ?? ''} disabled={!editable} placeholder="MŠMT-123" onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, resortIdentifier: value } }))} />
              <InputField icon={IcoPin} label={t('orgAddressStreet')} value={form.schoolOperator.registeredOfficeAddress.street} disabled={!editable} placeholder="Školní 1" onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, street: value } } }))} />
              <InputField icon={IcoCity} label={t('orgAddressCity')} value={form.schoolOperator.registeredOfficeAddress.city} disabled={!editable} placeholder="Praha" onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, city: value } } }))} />
              <InputField icon={IcoPostal} label={t('orgAddressPostalCode')} value={form.schoolOperator.registeredOfficeAddress.postalCode} disabled={!editable} placeholder="110 00" onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, postalCode: value } } }))} />
              <InputField icon={IcoGlobe} label={t('orgAddressCountry')} value={form.schoolOperator.registeredOfficeAddress.country} disabled={!editable} placeholder="CZ" onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, country: value } } }))} />
            </div>
            <div className="grid gap-3 xl:grid-cols-2">
              <TextAreaField icon={IcoUser} label={t('orgSchoolDirectorSummary')} value={form.schoolOperator.directorSummary ?? ''} disabled={!editable} placeholder="Jméno ředitele, funkce..." onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, directorSummary: value } }))} rows={4} />
              <TextAreaField icon={IcoUsers} label={t('orgSchoolStatutoryBodySummary')} value={form.schoolOperator.statutoryBodySummary ?? ''} disabled={!editable} placeholder="Popis statutárního orgánu..." onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, statutoryBodySummary: value } }))} rows={4} />
            </div>
          </div>
        ) : null}

        {activeTab === 'founder' ? (
          <div className="mt-4 space-y-4">
            <SectionTitle title={t('orgFounderTitle')} />
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
              <SelectField icon={IcoTag} label={t('orgFounderTypeLabel')} value={form.founder.founderType} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderType: value } }))} options={founderTypes.map((option) => ({ value: option, label: t(`orgFounderType${option}` as never) }))} />
              <SelectField icon={IcoFolder} label={t('orgFounderCategoryLabel')} value={form.founder.founderCategory} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderCategory: value } }))} options={founderCategories.map((option) => ({ value: option, label: t(`orgFounderCategory${option}` as never) }))} />
              <InputField icon={IcoBuilding} label={t('orgFounderName')} value={form.founder.founderName} disabled={!editable} placeholder="Obec / Město / Kraj..." onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderName: value } }))} />
              <SelectField icon={IcoClipboard} label={t('orgFounderLegalFormLabel')} value={form.founder.founderLegalForm} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderLegalForm: value } }))} options={legalForms.map((option) => ({ value: option, label: t(`orgLegalForm${option}` as never) }))} />
              <InputField icon={IcoHash} label={t('orgFounderIco')} value={form.founder.founderIco ?? ''} disabled={!editable} placeholder="00064581" onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderIco: value } }))} />
              <InputField icon={IcoMail} label={t('orgFounderEmail')} type="email" value={form.founder.founderEmail ?? ''} disabled={!editable} placeholder="kontakt@zakladatel.cz" onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderEmail: value } }))} />
              <InputField icon={IcoInbox} label={t('orgFounderDataBox')} value={form.founder.founderDataBox ?? ''} disabled={!editable} placeholder="xyz5678" onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderDataBox: value } }))} />
              <InputField icon={IcoPin} label={t('orgAddressStreet')} value={form.founder.founderAddress.street} disabled={!editable} placeholder="Školní 1" onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, street: value } } }))} />
              <InputField icon={IcoCity} label={t('orgAddressCity')} value={form.founder.founderAddress.city} disabled={!editable} placeholder="Praha" onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, city: value } } }))} />
              <InputField icon={IcoPostal} label={t('orgAddressPostalCode')} value={form.founder.founderAddress.postalCode} disabled={!editable} placeholder="110 00" onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, postalCode: value } } }))} />
              <InputField icon={IcoGlobe} label={t('orgAddressCountry')} value={form.founder.founderAddress.country} disabled={!editable} placeholder="CZ" onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, country: value } } }))} />
            </div>
          </div>
        ) : null}
      </Card>

      {editable ? (
        <Card>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-sm text-slate-600">{t('orgSchoolDetailSaveHint')}</p>
            <button className="sk-btn sk-btn-primary" type="button" disabled={!canSave} onClick={save}>
              {saving ? t('profileSaving') : t('save')}
            </button>
          </div>
        </Card>
      ) : null}
    </div>
  );
}

function SectionTitle({ title }: { title: string }) {
  return <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">{title}</p>;
}

function ReadOnlyLine({ label, value }: { label: string; value?: string | null }) {
  return (
    <div className="flex items-start justify-between gap-3">
      <span className="text-slate-500">{label}</span>
      <span className="text-right font-medium text-slate-900">{value || '-'}</span>
    </div>
  );
}

function InputField({
  label,
  value,
  onChange,
  disabled = false,
  type = 'text',
  placeholder,
  icon
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  type?: string;
  placeholder?: string;
  icon?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label flex items-center gap-1.5">
        {icon ? <span className="text-slate-400">{icon}</span> : null}
        {label}
      </label>
      <input className="sk-input" value={value} type={type} disabled={disabled} placeholder={placeholder} onChange={(e) => onChange(e.target.value)} />
    </div>
  );
}

function SelectField({
  label,
  value,
  onChange,
  options,
  disabled = false,
  icon
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
  disabled?: boolean;
  icon?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label flex items-center gap-1.5">
        {icon ? <span className="text-slate-400">{icon}</span> : null}
        {label}
      </label>
      <select className="sk-input" value={value} disabled={disabled} onChange={(e) => onChange(e.target.value)}>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </div>
  );
}

function TextAreaField({
  label,
  value,
  onChange,
  disabled = false,
  rows = 4,
  placeholder,
  icon
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  rows?: number;
  placeholder?: string;
  icon?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label flex items-center gap-1.5">
        {icon ? <span className="text-slate-400">{icon}</span> : null}
        {label}
      </label>
      <textarea className="sk-input min-h-24" rows={rows} value={value} disabled={disabled} placeholder={placeholder} onChange={(e) => onChange(e.target.value)} />
    </div>
  );
}
