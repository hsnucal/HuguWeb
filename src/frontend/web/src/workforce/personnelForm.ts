import type {
  BloodType,
  EducationLevel,
  EmergencyContactWrite,
  Gender,
  HrEmployeeCard,
  HrEmployeeWrite,
  MaritalStatus,
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
  }
}

export function formFromCard(card: HrEmployeeCard): PersonnelForm {
  const profile = card.profile
  return {
    givenName: card.givenName,
    familyName: card.familyName,
    personnelNumber: card.personnelNumber,
    employmentStartDate: card.currentEmployment?.startDate ?? card.employments[0]?.startDate ?? '',
    departmentId: card.currentPrimaryAssignment?.departmentId ?? '',
    positionId: card.currentPrimaryAssignment?.positionId ?? '',
    educationLevel: profile.educationLevel ?? '',
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
  }

  if (includeHireFields) {
    body.employmentStartDate = form.employmentStartDate
    body.departmentId = form.departmentId
    body.positionId = form.positionId
  }

  return body
}
