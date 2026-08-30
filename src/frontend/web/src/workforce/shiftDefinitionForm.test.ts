import assert from 'node:assert/strict'
import test from 'node:test'
import {
  compareShiftDefinitionsByStart,
  formatShiftClockRange,
  formatTimeForInput,
  isOvernightInconsistent,
  parseTimeInput,
  splitNetDuration,
} from './shiftDefinitionForm.ts'

test('formatTimeForInput strips seconds for time inputs and keeps midnight', () => {
  assert.equal(formatTimeForInput('08:00:00'), '08:00')
  assert.equal(formatTimeForInput('16:30'), '16:30')
  assert.equal(formatTimeForInput('00:00:00'), '00:00')
  assert.equal(formatTimeForInput('00:00:00.0000000'), '00:00')
  assert.equal(formatTimeForInput(''), '')
  assert.equal(formatTimeForInput('not-a-time'), '')
})

test('formatTimeForInput never invents 23:59 for midnight', () => {
  assert.notEqual(formatTimeForInput('00:00:00'), '23:59')
  assert.equal(formatShiftClockRange('16:00:00', '00:00:00'), '16:00 – 00:00')
  assert.ok(!formatShiftClockRange('16:00:00', '00:00:00').includes('23:59'))
})

test('formatShiftClockRange displays persisted times for normal and overnight shifts', () => {
  assert.equal(formatShiftClockRange('08:00:00', '16:00:00'), '08:00 – 16:00')
  assert.equal(formatShiftClockRange('23:00:00', '07:00:00'), '23:00 – 07:00')
  assert.equal(formatShiftClockRange('16:00', '00:00'), '16:00 – 00:00')
})

test('parseTimeInput normalizes to HH:mm:ss for API payloads including midnight', () => {
  assert.equal(parseTimeInput('8:00'), '08:00:00')
  assert.equal(parseTimeInput('08:00'), '08:00:00')
  assert.equal(parseTimeInput('00:00'), '00:00:00')
  assert.equal(parseTimeInput('08:00:30'), '08:00:30')
  assert.equal(parseTimeInput('24:00'), null)
  assert.equal(parseTimeInput('08:60'), null)
  assert.equal(parseTimeInput(''), null)
})

test('overnight inconsistency requires EndsNextDay when end <= start', () => {
  assert.equal(isOvernightInconsistent('22:00', '06:00', false), true)
  assert.equal(isOvernightInconsistent('22:00', '06:00', true), false)
  assert.equal(isOvernightInconsistent('08:00', '08:00', false), true)
  assert.equal(isOvernightInconsistent('08:00', '16:00', false), false)
  assert.equal(isOvernightInconsistent('16:00', '00:00', true), false)
  assert.equal(isOvernightInconsistent('16:00', '00:00', false), true)
  assert.equal(isOvernightInconsistent('08:00:00', '16:00:00', false), false)
})

test('splitNetDuration formats API planned minutes as hours and minutes', () => {
  assert.deepEqual(splitNetDuration(480), { hours: 8, minutes: 0 })
  assert.deepEqual(splitNetDuration(450), { hours: 7, minutes: 30 })
  assert.deepEqual(splitNetDuration(419), { hours: 6, minutes: 59 })
  assert.deepEqual(splitNetDuration(0), { hours: 0, minutes: 0 })
})

test('compareShiftDefinitionsByStart is chronological by start time', () => {
  const rows = [
    { id: '3', code: 'vrd300', startLocalTime: '23:00:00' },
    { id: '1', code: 'vrd100', startLocalTime: '08:00:00' },
    { id: '2', code: 'vrd200', startLocalTime: '16:00:00' },
  ]
  const sorted = [...rows].sort(compareShiftDefinitionsByStart)
  assert.deepEqual(
    sorted.map((row) => row.code),
    ['vrd100', 'vrd200', 'vrd300'],
  )
})
