import assert from 'node:assert/strict'
import test from 'node:test'
import {
  addMonthsIso,
  derivedProbationEndDate,
  emptyPersonnelForm,
  formFromCard,
  isPersonnelFormDirty,
  snapshotOf,
  toHrWrite,
  WORK_TYPE_VALUES,
} from './personnelForm.ts'
import {
  DEFAULT_WORK_SECTION,
  WORK_SECTION_IDS,
  WORK_SECTION_LABEL_KEYS,
  firstInvalidTarget,
  invalidWorkSections,
  validatePersonnelField,
  validatePersonnelForm,
  workSectionForField,
} from './personnelValidation.ts'
import { isoToDisplayDate } from '../ui/dateEntry.ts'
import { tr as hrTr } from '../i18n/hr/tr.ts'
import { en as hrEn } from '../i18n/hr/en.ts'
import { ru as hrRu } from '../i18n/hr/ru.ts'
import {
  canEditOnboarding,
  canExecutePersistedOnboardingTemplateActions,
  canGenerateOnboardingDocuments,
  canGenerateOnboardingDraft,
  canPreviewOnboardingDraft,
  canSelectOnboardingTemplates,
  completedRequirementIds,
  countDraftCompleted,
  countSelectedTemplates,
  draftItemsFromCatalog,
  emptyOnboardingDraft,
  isOnboardingDocumentDraftReady,
  onboardingProgressText,
  shouldShowOnboardingTab,
  showOnboardingTemplateActions,
  toggleDraftItem,
  toggleSelectedTemplate,
} from './onboardingUi.ts'
import type { HrEmployeeCard } from './hrApi.ts'

function validCreateForm(today = '2026-08-28') {
  const form = emptyPersonnelForm(today)
  form.givenName = 'Ayşe'
  form.familyName = 'Yılmaz'
  form.departmentId = 'dept-1'
  form.positionId = 'pos-1'
  form.workType = 'FullTime'
  return form
}

function sampleCard(overrides: Partial<HrEmployeeCard> = {}): HrEmployeeCard {
  return {
    employeeId: 'e1',
    personnelNumber: 'P-1',
    givenName: 'Ayşe',
    familyName: 'Yılmaz',
    hasPhoto: false,
    currentEmployment: {
      id: 'emp-1',
      startDate: '2026-01-01',
      endDate: null,
      status: 'Active',
      seniorityStartDate: null,
      terminationReason: null,
      primaryAssignments: [],
    },
    currentPrimaryAssignment: {
      id: 'a1',
      departmentId: 'dept-1',
      departmentName: 'ENG',
      positionId: 'pos-1',
      positionName: 'Tech',
      startDate: '2026-01-01',
      endDate: null,
      kind: 'Primary',
    },
    organizationName: 'HuGu',
    propertyName: 'Main',
    employments: [],
    profile: {
      educationLevel: null,
      educationDescription: null,
      schoolName: null,
      graduationDate: null,
      foreignLanguage: null,
      hrNotes: null,
      nationality: null,
      gender: null,
      birthDate: null,
      birthPlace: null,
      maritalStatus: null,
      bloodType: null,
      drivingLicenceCategory: null,
      militaryServiceStatus: null,
      militaryExemptionReason: null,
      militaryDefermentReason: null,
      kepAddress: null,
      mobilePhone: null,
      homePhone: null,
      email: null,
      nationalIdentityScheme: null,
      nationalIdentityNumber: null,
      residenceAddress: null,
      residenceCity: null,
      residenceDistrict: null,
      notificationAddress: null,
      emergencyContacts: [],
    },
    certificates: [],
    canReadSensitive: true,
    officialProfile: null,
    workforceTerms: {
      contractType: null,
      contractEndDate: null,
      partTimeMonthlyHours: null,
      iskurStatus: null,
      incentiveStartDate: null,
      incentiveEndDate: null,
      iskurWorkforceStatus: null,
      workPermitStartDate: null,
      workPermitEndDate: null,
      workType: 'FullTime',
      probationPeriodMonths: null,
      probationStartDate: null,
      probationEndDate: null,
      recruitmentSourceId: null,
    },
    besSettings: null,
    paymentProfile: null,
    ...overrides,
  }
}

test('work type is required on create and save payload', () => {
  const form = validCreateForm()
  form.workType = ''
  const errors = validatePersonnelForm(form, { createMode: true, today: '2026-08-28' })
  assert.equal(errors.workType, 'work-type-required')
  assert.equal(firstInvalidTarget(errors, form, true)?.tab, 'work')
  assert.equal(firstInvalidTarget(errors, form, true)?.controlId, 'hr-work-type')

  form.workType = 'Intern'
  assert.equal(
    validatePersonnelField(form, 'workType', { createMode: true, today: '2026-08-28' }),
    undefined,
  )
  assert.equal(toHrWrite(form, true).workforceTerms.workType, 'Intern')
})

test('work type dropdown values match backend enum', () => {
  assert.deepEqual([...WORK_TYPE_VALUES], ['FullTime', 'PartTime', 'ReducedHours', 'Intern'])
})

test('probation none clears start; two months requires start and derives end', () => {
  const form = validCreateForm()
  form.probationPeriodMonths = ''
  form.probationStartDate = ''
  assert.equal(
    validatePersonnelField(form, 'probationPeriodMonths', { createMode: true, today: '2026-08-28' }),
    undefined,
  )
  assert.equal(toHrWrite(form, true).workforceTerms.probationPeriodMonths, null)
  assert.equal(toHrWrite(form, true).workforceTerms.probationStartDate, null)
  assert.equal(derivedProbationEndDate('', ''), null)

  form.probationPeriodMonths = '2'
  form.probationStartDate = ''
  assert.equal(
    validatePersonnelField(form, 'probationStartDate', { createMode: true, today: '2026-08-28' }),
    'probation-start-date-required',
  )

  form.probationStartDate = '2026-08-31'
  assert.equal(
    validatePersonnelField(form, 'probationStartDate', { createMode: true, today: '2026-08-28' }),
    undefined,
  )
  assert.equal(derivedProbationEndDate('2', '2026-08-31'), '2026-10-31')
  assert.equal(addMonthsIso('2026-01-31', 1), '2026-02-28')
  assert.equal(toHrWrite(form, true).workforceTerms.probationPeriodMonths, 2)
  assert.equal(toHrWrite(form, true).workforceTerms.probationStartDate, '2026-08-31')
})

test('recruitment source is included in write payload', () => {
  const form = validCreateForm()
  form.recruitmentSourceId = 'src-1'
  assert.equal(toHrWrite(form, true).workforceTerms.recruitmentSourceId, 'src-1')
  form.recruitmentSourceId = ''
  assert.equal(toHrWrite(form, true).workforceTerms.recruitmentSourceId, null)
})

test('certificates add remove and write; blank name invalid', () => {
  const form = validCreateForm()
  form.certificates = [{ name: 'HACCP' }, { name: 'First Aid' }]
  const body = toHrWrite(form, true)
  assert.equal(body.certificates.length, 2)
  assert.equal(body.certificates[0]?.name, 'HACCP')

  form.certificates = [{ name: '' }]
  assert.equal(
    validatePersonnelField(form, 'certificates[0].name', { createMode: true, today: '2026-08-28' }),
    'certificate-name-required',
  )

  const card = sampleCard({
    certificates: [
      { id: 'c1', name: 'HACCP' },
      { id: 'c2', name: 'First Aid' },
    ],
  })
  const loaded = formFromCard(card)
  assert.equal(loaded.certificates.length, 2)
  loaded.certificates = loaded.certificates.filter((_, index) => index !== 0)
  assert.equal(loaded.certificates.length, 1)
  assert.equal(loaded.certificates[0]?.name, 'First Aid')
})

test('enrichment fields mark personnel form dirty; snapshot stays stable otherwise', () => {
  const form = validCreateForm()
  const snap = snapshotOf(form)
  assert.equal(isPersonnelFormDirty(form, snap), false)

  form.workType = 'PartTime'
  assert.equal(isPersonnelFormDirty(form, snap), true)
  form.workType = 'FullTime'
  assert.equal(isPersonnelFormDirty(form, snap), false)

  form.probationPeriodMonths = '2'
  form.probationStartDate = '2026-08-28'
  assert.equal(isPersonnelFormDirty(form, snap), true)
  form.probationPeriodMonths = ''
  form.probationStartDate = ''
  assert.equal(isPersonnelFormDirty(form, snap), false)

  form.recruitmentSourceId = 'src-1'
  assert.equal(isPersonnelFormDirty(form, snap), true)
  form.recruitmentSourceId = ''
  assert.equal(isPersonnelFormDirty(form, snap), false)

  form.certificates = [{ name: 'HACCP' }]
  assert.equal(isPersonnelFormDirty(form, snap), true)
})

test('onboarding tab is visible in create and edit modes', () => {
  assert.equal(shouldShowOnboardingTab(), true)
  assert.equal(shouldShowOnboardingTab(), true)
  assert.equal(onboardingProgressText(5, 7), '5 / 7')
  assert.equal(countSelectedTemplates(['a', 'b', 'c']), 3)
})

test('onboarding draft starts unchecked and tracks progress', () => {
  const requirements = [
    { id: 'r1', code: 'ID_COPY', name: 'Kimlik', isRequiredByDefault: true },
    { id: 'r2', code: 'PHOTO', name: 'Foto', isRequiredByDefault: true },
  ]
  const draft = emptyOnboardingDraft(requirements)
  assert.equal(countDraftCompleted(draft), 0)
  const next = toggleDraftItem(draft, 'r1', true)
  assert.equal(countDraftCompleted(next), 1)
  assert.deepEqual(completedRequirementIds(next), ['r1'])
  const items = draftItemsFromCatalog(requirements, next)
  assert.equal(items[0]?.isCompleted, true)
  assert.equal(items[1]?.isCompleted, false)
})

test('onboarding capability helpers follow backend flags', () => {
  const inProgress = {
    employmentId: 'e1',
    onboardingStatus: 'InProgress',
    canEditChecklist: true,
    canGenerateDocuments: true,
    items: [],
    totalCount: 0,
    completedCount: 0,
    documentTemplates: [{ id: 't1', code: 'OVERTIME-CONSENT', name: 'Fazla Çalışma', description: null, category: 'Onboarding' as const, version: '1', sortOrder: 1, hasDocxAsset: true }],
  }
  const completed = {
    ...inProgress,
    onboardingStatus: 'Completed',
    canEditChecklist: false,
    canGenerateDocuments: false,
  }
  assert.equal(canEditOnboarding(inProgress, true), true)
  assert.equal(canGenerateOnboardingDocuments(inProgress, true), true)
  assert.equal(canEditOnboarding(completed, true), false)
  assert.equal(canGenerateOnboardingDocuments(completed, true), false)
  assert.equal(completed.documentTemplates.length, 1)
})

test('matbu template action visibility separates show from execute', () => {
  const inProgress = {
    employmentId: 'e1',
    onboardingStatus: 'InProgress',
    canEditChecklist: true,
    canGenerateDocuments: true,
    items: [],
    totalCount: 7,
    completedCount: 0,
    documentTemplates: [],
  }
  const completed = {
    ...inProgress,
    onboardingStatus: 'Completed',
    canEditChecklist: false,
    canGenerateDocuments: false,
  }
  const draftFields = {
    givenName: 'Hasan',
    familyName: 'Uçal',
    employmentStartDate: '2026-09-02',
  }
  const emptyDraft = { givenName: '', familyName: '', employmentStartDate: '' }

  assert.equal(showOnboardingTemplateActions('create', null, true), true)
  assert.equal(canSelectOnboardingTemplates('create', null, true), true)
  assert.equal(canPreviewOnboardingDraft('create', null, true, draftFields), true)
  assert.equal(canGenerateOnboardingDraft('create', null, true, draftFields), true)
  assert.equal(canExecutePersistedOnboardingTemplateActions('create', null, true), false)
  assert.equal(canPreviewOnboardingDraft('create', null, true, emptyDraft), false)

  assert.equal(showOnboardingTemplateActions('edit', inProgress, true), true)
  assert.equal(canPreviewOnboardingDraft('edit', inProgress, true, emptyDraft), true)
  assert.equal(canExecutePersistedOnboardingTemplateActions('edit', inProgress, true), true)

  assert.equal(showOnboardingTemplateActions('edit', completed, true), false)
  assert.equal(canSelectOnboardingTemplates('edit', completed, true), false)
  assert.equal(canExecutePersistedOnboardingTemplateActions('edit', completed, true), false)
})

test('matbu template selection draft toggles and preserves state', () => {
  assert.deepEqual(toggleSelectedTemplate([], 't1'), ['t1'])
  assert.deepEqual(toggleSelectedTemplate(['t1'], 't1'), [])
  assert.deepEqual(toggleSelectedTemplate(['t1'], 't2'), ['t1', 't2'])

  let selected = ['t1']
  selected = toggleSelectedTemplate(selected, 't2')
  assert.deepEqual(selected, ['t1', 't2'])
  selected = toggleSelectedTemplate(selected, 't1')
  assert.deepEqual(selected, ['t2'])
})

test('onboarding document draft readiness requires minimum personnel fields', () => {
  assert.equal(
    isOnboardingDocumentDraftReady({ givenName: 'Hasan', familyName: 'Uçal', employmentStartDate: '2026-09-02' }),
    true,
  )
  assert.equal(
    isOnboardingDocumentDraftReady({ givenName: 'Hasan', familyName: '', employmentStartDate: '2026-09-02' }),
    false,
  )
})

test('onboarding draft merge preserves checked items when catalog reloads', () => {
  const requirements = [
    { id: 'r1', code: 'ID_COPY', name: 'Kimlik', isRequiredByDefault: true },
    { id: 'r2', code: 'PHOTO', name: 'Foto', isRequiredByDefault: true },
  ]
  const draft = toggleDraftItem(emptyOnboardingDraft(requirements), 'r1', true)
  const merged = { ...emptyOnboardingDraft(requirements), ...draft }
  assert.equal(merged.r1, true)
  assert.equal(merged.r2, false)
  assert.equal(onboardingProgressText(countDraftCompleted(merged), requirements.length), '1 / 2')
})

test('formFromCard maps workforce enrichment fields', () => {
  const card = sampleCard({
    workforceTerms: {
      contractType: 'Indefinite',
      contractEndDate: null,
      partTimeMonthlyHours: null,
      iskurStatus: null,
      incentiveStartDate: null,
      incentiveEndDate: null,
      iskurWorkforceStatus: null,
      workPermitStartDate: null,
      workPermitEndDate: null,
      workType: 'ReducedHours',
      probationPeriodMonths: 2,
      probationStartDate: '2026-08-01',
      probationEndDate: '2026-10-01',
      recruitmentSourceId: 'src-9',
    },
  })
  const form = formFromCard(card)
  assert.equal(form.workType, 'ReducedHours')
  assert.equal(form.probationPeriodMonths, '2')
  assert.equal(form.probationStartDate, '2026-08-01')
  assert.equal(form.recruitmentSourceId, 'src-9')
  assert.equal(derivedProbationEndDate(form.probationPeriodMonths, form.probationStartDate), '2026-10-01')
})

test('çalışma bilgileri submenu defaults to istihdam and exposes five sections', () => {
  assert.equal(DEFAULT_WORK_SECTION, 'employment')
  assert.deepEqual([...WORK_SECTION_IDS], [
    'employment',
    'probation',
    'contract',
    'organization',
    'termination',
  ])
  assert.equal(WORK_SECTION_LABEL_KEYS.employment, 'personnel.sectionEmployment')
  assert.equal(WORK_SECTION_LABEL_KEYS.probation, 'personnel.sectionProbation')
  assert.equal(WORK_SECTION_LABEL_KEYS.contract, 'personnel.sectionContract')
  assert.equal(WORK_SECTION_LABEL_KEYS.organization, 'personnel.sectionOrganization')
  assert.equal(WORK_SECTION_LABEL_KEYS.termination, 'personnel.sectionTermination')
})

test('TR/EN/RU work submenu labels and probation helper are available', () => {
  for (const locale of [hrTr, hrEn, hrRu]) {
    assert.equal(typeof locale.personnel.sectionEmployment, 'string')
    assert.equal(typeof locale.personnel.sectionProbation, 'string')
    assert.equal(typeof locale.personnel.sectionContract, 'string')
    assert.equal(typeof locale.personnel.sectionOrganization, 'string')
    assert.equal(typeof locale.personnel.sectionTermination, 'string')
    assert.equal(typeof locale.personnel.workSubNav, 'string')
    assert.equal(typeof locale.personnel.probationEndHint, 'string')
    assert.ok(locale.personnel.sectionEmployment.length > 0)
    assert.ok(locale.personnel.probationEndHint.length > 0)
  }

  assert.equal(hrTr.personnel.sectionEmployment, 'İstihdam')
  assert.equal(hrTr.personnel.sectionProbation, 'Deneme Süresi')
  assert.equal(hrTr.personnel.sectionContract, 'Sözleşme')
  assert.equal(hrTr.personnel.sectionOrganization, 'Organizasyon')
  assert.equal(hrTr.personnel.sectionTermination, 'İşten Ayrılma')
  assert.equal(hrTr.personnel.probationEndHint, 'Otomatik hesaplanır')
})

test('field → work submenu mapping is explicit', () => {
  assert.equal(workSectionForField('workType'), 'employment')
  assert.equal(workSectionForField('employmentStartDate'), 'employment')
  assert.equal(workSectionForField('recruitmentSourceId'), 'employment')
  assert.equal(workSectionForField('seniorityStartDate'), 'employment')
  assert.equal(workSectionForField('probationPeriodMonths'), 'probation')
  assert.equal(workSectionForField('probationStartDate'), 'probation')
  assert.equal(workSectionForField('contractType'), 'contract')
  assert.equal(workSectionForField('contractEndDate'), 'contract')
  assert.equal(workSectionForField('partTimeMonthlyHours'), 'contract')
  assert.equal(workSectionForField('departmentId'), 'organization')
  assert.equal(workSectionForField('positionId'), 'organization')
  assert.equal(workSectionForField('terminationReason'), 'termination')
})

test('submenu switch does not reset form values or dirty state; multi-section save payload survives', () => {
  const form = validCreateForm()
  const snap = snapshotOf(form)
  let selected = DEFAULT_WORK_SECTION
  assert.equal(selected, 'employment')

  form.workType = 'PartTime'
  selected = 'probation'
  form.probationPeriodMonths = '2'
  form.probationStartDate = '2026-09-01'
  selected = 'contract'
  form.contractType = 'FixedTerm'
  form.contractEndDate = '2026-12-31'
  selected = 'organization'

  assert.equal(selected, 'organization')
  assert.equal(isPersonnelFormDirty(form, snap), true)
  assert.equal(form.workType, 'PartTime')
  assert.equal(form.probationPeriodMonths, '2')
  assert.equal(form.probationStartDate, '2026-09-01')
  assert.equal(form.contractType, 'FixedTerm')
  assert.equal(form.contractEndDate, '2026-12-31')

  const body = toHrWrite(form, true)
  assert.equal(body.workforceTerms.workType, 'PartTime')
  assert.equal(body.workforceTerms.probationPeriodMonths, 2)
  assert.equal(body.workforceTerms.probationStartDate, '2026-09-01')
  assert.equal(body.workforceTerms.contractType, 'FixedTerm')
  assert.equal(body.workforceTerms.contractEndDate, '2026-12-31')
})

test('validation error navigates to deneme süresi submenu', () => {
  const form = validCreateForm()
  form.probationPeriodMonths = '2'
  form.probationStartDate = ''
  const errors = validatePersonnelForm(form, { createMode: true, today: '2026-08-28' })
  assert.equal(errors.probationStartDate, 'probation-start-date-required')

  const startOnly = { probationStartDate: errors.probationStartDate }
  const target = firstInvalidTarget(startOnly, form, true)
  assert.equal(target?.tab, 'work')
  assert.equal(target?.workSection, 'probation')
  assert.equal(target?.controlId, 'hr-probation-start')

  const sections = invalidWorkSections(startOnly, form, true)
  assert.equal(sections.has('probation'), true)
  assert.equal(sections.has('employment'), false)

  const anyProbation = firstInvalidTarget(errors, form, true)
  assert.equal(anyProbation?.tab, 'work')
  assert.equal(anyProbation?.workSection, 'probation')
})

test('probation end is derived readonly and uses DD.MM.YYYY for 01.09.2026 + 2 months', () => {
  assert.equal(derivedProbationEndDate('2', '2026-09-01'), '2026-11-01')
  assert.equal(isoToDisplayDate('2026-09-01'), '01.09.2026')
  assert.equal(isoToDisplayDate('2026-11-01'), '01.11.2026')
  assert.equal(
    validatePersonnelField(
      { ...validCreateForm(), probationPeriodMonths: '2', probationStartDate: '2026-09-01' },
      'probationStartDate',
      { createMode: true, today: '2026-08-28' },
    ),
    undefined,
  )
})

test('organization and termination remain dedicated work submenus for transfer/end actions', () => {
  assert.equal(WORK_SECTION_IDS.includes('organization'), true)
  assert.equal(WORK_SECTION_IDS.includes('termination'), true)
  assert.equal(workSectionForField('departmentId'), 'organization')
  assert.equal(workSectionForField('terminationReason'), 'termination')
})
