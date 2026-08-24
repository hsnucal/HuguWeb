import { ApiError, apiRequest } from '../shared/apiClient'
import type { SgkWorkplaceRecord } from './hrApi'

export type DepartmentRecord = {
  id: string
  propertyId: string
  name: string
  code: string | null
  isActive: boolean
}

export type PositionRecord = {
  id: string
  propertyId: string
  name: string
  code: string | null
  isActive: boolean
  applicableDepartmentIds: string[]
}

export type ActiveWorkforceMember = {
  employeeId: string
  personnelNumber: string
  givenName: string
  familyName: string
  employmentId: string
  employmentStartDate: string
  departmentId: string
  departmentName: string
  positionId: string
  positionName: string
}

export type EmployeeDirectoryItem = {
  employeeId: string
  personnelNumber: string
  givenName: string
  familyName: string
  employmentStatus: 'Scheduled' | 'Active' | 'Ended'
  employmentStartDate: string
  employmentEndDate: string | null
  departmentName: string | null
  positionName: string | null
}

export type AssignmentHistoryRecord = {
  id: string
  departmentId: string
  departmentName: string
  positionId: string
  positionName: string
  startDate: string
  endDate: string | null
  kind: 'Primary' | 'Temporary'
}

export type EmploymentHistoryRecord = {
  id: string
  startDate: string
  endDate: string | null
  status: 'Scheduled' | 'Active' | 'Ended'
  primaryAssignments: AssignmentHistoryRecord[]
}

export type EmployeeHistory = {
  id: string
  personnelNumber: string
  givenName: string
  familyName: string
  currentEmployment: EmploymentHistoryRecord | null
  currentPrimaryAssignment: AssignmentHistoryRecord | null
  employments: EmploymentHistoryRecord[]
}

export async function listDepartments() {
  return apiRequest<DepartmentRecord[]>('/api/workforce/departments')
}

export async function createDepartment(name: string, code: string) {
  return apiRequest<DepartmentRecord>('/api/workforce/departments', {
    method: 'POST',
    body: JSON.stringify({ name, code: code.trim() === '' ? null : code }),
  })
}

export async function updateDepartment(
  id: string,
  body: { name?: string; code?: string | null; isActive?: boolean },
) {
  return apiRequest<DepartmentRecord>(`/api/workforce/departments/${id}`, {
    method: 'PATCH',
    body: JSON.stringify(body),
  })
}

export async function listPositions() {
  return apiRequest<PositionRecord[]>('/api/workforce/positions')
}

export async function createPosition(name: string, code: string, departmentIds: string[]) {
  return apiRequest<PositionRecord>('/api/workforce/positions', {
    method: 'POST',
    body: JSON.stringify({
      name,
      code: code.trim() === '' ? null : code,
      departmentIds,
    }),
  })
}

export async function updatePosition(
  id: string,
  body: { name?: string; code?: string | null; isActive?: boolean; departmentIds?: string[] },
) {
  return apiRequest<PositionRecord>(`/api/workforce/positions/${id}`, {
    method: 'PATCH',
    body: JSON.stringify(body),
  })
}

export async function listActiveWorkforce() {
  return apiRequest<ActiveWorkforceMember[]>('/api/workforce/active')
}

export async function listEmployees() {
  return apiRequest<EmployeeDirectoryItem[]>('/api/workforce/employees')
}

export async function getEmployee(id: string) {
  return apiRequest<EmployeeHistory>(`/api/workforce/employees/${id}`)
}

export async function hireEmployee(input: {
  givenName: string
  familyName: string
  personnelNumber: string
  employmentStartDate: string
  departmentId: string
  positionId: string
}) {
  return apiRequest<unknown>('/api/workforce/employees/hire', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function transferEmployee(
  employeeId: string,
  input: { departmentId: string; positionId: string; effectiveDate: string },
) {
  return apiRequest<unknown>(`/api/workforce/employees/${employeeId}/transfer`, {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function endEmployment(employeeId: string, endDate: string) {
  return apiRequest<unknown>(`/api/workforce/employees/${employeeId}/end-employment`, {
    method: 'POST',
    body: JSON.stringify({ endDate }),
  })
}

export async function listSgkWorkplaces() {
  return apiRequest<SgkWorkplaceRecord[]>('/api/workforce/sgk-workplace-registrations')
}

export async function createSgkWorkplace(registrationNumber: string, displayName: string) {
  return apiRequest<SgkWorkplaceRecord>('/api/workforce/sgk-workplace-registrations', {
    method: 'POST',
    body: JSON.stringify({
      registrationNumber,
      displayName: displayName.trim() === '' ? null : displayName,
    }),
  })
}

export async function updateSgkWorkplace(
  id: string,
  body: { registrationNumber?: string; displayName?: string | null; isActive?: boolean },
) {
  return apiRequest<SgkWorkplaceRecord>(`/api/workforce/sgk-workplace-registrations/${id}`, {
    method: 'PATCH',
    body: JSON.stringify(body),
  })
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
  'invalid-sgk-workplace': 'workforce.errors.invalidSgkWorkplace',
  'registration-number-required': 'workforce.errors.invalidSgkWorkplace',
}

export function workforceErrorKey(error: unknown): string {
  if (error instanceof ApiError && error.problem?.code && errorKeys[error.problem.code]) {
    return errorKeys[error.problem.code]
  }

  return 'workforce.errors.generic'
}
