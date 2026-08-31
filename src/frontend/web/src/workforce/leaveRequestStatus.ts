import type {
  LeaveRequestApprovalStage,
  LeaveRequestDecision,
  LeaveRequestStatus,
  LeaveScheduleDayState,
  LeaveSchedulePreviewDay,
} from './hrLeaveApi'

export type LeaveRequestStatusTone = 'warning' | 'info' | 'success' | 'danger' | 'neutral'

export function leaveScheduleStateLabelKey(state: LeaveScheduleDayState): string {
  if (state === 'Scheduled') {
    return 'personnel.leave.scheduleStateScheduled'
  }
  if (state === 'RestDay') {
    return 'personnel.leave.scheduleStateRestDay'
  }
  return 'personnel.leave.scheduleStateUnscheduled'
}

export function countScheduleStates(days: LeaveSchedulePreviewDay[]) {
  let scheduled = 0
  let restDay = 0
  let unscheduled = 0
  for (const day of days) {
    if (day.state === 'Scheduled') {
      scheduled += 1
    } else if (day.state === 'RestDay') {
      restDay += 1
    } else {
      unscheduled += 1
    }
  }
  return { scheduled, restDay, unscheduled }
}

export function leaveDecisionLabelKey(
  decision: Pick<LeaveRequestDecision, 'stage' | 'decision'>,
): string {
  if (decision.decision === 'Rejected') {
    return 'personnel.leave.decisionRejected'
  }
  if (decision.decision === 'Cancelled') {
    if (decision.stage === 'Department' || decision.stage === 'Hr') {
      return 'personnel.leave.decisionWithdrawn'
    }
    return 'personnel.leave.decisionCancelled'
  }
  if (decision.stage === 'Department') {
    return 'personnel.leave.decisionDepartmentApproved'
  }
  if (decision.stage === 'Hr') {
    return 'personnel.leave.decisionHrApproved'
  }
  return 'personnel.leave.decisionHrApproved'
}

export function leaveRequestStatusLabelKey(
  status: LeaveRequestStatus,
  approvalStage: LeaveRequestApprovalStage,
): string {
  if (status === 'Pending' && approvalStage === 'Department') {
    return 'personnel.leave.requests.status.pendingDepartment'
  }
  if (status === 'Pending' && approvalStage === 'Hr') {
    return 'personnel.leave.requests.status.pendingHr'
  }
  if (status === 'Approved') {
    return 'personnel.leave.requests.status.approved'
  }
  if (status === 'Rejected') {
    return 'personnel.leave.requests.status.rejected'
  }
  return 'personnel.leave.requests.status.cancelled'
}

export function leaveRequestStatusTone(
  status: LeaveRequestStatus,
  approvalStage: LeaveRequestApprovalStage,
): LeaveRequestStatusTone {
  if (status === 'Pending' && approvalStage === 'Department') {
    return 'warning'
  }
  if (status === 'Pending' && approvalStage === 'Hr') {
    return 'info'
  }
  if (status === 'Approved') {
    return 'success'
  }
  if (status === 'Rejected') {
    return 'danger'
  }
  return 'neutral'
}

export function formatLeaveDateRange(startDate: string, endDate: string): string {
  if (startDate === endDate) {
    return startDate
  }
  return `${startDate} – ${endDate}`
}

export function amountAfterPreviewSuggestion(
  amountTouched: boolean,
  suggestedAmount: number | null,
  currentAmount: string,
): string {
  if (amountTouched || suggestedAmount === null || suggestedAmount <= 0) {
    return currentAmount
  }
  return String(suggestedAmount)
}
