import type {
  BloodType,
  DrivingLicenceCategory,
  EducationLevel,
  EmergencyContactWrite,
  EmployeeCertificateWrite,
  EmploymentContractType,
  ForeignLanguageSummary,
  Gender,
  HrEmployeeCard,
  HrEmployeeWrite,
  IskurStatus,
  IskurWorkforceStatus,
  MaritalStatus,
  MilitaryServiceStatus,
  NationalIdentityScheme,
  WorkType,
} from './hrApi.ts'
import { toIsoDate } from '../ui/dateEntry.ts'
import { normalizeMobileDigits } from './personnelInput.ts'
import { toPersistedIban } from './paymentIban.ts'

export const WORK_TYPE_VALUES = ['FullTime', 'PartTime', 'ReducedHours', 'Intern'] as const satisfies readonly WorkType[]

export type ProbationPeriodChoice = '' | '2'

export type PersonnelForm = {
  givenName: string
  familyName: string
  personnelNumber: string
  employmentStartDate: string
  departmentId: string
  positionId: string
  educationLevel: EducationLevel | ''
  educationDescription: string
  schoolName: string
  graduationDate: string
  foreignLanguage: ForeignLanguageSummary | ''
  bloodType: BloodType | ''
  mobilePhone: string
  email: string
  hrNotes: string
  nationalIdentityScheme: NationalIdentityScheme | ''
  nationalIdentityNumber: string
  nationality: string
  gender: Gender | ''
  birthDate: string
  birthPlace: string
  maritalStatus: MaritalStatus | ''
  homePhone: string
  residenceAddress: string
  residenceCity: string
  residenceDistrict: string
  notificationAddress: string
  emergencyContacts: EmergencyContactWrite[]
  certificates: EmployeeCertificateWrite[]
  sgkWorkplaceRegistrationId: string
  documentTypeCode: string
  applicableLawCode: string
  insuranceBranchCode: string
  occupationCode: string
  occupationLabel: string
  dutyCode: string
  workType: WorkType | ''
  probationPeriodMonths: ProbationPeriodChoice
  probationStartDate: string
  recruitmentSourceId: string
  recruitmentSourceName: string
  contractType: EmploymentContractType | ''
  contractEndDate: string
  partTimeMonthlyHours: string
  iskurStatus: IskurStatus | ''
  incentiveStartDate: string
  incentiveEndDate: string
  iskurWorkforceStatus: IskurWorkforceStatus | ''
  besDeductionEnabled: boolean
  besRatePercent: string
  besExtraAmount: string
  drivingLicenceCategory: DrivingLicenceCategory | ''
  militaryServiceStatus: MilitaryServiceStatus | ''
  militaryExemptionReason: string
  militaryDefermentReason: string
  kepAddress: string
  workPermitStartDate: string
  workPermitEndDate: string
  seniorityStartDate: string
  paymentIban: string
  paymentBankName: string
}

/** Calendar AddMonths matching System.DateOnly (clamps to last day of month). */
export function addMonthsIso(isoDate: string, months: number): string {
  const iso = toIsoDate(isoDate) ?? isoDate
  const [yearText, monthText, dayText] = iso.split('-')
  const year = Number(yearText)
  const month = Number(monthText)
  const day = Number(dayText)
  if (!year || !month || !day) {
    return isoDate
  }

  const absolute = year * 12 + (month - 1) + months
  const targetYear = Math.floor(absolute / 12)
  const targetMonthIndex = ((absolute % 12) + 12) % 12
  const lastDay = new Date(Date.UTC(targetYear, targetMonthIndex + 1, 0)).getUTCDate()
  const clampedDay = Math.min(day, lastDay)
  return `${targetYear}-${String(targetMonthIndex + 1).padStart(2, '0')}-${String(clampedDay).padStart(2, '0')}`
}

export function derivedProbationEndDate(probationPeriodMonths: ProbationPeriodChoice, probationStartDate: string): string | null {
  if (probationPeriodMonths !== '2') {
    return null
  }
  const start = toIsoDate(probationStartDate)
  return start ? addMonthsIso(start, 2) : null
}

export function emptyPersonnelForm(today: string): PersonnelForm {
  return {
    givenName: '',
    familyName: '',
    personnelNumber: '',
    employmentStartDate: today,
    departmentId: '',
    positionId: '',
    educationLevel: '',
    educationDescription: '',
    schoolName: '',
    graduationDate: '',
    foreignLanguage: '',
    bloodType: '',
    mobilePhone: '',
    email: '',
    hrNotes: '',
    nationalIdentityScheme: '',
    nationalIdentityNumber: '',
    nationality: '',
    gender: '',
    birthDate: '',
    birthPlace: '',
    maritalStatus: '',
    homePhone: '',
    residenceAddress: '',
    residenceCity: '',
    residenceDistrict: '',
    notificationAddress: '',
    emergencyContacts: [],
    certificates: [],
    sgkWorkplaceRegistrationId: '',
    documentTypeCode: '',
    applicableLawCode: '',
    insuranceBranchCode: '',
    occupationCode: '',
    occupationLabel: '',
    dutyCode: '',
    workType: '',
    probationPeriodMonths: '',
    probationStartDate: '',
    recruitmentSourceId: '',
    recruitmentSourceName: '',
    contractType: '',
    contractEndDate: '',
    partTimeMonthlyHours: '',
    iskurStatus: '',
    incentiveStartDate: '',
    incentiveEndDate: '',
    iskurWorkforceStatus: '',
    besDeductionEnabled: false,
    besRatePercent: '',
    besExtraAmount: '',
    drivingLicenceCategory: '',
    militaryServiceStatus: '',
    militaryExemptionReason: '',
    militaryDefermentReason: '',
    kepAddress: '',
    workPermitStartDate: '',
    workPermitEndDate: '',
    seniorityStartDate: '',
    paymentIban: '',
    paymentBankName: '',
  }
}

export function formFromCard(card: HrEmployeeCard): PersonnelForm {
  const profile = card.profile
  const terms = card.workforceTerms
  const bes = card.besSettings
  return {
    givenName: card.givenName,
    familyName: card.familyName,
    personnelNumber: card.personnelNumber,
    employmentStartDate: card.currentEmployment?.startDate ?? card.employments[0]?.startDate ?? '',
    departmentId: card.currentPrimaryAssignment?.departmentId ?? '',
    positionId: card.currentPrimaryAssignment?.positionId ?? '',
    educationLevel: profile.educationLevel ?? '',
    educationDescription: profile.educationDescription ?? '',
    schoolName: profile.schoolName ?? '',
    graduationDate: profile.graduationDate ?? '',
    foreignLanguage: profile.foreignLanguage ?? '',
    bloodType: profile.bloodType ?? '',
    mobilePhone: normalizeMobileDigits(profile.mobilePhone ?? ''),
    email: profile.email ?? '',
    hrNotes: profile.hrNotes ?? '',
    nationalIdentityScheme: profile.nationalIdentityScheme ?? '',
    nationalIdentityNumber: profile.nationalIdentityNumber ?? '',
    nationality: profile.nationality ?? '',
    gender: profile.gender ?? '',
    birthDate: profile.birthDate ?? '',
    birthPlace: profile.birthPlace ?? '',
    maritalStatus: profile.maritalStatus ?? '',
    homePhone: profile.homePhone ?? '',
    residenceAddress: profile.residenceAddress ?? '',
    residenceCity: profile.residenceCity ?? '',
    residenceDistrict: profile.residenceDistrict ?? '',
    notificationAddress: profile.notificationAddress ?? '',
    emergencyContacts: profile.emergencyContacts.map((item) => ({
      id: item.id,
      name: item.name,
      relationship: item.relationship ?? '',
      phone: normalizeMobileDigits(item.phone),
      isPrimary: item.isPrimary,
    })),
    certificates: (card.certificates ?? []).map((item) => ({
      id: item.id,
      name: item.name,
    })),
    sgkWorkplaceRegistrationId: card.officialProfile?.sgkWorkplaceRegistrationId ?? '',
    documentTypeCode: card.officialProfile?.documentTypeCode ?? '',
    applicableLawCode: card.officialProfile?.applicableLawCode ?? '',
    insuranceBranchCode: card.officialProfile?.insuranceBranchCode ?? '',
    occupationCode: card.officialProfile?.occupationCode ?? '',
    occupationLabel: card.officialProfile?.occupation
      ? `${card.officialProfile.occupation.code} — ${card.officialProfile.occupation.description}`
      : '',
    dutyCode: card.officialProfile?.dutyCode ?? '',
    workType: terms?.workType ?? '',
    probationPeriodMonths: terms?.probationPeriodMonths === 2 ? '2' : '',
    probationStartDate: terms?.probationStartDate ?? '',
    recruitmentSourceId: terms?.recruitmentSourceId ?? '',
    recruitmentSourceName: terms?.recruitmentSourceName ?? '',
    contractType: terms?.contractType ?? '',
    contractEndDate: terms?.contractEndDate ?? '',
    partTimeMonthlyHours: numberToInput(terms?.partTimeMonthlyHours),
    iskurStatus: terms?.iskurStatus ?? '',
    incentiveStartDate: terms?.incentiveStartDate ?? '',
    incentiveEndDate: terms?.incentiveEndDate ?? '',
    iskurWorkforceStatus: terms?.iskurWorkforceStatus ?? '',
    besDeductionEnabled: bes?.deductionEnabled ?? false,
    besRatePercent: numberToInput(bes?.ratePercent),
    besExtraAmount: numberToInput(bes?.extraAmount),
    drivingLicenceCategory: profile.drivingLicenceCategory ?? '',
    militaryServiceStatus: profile.militaryServiceStatus ?? '',
    militaryExemptionReason: profile.militaryExemptionReason ?? '',
    militaryDefermentReason: profile.militaryDefermentReason ?? '',
    kepAddress: profile.kepAddress ?? '',
    workPermitStartDate: terms?.workPermitStartDate ?? '',
    workPermitEndDate: terms?.workPermitEndDate ?? '',
    seniorityStartDate:
      card.currentEmployment?.seniorityStartDate ?? card.employments[0]?.seniorityStartDate ?? '',
    paymentIban: card.paymentProfile?.iban ?? '',
    paymentBankName: card.paymentProfile?.bankName ?? '',
  }
}

export function snapshotOf(form: PersonnelForm): string {
  return JSON.stringify(form)
}

export function isPersonnelFormDirty(form: PersonnelForm, snapshot: string): boolean {
  return snapshotOf(form) !== snapshot
}

function emptyToNull(value: string): string | null {
  const trimmed = value.trim()
  return trimmed === '' ? null : trimmed
}

function isoOrNull(value: string): string | null {
  return toIsoDate(value)
}

export function hasPaymentInput(form: PersonnelForm): boolean {
  return toPersistedIban(form.paymentIban) !== '' || form.paymentBankName.trim() !== ''
}

function emptyToNumber(value: string): number | null {
  const trimmed = value.trim().replace(',', '.')
  if (trimmed === '') {
    return null
  }

  const parsed = Number(trimmed)
  return Number.isFinite(parsed) ? parsed : Number.NaN
}

function numberToInput(value: number | null | undefined): string {
  return value == null ? '' : String(value)
}

export function toHrWrite(form: PersonnelForm, includeHireFields: boolean): HrEmployeeWrite {
  const hasProbation = form.probationPeriodMonths === '2'
  const body: HrEmployeeWrite = {
    givenName: form.givenName.trim(),
    familyName: form.familyName.trim(),
    nationalIdentityScheme: form.nationalIdentityScheme === '' ? null : form.nationalIdentityScheme,
    nationalIdentityNumber: emptyToNull(form.nationalIdentityNumber),
    nationality: emptyToNull(form.nationality),
    gender: form.gender === '' ? null : form.gender,
    birthDate: isoOrNull(form.birthDate),
    birthPlace: emptyToNull(form.birthPlace),
    maritalStatus: form.maritalStatus === '' ? null : form.maritalStatus,
    bloodType: form.bloodType === '' ? null : form.bloodType,
    educationLevel: form.educationLevel === '' ? null : form.educationLevel,
    educationDescription: emptyToNull(form.educationDescription),
    schoolName: emptyToNull(form.schoolName),
    graduationDate: isoOrNull(form.graduationDate),
    foreignLanguage: form.foreignLanguage === '' ? null : form.foreignLanguage,
    drivingLicenceCategory: form.drivingLicenceCategory === '' ? null : form.drivingLicenceCategory,
    militaryServiceStatus: form.militaryServiceStatus === '' ? null : form.militaryServiceStatus,
    militaryExemptionReason:
      form.militaryServiceStatus === 'Exempt' ? emptyToNull(form.militaryExemptionReason) : null,
    militaryDefermentReason:
      form.militaryServiceStatus === 'Deferred' ? emptyToNull(form.militaryDefermentReason) : null,
    kepAddress: emptyToNull(form.kepAddress),
    mobilePhone: emptyToNull(normalizeMobileDigits(form.mobilePhone)),
    homePhone: emptyToNull(form.homePhone),
    email: emptyToNull(form.email),
    residenceAddress: emptyToNull(form.residenceAddress),
    residenceCity: emptyToNull(form.residenceCity),
    residenceDistrict: emptyToNull(form.residenceDistrict),
    notificationAddress: emptyToNull(form.notificationAddress),
    hrNotes: emptyToNull(form.hrNotes),
    emergencyContacts: form.emergencyContacts.map((item) => ({
      id: item.id,
      name: item.name,
      relationship: item.relationship,
      phone: item.phone.trim() === '' ? '' : normalizeMobileDigits(item.phone),
      isPrimary: item.isPrimary,
    })),
    certificates: form.certificates.map((item) => ({
      id: item.id,
      name: item.name,
    })),
    officialProfile: {
      sgkWorkplaceRegistrationId: emptyToNull(form.sgkWorkplaceRegistrationId),
      documentTypeCode: emptyToNull(form.documentTypeCode),
      applicableLawCode: emptyToNull(form.applicableLawCode),
      insuranceBranchCode: emptyToNull(form.insuranceBranchCode),
      occupationCode: emptyToNull(form.occupationCode),
      dutyCode: emptyToNull(form.dutyCode),
    },
    workforceTerms: {
      contractType: form.contractType === '' ? null : form.contractType,
      contractEndDate: form.contractType === 'FixedTerm' ? isoOrNull(form.contractEndDate) : null,
      partTimeMonthlyHours: form.contractType === 'PartTime' ? emptyToNumber(form.partTimeMonthlyHours) : null,
      iskurStatus: form.iskurStatus === '' ? null : form.iskurStatus,
      incentiveStartDate: isoOrNull(form.incentiveStartDate),
      incentiveEndDate: isoOrNull(form.incentiveEndDate),
      iskurWorkforceStatus: form.iskurWorkforceStatus === '' ? null : form.iskurWorkforceStatus,
      workPermitStartDate: isoOrNull(form.workPermitStartDate),
      workPermitEndDate: isoOrNull(form.workPermitEndDate),
      workType: form.workType === '' ? null : form.workType,
      probationPeriodMonths: hasProbation ? 2 : null,
      probationStartDate: hasProbation ? isoOrNull(form.probationStartDate) : null,
      recruitmentSourceId: emptyToNull(form.recruitmentSourceId),
    },
    besSettings: {
      deductionEnabled: form.besDeductionEnabled,
      ratePercent: form.besDeductionEnabled ? emptyToNumber(form.besRatePercent) : null,
      extraAmount: form.besDeductionEnabled ? emptyToNumber(form.besExtraAmount) : null,
    },
    seniorityStartDate: isoOrNull(form.seniorityStartDate),
  }

  if (includeHireFields) {
    body.employmentStartDate = isoOrNull(form.employmentStartDate) ?? form.employmentStartDate
    body.departmentId = form.departmentId
    body.positionId = form.positionId
  }

  return body
}
