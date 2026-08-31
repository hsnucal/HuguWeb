import { ApiError, apiRequest } from '../shared/apiClient'

export type LeaveTypeSystemKind =
  | 'Annual'
  | 'Unpaid'
  | 'Sick'
  | 'Marriage'
  | 'Paternity'
  | 'Maternity'
  | 'Bereavement'
  | 'Excuse'
  | 'Administrative'
  | 'Other'

export type LeaveEntitlementSource = 'Entitlement' | 'CarryOver' | 'ManualAdjustment'
export type LeaveRecordStatus = 'Recorded' | 'Cancelled'

export type LeaveTypeRecord = {
  id: string
  code: string
  name: string
  systemKind: LeaveTypeSystemKind | null
  tracksBalance: boolean
  isActive: boolean
  defaultRequestAmount: number | null
}

export type LeaveBalanceRecord = {
  leaveTypeId: string
  code: string
  name: string
  systemKind: LeaveTypeSystemKind | null
  netMovement: number
  used: number
  remaining: number
}

export type LeaveEntitlementRecord = {
  id: string
  leaveTypeId: string
  effectiveDate: string
  amount: number
  source: LeaveEntitlementSource
  note: string | null
  createdAtUtc: string
}

export type LeaveRecordItem = {
  id: string
  leaveTypeId: string
  startDate: string
  endDate: string
  amount: number
  status: LeaveRecordStatus
  note: string | null
  createdAtUtc: string
  cancelledAtUtc: string | null
  cancellationReason: string | null
}

export type EmployeeLeaveOverview = {
  employeeId: string
  employmentId: string
  employmentStartDate: string
  employmentEndDate: string | null
  employmentStatus: string
  leaveTypes: LeaveTypeRecord[]
  balances: LeaveBalanceRecord[]
  entitlements: LeaveEntitlementRecord[]
  records: LeaveRecordItem[]
}

export async function listHrLeaveTypes(activeOnly = false) {
  const query = activeOnly ? '?activeOnly=true' : ''
  return apiRequest<LeaveTypeRecord[]>(`/api/hr/leave-types${query}`)
}

export async function createHrLeaveType(input: {
  code: string
  name: string
  tracksBalance: boolean
  defaultRequestAmount?: number | null
}) {
  return apiRequest<LeaveTypeRecord>('/api/hr/leave-types', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function updateHrLeaveType(
  id: string,
  input: {
    name?: string
    tracksBalance?: boolean
    isActive?: boolean
    defaultRequestAmount?: number | null
  },
) {
  return apiRequest<LeaveTypeRecord>(`/api/hr/leave-types/${id}`, {
    method: 'PATCH',
    body: JSON.stringify(input),
  })
}

export async function getHrEmployeeLeave(employeeId: string) {
  return apiRequest<EmployeeLeaveOverview>(`/api/hr/employees/${employeeId}/leave`)
}

export async function createHrLeaveEntitlement(
  employeeId: string,
  input: {
    employmentId?: string
    leaveTypeId: string
    effectiveDate: string
    amount: number
    source: LeaveEntitlementSource
    note?: string | null
  },
) {
  return apiRequest<EmployeeLeaveOverview>(`/api/hr/employees/${employeeId}/leave-entitlements`, {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function createHrLeaveRecord(
  employeeId: string,
  input: {
    employmentId?: string
    leaveTypeId: string
    startDate: string
    endDate: string
    amount: number
    note?: string | null
  },
) {
  return apiRequest<EmployeeLeaveOverview>(`/api/hr/employees/${employeeId}/leave-records`, {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function cancelHrLeaveRecord(
  employeeId: string,
  recordId: string,
  cancellationReason: string,
) {
  return apiRequest<EmployeeLeaveOverview>(
    `/api/hr/employees/${employeeId}/leave-records/${recordId}/cancel`,
    {
      method: 'POST',
      body: JSON.stringify({ cancellationReason }),
    },
  )
}

export type LeaveRequestStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled'
export type LeaveRequestApprovalStage = 'Department' | 'Hr' | 'Done'
export type LeaveRequestDecisionKind = 'Approved' | 'Rejected' | 'Cancelled'
export type LeaveScheduleDayState = 'Scheduled' | 'RestDay' | 'Unscheduled'

export type LeaveRequestListItem = {
  id: string
  employmentId: string
  employeeId: string
  personnelNumber: string
  displayName: string
  assignmentId: string
  departmentId: string
  departmentName: string
  leaveTypeId: string
  leaveTypeCode: string
  leaveTypeName: string
  startDate: string
  endDate: string
  requestedAmount: number
  finalAmount: number | null
  status: LeaveRequestStatus
  approvalStage: LeaveRequestApprovalStage
  reason: string | null
  createdAtUtc: string
  scheduleIncomplete: boolean
}

export type LeaveSchedulePreviewDay = {
  date: string
  state: LeaveScheduleDayState
  chargeableCandidate: number
}

export type LeaveRequestDecision = {
  id: string
  stage: LeaveRequestApprovalStage
  decision: LeaveRequestDecisionKind
  actorUserId: string
  decisionAtUtc: string
  note: string | null
}

export type LeaveRequestLinkedRecord = {
  id: string
  amount: number
  status: LeaveRecordStatus
  createdAtUtc: string
  cancelledAtUtc: string | null
  cancellationReason: string | null
}

export type LeaveRequestBalanceWarning = {
  leaveTypeId: string
  leaveTypeCode: string
  currentBalance: number
  projectedBalance: number
  isNegativeProjected: boolean
}

export type LeaveRequestDetail = {
  id: string
  employmentId: string
  employeeId: string
  personnelNumber: string
  displayName: string
  assignmentId: string
  departmentId: string
  departmentName: string
  positionId: string | null
  positionName: string | null
  propertyId: string
  leaveTypeId: string
  leaveTypeCode: string
  leaveTypeName: string
  tracksBalance: boolean
  startDate: string
  endDate: string
  requestedAmount: number
  finalAmount: number | null
  suggestedAmount: number
  scheduleIncomplete: boolean
  status: LeaveRequestStatus
  approvalStage: LeaveRequestApprovalStage
  reason: string | null
  createdByUserId: string
  createdAtUtc: string
  updatedAtUtc: string
  scheduleDays: LeaveSchedulePreviewDay[]
  decisions: LeaveRequestDecision[]
  linkedRecord: LeaveRequestLinkedRecord | null
  balance: LeaveRequestBalanceWarning | null
  warnings: string[]
}

export type LeaveRequestPreview = {
  startDate: string
  endDate: string
  suggestedAmount: number
  scheduleIncomplete: boolean
  days: LeaveSchedulePreviewDay[]
  balance: LeaveRequestBalanceWarning | null
  warnings: string[]
}

export type LeaveRequestListPage = {
  items: LeaveRequestListItem[]
  page: number
  pageSize: number
  totalCount: number
}

export type LeaveRequestMutationResult = {
  request: LeaveRequestDetail
  warnings: string[]
}

export type LeaveRequestListQuery = {
  status?: LeaveRequestStatus
  approvalStage?: LeaveRequestApprovalStage
  leaveTypeId?: string
  departmentId?: string
  from?: string
  to?: string
  search?: string
  page?: number
  pageSize?: number
}

function leaveRequestQueryString(query: LeaveRequestListQuery) {
  const params = new URLSearchParams()
  if (query.status) params.set('status', query.status)
  if (query.approvalStage) params.set('approvalStage', query.approvalStage)
  if (query.leaveTypeId) params.set('leaveTypeId', query.leaveTypeId)
  if (query.departmentId) params.set('departmentId', query.departmentId)
  if (query.from) params.set('from', query.from)
  if (query.to) params.set('to', query.to)
  if (query.search) params.set('search', query.search)
  if (query.page) params.set('page', String(query.page))
  if (query.pageSize) params.set('pageSize', String(query.pageSize))
  const text = params.toString()
  return text ? `?${text}` : ''
}

export async function listHrLeaveRequests(query: LeaveRequestListQuery = {}) {
  return apiRequest<LeaveRequestListPage>(`/api/hr/leave-requests${leaveRequestQueryString(query)}`)
}

export async function getHrLeaveRequest(id: string) {
  return apiRequest<LeaveRequestDetail>(`/api/hr/leave-requests/${id}`)
}

export async function departmentApproveLeaveRequest(id: string, note?: string | null) {
  return apiRequest<LeaveRequestMutationResult>(`/api/hr/leave-requests/${id}/department-approve`, {
    method: 'POST',
    body: JSON.stringify({ note: note ?? null }),
  })
}

export async function rejectLeaveRequest(id: string, note?: string | null) {
  return apiRequest<LeaveRequestMutationResult>(`/api/hr/leave-requests/${id}/reject`, {
    method: 'POST',
    body: JSON.stringify({ note: note ?? null }),
  })
}

export async function hrApproveLeaveRequest(id: string, finalAmount: number, note?: string | null) {
  return apiRequest<LeaveRequestMutationResult>(`/api/hr/leave-requests/${id}/approve`, {
    method: 'POST',
    body: JSON.stringify({ finalAmount, note: note ?? null }),
  })
}

export async function cancelApprovedLeaveRequest(id: string, reason: string) {
  return apiRequest<LeaveRequestMutationResult>(`/api/hr/leave-requests/${id}/cancel-approved`, {
    method: 'POST',
    body: JSON.stringify({ reason }),
  })
}

export async function listMyLeaveRequests(page = 1, pageSize = 50) {
  return apiRequest<LeaveRequestListPage>(
    `/api/hr/my/leave-requests?page=${page}&pageSize=${pageSize}`,
  )
}

export type MyLeaveCatalog = {
  leaveTypes: LeaveTypeRecord[]
  balances: LeaveBalanceRecord[]
}

export async function getMyLeaveCatalog() {
  return apiRequest<MyLeaveCatalog>('/api/hr/my/leave')
}

export async function getMyLeaveRequest(id: string) {
  return apiRequest<LeaveRequestDetail>(`/api/hr/my/leave-requests/${id}`)
}

export async function createMyLeaveRequest(input: {
  leaveTypeId: string
  startDate: string
  endDate: string
  requestedAmount: number
  reason?: string | null
}) {
  return apiRequest<LeaveRequestDetail>('/api/hr/my/leave-requests', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function previewMyLeaveRequest(input: {
  leaveTypeId?: string | null
  startDate: string
  endDate: string
  requestedAmount?: number | null
}) {
  return apiRequest<LeaveRequestPreview>('/api/hr/my/leave-requests/preview', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function withdrawMyLeaveRequest(id: string, note?: string | null) {
  return apiRequest<LeaveRequestDetail>(`/api/hr/my/leave-requests/${id}/withdraw`, {
    method: 'POST',
    body: JSON.stringify({ note: note ?? null }),
  })
}

const leaveErrorKeys: Record<string, string> = {
  'leave-type-not-found': 'personnel.leave.errors.typeNotFound',
  'leave-type-inactive': 'personnel.leave.errors.typeInactive',
  'leave-type-code-conflict': 'personnel.leave.errors.codeConflict',
  'leave-type-code-required': 'personnel.leave.errors.codeRequired',
  'leave-type-name-required': 'personnel.leave.errors.nameRequired',
  'leave-type-has-history': 'personnel.leave.errors.hasHistory',
  'leave-entitlement-invalid-amount': 'personnel.leave.errors.entitlementAmount',
  'leave-entitlement-balance-not-supported': 'personnel.leave.errors.balanceNotSupported',
  'leave-entitlement-note-required': 'personnel.leave.errors.noteRequired',
  'leave-date-outside-employment': 'personnel.leave.errors.dateOutsideEmployment',
  'leave-invalid-date-range': 'personnel.leave.errors.invalidDateRange',
  'leave-invalid-amount': 'personnel.leave.errors.invalidAmount',
  'leave-overlap': 'personnel.leave.errors.overlap',
  'leave-record-not-found': 'personnel.leave.errors.recordNotFound',
  'leave-already-cancelled': 'personnel.leave.errors.alreadyCancelled',
  'leave-cancellation-reason-required': 'personnel.leave.errors.reasonRequired',
  'leave-request-type-inactive': 'personnel.leave.errors.typeInactive',
  'leave-request-invalid-amount': 'personnel.leave.errors.invalidAmount',
  'leave-request-invalid-final-amount': 'personnel.leave.errors.invalidAmount',
  'leave-request-date-outside-employment': 'personnel.leave.errors.dateOutsideEmployment',
  'leave-request-overlap': 'personnel.leave.errors.requestOverlap',
  'leave-request-cross-assignment-range': 'personnel.leave.errors.crossAssignment',
  'leave-request-assignment-not-found': 'personnel.leave.errors.assignmentNotFound',
  'leave-request-account-link-required': 'personnel.leave.errors.accountLinkRequired',
  'leave-request-current-employment-not-found': 'personnel.leave.errors.currentEmploymentNotFound',
  'leave-request-not-owned': 'personnel.leave.errors.requestNotFound',
  'leave-request-not-found': 'personnel.leave.errors.requestNotFound',
  'leave-request-not-pending': 'personnel.leave.errors.notPending',
  'leave-request-invalid-approval-stage': 'personnel.leave.errors.invalidStage',
  'leave-request-already-finalized': 'personnel.leave.errors.alreadyFinalized',
  'leave-request-department-access-denied': 'personnel.leave.errors.departmentAccessDenied',
  'leave-request-approval-permission-denied': 'personnel.leave.errors.approvalPermissionDenied',
  'leave-request-record-conflict': 'personnel.leave.errors.recordConflict',
  'employee-not-found': 'personnel.errors.generic',
  'employment-not-found': 'personnel.errors.employmentNotFound',
}

export function hrLeaveErrorKey(error: unknown): string {
  if (error instanceof ApiError && error.problem?.code && leaveErrorKeys[error.problem.code]) {
    return leaveErrorKeys[error.problem.code]
  }

  return 'personnel.leave.errors.generic'
}
