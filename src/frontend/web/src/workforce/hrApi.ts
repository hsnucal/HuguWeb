import { ApiError, apiRequest, apiUpload } from '../shared/apiClient'
import type {
  AssignmentHistoryRecord,
  EmploymentHistoryRecord,
} from './workforceApi'

export type NationalIdentityScheme = 'Tckn' | 'Ykn' | 'Passport' | 'Other'
export type Gender = 'Unspecified' | 'Female' | 'Male'
export type MaritalStatus = 'Unspecified' | 'Single' | 'Married' | 'Divorced' | 'Widowed'
export type BloodType =
  | 'Unknown'
  | 'APositive'
  | 'ANegative'
  | 'BPositive'
  | 'BNegative'
  | 'AbPositive'
  | 'AbNegative'
  | 'OPositive'
  | 'ONegative'
export type EducationLevel =
  | 'Unspecified'
  | 'Primary'
  | 'Secondary'
  | 'HighSchool'
  | 'Associate'
  | 'Bachelor'
  | 'Master'
  | 'Doctorate'
export type EmploymentContractType = 'Indefinite' | 'FixedTerm' | 'PartTime'
export type IskurStatus = 'Normal' | 'FormerConvict' | 'TerrorVictim' | 'TmyInjured'
export type IskurWorkforceStatus =
  | 'Indefinite'
  | 'FixedTerm'
  | 'PartTime'
  | 'DisabledIndefinite'
  | 'DisabledFixedTerm'
  | 'FormerConvict'
  | 'TerrorVictim'
export type DrivingLicenceCategory =
  | 'A'
  | 'A1'
  | 'A2'
  | 'B'
  | 'B1'
  | 'Be'
  | 'C'
  | 'Ce'
  | 'D'
  | 'De'
  | 'F'
  | 'G'
export type MilitaryServiceStatus = 'Completed' | 'Exempt' | 'Deferred' | 'NotCompleted'
export type ForeignLanguageSummary =
  | 'English'
  | 'German'
  | 'French'
  | 'Arabic'
  | 'Russian'
  | 'Spanish'
  | 'Chinese'
  | 'Japanese'
  | 'Korean'
  | 'Other'

export type HrEmployeeListItem = {
  employeeId: string
  personnelNumber: string
  givenName: string
  familyName: string
  employmentStatus: 'Scheduled' | 'Active' | 'Ended'
  employmentStartDate: string
  employmentEndDate: string | null
  departmentId: string | null
  departmentName: string | null
  positionId: string | null
  positionName: string | null
  hasPhoto: boolean
  educationLevel: EducationLevel | null
  mobilePhone: string | null
  email: string | null
  bloodType: BloodType | null
  nationalIdentityScheme: NationalIdentityScheme | null
  nationalIdentityNumber: string | null
}

export type OfficialLookupItem = {
  code: string
  description: string
  isActive: boolean
}

export type OfficialLookups = {
  documentTypes: OfficialLookupItem[]
  applicableLaws: OfficialLookupItem[]
  insuranceBranches: OfficialLookupItem[]
  dutyCodes: OfficialLookupItem[]
  nationalities: string[]
}

export type SgkWorkplaceRecord = {
  id: string
  propertyId: string
  registrationNumber: string | null
  displayName: string | null
  pickerLabel: string
  isActive: boolean
}

export type OfficialEmploymentProfileRead = {
  employmentId: string
  sgkWorkplaceRegistrationId: string | null
  sgkWorkplace: SgkWorkplaceRecord | null
  documentTypeCode: string | null
  applicableLawCode: string | null
  insuranceBranchCode: string | null
  occupationCode: string | null
  occupation: OfficialLookupItem | null
  dutyCode: string | null
}

export type OfficialEmploymentWrite = {
  sgkWorkplaceRegistrationId: string | null
  documentTypeCode: string | null
  applicableLawCode: string | null
  insuranceBranchCode: string | null
  occupationCode: string | null
  dutyCode: string | null
}

export type EmploymentWorkforceRead = {
  contractType: EmploymentContractType | null
  contractEndDate: string | null
  partTimeMonthlyHours: number | null
  iskurStatus: IskurStatus | null
  incentiveStartDate: string | null
  incentiveEndDate: string | null
  iskurWorkforceStatus: IskurWorkforceStatus | null
  workPermitStartDate: string | null
  workPermitEndDate: string | null
}

export type EmploymentWorkforceWrite = {
  contractType: EmploymentContractType | null
  contractEndDate: string | null
  partTimeMonthlyHours: number | null
  iskurStatus: IskurStatus | null
  incentiveStartDate: string | null
  incentiveEndDate: string | null
  iskurWorkforceStatus: IskurWorkforceStatus | null
  workPermitStartDate: string | null
  workPermitEndDate: string | null
}

export type EmploymentBesRead = {
  deductionEnabled: boolean
  ratePercent: number | null
  extraAmount: number | null
}

export type EmploymentBesWrite = {
  deductionEnabled: boolean
  ratePercent: number | null
  extraAmount: number | null
}

export type EmergencyContactRead = {
  id: string
  name: string
  relationship: string | null
  phone: string
  isPrimary: boolean
}

export type HrProfileRead = {
  educationLevel: EducationLevel | null
  educationDescription: string | null
  schoolName: string | null
  graduationDate: string | null
  foreignLanguage: ForeignLanguageSummary | null
  argeProjectCode: string | null
  hrNotes: string | null
  nationality: string | null
  gender: Gender | null
  birthDate: string | null
  birthPlace: string | null
  maritalStatus: MaritalStatus | null
  bloodType: BloodType | null
  drivingLicenceCategory: DrivingLicenceCategory | null
  militaryServiceStatus: MilitaryServiceStatus | null
  militaryExemptionReason: string | null
  militaryDefermentReason: string | null
  kepAddress: string | null
  mobilePhone: string | null
  homePhone: string | null
  email: string | null
  nationalIdentityScheme: NationalIdentityScheme | null
  nationalIdentityNumber: string | null
  residenceAddress: string | null
  residenceCity: string | null
  residenceDistrict: string | null
  notificationAddress: string | null
  emergencyContacts: EmergencyContactRead[]
}

export type HrEmployeeCard = {
  employeeId: string
  personnelNumber: string
  givenName: string
  familyName: string
  hasPhoto: boolean
  currentEmployment: EmploymentHistoryRecord | null
  currentPrimaryAssignment: AssignmentHistoryRecord | null
  organizationName: string
  propertyName: string
  employments: EmploymentHistoryRecord[]
  profile: HrProfileRead
  canReadSensitive: boolean
  officialProfile: OfficialEmploymentProfileRead | null
  workforceTerms: EmploymentWorkforceRead | null
  besSettings: EmploymentBesRead | null
  paymentProfile: EmployeePaymentProfileRead | null
}

export type EmployeePaymentProfileRead = {
  iban: string
  bankName: string | null
}

export type EmergencyContactWrite = {
  id?: string
  name: string
  relationship: string
  phone: string
  isPrimary: boolean
}

export type HrEmployeeWrite = {
  givenName: string
  familyName: string
  personnelNumber?: string
  employmentStartDate?: string
  departmentId?: string
  positionId?: string
  nationalIdentityScheme: NationalIdentityScheme | null
  nationalIdentityNumber: string | null
  nationality: string | null
  gender: Gender | null
  birthDate: string | null
  birthPlace: string | null
  maritalStatus: MaritalStatus | null
  bloodType: BloodType | null
  educationLevel: EducationLevel | null
  educationDescription: string | null
  schoolName: string | null
  graduationDate: string | null
  foreignLanguage: ForeignLanguageSummary | null
  argeProjectCode: string | null
  drivingLicenceCategory: DrivingLicenceCategory | null
  militaryServiceStatus: MilitaryServiceStatus | null
  militaryExemptionReason: string | null
  militaryDefermentReason: string | null
  kepAddress: string | null
  mobilePhone: string | null
  homePhone: string | null
  email: string | null
  residenceAddress: string | null
  residenceCity: string | null
  residenceDistrict: string | null
  notificationAddress: string | null
  hrNotes: string | null
  emergencyContacts: EmergencyContactWrite[]
  officialProfile: OfficialEmploymentWrite
  workforceTerms: EmploymentWorkforceWrite
  besSettings: EmploymentBesWrite
}

export function hrEmployeePhotoUrl(employeeId: string) {
  return `/api/hr/employees/${employeeId}/photo`
}

export async function listHrEmployees() {
  return apiRequest<HrEmployeeListItem[]>('/api/hr/employees')
}

export async function getHrEmployee(id: string) {
  return apiRequest<HrEmployeeCard>(`/api/hr/employees/${id}`)
}

export async function createHrEmployee(input: HrEmployeeWrite) {
  return apiRequest<HrEmployeeCard>('/api/hr/employees', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function updateHrEmployee(id: string, input: HrEmployeeWrite) {
  return apiRequest<HrEmployeeCard>(`/api/hr/employees/${id}`, {
    method: 'PUT',
    body: JSON.stringify(input),
  })
}

export async function uploadHrEmployeePhoto(id: string, file: File) {
  const body = new FormData()
  body.append('file', file)
  return apiUpload<HrEmployeeCard>(`/api/hr/employees/${id}/photo`, body)
}

export async function removeHrEmployeePhoto(id: string) {
  return apiRequest<void>(`/api/hr/employees/${id}/photo`, { method: 'DELETE' })
}

export async function listOfficialLookups() {
  return apiRequest<OfficialLookups>('/api/hr/official-lookups')
}

export async function searchOccupationCodes(query: string) {
  const q = query.trim()
  const path = q === '' ? '/api/hr/occupation-codes' : `/api/hr/occupation-codes?q=${encodeURIComponent(q)}`
  return apiRequest<OfficialLookupItem[]>(path)
}

export async function listHrSgkWorkplaces() {
  return apiRequest<SgkWorkplaceRecord[]>('/api/hr/sgk-workplace-registrations')
}

const errorKeys: Record<string, string> = {
  'personnel-number-in-use': 'workforce.errors.personnelNumberInUse',
  'department-inactive': 'workforce.errors.departmentInactive',
  'position-inactive': 'workforce.errors.positionInactive',
  'employment-ended': 'workforce.errors.employmentEnded',
  'no-current-employment': 'workforce.errors.noCurrentEmployment',
  'invalid-transfer-date': 'workforce.errors.invalidTransferDate',
  'overlapping-primary-assignment': 'workforce.errors.overlappingPrimaryAssignment',
  'invalid-employment-period': 'workforce.errors.invalidEmploymentPeriod',
  'same-assignment': 'workforce.errors.sameAssignment',
  'position-not-available-for-department': 'personnel.validation.positionNotAvailable',
  'national-identity-in-use': 'personnel.errors.nationalIdentityInUse',
  'invalid-hr-profile': 'personnel.errors.invalidHrProfile',
  'invalid-emergency-contact': 'personnel.errors.invalidEmergencyContact',
  'invalid-photo': 'personnel.errors.invalidPhoto',
  'sensitive-write-forbidden': 'personnel.errors.sensitiveWriteForbidden',
  'sgk-workplace-not-found': 'personnel.errors.sgkWorkplaceNotFound',
  'sgk-workplace-inactive': 'personnel.errors.sgkWorkplaceInactive',
  'sgk-workplace-not-for-property': 'personnel.errors.sgkWorkplaceNotForProperty',
  'invalid-document-type-code': 'personnel.validation.invalidDocumentType',
  'invalid-applicable-law-code': 'personnel.validation.invalidApplicableLaw',
  'invalid-insurance-branch-code': 'personnel.validation.invalidInsuranceBranch',
  'invalid-occupation-code': 'personnel.validation.invalidOccupation',
  'invalid-duty-code': 'personnel.validation.invalidDutyCode',
  'invalid-nationality': 'personnel.validation.invalidNationality',
  'military-exemption-reason-required': 'personnel.validation.militaryExemptionRequired',
  'military-deferment-reason-required': 'personnel.validation.militaryDefermentRequired',
  'contract-end-date-required': 'personnel.validation.contractEndRequired',
  'part-time-hours-required': 'personnel.validation.partTimeHoursRequired',
  'part-time-hours-invalid': 'personnel.validation.partTimeHoursInvalid',
  'incentive-range-invalid': 'personnel.validation.incentiveRangeInvalid',
  'work-permit-range-invalid': 'personnel.validation.workPermitRangeInvalid',
  'bes-rate-invalid': 'personnel.validation.besRateInvalid',
  'bes-extra-amount-invalid': 'personnel.validation.besExtraInvalid',
  'kep-invalid': 'personnel.validation.kepInvalid',
  'employment-not-found': 'personnel.errors.employmentNotFound',
  'employment-property-unresolved': 'personnel.errors.employmentPropertyUnresolved',
  'property-context-required': 'common.propertySelectionRequired',
}

export function hrErrorKey(error: unknown): string {
  if (error instanceof ApiError && error.problem?.code && errorKeys[error.problem.code]) {
    return errorKeys[error.problem.code]
  }

  return 'personnel.errors.generic'
}

export function hrFieldErrorsFromProblem(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError) || !error.problem) {
    return {}
  }

  const mapped: Record<string, string> = {}
  if (error.problem.errors) {
    for (const [field, reasons] of Object.entries(error.problem.errors)) {
      const reason = reasons[0]
      if (reason) {
        mapped[normalizeProblemField(field)] = reason
      }
    }
  }

  return mapped
}

function normalizeProblemField(field: string): string {
  return field.replace(/^[A-Z]/, (character) => character.toLowerCase())
}
