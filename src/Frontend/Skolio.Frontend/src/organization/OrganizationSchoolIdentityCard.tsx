import React, { useEffect, useMemo, useState } from 'react';
import { useI18n } from '../i18n';
import type { School, SchoolMutation } from './api';
import { Card } from '../shared/ui/primitives';

const schoolKinds = ['General', 'Specialized'] as const;
const platformStatuses = ['Draft', 'Active', 'Suspended', 'Archived'] as const;
const founderTypes = ['State', 'Region', 'Municipality', 'AssociationOfMunicipalities', 'Church', 'PrivateLegalEntity', 'NaturalPerson'] as const;
const founderCategories = ['Public', 'Church', 'Private'] as const;
const legalForms = ['PublicInstitution', 'Municipality', 'Region', 'Association', 'ChurchLegalEntity', 'LimitedLiabilityCompany', 'JointStockCompany', 'NonProfitOrganization', 'NaturalPersonEntrepreneur'] as const;

const defaultAddress = { street: '', city: '', postalCode: '', country: 'CZ' };

function toMutation(school: School): SchoolMutation {
  return {
    name: school.name,
    schoolType: school.schoolType,
    schoolKind: school.schoolKind,
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
    platformStatus: school.platformStatus,
    schoolOperator: {
      legalEntityName: school.schoolOperator?.legalEntityName ?? school.name,
      legalForm: school.schoolOperator?.legalForm ?? 'PublicInstitution',
      companyNumberIco: school.schoolOperator?.companyNumberIco,
      registeredOfficeAddress: school.schoolOperator?.registeredOfficeAddress ?? school.mainAddress ?? defaultAddress,
      resortIdentifier: school.schoolOperator?.resortIdentifier,
      directorSummary: school.schoolOperator?.directorSummary,
      statutoryBodySummary: school.schoolOperator?.statutoryBodySummary
    },
    founder: {
      founderType: school.founder?.founderType ?? 'Municipality',
      founderCategory: school.founder?.founderCategory ?? 'Public',
      founderName: school.founder?.founderName ?? `${school.name} founder`,
      founderLegalForm: school.founder?.founderLegalForm ?? 'Municipality',
      founderIco: school.founder?.founderIco,
      founderAddress: school.founder?.founderAddress ?? school.mainAddress ?? defaultAddress,
      founderEmail: school.founder?.founderEmail
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

  useEffect(() => {
    setForm(toMutation(school));
  }, [school]);

  const canSave = useMemo(() => editable && isValid(form) && !saving, [editable, form, saving]);

  const save = () => {
    if (!canSave) {
      return;
    }

    setSaving(true);
    void onSave(school.id, form).finally(() => setSaving(false));
  };

  return (
    <div className="space-y-3">
      <Card>
        <p className="font-semibold text-sm">{t('orgSchoolIdentityTitle')}</p>
        <div className="mt-2 grid gap-2 md:grid-cols-3">
          <input className="sk-input" value={form.name} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, name: e.target.value }))} placeholder={t('orgSchoolName')} />
          <select className="sk-input" value={form.schoolType} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, schoolType: e.target.value }))}>
            <option value="Kindergarten">Kindergarten</option>
            <option value="ElementarySchool">ElementarySchool</option>
            <option value="SecondarySchool">SecondarySchool</option>
          </select>
          <select className="sk-input" value={form.schoolKind} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, schoolKind: e.target.value }))}>
            {schoolKinds.map((option) => <option key={option} value={option}>{t(`orgSchoolKind${option}` as never)}</option>)}
          </select>
          <input className="sk-input" value={form.schoolIzo ?? ''} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, schoolIzo: e.target.value }))} placeholder={t('orgSchoolIzo')} />
          <input className="sk-input" type="email" value={form.schoolEmail ?? ''} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, schoolEmail: e.target.value }))} placeholder={t('orgSchoolEmail')} />
          <input className="sk-input" value={form.schoolPhone ?? ''} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, schoolPhone: e.target.value }))} placeholder={t('orgSchoolPhone')} />
          <input className="sk-input" value={form.mainAddress.street} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, street: e.target.value } }))} placeholder={t('orgAddressStreet')} />
          <input className="sk-input" value={form.mainAddress.city} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, city: e.target.value } }))} placeholder={t('orgAddressCity')} />
          <input className="sk-input" value={form.mainAddress.postalCode} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, postalCode: e.target.value } }))} placeholder={t('orgAddressPostalCode')} />
          <input className="sk-input" value={form.mainAddress.country} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, mainAddress: { ...v.mainAddress, country: e.target.value } }))} placeholder={t('orgAddressCountry')} />
          <input className="sk-input" value={form.teachingLanguage ?? ''} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, teachingLanguage: e.target.value }))} placeholder={t('orgTeachingLanguage')} />
          <select className="sk-input" value={form.platformStatus} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, platformStatus: e.target.value }))}>
            {platformStatuses.map((option) => <option key={option} value={option}>{t(`orgPlatformStatus${option}` as never)}</option>)}
          </select>
        </div>
      </Card>

      <Card>
        <p className="font-semibold text-sm">{t('orgSchoolOperatorTitle')}</p>
        <div className="mt-2 grid gap-2 md:grid-cols-3">
          <input className="sk-input" value={form.schoolOperator.legalEntityName} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, legalEntityName: e.target.value } }))} placeholder={t('orgLegalEntityName')} />
          <select className="sk-input" value={form.schoolOperator.legalForm} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, legalForm: e.target.value } }))}>
            {legalForms.map((option) => <option key={option} value={option}>{t(`orgLegalForm${option}` as never)}</option>)}
          </select>
          <input className="sk-input" value={form.schoolOperator.companyNumberIco ?? ''} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, companyNumberIco: e.target.value } }))} placeholder={t('orgCompanyNumberIco')} />
          <input className="sk-input" value={form.schoolOperator.registeredOfficeAddress.street} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, street: e.target.value } } }))} placeholder={t('orgAddressStreet')} />
          <input className="sk-input" value={form.schoolOperator.registeredOfficeAddress.city} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, city: e.target.value } } }))} placeholder={t('orgAddressCity')} />
          <input className="sk-input" value={form.schoolOperator.registeredOfficeAddress.postalCode} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, schoolOperator: { ...v.schoolOperator, registeredOfficeAddress: { ...v.schoolOperator.registeredOfficeAddress, postalCode: e.target.value } } }))} placeholder={t('orgAddressPostalCode')} />
        </div>
      </Card>

      <Card>
        <p className="font-semibold text-sm">{t('orgFounderTitle')}</p>
        <div className="mt-2 grid gap-2 md:grid-cols-3">
          <select className="sk-input" value={form.founder.founderType} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, founder: { ...v.founder, founderType: e.target.value } }))}>
            {founderTypes.map((option) => <option key={option} value={option}>{t(`orgFounderType${option}` as never)}</option>)}
          </select>
          <select className="sk-input" value={form.founder.founderCategory} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, founder: { ...v.founder, founderCategory: e.target.value } }))}>
            {founderCategories.map((option) => <option key={option} value={option}>{t(`orgFounderCategory${option}` as never)}</option>)}
          </select>
          <input className="sk-input" value={form.founder.founderName} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, founder: { ...v.founder, founderName: e.target.value } }))} placeholder={t('orgFounderName')} />
          <select className="sk-input" value={form.founder.founderLegalForm} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, founder: { ...v.founder, founderLegalForm: e.target.value } }))}>
            {legalForms.map((option) => <option key={option} value={option}>{t(`orgLegalForm${option}` as never)}</option>)}
          </select>
          <input className="sk-input" value={form.founder.founderIco ?? ''} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, founder: { ...v.founder, founderIco: e.target.value } }))} placeholder={t('orgFounderIco')} />
          <input className="sk-input" type="email" value={form.founder.founderEmail ?? ''} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, founder: { ...v.founder, founderEmail: e.target.value } }))} placeholder={t('orgFounderEmail')} />
          <input className="sk-input" value={form.founder.founderAddress.street} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, street: e.target.value } } }))} placeholder={t('orgAddressStreet')} />
          <input className="sk-input" value={form.founder.founderAddress.city} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, city: e.target.value } } }))} placeholder={t('orgAddressCity')} />
          <input className="sk-input" value={form.founder.founderAddress.postalCode} disabled={!editable} onChange={(e) => setForm((v) => ({ ...v, founder: { ...v.founder, founderAddress: { ...v.founder.founderAddress, postalCode: e.target.value } } }))} placeholder={t('orgAddressPostalCode')} />
        </div>
      </Card>

      {editable ? (
        <Card>
          <button className="sk-btn sk-btn-primary" type="button" disabled={!canSave} onClick={save}>
            {saving ? t('profileSaving') : t('save')}
          </button>
        </Card>
      ) : null}
    </div>
  );
}
