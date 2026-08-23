import { ApiError, apiRequest } from '../shared/apiClient'

export type RoomReadiness = 'Dirty' | 'Clean' | 'Inspected' | 'Ready'
export type TaskPriority = 'Normal' | 'High' | 'Urgent'
export type HousekeepingWorkState = 'Open' | 'Completed'
export type HousekeepingWorkOrigin = 'NeedsCleaning' | 'Rework'
export type InspectionResult = 'Accepted' | 'Rejected'
export type NeededAction = 'needs-cleaning' | 'complete-cleaning' | 'inspect' | 'none'
export type RoomTechnicalServiceability = 'Serviceable' | 'OutOfOrder' | 'OutOfService'

export type RoomOperationsListItem = {
  id: string
  number: string
  isActive: boolean
  readiness: RoomReadiness
  readinessCycleId: string
  currentWorkItemId: string | null
  currentWorkState: HousekeepingWorkState | null
  currentWorkOrigin: HousekeepingWorkOrigin | null
  priority: TaskPriority | null
  assignedEmployeeId: string | null
  assignedEmployeeName: string | null
  neededAction: NeededAction
  technicalServiceability: RoomTechnicalServiceability
  hasActiveTechnicalIssue: boolean
}

export type HousekeepingWorkSummary = {
  id: string
  state: HousekeepingWorkState
  origin: HousekeepingWorkOrigin
  priority: TaskPriority
  assignedEmployeeId: string
  assignedEmployeeName: string
  createdAt: string
  completedAt: string | null
  completedByEmployeeId: string | null
  readinessCycleId: string
  sourceInspectionId: string | null
}

export type ReadinessHistoryItem = {
  id: string
  readiness: RoomReadiness
  cause: string
  occurredAt: string
  actorEmployeeId: string | null
  actorEmployeeName: string | null
  workItemId: string | null
  inspectionId: string | null
  comment: string | null
}

export type InspectionHistoryItem = {
  id: string
  result: InspectionResult
  occurredAt: string
  inspectorUserId: string
  reason: string | null
  readinessCycleId: string
  workItemId: string | null
}

export type RoomOperationsDetail = {
  id: string
  number: string
  isActive: boolean
  readiness: RoomReadiness
  readinessCycleId: string
  currentWork: HousekeepingWorkSummary | null
  readinessHistory: ReadinessHistoryItem[]
  inspectionHistory: InspectionHistoryItem[]
  technicalServiceability: RoomTechnicalServiceability
  hasActiveTechnicalIssue: boolean
  governingIssueId: string | null
  activeTechnicalIssueDescription: string | null
}

export type AssignableEmployeeItem = {
  employeeId: string
  givenName: string
  familyName: string
  personnelNumber: string
  displayName: string
}

export function listRooms() {
  return apiRequest<RoomOperationsListItem[]>('/api/room-operations/rooms')
}

export function getRoom(id: string) {
  return apiRequest<RoomOperationsDetail>(`/api/room-operations/rooms/${id}`)
}

export function listAssignableEmployees() {
  return apiRequest<AssignableEmployeeItem[]>('/api/room-operations/assignable-employees')
}

export function requestNeedsCleaning(roomId: string, assignedEmployeeId: string, priority: TaskPriority) {
  return apiRequest<RoomOperationsDetail>(`/api/room-operations/rooms/${roomId}/needs-cleaning`, {
    method: 'POST',
    body: JSON.stringify({ assignedEmployeeId, priority }),
  })
}

export function completeCleaning(workItemId: string) {
  return apiRequest<RoomOperationsDetail>(`/api/room-operations/work-items/${workItemId}/complete-cleaning`, {
    method: 'POST',
  })
}

export function inspectRoom(roomId: string, result: 'accepted' | 'rejected', reason?: string) {
  return apiRequest<RoomOperationsDetail>(`/api/room-operations/rooms/${roomId}/inspections`, {
    method: 'POST',
    body: JSON.stringify({ result, reason }),
  })
}

export function roomOperationsErrorKey(reason: unknown): string {
  if (reason instanceof ApiError) {
    const code = reason.problem?.code
    switch (code) {
      case 'room-not-found':
        return 'roomOperations.errors.roomNotFound'
      case 'employee-not-found':
        return 'roomOperations.errors.employeeNotFound'
      case 'invalid-readiness-transition':
        return 'roomOperations.errors.invalidTransition'
      case 'active-work-already-exists':
        return 'roomOperations.errors.activeWork'
      case 'stale-work-item':
        return 'roomOperations.errors.staleWork'
      case 'work-item-not-current':
        return 'roomOperations.errors.workNotCurrent'
      case 'rejection-reason-required':
        return 'roomOperations.errors.rejectionRequired'
      case 'inspection-not-allowed':
        return 'roomOperations.errors.inspectionNotAllowed'
      case 'assignment-required':
        return 'roomOperations.errors.assignmentRequired'
      default:
        break
    }
  }

  return 'roomOperations.errors.generic'
}
