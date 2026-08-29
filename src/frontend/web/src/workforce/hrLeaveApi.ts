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

export async function createHrLeaveType(input: { code: string; name: string; tracksBalance: boolean }) {
  return apiRequest<LeaveTypeRecord>('/api/hr/leave-types', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function updateHrLeaveType(
  id: string,
  input: { name?: string; tracksBalance?: boolean; isActive?: boolean },
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
  'employee-not-found': 'personnel.errors.generic',
  'employment-not-found': 'personnel.errors.employmentNotFound',
}

export function hrLeaveErrorKey(error: unknown): string {
  if (error instanceof ApiError && error.problem?.code && leaveErrorKeys[error.problem.code]) {
    return leaveErrorKeys[error.problem.code]
  }

  return 'personnel.leave.errors.generic'
}
