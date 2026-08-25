import { ApiError, apiRequest } from '../shared/apiClient'

export type MaintenancePriority = 'Normal' | 'High' | 'Urgent'
export type MaintenanceIssueStatus = 'Open' | 'InProgress' | 'UnableToResolve' | 'Resolved'
export type OutageClassification = 'OutOfOrder' | 'OutOfService'
export type PreparationImpact = 'None' | 'RequiresPreparation'
export type RoomServiceabilityState = 'Serviceable' | 'OutOfOrder' | 'OutOfService'
export type MaintenanceNeededAction = 'assign' | 'start' | 'resolve' | 'resume' | 'none'
export type MaintenanceHistoryEvent =
  | 'Created'
  | 'Assigned'
  | 'Reassigned'
  | 'PriorityChanged'
  | 'BlockingChanged'
  | 'Started'
  | 'UnableToResolve'
  | 'Resumed'
  | 'Resolved'

export type MaintenanceIssueListItem = {
  id: string
  roomId: string
  roomNumber: string
  description: string
  categoryId: string
  categoryName: string
  priority: MaintenancePriority
  status: MaintenanceIssueStatus
  assignedEmployeeId: string | null
  assignedEmployeeName: string | null
  blocksRoomUse: boolean
  outageClassification: OutageClassification | null
  roomServiceability: RoomServiceabilityState
  createdAt: string
  version: number
  neededAction: MaintenanceNeededAction
}

export type MaintenanceIssueHistoryItem = {
  id: string
  eventType: MaintenanceHistoryEvent
  occurredAt: string
  fromStatus: MaintenanceIssueStatus | null
  toStatus: MaintenanceIssueStatus | null
  fromEmployeeId: string | null
  fromEmployeeName: string | null
  toEmployeeId: string | null
  toEmployeeName: string | null
  fromPriority: MaintenancePriority | null
  toPriority: MaintenancePriority | null
  blocksRoomUse: boolean | null
  outageClassification: OutageClassification | null
  preparationImpact: PreparationImpact | null
  note: string | null
}

export type MaintenanceIssueDetail = {
  id: string
  roomId: string
  roomNumber: string
  description: string
  categoryId: string
  categoryName: string
  priority: MaintenancePriority
  status: MaintenanceIssueStatus
  assignedEmployeeId: string | null
  assignedEmployeeName: string | null
  reportedByEmployeeId: string | null
  reportedByEmployeeName: string | null
  originNote: string | null
  blocksRoomUse: boolean
  outageClassification: OutageClassification | null
  roomServiceability: RoomServiceabilityState
  resolutionNote: string | null
  unableToResolveNote: string | null
  preparationImpact: PreparationImpact | null
  createdAt: string
  startedAt: string | null
  resolvedAt: string | null
  version: number
  neededAction: MaintenanceNeededAction
  history: MaintenanceIssueHistoryItem[]
}

export type AssignableEmployeeItem = {
  employeeId: string
  givenName: string
  familyName: string
  personnelNumber: string
  displayName: string
}

export type MaintenanceRoomItem = {
  roomId: string
  number: string
}

export type MaintenanceCategoryItem = {
  id: string
  name: string
}

export function listIssues() {
  return apiRequest<MaintenanceIssueListItem[]>('/api/maintenance/issues')
}

export function getIssue(id: string) {
  return apiRequest<MaintenanceIssueDetail>(`/api/maintenance/issues/${id}`)
}

export function listRooms() {
  return apiRequest<MaintenanceRoomItem[]>('/api/maintenance/rooms')
}

export function listCategories() {
  return apiRequest<MaintenanceCategoryItem[]>('/api/maintenance/categories')
}

export function listAssignableEmployees() {
  return apiRequest<AssignableEmployeeItem[]>('/api/maintenance/assignable-employees')
}

export function createIssue(body: {
  roomId: string
  categoryId: string
  description: string
  priority: MaintenancePriority
  assignedEmployeeId?: string
  blocksRoomUse: boolean
  outageClassification?: OutageClassification
}) {
  return apiRequest<MaintenanceIssueDetail>('/api/maintenance/issues', {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

export function assignIssue(id: string, assignedEmployeeId: string, expectedVersion: number) {
  return apiRequest<MaintenanceIssueDetail>(`/api/maintenance/issues/${id}/assign`, {
    method: 'POST',
    body: JSON.stringify({ assignedEmployeeId, expectedVersion }),
  })
}

export function changePriority(id: string, priority: MaintenancePriority, expectedVersion: number) {
  return apiRequest<MaintenanceIssueDetail>(`/api/maintenance/issues/${id}/priority`, {
    method: 'POST',
    body: JSON.stringify({ priority, expectedVersion }),
  })
}

export function changeBlocking(
  id: string,
  blocksRoomUse: boolean,
  expectedVersion: number,
  outageClassification?: OutageClassification,
) {
  return apiRequest<MaintenanceIssueDetail>(`/api/maintenance/issues/${id}/blocking`, {
    method: 'POST',
    body: JSON.stringify({ blocksRoomUse, outageClassification, expectedVersion }),
  })
}

export function startWork(id: string, expectedVersion: number) {
  return apiRequest<MaintenanceIssueDetail>(`/api/maintenance/issues/${id}/start`, {
    method: 'POST',
    body: JSON.stringify({ expectedVersion }),
  })
}

export function markUnableToResolve(id: string, note: string, expectedVersion: number) {
  return apiRequest<MaintenanceIssueDetail>(`/api/maintenance/issues/${id}/unable-to-resolve`, {
    method: 'POST',
    body: JSON.stringify({ note, expectedVersion }),
  })
}

export function resumeWork(id: string, expectedVersion: number) {
  return apiRequest<MaintenanceIssueDetail>(`/api/maintenance/issues/${id}/resume`, {
    method: 'POST',
    body: JSON.stringify({ expectedVersion }),
  })
}

export function resolveWork(
  id: string,
  note: string,
  preparationImpact: PreparationImpact,
  expectedVersion: number,
) {
  return apiRequest<MaintenanceIssueDetail>(`/api/maintenance/issues/${id}/resolve`, {
    method: 'POST',
    body: JSON.stringify({ note, preparationImpact, expectedVersion }),
  })
}

export function maintenanceErrorKey(reason: unknown): string {
  if (reason instanceof ApiError) {
    switch (reason.problem?.code) {
      case 'issue-not-found':
        return 'maintenance.errors.issueNotFound'
      case 'room-not-found':
        return 'maintenance.errors.roomNotFound'
      case 'category-not-found':
        return 'maintenance.errors.categoryNotFound'
      case 'employee-not-found':
        return 'maintenance.errors.employeeNotFound'
      case 'invalid-transition':
        return 'maintenance.errors.invalidTransition'
      case 'assignment-required':
        return 'maintenance.errors.assignmentRequired'
      case 'invalid-priority':
        return 'maintenance.errors.invalidPriority'
      case 'invalid-blocking':
        return 'maintenance.errors.invalidBlocking'
      case 'note-required':
        return 'maintenance.errors.noteRequired'
      case 'invalid-preparation-impact':
        return 'maintenance.errors.invalidPreparationImpact'
      case 'stale-issue':
        return 'maintenance.errors.staleIssue'
      case 'room-inactive':
        return 'maintenance.errors.roomInactive'
      case 'preparation-impact-failed':
        return 'maintenance.errors.preparationFailed'
      case 'property-context-required':
        return 'common.propertySelectionRequired'
      default:
        break
    }
  }

  return 'maintenance.errors.generic'
}
