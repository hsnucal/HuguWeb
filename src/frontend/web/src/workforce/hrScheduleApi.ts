import { ApiError, apiRequest } from '../shared/apiClient'

export type ShiftDefinitionRecord = {
  id: string
  propertyId: string
  code: string
  name: string
  startLocalTime: string
  endLocalTime: string
  endsNextDay: boolean
  breakMinutes: number
  grossMinutes: number
  plannedNetMinutes: number
  isActive: boolean
  semanticFieldsLocked: boolean
}

export type CreateShiftDefinitionInput = {
  code: string
  name: string
  startLocalTime: string
  endLocalTime: string
  endsNextDay: boolean
  breakMinutes: number
}

export type UpdateShiftDefinitionInput = {
  name?: string
  startLocalTime?: string
  endLocalTime?: string
  endsNextDay?: boolean
  breakMinutes?: number
  isActive?: boolean
}

export type ScheduleCellEligibility = 'Editable' | 'OutOfScope' | 'NotEmployed'
export type ScheduleCellState = 'Unscheduled' | 'Shift' | 'RestDay'
export type ScheduleEntryKind = 'Shift' | 'RestDay'

export type ScheduleWeekDepartment = {
  id: string
  name: string
  isActive: boolean
}

export type ScheduleWeekShiftDefinition = {
  id: string
  code: string
  name: string
  startLocalTime: string
  endLocalTime: string
  endsNextDay: boolean
  breakMinutes: number
  grossMinutes: number
  plannedNetMinutes: number
  isActive: boolean
}

export type ScheduleWeekCell = {
  date: string
  eligibility: ScheduleCellEligibility
  state: ScheduleCellState | null
  scheduleEntryId: string | null
  employmentId: string | null
  assignmentId: string | null
  departmentId: string | null
  departmentName: string | null
  note: string | null
  shiftDefinitionId: string | null
  shiftCode: string | null
  shiftName: string | null
  shiftIsActive: boolean | null
  startLocalTime: string | null
  endLocalTime: string | null
  endsNextDay: boolean | null
  breakMinutes: number | null
  grossMinutes: number | null
  plannedNetMinutes: number | null
}

export type ScheduleWeekEmployee = {
  employeeId: string
  givenName: string
  familyName: string
  personnelNumber: string
  rowDepartmentId: string | null
  rowDepartmentName: string | null
  cells: ScheduleWeekCell[]
}

export type ScheduleWeekDto = {
  weekStart: string
  weekEnd: string
  dates: string[]
  propertyId: string
  propertyWide: boolean
  selectedDepartmentId: string | null
  filterDepartments: ScheduleWeekDepartment[]
  employees: ScheduleWeekEmployee[]
  shiftDefinitions: ScheduleWeekShiftDefinition[]
}

export type BulkScheduleOperationInput = {
  employeeId: string
  date: string
  clear: boolean
  kind?: ScheduleEntryKind | null
  shiftDefinitionId?: string | null
  note?: string | null
}

export type UpsertScheduleInput = {
  kind: ScheduleEntryKind
  shiftDefinitionId?: string | null
  note?: string | null
}

export type CopyScheduleWeekInput = {
  targetWeekStart: string
  departmentId?: string | null
}

export type CopyScheduleWeekOperation = {
  employeeId: string
  givenName: string
  familyName: string
  personnelNumber: string
  sourceDate: string
  targetDate: string
  kind: string
  shiftDefinitionId: string | null
  shiftCode: string | null
  shiftName: string | null
  wouldOverwrite: boolean
  targetAssignmentId: string
  targetDepartmentId: string
  targetDepartmentName: string
}

export type CopyScheduleWeekInvalid = {
  employeeId: string
  givenName: string
  familyName: string
  personnelNumber: string
  sourceDate: string
  targetDate: string
  code: string
  detail: string
}

export type CopyScheduleWeekPreview = {
  sourceWeekStart: string
  sourceWeekEnd: string
  targetWeekStart: string
  targetWeekEnd: string
  departmentId: string | null
  copyCount: number
  overwriteCount: number
  invalidCount: number
  operations: CopyScheduleWeekOperation[]
  invalid: CopyScheduleWeekInvalid[]
}

export async function listHrShiftDefinitions(activeOnly = false) {
  const query = activeOnly ? '?activeOnly=true' : ''
  return apiRequest<ShiftDefinitionRecord[]>(`/api/hr/shift-definitions${query}`)
}

export async function createHrShiftDefinition(input: CreateShiftDefinitionInput) {
  return apiRequest<ShiftDefinitionRecord>('/api/hr/shift-definitions', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function updateHrShiftDefinition(id: string, patch: UpdateShiftDefinitionInput) {
  return apiRequest<ShiftDefinitionRecord>(`/api/hr/shift-definitions/${id}`, {
    method: 'PATCH',
    body: JSON.stringify(patch),
  })
}

export async function getHrScheduleWeek(weekStart: string, departmentId?: string | null) {
  const params = new URLSearchParams({ weekStart })
  if (departmentId) {
    params.set('departmentId', departmentId)
  }
  return apiRequest<ScheduleWeekDto>(`/api/hr/schedule/week?${params.toString()}`)
}

export async function bulkHrSchedule(operations: BulkScheduleOperationInput[]) {
  return apiRequest<{ results: unknown[] }>('/api/hr/schedule/bulk', {
    method: 'POST',
    body: JSON.stringify({ operations }),
  })
}

export async function previewCopyHrScheduleWeek(input: CopyScheduleWeekInput) {
  return apiRequest<CopyScheduleWeekPreview>('/api/hr/schedule/copy-week/preview', {
    method: 'POST',
    body: JSON.stringify({
      targetWeekStart: input.targetWeekStart,
      departmentId: input.departmentId ?? null,
    }),
  })
}

export async function copyHrScheduleWeek(input: CopyScheduleWeekInput) {
  return apiRequest<{ results: unknown[] }>('/api/hr/schedule/copy-week', {
    method: 'POST',
    body: JSON.stringify({
      targetWeekStart: input.targetWeekStart,
      departmentId: input.departmentId ?? null,
    }),
  })
}

export async function upsertHrEmployeeSchedule(
  employeeId: string,
  date: string,
  input: UpsertScheduleInput,
) {
  return apiRequest<unknown>(`/api/hr/employees/${employeeId}/schedule/${date}`, {
    method: 'PUT',
    body: JSON.stringify({
      kind: input.kind,
      shiftDefinitionId: input.shiftDefinitionId ?? null,
      note: input.note ?? null,
    }),
  })
}

export async function clearHrEmployeeSchedule(employeeId: string, date: string) {
  return apiRequest<unknown>(`/api/hr/employees/${employeeId}/schedule/${date}/clear`, {
    method: 'POST',
  })
}

const scheduleErrorKeys: Record<string, string> = {
  'shift-definition-not-found': 'workforce.scheduleErrors.notFound',
  'shift-definition-code-exists': 'workforce.scheduleErrors.codeExists',
  'shift-definition-code-required': 'workforce.scheduleErrors.codeRequired',
  'shift-definition-code-too-long': 'workforce.scheduleErrors.codeTooLong',
  'shift-definition-name-required': 'workforce.scheduleErrors.nameRequired',
  'shift-definition-name-too-long': 'workforce.scheduleErrors.nameTooLong',
  'shift-definition-invalid-time': 'workforce.scheduleErrors.invalidTime',
  'shift-definition-invalid-break': 'workforce.scheduleErrors.invalidBreak',
  'shift-definition-inactive': 'workforce.scheduleErrors.inactive',
  'shift-definition-semantic-fields-locked': 'workforce.scheduleErrors.semanticFieldsLocked',
  'schedule-employment-not-covering-date': 'workforce.scheduleErrors.employmentNotCoveringDate',
  'schedule-assignment-not-found': 'workforce.scheduleErrors.assignmentNotFound',
  'schedule-cross-property-shift': 'workforce.scheduleErrors.crossPropertyShift',
  'schedule-invalid-kind': 'workforce.scheduleErrors.invalidKind',
  'schedule-entry-conflict': 'workforce.scheduleErrors.entryConflict',
  'schedule-note-too-long': 'workforce.scheduleErrors.noteTooLong',
  'schedule-invalid-range': 'workforce.scheduleErrors.invalidRange',
  'schedule-shift-definition-required': 'workforce.scheduleErrors.shiftDefinitionRequired',
  'schedule-shift-definition-must-be-null': 'workforce.scheduleErrors.shiftDefinitionMustBeNull',
  'schedule-property-access-denied': 'workforce.scheduleErrors.propertyAccessDenied',
  'workplace-not-configured': 'workforce.scheduleErrors.workplaceNotConfigured',
  'property-context-required': 'workforce.scheduleErrors.propertyContextRequired',
  'schedule-week-start-invalid': 'workforce.scheduleErrors.weekStartInvalid',
  'schedule-department-filter-denied': 'workforce.scheduleErrors.departmentFilterDenied',
  'schedule-bulk-failed': 'workforce.scheduleErrors.bulkFailed',
  'schedule-copy-week-blocked': 'workforce.scheduleErrors.copyWeekBlocked',
  'schedule-copy-week-empty': 'workforce.scheduleErrors.copyWeekEmpty',
}

export function hrScheduleErrorKey(error: unknown): string {
  if (error instanceof ApiError && error.problem?.code && scheduleErrorKeys[error.problem.code]) {
    return scheduleErrorKeys[error.problem.code]
  }

  return 'workforce.scheduleErrors.generic'
}

