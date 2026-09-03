import assert from 'node:assert/strict'
import test from 'node:test'
import {
  ATTENDANCE_CORRECTION_KINDS,
  attendanceCorrectionBody,
  attendanceCorrectionPath,
  attendanceHistoryPath,
  buildAttendanceMonthPath,
  hrAttendanceErrorKeyFromCode,
  hrAttendanceErrorMessage,
  isAttendanceCorrectionKind,
} from './attendancePaths.ts'

test('monthly query sends year, month, departmentId and search without property or organization ids', () => {
  const path = buildAttendanceMonthPath({
    year: 2026,
    month: 9,
    departmentId: 'dep-1',
    search: 'Zeynep',
  })
  assert.equal(path.startsWith('/api/hr/attendance/monthly?'), true)
  assert.match(path, /year=2026/)
  assert.match(path, /month=9/)
  assert.match(path, /departmentId=dep-1/)
  assert.match(path, /search=Zeynep/)
  assert.doesNotMatch(path, /OrganizationId/i)
  assert.doesNotMatch(path, /PropertyId/i)
  assert.doesNotMatch(path, /organizationId/)
  assert.doesNotMatch(path, /propertyId/)
})

test('blank search and department are omitted from the monthly query', () => {
  const path = buildAttendanceMonthPath({
    year: 2026,
    month: 9,
    departmentId: '',
    search: '  ',
  })
  assert.equal(path, '/api/hr/attendance/monthly?year=2026&month=9')
})

test('correction and history paths use employment id and ISO date', () => {
  assert.equal(
    attendanceCorrectionPath('a1e1c0de-0003-4000-8000-000000000422', '2026-09-03'),
    '/api/hr/attendance/a1e1c0de-0003-4000-8000-000000000422/2026-09-03/correction',
  )
  assert.equal(
    attendanceHistoryPath('a1e1c0de-0003-4000-8000-000000000422', '2026-09-03'),
    '/api/hr/attendance/a1e1c0de-0003-4000-8000-000000000422/2026-09-03/history',
  )
})

test('stable attendance problem codes map to localized keys', () => {
  assert.equal(
    hrAttendanceErrorKeyFromCode('attendance-correction-reason-required'),
    'attendance.errors.reasonRequired',
  )
  assert.equal(hrAttendanceErrorKeyFromCode('attendance-invalid-month'), 'attendance.errors.invalidMonth')
  assert.equal(
    hrAttendanceErrorKeyFromCode('attendance-department-filter-denied'),
    'attendance.errors.departmentFilterDenied',
  )
  assert.equal(hrAttendanceErrorKeyFromCode('unknown'), 'attendance.errors.generic')
})

test('set correction payload uses named kind, not a numeric enum', () => {
  const body = attendanceCorrectionBody('Absent', 'No-show')
  const json = JSON.stringify(body)
  assert.deepEqual(body, { kind: 'Absent', reason: 'No-show' })
  assert.doesNotMatch(json, /"kind":\s*4/)
  assert.equal(isAttendanceCorrectionKind('Absent'), true)
  assert.equal(isAttendanceCorrectionKind('Unresolved'), false)
  assert.deepEqual([...ATTENDANCE_CORRECTION_KINDS], ['Worked', 'Leave', 'RestDay', 'Absent'])
})

test('correction errors prefer localized Problem Details over a generic key', () => {
  const error = {
    message: 'fallback',
    problem: {
      detail: 'İstenen puantaj tarihini kapsayan birincil bir atama yok.',
      code: 'attendance-assignment-not-found',
    },
  }
  assert.equal(
    hrAttendanceErrorMessage(error, () => 'hidden generic'),
    'İstenen puantaj tarihini kapsayan birincil bir atama yok.',
  )
})

test('month change and search stay on attendance monthly query', () => {
  const august = buildAttendanceMonthPath({ year: 2026, month: 8 })
  const september = buildAttendanceMonthPath({ year: 2026, month: 9, search: 'Demir' })
  assert.match(august, /year=2026/)
  assert.match(august, /month=8/)
  assert.match(september, /month=9/)
  assert.match(september, /search=Demir/)
  assert.doesNotMatch(august, /organizationId|propertyId/i)
})
