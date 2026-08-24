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

export type EmergencyContactRead = {
  id: string
  name: string
  relationship: string | null
  phone: string
  isPrimary: boolean
}

export type HrProfileRead = {
  educationLevel: EducationLevel | null
  hrNotes: string | null
  nationality: string | null
  gender: Gender | null
  birthDate: string | null
  birthPlace: string | null
  maritalStatus: MaritalStatus | null
  bloodType: BloodType | null
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
  mobilePhone: string | null
  homePhone: string | null
  email: string | null
  residenceAddress: string | null
  residenceCity: string | null
  residenceDistrict: string | null
  notificationAddress: string | null
  hrNotes: string | null
  emergencyContacts: EmergencyContactWrite[]
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
