export const MOVEMENT_REASON_MAX = 500
export const MOVEMENT_NOTE_MAX = 500

export const MOVEMENT_TYPES = [
  'DepartmentChange',
  'PositionChange',
  'Promotion',
  'PropertyTransfer',
  'ManagerChange',
  'AssignmentChange',
] as const

export const CREATABLE_MOVEMENT_TYPES = [
  'DepartmentChange',
  'PositionChange',
  'Promotion',
  'PropertyTransfer',
  'ManagerChange',
] as const

export const MOVEMENT_LIFECYCLES = ['Scheduled', 'Effective', 'Cancelled'] as const

export type MovementType = (typeof MOVEMENT_TYPES)[number]
export type CreatableMovementType = (typeof CREATABLE_MOVEMENT_TYPES)[number]
export type MovementLifecycle = (typeof MOVEMENT_LIFECYCLES)[number]

export function isMovementType(value: string | null | undefined): value is MovementType {
  return MOVEMENT_TYPES.includes(value as MovementType)
}

export function isCreatableMovementType(value: string | null | undefined): value is CreatableMovementType {
  return CREATABLE_MOVEMENT_TYPES.includes(value as CreatableMovementType)
}

export function isMovementLifecycle(value: string | null | undefined): value is MovementLifecycle {
  return MOVEMENT_LIFECYCLES.includes(value as MovementLifecycle)
}

export type MovementAssignmentSummary = {
  id: string
  departmentId: string
  departmentName: string
  positionId: string
  positionName: string
  propertyId: string
  propertyName: string
  startDate: string
  endDate: string | null
}

export type MovementReportingLineSummary = {
  id: string
  managerEmploymentId: string
  managerEmployeeId: string
  managerGivenName: string
  managerFamilyName: string
  effectiveFrom: string
  effectiveTo: string | null
}

export type CreateMovementRequest = {
  employmentId: string
  type: string
  effectiveDate: string
  targetPropertyId?: string | null
  targetDepartmentId?: string | null
  targetPositionId?: string | null
  targetManagerEmploymentId?: string | null
  clearManager: boolean
  reason: string
  note?: string | null
}

export type ListMovementsQuery = {
  dateFrom?: string | null
  dateTo?: string | null
  type?: string | null
  departmentId?: string | null
  employeeId?: string | null
  propertyId?: string | null
  search?: string | null
}

export function buildMovementsListPath(query: ListMovementsQuery): string {
  const params = new URLSearchParams()
  const dateFrom = query.dateFrom?.trim()
  const dateTo = query.dateTo?.trim()
  const type = query.type?.trim()
  const departmentId = query.departmentId?.trim()
  const employeeId = query.employeeId?.trim()
  const propertyId = query.propertyId?.trim()
  const search = query.search?.trim()
  if (dateFrom) {
    params.set('dateFrom', dateFrom)
  }
  if (dateTo) {
    params.set('dateTo', dateTo)
  }
  if (type) {
    params.set('type', type)
  }
  if (departmentId) {
    params.set('departmentId', departmentId)
  }
  if (employeeId) {
    params.set('employeeId', employeeId)
  }
  if (propertyId) {
    params.set('propertyId', propertyId)
  }
  if (search) {
    params.set('search', search)
  }
  const suffix = params.toString()
  return suffix === '' ? '/api/hr/movements' : `/api/hr/movements?${suffix}`
}

export function movementDetailPath(id: string): string {
  return `/api/hr/movements/${id}`
}

export function movementCancelPath(id: string): string {
  return `/api/hr/movements/${id}/cancel`
}

export function movementStructurePath(propertyId: string): string {
  return `/api/hr/movements/structure?propertyId=${encodeURIComponent(propertyId)}`
}

export function movementManagerCandidatesPath(employmentId: string, effectiveDate: string): string {
  const params = new URLSearchParams()
  params.set('employmentId', employmentId)
  params.set('effectiveDate', effectiveDate)
  return `/api/hr/movements/manager-candidates?${params.toString()}`
}

export const movementErrorKeys: Record<string, string> = {
  'movement-invalid-type': 'movements.errors.invalidType',
  'movement-reason-required': 'movements.errors.reasonRequired',
  'movement-reason-too-long': 'movements.errors.reasonTooLong',
  'movement-note-too-long': 'movements.errors.noteTooLong',
  'movement-effective-date-invalid': 'movements.errors.effectiveDateInvalid',
  'movement-employment-not-found': 'movements.errors.employmentNotFound',
  'movement-assignment-not-found': 'movements.errors.assignmentNotFound',
  'movement-same-target': 'movements.errors.sameTarget',
  'movement-position-not-applicable': 'movements.errors.positionNotApplicable',
  'movement-property-access-denied': 'movements.errors.propertyAccessDenied',
  'movement-cross-organization-not-supported': 'movements.errors.crossOrganization',
  'movement-pending-leave-conflict': 'movements.errors.pendingLeaveConflict',
  'movement-schedule-conflict': 'movements.errors.scheduleConflict',
  'movement-not-cancellable': 'movements.errors.notCancellable',
  'movement-already-effective': 'movements.errors.alreadyEffective',
  'movement-already-cancelled': 'movements.errors.alreadyCancelled',
  'movement-not-found': 'movements.errors.notFound',
  'movement-target-position-required': 'movements.errors.targetPositionRequired',
  'movement-target-department-required': 'movements.errors.targetDepartmentRequired',
  'movement-target-property-required': 'movements.errors.targetPropertyRequired',
  'reporting-line-self-manager': 'movements.errors.selfManager',
  'reporting-line-cycle': 'movements.errors.reportingCycle',
  'reporting-line-overlap': 'movements.errors.reportingOverlap',
  'reporting-line-manager-not-found': 'movements.errors.managerNotFound',
  'reporting-line-organization-mismatch': 'movements.errors.organizationMismatch',
  'movement-manager-level-invalid': 'movements.errors.managerLevelInvalid',
  'movement-manager-cannot-manage': 'movements.errors.managerCannotManage',
  'movement-target-not-promotion': 'movements.errors.targetNotPromotion',
  'movement-cancellation-reason-required': 'movements.errors.cancellationReasonRequired',
  'movement-cancellation-reason-too-long': 'movements.errors.cancellationReasonTooLong',
  'overlapping-primary-assignment': 'movements.errors.dateConflict',
  'invalid-transfer-date': 'movements.errors.dateConflict',
  'no-current-employment': 'workforce.errors.noCurrentEmployment',
  'employment-ended': 'workforce.errors.employmentEnded',
  'property-context-required': 'common.propertySelectionRequired',
}

export const movementDateHistoryConflictCodes = new Set([
  'overlapping-primary-assignment',
  'invalid-transfer-date',
])

export function hrMovementErrorKeyFromCode(code: string | undefined): string {
  if (code && movementErrorKeys[code]) {
    return movementErrorKeys[code]
  }
  return 'movements.errors.generic'
}

export function hrMovementErrorStep(code: string | undefined): 'date' | null {
  if (!code) {
    return null
  }
  if (movementDateHistoryConflictCodes.has(code) || code === 'movement-effective-date-invalid') {
    return 'date'
  }
  return null
}

export type MovementProblemError = {
  message: string
  problem?: {
    detail?: string
    code?: string
  }
}

export function hrMovementErrorMessage(
  error: unknown,
  translate: (key: string, options?: Record<string, string>) => string,
  options?: { earliestEffectiveDateLabel?: string },
): string {
  if (typeof error !== 'object' || error === null || !('message' in error)) {
    return translate('movements.errors.generic')
  }

  const problem = (error as MovementProblemError).problem
  const code = problem?.code
  const key = hrMovementErrorKeyFromCode(code)
  if (code && movementDateHistoryConflictCodes.has(code) && options?.earliestEffectiveDateLabel) {
    return translate('movements.errors.dateConflictWithBound', { date: options.earliestEffectiveDateLabel })
  }
  if (key !== 'movements.errors.generic') {
    return translate(key)
  }

  const message = (error as MovementProblemError).message
  if (/primary assignments cannot overlap/i.test(message) || /previous primary must end/i.test(message)) {
    return translate('movements.errors.dateConflict')
  }
  if (/transfer date would invert/i.test(message)) {
    return translate('movements.errors.dateConflict')
  }
  return message.trim() === '' ? translate('movements.errors.generic') : translate('movements.errors.generic')
}
