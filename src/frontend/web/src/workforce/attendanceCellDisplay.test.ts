import assert from 'node:assert/strict'
import test from 'node:test'
import {
  attendanceCellTooltipText,
  attendanceCellVisible,
  attendanceProvenanceStatus,
  formatAttendanceClockRange,
  reverseChronological,
} from './attendanceCellDisplay.ts'
import type { AttendanceDayResult } from './hrAttendanceApi.ts'

const labels = {
  restDay: 'OFF',
  absent: 'DEV',
  unresolved: '—',
  worked: 'Worked',
  leave: 'Leave',
  notEmployed: '',
  notEmployedTooltip: 'Employment is not active on this date',
  unresolvedTooltip: 'Attendance result was not created',
  outOfScopeTooltip: 'Out of scope',
  leaveFallback: 'Leave',
}

function day(overrides: Partial<AttendanceDayResult>): AttendanceDayResult {
  return {
    localDate: '2026-09-03',
    coverage: 'InEmployment',
    acceptedKind: 'Unresolved',
    source: null,
    isProvisional: false,
    isManual: false,
    isUnresolved: true,
    correctionReason: null,
    employmentId: 'emp-1',
    assignmentId: 'asg-1',
    departmentId: 'dep-1',
    departmentName: 'Front Office',
    schedule: null,
    leave: null,
    plannedMinutes: null,
    acceptedWorkedMinutes: null,
    ...overrides,
  }
}

test('scheduled provisional Worked shows planned time, not shift code', () => {
  const result = attendanceCellVisible(
    day({
      acceptedKind: 'Worked',
      source: 'Schedule',
      isProvisional: true,
      isUnresolved: false,
      schedule: {
        state: 'Shift',
        scheduleEntryId: 's1',
        shiftDefinitionId: 'd1',
        shiftCode: 'vrd200',
        shiftName: 'DAY',
        startLocalTime: '08:00:00',
        endLocalTime: '17:00:00',
        endsNextDay: false,
      },
    }),
    labels,
  )
  assert.equal(result.primary, '08:00–17:00')
  assert.equal(result.tone, 'worked')
  assert.equal(result.interactive, true)
  assert.equal(result.primary.includes('vrd200'), false)
  assert.equal(attendanceProvenanceStatus(day({ source: 'Schedule', isProvisional: true })), 'fromPlan')
})

test('RestDay displays OFF whether derived or manual', () => {
  const derived = attendanceCellVisible(
    day({ acceptedKind: 'RestDay', source: 'Schedule', isUnresolved: false }),
    labels,
  )
  const manual = attendanceCellVisible(
    day({ acceptedKind: 'RestDay', source: 'Manual', isManual: true, isUnresolved: false }),
    labels,
  )
  assert.equal(derived.primary, 'OFF')
  assert.equal(manual.primary, 'OFF')
  assert.equal(manual.isManual, true)
})

test('Leave displays type code and tooltip uses full name', () => {
  const leaveDay = day({
    acceptedKind: 'Leave',
    source: 'Leave',
    isUnresolved: false,
    leave: {
      leaveRecordId: 'lr1',
      leaveTypeId: 'lt1',
      leaveTypeCode: 'Yİ',
      leaveTypeName: 'Yıllık İzin',
      startDate: '2026-09-01',
      endDate: '2026-09-03',
      amount: 3,
    },
  })
  const visible = attendanceCellVisible(leaveDay, labels)
  assert.equal(visible.primary, 'Yİ')
  assert.equal(attendanceCellTooltipText(leaveDay, labels), 'Yıllık İzin')
})

test('Absent displays DEV', () => {
  const visible = attendanceCellVisible(
    day({ acceptedKind: 'Absent', source: 'Manual', isManual: true, isUnresolved: false }),
    labels,
  )
  assert.equal(visible.primary, 'DEV')
  assert.equal(visible.tone, 'absent')
})

test('Unresolved displays em dash and is still inspectable', () => {
  const unresolved = day({ acceptedKind: 'Unresolved', isUnresolved: true })
  const visible = attendanceCellVisible(unresolved, labels)
  assert.equal(visible.primary, '—')
  assert.equal(visible.tone, 'unresolved')
  assert.equal(visible.interactive, true)
  assert.equal(attendanceCellTooltipText(unresolved, labels), labels.unresolvedTooltip)
})

test('NotEmployed is muted and not shown as dash, DEV, or OFF', () => {
  const notEmployed = day({
    coverage: 'NotEmployed',
    acceptedKind: null,
    isUnresolved: false,
    employmentId: null,
  })
  const visible = attendanceCellVisible(notEmployed, labels)
  assert.equal(visible.primary, '')
  assert.equal(visible.tone, 'notEmployed')
  assert.equal(visible.interactive, false)
  assert.notEqual(visible.primary, '—')
  assert.notEqual(visible.primary, 'DEV')
  assert.notEqual(visible.primary, 'OFF')
  assert.equal(attendanceCellTooltipText(notEmployed, labels), labels.notEmployedTooltip)
})

test('weekend calendar date does not imply RestDay', () => {
  const saturdayWorked = day({
    localDate: '2026-09-05',
    acceptedKind: 'Worked',
    source: 'Schedule',
    isProvisional: true,
    isUnresolved: false,
    schedule: {
      state: 'Shift',
      scheduleEntryId: 's1',
      shiftDefinitionId: 'd1',
      shiftCode: 'vrd200',
      shiftName: 'DAY',
      startLocalTime: '08:00:00',
      endLocalTime: '17:00:00',
      endsNextDay: false,
    },
  })
  const saturdayUnresolved = day({
    localDate: '2026-09-05',
    acceptedKind: 'Unresolved',
    isUnresolved: true,
  })
  assert.equal(attendanceCellVisible(saturdayWorked, labels).primary, '08:00–17:00')
  assert.equal(attendanceCellVisible(saturdayUnresolved, labels).primary, '—')
  assert.notEqual(attendanceCellVisible(saturdayWorked, labels).primary, 'OFF')
})

test('formatAttendanceClockRange stays compact for dense cells', () => {
  assert.equal(formatAttendanceClockRange('08:00:00', '17:00:00'), '08:00–17:00')
})

test('history is shown newest first', () => {
  const ordered = reverseChronological([
    { id: 'a', changedAtUtc: '2026-09-03T08:00:00Z' },
    { id: 'b', changedAtUtc: '2026-09-03T10:00:00Z' },
  ])
  assert.deepEqual(
    ordered.map((item) => item.id),
    ['b', 'a'],
  )
})
