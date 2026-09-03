import assert from 'node:assert/strict'
import test from 'node:test'
import {
  attendanceLeaveCellLabel,
  attendanceLeaveDetailLabel,
  resolveLeaveSystemKind,
} from './leaveDisplay.ts'

const labels = {
  'attendance.leaveShort.Annual': 'Yİ',
  'attendance.leaveShort.Sick': 'Hİ',
  'attendance.leaveFull.Annual': 'Yıllık İzin',
  'attendance.leaveFull.Sick': 'Hastalık İzni',
  'attendance.kindLeave': 'İzin',
} as const

const en = {
  'attendance.leaveShort.Annual': 'AL',
  'attendance.leaveFull.Annual': 'Annual Leave',
  'attendance.kindLeave': 'On leave',
} as const

const ru = {
  'attendance.leaveShort.Annual': 'ОТП',
  'attendance.leaveFull.Annual': 'Ежегодный отпуск',
  'attendance.kindLeave': 'В отпуске',
} as const

function t(table: Record<string, string>) {
  return (key: string) => table[key] ?? key
}

const annual = {
  systemKind: 'Annual',
  leaveTypeCode: 'annual',
  leaveTypeName: 'Yıllık İzin',
}

test('system annual leave uses localized short cell labels, never the internal code', () => {
  assert.equal(resolveLeaveSystemKind(annual), 'Annual')
  assert.equal(attendanceLeaveCellLabel(annual, t(labels)), 'Yİ')
  assert.equal(attendanceLeaveCellLabel(annual, t(en)), 'AL')
  assert.equal(attendanceLeaveCellLabel(annual, t(ru)), 'ОТП')
  for (const label of [
    attendanceLeaveCellLabel(annual, t(labels)),
    attendanceLeaveCellLabel(annual, t(en)),
    attendanceLeaveCellLabel(annual, t(ru)),
  ]) {
    assert.equal(label.toLowerCase().includes('annual'), false)
    assert.equal(/\s/.test(label), false)
  }
})

test('system annual leave detail uses the full localized name without the catalog code', () => {
  assert.equal(attendanceLeaveDetailLabel(annual, t(labels)), 'Yıllık İzin')
  assert.equal(attendanceLeaveDetailLabel(annual, t(en)), 'Annual Leave')
  assert.equal(attendanceLeaveDetailLabel(annual, t(ru)), 'Ежегодный отпуск')
  assert.equal(attendanceLeaveDetailLabel(annual, t(labels)).includes('annual'), false)
  assert.equal(attendanceLeaveDetailLabel(annual, t(en)).toLowerCase().includes('annual leave'), true)
  assert.equal(attendanceLeaveDetailLabel({ ...annual, leaveTypeCode: 'ANNUAL' }, t(en)).includes('ANNUAL'), false)
})

test('known system code still localizes when systemKind is omitted', () => {
  const fromCode = { systemKind: null, leaveTypeCode: 'annual', leaveTypeName: 'Hotel renamed' }
  assert.equal(resolveLeaveSystemKind(fromCode), 'Annual')
  assert.equal(attendanceLeaveCellLabel(fromCode, t(labels)), 'Yİ')
  assert.equal(attendanceLeaveDetailLabel(fromCode, t(labels)), 'Yıllık İzin')
})

test('custom tenant leave types keep the configured display name and are not translated', () => {
  const custom = {
    systemKind: null,
    leaveTypeCode: 'birthday',
    leaveTypeName: 'Doğum Günü İzni',
  }
  assert.equal(resolveLeaveSystemKind(custom), null)
  assert.equal(attendanceLeaveCellLabel(custom, t(labels)), 'Doğum Günü İzni')
  assert.equal(attendanceLeaveDetailLabel(custom, t(en)), 'Doğum Günü İzni')
  assert.equal(attendanceLeaveCellLabel(custom, t(en)).includes('AL'), false)
})

test('custom leave without a name can keep the configured code', () => {
  const custom = { systemKind: null, leaveTypeCode: 'study-day', leaveTypeName: null }
  assert.equal(attendanceLeaveCellLabel(custom, t(labels)), 'study-day')
  assert.equal(attendanceLeaveDetailLabel(custom, t(labels)), 'study-day')
})
