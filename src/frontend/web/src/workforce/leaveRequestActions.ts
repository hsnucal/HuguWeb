import type { LeaveRequestApprovalStage, LeaveRequestListItem, LeaveRequestStatus } from './hrLeaveApi.ts'
import { formatLeaveAmount } from './leaveAmount.ts'

export type LeaveRequestUiActions = {
  canReview: boolean
  canDepartmentApprove: boolean
  canHrApprove: boolean
  canReject: boolean
  canCancelApproved: boolean
  canWithdraw: boolean
}

export function managementActionsForRequest(
  item: Pick<LeaveRequestListItem, 'status' | 'approvalStage'>,
  options: { canApprove: boolean; canManage: boolean },
): LeaveRequestUiActions {
  const pendingDepartment = item.status === 'Pending' && item.approvalStage === 'Department'
  const pendingHr = item.status === 'Pending' && item.approvalStage === 'Hr'
  const approved = item.status === 'Approved'

  return {
    canReview: true,
    canDepartmentApprove: pendingDepartment && options.canApprove,
    canHrApprove: pendingHr && options.canManage,
    canReject: (pendingDepartment && options.canApprove) || (pendingHr && options.canManage),
    canCancelApproved: approved && options.canManage,
    canWithdraw: false,
  }
}

/** Management list shows only Review; workflow actions live in the detail workspace. */
export function managementListShowsInlineWorkflowActions(): boolean {
  return false
}

/** Nested approval/reject confirmations are forbidden; detail is the single workspace. */
export function usesNestedApprovalModal(): boolean {
  return false
}

export function departmentPrimaryActionLabelKey(): string {
  return 'personnel.leave.sendToHr'
}

export function hrPrimaryActionLabelKey(): string {
  return 'personnel.leave.hrApproveAction'
}

export function defaultHrFinalAmount(requestedAmount: number): string {
  return formatLeaveAmount(requestedAmount)
}

export function projectedBalanceAfterFinal(
  currentBalance: number | null | undefined,
  finalAmount: number | null,
): number | null {
  if (currentBalance === null || currentBalance === undefined || finalAmount === null) {
    return null
  }
  return currentBalance - finalAmount
}

export function selfServiceActionsForRequest(
  item: Pick<LeaveRequestListItem, 'status'>,
): LeaveRequestUiActions {
  return {
    canReview: true,
    canDepartmentApprove: false,
    canHrApprove: false,
    canReject: false,
    canCancelApproved: false,
    canWithdraw: item.status === 'Pending',
  }
}

export function tabStatus(tab: 'pending' | 'approved' | 'rejected' | 'cancelled'): LeaveRequestStatus {
  switch (tab) {
    case 'pending':
      return 'Pending'
    case 'approved':
      return 'Approved'
    case 'rejected':
      return 'Rejected'
    case 'cancelled':
      return 'Cancelled'
  }
}

export function stageFilterValue(
  chip: 'all' | 'department' | 'hr',
): LeaveRequestApprovalStage | undefined {
  if (chip === 'department') {
    return 'Department'
  }
  if (chip === 'hr') {
    return 'Hr'
  }
  return undefined
}
