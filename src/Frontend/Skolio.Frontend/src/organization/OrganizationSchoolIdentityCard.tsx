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
              <InputField label={t('orgSchoolName')} value={form.name} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, name: value }))} />
              <SelectField label={t('orgSchoolTypeLabel')} value={form.schoolType} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolType: value }))} options={[
                { value: 'Kindergarten', label: t('orgSchoolTypeKindergarten') },
                { value: 'ElementarySchool', label: t('orgSchoolTypeElementarySchool') },
                { value: 'SecondarySchool', label: t('orgSchoolTypeSecondarySchool') }
              ]} />
              <SelectField label={t('orgSchoolKindLabel')} value={form.schoolKind} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolKind: value }))} options={schoolKinds.map((option) => ({ value: option, label: t(`orgSchoolKind${option}` as never) }))} />
              <InputField label={t('orgSchoolIzo')} value={form.schoolIzo ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolIzo: value }))} />
              <InputField label={t('orgSchoolEmail')} type="email" value={form.schoolEmail ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolEmail: value }))} />
              <InputField label={t('orgSchoolPhone')} value={form.schoolPhone ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolPhone: value }))} />
              <InputField label={t('orgSchoolWebsite')} value={form.schoolWebsite ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolWebsite: value }))} />
              <InputField label={t('orgAddressStreet')} value={form.mainAddress.street} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, street: value } }))} />
              <InputField label={t('orgAddressCity')} value={form.mainAddress.city} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, city: value } }))} />
              <InputField label={t('orgAddressPostalCode')} value={form.mainAddress.postalCode} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, postalCode: value } }))} />
              <InputField label={t('orgAddressCountry')} value={form.mainAddress.country} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, country: value } }))} />
            </div>

            <SectionTitle title={t('orgSchoolOperationsTitle')} />
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
              <InputField label={t('orgRegistryEntryDate')} type="date" value={form.registryEntryDate ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, registryEntryDate: value || undefined }))} />
              <InputField label={t('orgEducationStartDate')} type="date" value={form.educationStartDate ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, educationStartDate: value || undefined }))} />
              <InputField label={t('orgSchoolMaxStudentCapacity')} type="number" value={form.maxStudentCapacity?.toString() ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, maxStudentCapacity: value ? Number(value) : undefined }))} />
              <InputField label={t('orgTeachingLanguage')} value={form.teachingLanguage ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, teachingLanguage: value }))} />
              <SelectField label={t('orgSchoolPlatformStatusLabel')} value={form.platformStatus} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, platformStatus: value }))} options={platformStatuses.map((option) => ({ value: option, label: t(`orgPlatformStatus${option}` as never) }))} />
            </div>
            <TextAreaField label={t('orgSchoolEducationLocationsSummary')} value={form.educationLocationsSummary ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, educationLocationsSummary: value }))} rows={3} />
          </div>
        ) : null}

        {activeTab === 'operator' ? (
          <div className="mt-4 space-y-4">
            <SectionTitle title={t('orgSchoolOperatorTitle')} />
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
              <InputField label={t('orgLegalEntityName')} value={form.schoolOperator.legalEntityName} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, legalEntityName: value } }))} />
              <SelectField label={t('orgLegalForm')} value={form.schoolOperator.legalForm} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, legalForm: value } }))} options={legalForms.map((option) => ({ value: option, label: t(`orgLegalForm${option}` as never) }))} />
              <InputField label={t('orgCompanyNumberIco')} value={form.schoolOperator.companyNumberIco ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, companyNumberIco: value } }))} />
              <InputField label={t('orgSchoolOperatorRedIzo')} value={form.schoolOperator.redIzo ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, redIzo: value } }))} />
              <InputField label={t('orgSchoolOperatorEmail')} type="email" value={form.schoolOperator.operatorEmail ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, operatorEmail: value } }))} />
              <InputField label={t('orgSchoolOperatorDataBox')} value={form.schoolOperator.dataBox ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, dataBox: value } }))} />
              <InputField label={t('orgSchoolOperatorResortIdentifier')} value={form.schoolOperator.resortIdentifier ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, resortIdentifier: value } }))} />
              <InputField label={t('orgAddressStreet')} value={form.schoolOperator.registeredOfficeAddress.street} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, street: value } } }))} />
              <InputField label={t('orgAddressCity')} value={form.schoolOperator.registeredOfficeAddress.city} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, city: value } } }))} />
              <InputField label={t('orgAddressPostalCode')} value={form.schoolOperator.registeredOfficeAddress.postalCode} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, postalCode: value } } }))} />
              <InputField label={t('orgAddressCountry')} value={form.schoolOperator.registeredOfficeAddress.country} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, country: value } } }))} />
            </div>
            <div className="grid gap-3 xl:grid-cols-2">
              <TextAreaField label={t('orgSchoolDirectorSummary')} value={form.schoolOperator.directorSummary ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, directorSummary: value } }))} rows={4} />
              <TextAreaField label={t('orgSchoolStatutoryBodySummary')} value={form.schoolOperator.statutoryBodySummary ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, statutoryBodySummary: value } }))} rows={4} />
            </div>
          </div>
        ) : null}

        {activeTab === 'founder' ? (
          <div className="mt-4 space-y-4">
            <SectionTitle title={t('orgFounderTitle')} />
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
              <SelectField label={t('orgFounderTypeLabel')} value={form.founder.founderType} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderType: value } }))} options={founderTypes.map((option) => ({ value: option, label: t(`orgFounderType${option}` as never) }))} />
              <SelectField label={t('orgFounderCategoryLabel')} value={form.founder.founderCategory} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderCategory: value } }))} options={founderCategories.map((option) => ({ value: option, label: t(`orgFounderCategory${option}` as never) }))} />
              <InputField label={t('orgFounderName')} value={form.founder.founderName} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderName: value } }))} />
              <SelectField label={t('orgFounderLegalFormLabel')} value={form.founder.founderLegalForm} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderLegalForm: value } }))} options={legalForms.map((option) => ({ value: option, label: t(`orgLegalForm${option}` as never) }))} />
              <InputField label={t('orgFounderIco')} value={form.founder.founderIco ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderIco: value } }))} />
              <InputField label={t('orgFounderEmail')} type="email" value={form.founder.founderEmail ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderEmail: value } }))} />
              <InputField label={t('orgFounderDataBox')} value={form.founder.founderDataBox ?? ''} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderDataBox: value } }))} />
              <InputField label={t('orgAddressStreet')} value={form.founder.founderAddress.street} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, street: value } } }))} />
              <InputField label={t('orgAddressCity')} value={form.founder.founderAddress.city} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, city: value } } }))} />
              <InputField label={t('orgAddressPostalCode')} value={form.founder.founderAddress.postalCode} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, postalCode: value } } }))} />
              <InputField label={t('orgAddressCountry')} value={form.founder.founderAddress.country} disabled={!editable} onChange={(value) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, country: value } } }))} />
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
  type = 'text'
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  type?: string;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label">{label}</label>
      <input className="sk-input" value={value} type={type} disabled={disabled} onChange={(e) => onChange(e.target.value)} />
    </div>
  );
}

function SelectField({
  label,
  value,
  onChange,
  options,
  disabled = false
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
  disabled?: boolean;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label">{label}</label>
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
  rows = 4
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  rows?: number;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="sk-label">{label}</label>
      <textarea className="sk-input min-h-24" rows={rows} value={value} disabled={disabled} onChange={(e) => onChange(e.target.value)} />
    </div>
  );
}
