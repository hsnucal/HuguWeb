import assert from 'node:assert/strict'
import test from 'node:test'
import {
  defaultHrFinalAmount,
  departmentPrimaryActionLabelKey,
  hrPrimaryActionLabelKey,
  managementActionsForRequest,
  managementListShowsInlineWorkflowActions,
  projectedBalanceAfterFinal,
  selfServiceActionsForRequest,
  stageFilterValue,
  tabStatus,
  usesNestedApprovalModal,
} from './leaveRequestActions.ts'
import {
  amountAfterPreviewSuggestion,
  countScheduleStates,
  formatLeaveDateRange,
  leaveDecisionLabelKey,
  leaveRequestStatusLabelKey,
  leaveRequestStatusTone,
  leaveScheduleStateLabelKey,
} from './leaveRequestStatus.ts'
import { isPositiveHalfDayAmount } from './leaveAmount.ts'
import {
  canApproveHrLeave,
  canManageHrLeave,
  canReadHrLeave,
  canRequestHrLeave,
} from './hrAccess.ts'
import type { CurrentUser } from '../shared/types.ts'

function userWith(...permissions: string[]): CurrentUser {
  return {
    id: 'u1',
    email: 'a@b.c',
    preferredLanguage: 'en',
    permissions,
  }
}

test('leave request status labels distinguish Pending Department and Pending Hr', () => {
  assert.equal(
    leaveRequestStatusLabelKey('Pending', 'Department'),
    'personnel.leave.requests.status.pendingDepartment',
  )
  assert.equal(leaveRequestStatusLabelKey('Pending', 'Hr'), 'personnel.leave.requests.status.pendingHr')
  assert.equal(leaveRequestStatusLabelKey('Approved', 'Done'), 'personnel.leave.requests.status.approved')
  assert.equal(leaveRequestStatusLabelKey('Rejected', 'Done'), 'personnel.leave.requests.status.rejected')
  assert.equal(leaveRequestStatusLabelKey('Cancelled', 'Done'), 'personnel.leave.requests.status.cancelled')
})

test('leave request status tones are not color-only enums', () => {
  assert.equal(leaveRequestStatusTone('Pending', 'Department'), 'warning')
  assert.equal(leaveRequestStatusTone('Pending', 'Hr'), 'info')
  assert.equal(leaveRequestStatusTone('Approved', 'Done'), 'success')
  assert.equal(leaveRequestStatusTone('Rejected', 'Done'), 'danger')
  assert.equal(leaveRequestStatusTone('Cancelled', 'Done'), 'neutral')
})

test('management list keeps workflow actions out of the row', () => {
  assert.equal(managementListShowsInlineWorkflowActions(), false)
  assert.equal(usesNestedApprovalModal(), false)
  assert.equal(departmentPrimaryActionLabelKey(), 'personnel.leave.sendToHr')
  assert.equal(hrPrimaryActionLabelKey(), 'personnel.leave.hrApproveAction')
  assert.equal(defaultHrFinalAmount(1), '1')
  assert.equal(defaultHrFinalAmount(1.5), '1.5')
  assert.equal(projectedBalanceAfterFinal(0, 1), -1)
  assert.equal(projectedBalanceAfterFinal(null, 1), null)
})

test('management actions respect approve vs manage permissions', () => {
  const departmentPending = { status: 'Pending' as const, approvalStage: 'Department' as const }
  const hrPending = { status: 'Pending' as const, approvalStage: 'Hr' as const }
  const approved = { status: 'Approved' as const, approvalStage: 'Done' as const }
  const rejected = { status: 'Rejected' as const, approvalStage: 'Done' as const }

  const deptApprover = managementActionsForRequest(departmentPending, { canApprove: true, canManage: false })
  assert.equal(deptApprover.canDepartmentApprove, true)
  assert.equal(deptApprover.canReject, true)
  assert.equal(deptApprover.canHrApprove, false)
  assert.equal(deptApprover.canCancelApproved, false)

  const readOnly = managementActionsForRequest(departmentPending, { canApprove: false, canManage: false })
  assert.equal(readOnly.canDepartmentApprove, false)
  assert.equal(readOnly.canReject, false)
  assert.equal(readOnly.canReview, true)

  const hrManager = managementActionsForRequest(hrPending, { canApprove: true, canManage: true })
  assert.equal(hrManager.canHrApprove, true)
  assert.equal(hrManager.canReject, true)
  assert.equal(hrManager.canDepartmentApprove, false)

  const unauthorizedHr = managementActionsForRequest(hrPending, { canApprove: true, canManage: false })
  assert.equal(unauthorizedHr.canHrApprove, false)
  assert.equal(unauthorizedHr.canReject, false)

  const cancel = managementActionsForRequest(approved, { canApprove: false, canManage: true })
  assert.equal(cancel.canCancelApproved, true)

  const rejectedActions = managementActionsForRequest(rejected, { canApprove: true, canManage: true })
  assert.equal(rejectedActions.canReject, false)
  assert.equal(rejectedActions.canCancelApproved, false)
  assert.equal(rejectedActions.canReview, true)
})

test('self-service withdraw only for Pending and never cancel-approved', () => {
  assert.equal(selfServiceActionsForRequest({ status: 'Pending' }).canWithdraw, true)
  assert.equal(selfServiceActionsForRequest({ status: 'Approved' }).canWithdraw, false)
  assert.equal(selfServiceActionsForRequest({ status: 'Approved' }).canCancelApproved, false)
})

test('schedule preview distinguishes RestDay and Unscheduled', () => {
  assert.equal(leaveScheduleStateLabelKey('RestDay'), 'personnel.leave.scheduleStateRestDay')
  assert.equal(leaveScheduleStateLabelKey('Unscheduled'), 'personnel.leave.scheduleStateUnscheduled')
  assert.notEqual(leaveScheduleStateLabelKey('RestDay'), leaveScheduleStateLabelKey('Unscheduled'))

  const counts = countScheduleStates([
    { date: '2026-09-09', state: 'Scheduled', chargeableCandidate: 1 },
    { date: '2026-09-10', state: 'RestDay', chargeableCandidate: 0 },
    { date: '2026-09-11', state: 'Unscheduled', chargeableCandidate: 0 },
  ])
  assert.deepEqual(counts, { scheduled: 1, restDay: 1, unscheduled: 1 })
})

test('FinalAmount and RequestedAmount use 0.5 quantum', () => {
  assert.equal(isPositiveHalfDayAmount('0.5'), true)
  assert.equal(isPositiveHalfDayAmount('1'), true)
  assert.equal(isPositiveHalfDayAmount('1.5'), true)
  assert.equal(isPositiveHalfDayAmount('1.25'), false)
  assert.equal(isPositiveHalfDayAmount('0'), false)
})

test('SuggestedAmount does not overwrite user-edited RequestedAmount', () => {
  assert.equal(amountAfterPreviewSuggestion(false, 2, ''), '2')
  assert.equal(amountAfterPreviewSuggestion(true, 2, '1.5'), '1.5')
  assert.equal(amountAfterPreviewSuggestion(false, 0, '1'), '1')
})

test('date range collapses same-day requests', () => {
  assert.equal(formatLeaveDateRange('2026-09-09', '2026-09-09'), '2026-09-09')
  assert.equal(formatLeaveDateRange('2026-09-09', '2026-09-11'), '2026-09-09 – 2026-09-11')
})

test('decision labels avoid exposing actor GUID', () => {
  assert.equal(
    leaveDecisionLabelKey({ stage: 'Department', decision: 'Approved' }),
    'personnel.leave.decisionDepartmentApproved',
  )
  assert.equal(leaveDecisionLabelKey({ stage: 'Hr', decision: 'Approved' }), 'personnel.leave.decisionHrApproved')
  assert.equal(leaveDecisionLabelKey({ stage: 'Department', decision: 'Cancelled' }), 'personnel.leave.decisionWithdrawn')
  assert.equal(leaveDecisionLabelKey({ stage: 'Done', decision: 'Cancelled' }), 'personnel.leave.decisionCancelled')
})

test('tab and stage filters map to backend query values', () => {
  assert.equal(tabStatus('pending'), 'Pending')
  assert.equal(tabStatus('approved'), 'Approved')
  assert.equal(stageFilterValue('all'), undefined)
  assert.equal(stageFilterValue('department'), 'Department')
  assert.equal(stageFilterValue('hr'), 'Hr')
})

test('hr leave access helpers for management and self-service', () => {
  assert.equal(canReadHrLeave(userWith('hr.leave.approve')), true)
  assert.equal(canApproveHrLeave(userWith('hr.leave.approve')), true)
  assert.equal(canApproveHrLeave(userWith('hr.leave.manage')), true)
  assert.equal(canManageHrLeave(userWith('hr.leave.approve')), false)
  assert.equal(canRequestHrLeave(userWith('hr.leave.request')), true)
  assert.equal(canRequestHrLeave(userWith('hr.leave.read')), false)
})
