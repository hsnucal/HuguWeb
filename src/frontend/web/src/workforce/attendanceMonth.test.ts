import assert from 'node:assert/strict'
import test from 'node:test'
import {
  canClearAttendanceCorrection,
  canOpenAttendancePanel,
  canShowAttendanceCorrectionForm,
  currentYearMonth,
  isPastYearMonth,
  isWeekendIsoDate,
  shouldShowPastMonthWarning,
  shiftYearMonth,
  validateAttendanceReason,
  weekdayFromIsoDate,
  yearMonthFromTimeZone,
  resolveAttendanceCorrectionEmploymentId,
} from './attendanceMonth.ts'

test('property timezone determines the default month, not the browser calendar', () => {
  const now = new Date('2026-09-01T01:30:00.000Z')
  const istanbul = yearMonthFromTimeZone('Europe/Istanbul', now)
  const utc = currentYearMonth({ timeZoneId: null, now })
  assert.equal(istanbul.year, 2026)
  assert.equal(istanbul.month, 9)
  assert.equal(utc.year, 2026)
  assert.equal(utc.month, 9)
})

test('month navigation preserves year boundaries', () => {
  assert.deepEqual(shiftYearMonth({ year: 2026, month: 1 }, -1), { year: 2025, month: 12 })
  assert.deepEqual(shiftYearMonth({ year: 2026, month: 12 }, 1), { year: 2027, month: 1 })
})

test('past-month warning appears only for earlier months when the correction form is open', () => {
  const current = { year: 2026, month: 9 }
  assert.equal(isPastYearMonth({ year: 2026, month: 8 }, current), true)
  assert.equal(
    shouldShowPastMonthWarning({
      canManage: true,
      formVisible: true,
      selected: { year: 2026, month: 8 },
      current,
    }),
    true,
  )
  assert.equal(
    shouldShowPastMonthWarning({
      canManage: true,
      formVisible: true,
      selected: { year: 2026, month: 9 },
      current,
    }),
    false,
  )
  assert.equal(
    shouldShowPastMonthWarning({
      canManage: false,
      formVisible: false,
      selected: { year: 2026, month: 8 },
      current,
    }),
    false,
  )
})

test('weekend is calendar information only', () => {
  assert.equal(weekdayFromIsoDate('2026-09-05'), 6)
  assert.equal(isWeekendIsoDate('2026-09-05'), true)
  assert.equal(isWeekendIsoDate('2026-09-03'), false)
})

test('panel opens for in-employment days; NotEmployed stays non-actionable', () => {
  assert.equal(canOpenAttendancePanel('InEmployment'), true)
  assert.equal(canOpenAttendancePanel('NotEmployed'), false)
  assert.equal(canShowAttendanceCorrectionForm(true, 'NotEmployed'), false)
  assert.equal(
    canClearAttendanceCorrection(true, { coverage: 'InEmployment', isManual: true }),
    true,
  )
  assert.equal(
    canClearAttendanceCorrection(false, { coverage: 'InEmployment', isManual: true }),
    false,
  )
})

test('empty correction reason is blocked before save', () => {
  assert.equal(validateAttendanceReason(''), 'required')
  assert.equal(validateAttendanceReason('   '), 'required')
  assert.equal(validateAttendanceReason('No-show'), null)
  assert.equal(validateAttendanceReason('x'.repeat(501)), 'tooLong')
})

test('correction employment id prefers the day value and never uses the employee id', () => {
  assert.equal(
    resolveAttendanceCorrectionEmploymentId(
      { employmentId: 'a1e1c0de-0003-4000-8000-000000000422' },
      { employmentId: 'a1e1c0de-0003-4000-8000-000000000421' },
    ),
    'a1e1c0de-0003-4000-8000-000000000422',
  )
})
