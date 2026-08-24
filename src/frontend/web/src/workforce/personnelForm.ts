import type {
  BloodType,
  DrivingLicenceCategory,
  EducationLevel,
  EmergencyContactWrite,
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
} from './hrApi'
import { normalizeMobileDigits } from './personnelInput'

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
  argeProjectCode: string
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
  sgkWorkplaceRegistrationId: string
  documentTypeCode: string
  applicableLawCode: string
  insuranceBranchCode: string
  occupationCode: string
  occupationLabel: string
  dutyCode: string
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
    argeProjectCode: '',
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
    sgkWorkplaceRegistrationId: '',
    documentTypeCode: '',
    applicableLawCode: '',
    insuranceBranchCode: '',
    occupationCode: '',
    occupationLabel: '',
    dutyCode: '',
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
    argeProjectCode: profile.argeProjectCode ?? '',
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
      phone: item.phone,
      isPrimary: item.isPrimary,
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
  const body: HrEmployeeWrite = {
    givenName: form.givenName.trim(),
    familyName: form.familyName.trim(),
    nationalIdentityScheme: form.nationalIdentityScheme === '' ? null : form.nationalIdentityScheme,
    nationalIdentityNumber: emptyToNull(form.nationalIdentityNumber),
    nationality: emptyToNull(form.nationality),
    gender: form.gender === '' ? null : form.gender,
    birthDate: emptyToNull(form.birthDate),
    birthPlace: emptyToNull(form.birthPlace),
    maritalStatus: form.maritalStatus === '' ? null : form.maritalStatus,
    bloodType: form.bloodType === '' ? null : form.bloodType,
    educationLevel: form.educationLevel === '' ? null : form.educationLevel,
    educationDescription: emptyToNull(form.educationDescription),
    schoolName: emptyToNull(form.schoolName),
    graduationDate: emptyToNull(form.graduationDate),
    foreignLanguage: form.foreignLanguage === '' ? null : form.foreignLanguage,
    argeProjectCode: emptyToNull(form.argeProjectCode),
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
      phone: item.phone,
      isPrimary: item.isPrimary,
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
      contractEndDate: form.contractType === 'FixedTerm' ? emptyToNull(form.contractEndDate) : null,
      partTimeMonthlyHours: form.contractType === 'PartTime' ? emptyToNumber(form.partTimeMonthlyHours) : null,
      iskurStatus: form.iskurStatus === '' ? null : form.iskurStatus,
      incentiveStartDate: emptyToNull(form.incentiveStartDate),
      incentiveEndDate: emptyToNull(form.incentiveEndDate),
      iskurWorkforceStatus: form.iskurWorkforceStatus === '' ? null : form.iskurWorkforceStatus,
      workPermitStartDate: emptyToNull(form.workPermitStartDate),
      workPermitEndDate: emptyToNull(form.workPermitEndDate),
    },
    besSettings: {
      deductionEnabled: form.besDeductionEnabled,
      ratePercent: form.besDeductionEnabled ? emptyToNumber(form.besRatePercent) : null,
      extraAmount: form.besDeductionEnabled ? emptyToNumber(form.besExtraAmount) : null,
    },
  }

  if (includeHireFields) {
    body.employmentStartDate = form.employmentStartDate
    body.departmentId = form.departmentId
    body.positionId = form.positionId
  }

  return body
}
