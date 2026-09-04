import { ApiError, apiRequest } from '../shared/apiClient'
import {
  buildMovementsListPath,
  hrMovementErrorKeyFromCode,
  movementCancelPath,
  movementDetailPath,
  movementStructurePath,
  movementManagerCandidatesPath,
  type CreateMovementRequest,
  type ListMovementsQuery,
  type MovementAssignmentSummary,
  type MovementLifecycle,
  type MovementReportingLineSummary,
  type MovementType,
} from './hrMovementsPaths.ts'
import type { DepartmentRecord, PositionRecord } from './workforceApi'

export type {
  CreatableMovementType,
  CreateMovementRequest,
  ListMovementsQuery,
  MovementAssignmentSummary,
  MovementLifecycle,
  MovementReportingLineSummary,
  MovementType,
} from './hrMovementsPaths.ts'
export {
  CREATABLE_MOVEMENT_TYPES,
  MOVEMENT_LIFECYCLES,
  MOVEMENT_NOTE_MAX,
  MOVEMENT_REASON_MAX,
  MOVEMENT_TYPES,
  buildMovementsListPath,
  hrMovementErrorKeyFromCode,
  hrMovementErrorMessage,
  hrMovementErrorStep,
  isCreatableMovementType,
  isMovementLifecycle,
  isMovementType,
} from './hrMovementsPaths.ts'

export type PersonnelMovementListItem = {
  id: string
  employmentId: string
  employeeId: string
  personnelNumber: string
  givenName: string
  familyName: string
  type: MovementType | string
  effectiveDate: string
  lifecycle: MovementLifecycle | string
  reason: string
  note: string | null
  previousAssignment: MovementAssignmentSummary | null
  newAssignment: MovementAssignmentSummary | null
  previousReportingLine: MovementReportingLineSummary | null
  newReportingLine: MovementReportingLineSummary | null
  createdByUserId: string
  actor: MovementActor | null
  createdAtUtc: string
}

export type MovementActor = {
  id: string | null
  displayName: string | null
}

export type PersonnelMovementDetail = PersonnelMovementListItem & {
  cancelledByUserId: string | null
  cancelledBy: MovementActor | null
  cancelledAtUtc: string | null
  cancellationReason: string | null
}

export type CancelMovementRequest = {
  reason: string
}

export type MovementStructure = {
  propertyId: string
  propertyName: string
  departments: DepartmentRecord[]
  positions: PositionRecord[]
}

export async function listHrMovements(query: ListMovementsQuery) {
  return apiRequest<PersonnelMovementListItem[]>(buildMovementsListPath(query))
}

export async function getHrMovement(id: string) {
  return apiRequest<PersonnelMovementDetail>(movementDetailPath(id))
}

export async function createHrMovement(input: CreateMovementRequest) {
  return apiRequest<PersonnelMovementDetail>('/api/hr/movements', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function cancelHrMovement(id: string, input: CancelMovementRequest) {
  return apiRequest<PersonnelMovementDetail>(movementCancelPath(id), {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function getHrMovementStructure(propertyId: string) {
  return apiRequest<MovementStructure>(movementStructurePath(propertyId))
}

export type ManagerCandidate = {
  employeeId: string
  employmentId: string
  personnelNumber: string
  givenName: string
  familyName: string
  departmentId: string | null
  departmentName: string | null
  positionId: string
  positionName: string
  propertyId: string | null
}

export async function listHrManagerCandidates(employmentId: string, effectiveDate: string) {
  return apiRequest<ManagerCandidate[]>(movementManagerCandidatesPath(employmentId, effectiveDate))
}

export function hrMovementErrorKey(error: unknown): string {
  if (error instanceof ApiError) {
    return hrMovementErrorKeyFromCode(error.problem?.code)
  }
  return 'movements.errors.generic'
}
