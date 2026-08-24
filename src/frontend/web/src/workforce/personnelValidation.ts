import type { PersonnelForm } from './personnelForm'
import { MOBILE_DIGIT_MAX, normalizeMobileDigits } from './personnelInput'

export type PersonnelTabId = 'general' | 'identity' | 'work' | 'official' | 'history'
export type OfficialSectionId = 'declaration' | 'iskur' | 'bes' | 'social' | 'education'

export const HrValidationCodes = {
  tcknLength: 'tckn-length',
  tcknInvalid: 'tckn-invalid',
  yknFormat: 'ykn-format',
  passportFormat: 'passport-format',
  identitySchemeRequired: 'identity-scheme-required',
  identityTooLong: 'identity-too-long',
  identityInvalid: 'identity-invalid',
  phoneInvalid: 'phone-invalid',
  phoneRequired: 'phone-required',
  mobilePhoneLength: 'mobile-phone-length',
  emailInvalid: 'email-invalid',
  emailTooLong: 'email-too-long',
  birthDateInvalid: 'birth-date-invalid',
  textTooLong: 'text-too-long',
  givenNameRequired: 'given-name-required',
  givenNameTooLong: 'given-name-too-long',
  familyNameRequired: 'family-name-required',
  familyNameTooLong: 'family-name-too-long',
  personnelNumberRequired: 'personnel-number-required',
  personnelNumberTooLong: 'personnel-number-too-long',
  emergencyNameRequired: 'emergency-name-required',
  emergencyNameTooLong: 'emergency-name-too-long',
  emergencyPrimaryMultiple: 'emergency-primary-multiple',
  departmentRequired: 'department-required',
  positionRequired: 'position-required',
  startDateRequired: 'start-date-required',
  personnelNumberInUse: 'personnel-number-in-use',
  positionNotAvailable: 'position-not-available-for-department',
  nationalIdentityInUse: 'national-identity-in-use',
  departmentInactive: 'department-inactive',
  positionInactive: 'position-inactive',
  departmentNotFound: 'department-not-found',
  positionNotFound: 'position-not-found',
  invalidDocumentType: 'invalid-document-type-code',
  invalidApplicableLaw: 'invalid-applicable-law-code',
  invalidInsuranceBranch: 'invalid-insurance-branch-code',
  invalidOccupation: 'invalid-occupation-code',
  invalidDutyCode: 'invalid-duty-code',
  invalidNationality: 'invalid-nationality',
  militaryExemptionRequired: 'military-exemption-reason-required',
  militaryDefermentRequired: 'military-deferment-reason-required',
  contractEndRequired: 'contract-end-date-required',
  partTimeHoursRequired: 'part-time-hours-required',
  partTimeHoursInvalid: 'part-time-hours-invalid',
  incentiveRangeInvalid: 'incentive-range-invalid',
  workPermitRangeInvalid: 'work-permit-range-invalid',
  besRateInvalid: 'bes-rate-invalid',
  besExtraInvalid: 'bes-extra-amount-invalid',
  kepInvalid: 'kep-invalid',
} as const

const NAME_MAX = 100
const PERSONNEL_NUMBER_MAX = 32
const IDENTITY_MAX = 32
const PHONE_MAX = 32
const EMAIL_MAX = 254
const ADDRESS_MAX = 500
const PLACE_MAX = 100
const NOTES_MAX = 2000
const RELATIONSHIP_MAX = 64

export type FieldErrors = Record<string, string>

export type ValidationContext = {
  createMode: boolean
  today: string
}

type FieldTarget = {
  field: string
  tab: PersonnelTabId
  controlId: string
  officialSection?: OfficialSectionId
}

export function validationMessageKey(code: string): string {
  switch (code) {
    case HrValidationCodes.tcknLength:
      return 'personnel.validation.tcknLength'
    case HrValidationCodes.tcknInvalid:
      return 'personnel.validation.tcknInvalid'
    case HrValidationCodes.yknFormat:
      return 'personnel.validation.yknFormat'
    case HrValidationCodes.passportFormat:
      return 'personnel.validation.passportFormat'
    case HrValidationCodes.identitySchemeRequired:
      return 'personnel.validation.identitySchemeRequired'
    case HrValidationCodes.identityTooLong:
      return 'personnel.validation.identityTooLong'
    case HrValidationCodes.identityInvalid:
      return 'personnel.validation.identityInvalid'
    case HrValidationCodes.phoneInvalid:
      return 'personnel.validation.phoneInvalid'
    case HrValidationCodes.phoneRequired:
      return 'personnel.validation.phoneRequired'
    case HrValidationCodes.mobilePhoneLength:
      return 'personnel.validation.mobilePhoneLength'
    case HrValidationCodes.emailInvalid:
      return 'personnel.validation.emailInvalid'
    case HrValidationCodes.emailTooLong:
      return 'personnel.validation.emailTooLong'
    case HrValidationCodes.birthDateInvalid:
      return 'personnel.validation.birthDateInvalid'
    case HrValidationCodes.textTooLong:
      return 'personnel.validation.textTooLong'
    case HrValidationCodes.givenNameRequired:
      return 'personnel.validation.givenNameRequired'
    case HrValidationCodes.givenNameTooLong:
      return 'personnel.validation.givenNameTooLong'
    case HrValidationCodes.familyNameRequired:
      return 'personnel.validation.familyNameRequired'
    case HrValidationCodes.familyNameTooLong:
      return 'personnel.validation.familyNameTooLong'
    case HrValidationCodes.personnelNumberRequired:
      return 'personnel.validation.personnelNumberRequired'
    case HrValidationCodes.personnelNumberTooLong:
      return 'personnel.validation.personnelNumberTooLong'
    case HrValidationCodes.emergencyNameRequired:
      return 'personnel.validation.emergencyNameRequired'
    case HrValidationCodes.emergencyNameTooLong:
      return 'personnel.validation.emergencyNameTooLong'
    case HrValidationCodes.emergencyPrimaryMultiple:
      return 'personnel.validation.emergencyPrimaryMultiple'
    case HrValidationCodes.departmentRequired:
      return 'personnel.validation.departmentRequired'
    case HrValidationCodes.positionRequired:
      return 'personnel.validation.positionRequired'
    case HrValidationCodes.startDateRequired:
      return 'personnel.validation.startDateRequired'
    case HrValidationCodes.personnelNumberInUse:
      return 'workforce.errors.personnelNumberInUse'
    case HrValidationCodes.positionNotAvailable:
      return 'personnel.validation.positionNotAvailable'
    case HrValidationCodes.nationalIdentityInUse:
      return 'personnel.errors.nationalIdentityInUse'
    case HrValidationCodes.departmentInactive:
      return 'workforce.errors.departmentInactive'
    case HrValidationCodes.positionInactive:
      return 'workforce.errors.positionInactive'
    case HrValidationCodes.departmentNotFound:
      return 'personnel.validation.departmentRequired'
    case HrValidationCodes.positionNotFound:
      return 'personnel.validation.positionRequired'
    case HrValidationCodes.invalidDocumentType:
      return 'personnel.validation.invalidDocumentType'
    case HrValidationCodes.invalidApplicableLaw:
      return 'personnel.validation.invalidApplicableLaw'
    case HrValidationCodes.invalidInsuranceBranch:
      return 'personnel.validation.invalidInsuranceBranch'
    case HrValidationCodes.invalidOccupation:
      return 'personnel.validation.invalidOccupation'
    case HrValidationCodes.invalidDutyCode:
      return 'personnel.validation.invalidDutyCode'
    case HrValidationCodes.invalidNationality:
      return 'personnel.validation.invalidNationality'
    case HrValidationCodes.militaryExemptionRequired:
      return 'personnel.validation.militaryExemptionRequired'
    case HrValidationCodes.militaryDefermentRequired:
      return 'personnel.validation.militaryDefermentRequired'
    case HrValidationCodes.contractEndRequired:
      return 'personnel.validation.contractEndRequired'
    case HrValidationCodes.partTimeHoursRequired:
      return 'personnel.validation.partTimeHoursRequired'
    case HrValidationCodes.partTimeHoursInvalid:
      return 'personnel.validation.partTimeHoursInvalid'
    case HrValidationCodes.incentiveRangeInvalid:
      return 'personnel.validation.incentiveRangeInvalid'
    case HrValidationCodes.workPermitRangeInvalid:
      return 'personnel.validation.workPermitRangeInvalid'
    case HrValidationCodes.besRateInvalid:
      return 'personnel.validation.besRateInvalid'
    case HrValidationCodes.besExtraInvalid:
      return 'personnel.validation.besExtraInvalid'
    case HrValidationCodes.kepInvalid:
      return 'personnel.validation.kepInvalid'
    default:
      return 'personnel.errors.generic'
  }
}

export function validationMessageKeyFor(field: string, code: string): string {
  if (field === 'homePhone' && code === HrValidationCodes.phoneInvalid) {
    return 'personnel.validation.homePhoneInvalid'
  }
  if (field.endsWith('.phone') && code === HrValidationCodes.phoneInvalid) {
    return 'personnel.validation.emergencyPhoneInvalid'
  }
  return validationMessageKey(code)
}

export function validatePersonnelField(
  form: PersonnelForm,
  field: string,
  context: ValidationContext,
): string | undefined {
  if (field === 'givenName') {
    return requiredName(form.givenName, HrValidationCodes.givenNameRequired, HrValidationCodes.givenNameTooLong)
  }
  if (field === 'familyName') {
    return requiredName(form.familyName, HrValidationCodes.familyNameRequired, HrValidationCodes.familyNameTooLong)
  }
  if (field === 'personnelNumber') {
    if (context.createMode) {
      return undefined
    }
    const value = form.personnelNumber.trim()
    if (value === '') {
      return HrValidationCodes.personnelNumberRequired
    }
    return value.length > PERSONNEL_NUMBER_MAX ? HrValidationCodes.personnelNumberTooLong : undefined
  }
  if (field === 'employmentStartDate') {
    return context.createMode && form.employmentStartDate.trim() === ''
      ? HrValidationCodes.startDateRequired
      : undefined
  }
  if (field === 'departmentId') {
    return context.createMode && form.departmentId === '' ? HrValidationCodes.departmentRequired : undefined
  }
  if (field === 'positionId') {
    return context.createMode && form.positionId === '' ? HrValidationCodes.positionRequired : undefined
  }
  if (field === 'nationalIdentityScheme' || field === 'nationalIdentityNumber') {
    return validateIdentity(form, field)
  }
  if (field === 'nationality') {
    const value = form.nationality.trim()
    if (value === '') {
      return undefined
    }
    return /^[A-Za-z]{2}$/.test(value) ? undefined : HrValidationCodes.invalidNationality
  }
  if (field === 'birthDate') {
    return validateBirthDate(form.birthDate, context.today)
  }
  if (field === 'birthPlace') {
    return optionalMax(form.birthPlace, PLACE_MAX)
  }
  if (field === 'mobilePhone') {
    return validateMobilePhone(form.mobilePhone)
  }
  if (field === 'homePhone') {
    return validatePhone(form.homePhone, false)
  }
  if (field === 'email') {
    return validateEmail(form.email)
  }
  if (field === 'residenceAddress' || field === 'notificationAddress') {
    return optionalMax(field === 'residenceAddress' ? form.residenceAddress : form.notificationAddress, ADDRESS_MAX)
  }
  if (field === 'residenceCity') {
    return optionalMax(form.residenceCity, PLACE_MAX)
  }
  if (field === 'residenceDistrict') {
    return optionalMax(form.residenceDistrict, PLACE_MAX)
  }
  if (field === 'hrNotes') {
    return optionalMax(form.hrNotes, NOTES_MAX)
  }
  if (field === 'occupationCode') {
    const value = form.occupationCode.trim()
    if (value === '') {
      return undefined
    }
    return /^\d{4}\.\d{2}$/.test(value) ? undefined : HrValidationCodes.invalidOccupation
  }
  if (field === 'contractEndDate') {
    return form.contractType === 'FixedTerm' && form.contractEndDate.trim() === ''
      ? HrValidationCodes.contractEndRequired
      : undefined
  }
  if (field === 'partTimeMonthlyHours') {
    if (form.contractType !== 'PartTime') {
      return undefined
    }
    const parsed = Number(form.partTimeMonthlyHours.trim().replace(',', '.'))
    if (form.partTimeMonthlyHours.trim() === '') {
      return HrValidationCodes.partTimeHoursRequired
    }
    return Number.isFinite(parsed) && parsed > 0 ? undefined : HrValidationCodes.partTimeHoursInvalid
  }
  if (field === 'incentiveStartDate' || field === 'incentiveEndDate') {
    if (form.incentiveStartDate && form.incentiveEndDate && form.incentiveEndDate < form.incentiveStartDate) {
      return field === 'incentiveEndDate' ? HrValidationCodes.incentiveRangeInvalid : undefined
    }
    return undefined
  }
  if (field === 'workPermitStartDate' || field === 'workPermitEndDate') {
    if (form.workPermitStartDate && form.workPermitEndDate && form.workPermitEndDate < form.workPermitStartDate) {
      return field === 'workPermitEndDate' ? HrValidationCodes.workPermitRangeInvalid : undefined
    }
    return undefined
  }
  if (field === 'besRatePercent') {
    if (!form.besDeductionEnabled || form.besRatePercent.trim() === '') {
      return undefined
    }
    const parsed = Number(form.besRatePercent.trim().replace(',', '.'))
    return Number.isFinite(parsed) && parsed >= 0 && parsed <= 100 ? undefined : HrValidationCodes.besRateInvalid
  }
  if (field === 'besExtraAmount') {
    if (!form.besDeductionEnabled || form.besExtraAmount.trim() === '') {
      return undefined
    }
    const parsed = Number(form.besExtraAmount.trim().replace(',', '.'))
    return Number.isFinite(parsed) && parsed >= 0 ? undefined : HrValidationCodes.besExtraInvalid
  }
  if (field === 'militaryExemptionReason') {
    return form.militaryServiceStatus === 'Exempt' && form.militaryExemptionReason.trim() === ''
      ? HrValidationCodes.militaryExemptionRequired
      : optionalMax(form.militaryExemptionReason, 200)
  }
  if (field === 'militaryDefermentReason') {
    return form.militaryServiceStatus === 'Deferred' && form.militaryDefermentReason.trim() === ''
      ? HrValidationCodes.militaryDefermentRequired
      : optionalMax(form.militaryDefermentReason, 200)
  }
  if (field === 'kepAddress') {
    const code = validateEmail(form.kepAddress)
    return code === HrValidationCodes.emailInvalid || code === HrValidationCodes.emailTooLong
      ? HrValidationCodes.kepInvalid
      : code
  }
  if (
    field === 'educationDescription'
    || field === 'schoolName'
    || field === 'argeProjectCode'
  ) {
    return optionalMax(
      field === 'educationDescription'
        ? form.educationDescription
        : field === 'schoolName'
          ? form.schoolName
          : form.argeProjectCode,
      field === 'argeProjectCode' ? 64 : 200,
    )
  }

  const emergency = /^emergencyContacts\[(\d+)\]\.(name|relationship|phone)$/.exec(field)
  if (emergency) {
    const index = Number(emergency[1])
    const contact = form.emergencyContacts[index]
    if (!contact) {
      return undefined
    }
    if (emergency[2] === 'name') {
      return requiredName(
        contact.name,
        HrValidationCodes.emergencyNameRequired,
        HrValidationCodes.emergencyNameTooLong,
      )
    }
    if (emergency[2] === 'relationship') {
      return optionalMax(contact.relationship, RELATIONSHIP_MAX)
    }
    return validatePhone(contact.phone, true)
  }

  if (field === 'emergencyContacts') {
    return form.emergencyContacts.filter((item) => item.isPrimary).length > 1
      ? HrValidationCodes.emergencyPrimaryMultiple
      : undefined
  }

  return undefined
}

export function validatePersonnelForm(form: PersonnelForm, context: ValidationContext): FieldErrors {
  const errors: FieldErrors = {}
  for (const target of fieldTargets(form, context.createMode)) {
    const code = validatePersonnelField(form, target.field, context)
    if (code) {
      errors[target.field] = code
    }
  }
  return errors
}

export function firstInvalidTarget(
  errors: FieldErrors,
  form: PersonnelForm,
  createMode: boolean,
): FieldTarget | null {
  for (const target of fieldTargets(form, createMode)) {
    if (errors[target.field]) {
      return target
    }
  }

  const leftover = Object.keys(errors)[0]
  return leftover
    ? {
        field: leftover,
        tab: tabForField(leftover),
        controlId: controlIdForField(leftover, form),
        officialSection: tabForField(leftover) === 'official' ? officialSectionForField(leftover) : undefined,
      }
    : null
}

export function officialSectionForField(field: string): OfficialSectionId {
  if (
    field === 'contractType'
    || field === 'contractEndDate'
    || field === 'partTimeMonthlyHours'
    || field === 'iskurStatus'
    || field === 'incentiveStartDate'
    || field === 'incentiveEndDate'
    || field === 'iskurWorkforceStatus'
  ) {
    return 'iskur'
  }

  if (field === 'besDeductionEnabled' || field === 'besRatePercent' || field === 'besExtraAmount') {
    return 'bes'
  }

  if (
    field === 'drivingLicenceCategory'
    || field === 'militaryServiceStatus'
    || field === 'militaryExemptionReason'
    || field === 'militaryDefermentReason'
    || field === 'kepAddress'
    || field === 'workPermitStartDate'
    || field === 'workPermitEndDate'
  ) {
    return 'social'
  }

  if (
    field === 'educationLevel'
    || field === 'educationDescription'
    || field === 'schoolName'
    || field === 'graduationDate'
    || field === 'foreignLanguage'
    || field === 'argeProjectCode'
  ) {
    return 'education'
  }

  return 'declaration'
}

export function tabForField(field: string): PersonnelTabId {
  if (
    field === 'nationalIdentityScheme'
    || field === 'nationalIdentityNumber'
    || field === 'nationality'
    || field === 'gender'
    || field === 'birthDate'
    || field === 'birthPlace'
    || field === 'maritalStatus'
    || field === 'homePhone'
    || field === 'residenceAddress'
    || field === 'residenceCity'
    || field === 'residenceDistrict'
    || field === 'notificationAddress'
    || field.startsWith('emergencyContacts')
  ) {
    return 'identity'
  }

  if (
    field === 'sgkWorkplaceRegistrationId'
    || field === 'documentTypeCode'
    || field === 'applicableLawCode'
    || field === 'insuranceBranchCode'
    || field === 'occupationCode'
    || field === 'dutyCode'
    || field === 'contractType'
    || field === 'contractEndDate'
    || field === 'partTimeMonthlyHours'
    || field === 'iskurStatus'
    || field === 'incentiveStartDate'
    || field === 'incentiveEndDate'
    || field === 'iskurWorkforceStatus'
    || field === 'besRatePercent'
    || field === 'besExtraAmount'
    || field === 'drivingLicenceCategory'
    || field === 'militaryServiceStatus'
    || field === 'militaryExemptionReason'
    || field === 'militaryDefermentReason'
    || field === 'kepAddress'
    || field === 'workPermitStartDate'
    || field === 'workPermitEndDate'
    || field === 'educationLevel'
    || field === 'educationDescription'
    || field === 'schoolName'
    || field === 'graduationDate'
    || field === 'foreignLanguage'
    || field === 'argeProjectCode'
  ) {
    return 'official'
  }

  return 'general'
}

export function controlIdForField(field: string, form: PersonnelForm): string {
  const emergency = /^emergencyContacts\[(\d+)\]\.(name|relationship|phone)$/.exec(field)
  if (emergency) {
    const part = emergency[2] === 'relationship' ? 'rel' : emergency[2]
    return `hr-em-${part}-${emergency[1]}`
  }

  const ids: Record<string, string> = {
    givenName: 'hr-given',
    familyName: 'hr-family',
    personnelNumber: 'hr-sicil',
    educationLevel: 'hr-education',
    bloodType: 'hr-blood',
    mobilePhone: 'hr-mobile',
    email: 'hr-email',
    employmentStartDate: 'hr-start',
    departmentId: 'hr-department',
    positionId: 'hr-position',
    hrNotes: 'hr-notes',
    nationalIdentityScheme: 'hr-scheme',
    nationalIdentityNumber: 'hr-id-number',
    nationality: 'hr-nationality',
    gender: 'hr-gender',
    birthDate: 'hr-birth',
    birthPlace: 'hr-birthplace',
    maritalStatus: 'hr-marital',
    homePhone: 'hr-home',
    residenceAddress: 'hr-address',
    residenceCity: 'hr-city',
    residenceDistrict: 'hr-district',
    notificationAddress: 'hr-notify',
    emergencyContacts: form.emergencyContacts.length > 0 ? 'hr-em-name-0' : 'hr-scheme',
    sgkWorkplaceRegistrationId: 'hr-sgk-workplace',
    documentTypeCode: 'hr-document-type',
    applicableLawCode: 'hr-applicable-law',
    insuranceBranchCode: 'hr-insurance-branch',
    occupationCode: 'hr-occupation',
    dutyCode: 'hr-duty-code',
    contractType: 'hr-contract-type',
    contractEndDate: 'hr-contract-end',
    partTimeMonthlyHours: 'hr-part-time-hours',
    iskurStatus: 'hr-iskur-status',
    incentiveStartDate: 'hr-incentive-start',
    incentiveEndDate: 'hr-incentive-end',
    iskurWorkforceStatus: 'hr-iskur-workforce',
    besDeductionEnabled: 'hr-bes-enabled',
    besRatePercent: 'hr-bes-rate',
    besExtraAmount: 'hr-bes-extra',
    drivingLicenceCategory: 'hr-licence',
    militaryServiceStatus: 'hr-military',
    militaryExemptionReason: 'hr-military-exemption',
    militaryDefermentReason: 'hr-military-deferment',
    kepAddress: 'hr-kep',
    workPermitStartDate: 'hr-work-permit-start',
    workPermitEndDate: 'hr-work-permit-end',
    educationDescription: 'hr-education-description',
    schoolName: 'hr-school',
    graduationDate: 'hr-graduation',
    foreignLanguage: 'hr-foreign-language',
    argeProjectCode: 'hr-arge-code',
  }

  return ids[field] ?? 'hr-given'
}

export function revalidateKnownErrors(
  errors: FieldErrors,
  form: PersonnelForm,
  context: ValidationContext,
  extraFields: string[] = [],
): FieldErrors {
  const next: FieldErrors = {}
  const fields = new Set([...Object.keys(errors), ...extraFields])
  for (const field of fields) {
    const code = validatePersonnelField(form, field, context)
    if (code) {
      next[field] = code
    }
  }
  return next
}

function fieldTargets(form: PersonnelForm, createMode: boolean): FieldTarget[] {
  const general: FieldTarget[] = [
    { field: 'givenName', tab: 'general', controlId: 'hr-given' },
    { field: 'familyName', tab: 'general', controlId: 'hr-family' },
    { field: 'mobilePhone', tab: 'general', controlId: 'hr-mobile' },
    { field: 'email', tab: 'general', controlId: 'hr-email' },
  ]

  if (createMode) {
    general.push(
      { field: 'employmentStartDate', tab: 'general', controlId: 'hr-start' },
      { field: 'departmentId', tab: 'general', controlId: 'hr-department' },
      { field: 'positionId', tab: 'general', controlId: 'hr-position' },
    )
  }

  general.push({ field: 'hrNotes', tab: 'general', controlId: 'hr-notes' })

  const identity: FieldTarget[] = [
    { field: 'nationalIdentityScheme', tab: 'identity', controlId: 'hr-scheme' },
    { field: 'nationalIdentityNumber', tab: 'identity', controlId: 'hr-id-number' },
    { field: 'nationality', tab: 'identity', controlId: 'hr-nationality' },
    { field: 'birthDate', tab: 'identity', controlId: 'hr-birth' },
    { field: 'birthPlace', tab: 'identity', controlId: 'hr-birthplace' },
    { field: 'homePhone', tab: 'identity', controlId: 'hr-home' },
    { field: 'residenceAddress', tab: 'identity', controlId: 'hr-address' },
    { field: 'residenceCity', tab: 'identity', controlId: 'hr-city' },
    { field: 'residenceDistrict', tab: 'identity', controlId: 'hr-district' },
    { field: 'notificationAddress', tab: 'identity', controlId: 'hr-notify' },
  ]

  form.emergencyContacts.forEach((_, index) => {
    identity.push(
      { field: `emergencyContacts[${index}].name`, tab: 'identity', controlId: `hr-em-name-${index}` },
      { field: `emergencyContacts[${index}].relationship`, tab: 'identity', controlId: `hr-em-rel-${index}` },
      { field: `emergencyContacts[${index}].phone`, tab: 'identity', controlId: `hr-em-phone-${index}` },
    )
  })

  return [
    ...general,
    ...identity,
    { field: 'sgkWorkplaceRegistrationId', tab: 'official', controlId: 'hr-sgk-workplace', officialSection: 'declaration' },
    { field: 'documentTypeCode', tab: 'official', controlId: 'hr-document-type', officialSection: 'declaration' },
    { field: 'applicableLawCode', tab: 'official', controlId: 'hr-applicable-law', officialSection: 'declaration' },
    { field: 'insuranceBranchCode', tab: 'official', controlId: 'hr-insurance-branch', officialSection: 'declaration' },
    { field: 'occupationCode', tab: 'official', controlId: 'hr-occupation', officialSection: 'declaration' },
    { field: 'dutyCode', tab: 'official', controlId: 'hr-duty-code', officialSection: 'declaration' },
    { field: 'contractType', tab: 'official', controlId: 'hr-contract-type', officialSection: 'iskur' },
    { field: 'contractEndDate', tab: 'official', controlId: 'hr-contract-end', officialSection: 'iskur' },
    { field: 'partTimeMonthlyHours', tab: 'official', controlId: 'hr-part-time-hours', officialSection: 'iskur' },
    { field: 'incentiveStartDate', tab: 'official', controlId: 'hr-incentive-start', officialSection: 'iskur' },
    { field: 'incentiveEndDate', tab: 'official', controlId: 'hr-incentive-end', officialSection: 'iskur' },
    { field: 'besRatePercent', tab: 'official', controlId: 'hr-bes-rate', officialSection: 'bes' },
    { field: 'besExtraAmount', tab: 'official', controlId: 'hr-bes-extra', officialSection: 'bes' },
    { field: 'militaryExemptionReason', tab: 'official', controlId: 'hr-military-exemption', officialSection: 'social' },
    { field: 'militaryDefermentReason', tab: 'official', controlId: 'hr-military-deferment', officialSection: 'social' },
    { field: 'kepAddress', tab: 'official', controlId: 'hr-kep', officialSection: 'social' },
    { field: 'workPermitStartDate', tab: 'official', controlId: 'hr-work-permit-start', officialSection: 'social' },
    { field: 'workPermitEndDate', tab: 'official', controlId: 'hr-work-permit-end', officialSection: 'social' },
    { field: 'educationDescription', tab: 'official', controlId: 'hr-education-description', officialSection: 'education' },
    { field: 'schoolName', tab: 'official', controlId: 'hr-school', officialSection: 'education' },
    { field: 'argeProjectCode', tab: 'official', controlId: 'hr-arge-code', officialSection: 'education' },
  ]
}

function requiredName(value: string, required: string, tooLong: string): string | undefined {
  const trimmed = value.trim()
  if (trimmed === '') {
    return required
  }
  return trimmed.length > NAME_MAX ? tooLong : undefined
}

function optionalMax(value: string, max: number): string | undefined {
  return value.trim().length > max ? HrValidationCodes.textTooLong : undefined
}

function validateMobilePhone(value: string): string | undefined {
  if (value.trim() === '') {
    return undefined
  }

  return normalizeMobileDigits(value).length === MOBILE_DIGIT_MAX
    ? undefined
    : HrValidationCodes.mobilePhoneLength
}

function validatePhone(value: string, required: boolean): string | undefined {
  if (value.trim() === '') {
    return required ? HrValidationCodes.phoneRequired : undefined
  }

  let normalized = ''
  for (const character of value.trim()) {
    if (character >= '0' && character <= '9') {
      normalized += character
    } else if (character === '+' && normalized.length === 0) {
      normalized += character
    }
  }

  return normalized.length < 7 || normalized.length > PHONE_MAX ? HrValidationCodes.phoneInvalid : undefined
}

function validateEmail(value: string): string | undefined {
  const trimmed = value.trim()
  if (trimmed === '') {
    return undefined
  }
  if (trimmed.length > EMAIL_MAX) {
    return HrValidationCodes.emailTooLong
  }

  const at = trimmed.indexOf('@')
  if (at <= 0 || at !== trimmed.lastIndexOf('@') || trimmed.includes(' ')) {
    return HrValidationCodes.emailInvalid
  }

  const domain = trimmed.slice(at + 1)
  if (domain.length === 0 || domain.startsWith('.') || domain.endsWith('.')) {
    return HrValidationCodes.emailInvalid
  }

  return undefined
}

function validateBirthDate(value: string, today: string): string | undefined {
  if (value.trim() === '') {
    return undefined
  }
  if (value > today) {
    return HrValidationCodes.birthDateInvalid
  }

  const earliest = addYears(today, -120)
  return value < earliest ? HrValidationCodes.birthDateInvalid : undefined
}

function addYears(isoDate: string, years: number): string {
  const [year, month, day] = isoDate.split('-').map(Number)
  const next = new Date(Date.UTC(year + years, (month ?? 1) - 1, day ?? 1))
  return next.toISOString().slice(0, 10)
}

function digitsOnly(value: string): string {
  let digits = ''
  for (const character of value) {
    if (character >= '0' && character <= '9') {
      digits += character
    }
  }
  return digits
}

function passportNormalize(value: string): string {
  let normalized = ''
  for (const character of value) {
    if ((character >= '0' && character <= '9') || (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z')) {
      normalized += character.toUpperCase()
    }
  }
  return normalized
}

function isValidTckn(digits: string): boolean {
  if (digits.length !== 11 || digits[0] === '0' || !/^\d+$/.test(digits)) {
    return false
  }

  const values = [...digits].map((character) => character.charCodeAt(0) - 48)
  const odd = values[0] + values[2] + values[4] + values[6] + values[8]
  const even = values[1] + values[3] + values[5] + values[7]
  let tenth = ((odd * 7) - even) % 10
  if (tenth < 0) {
    tenth += 10
  }
  if (values[9] !== tenth) {
    return false
  }

  const sum = values.slice(0, 10).reduce((total, item) => total + item, 0)
  return values[10] === sum % 10
}

function validateIdentity(form: PersonnelForm, field: string): string | undefined {
  const scheme = form.nationalIdentityScheme
  const number = form.nationalIdentityNumber.trim()

  if (!scheme && number === '') {
    return undefined
  }

  if (!scheme && number !== '') {
    return field === 'nationalIdentityScheme' || field === 'nationalIdentityNumber'
      ? HrValidationCodes.identitySchemeRequired
      : undefined
  }

  if (number === '') {
    return undefined
  }

  if (field === 'nationalIdentityScheme') {
    return undefined
  }

  if (number.length > IDENTITY_MAX) {
    return HrValidationCodes.identityTooLong
  }

  if (scheme === 'Tckn') {
    const digits = digitsOnly(number)
    if (digits.length !== 11) {
      return HrValidationCodes.tcknLength
    }
    return isValidTckn(digits) ? undefined : HrValidationCodes.tcknInvalid
  }

  if (scheme === 'Ykn') {
    const digits = digitsOnly(number)
    return digits.length === 11 && digits.startsWith('9') ? undefined : HrValidationCodes.yknFormat
  }

  if (scheme === 'Passport') {
    const normalized = passportNormalize(number)
    return normalized.length >= 5 && normalized.length <= 15 ? undefined : HrValidationCodes.passportFormat
  }

  return number.trim().length >= 1 ? undefined : HrValidationCodes.identityInvalid
}
