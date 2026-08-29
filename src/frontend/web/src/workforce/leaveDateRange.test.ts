import assert from 'node:assert/strict'
import test from 'node:test'
import { amountAfterDateChange } from './leaveAmount.ts'
import { endDateAfterStartChange, endMinDate, isStartOnOrBeforeEnd } from './leaveDateRange.ts'

test('End minDate equals StartDate ISO', () => {
  assert.equal(endMinDate('2026-08-29'), '2026-08-29')
  assert.equal(endMinDate('29.08.2026'), '2026-08-29')
  assert.equal(endMinDate('28.08'), undefined)
})

test('same-day Start/End is allowed', () => {
  assert.equal(isStartOnOrBeforeEnd('2026-08-29', '2026-08-29'), true)
})

test('End after Start is allowed', () => {
  assert.equal(isStartOnOrBeforeEnd('2026-08-29', '2026-08-31'), true)
})

test('End before Start is rejected', () => {
  assert.equal(isStartOnOrBeforeEnd('2026-08-29', '2026-08-28'), false)
  assert.equal(isStartOnOrBeforeEnd('29.08.2026', '28.08.2026'), false)
})

test('moving Start past current End moves End to new Start', () => {
  assert.equal(endDateAfterStartChange('2026-08-30', '2026-08-29'), '2026-08-30')
})

test('moving Start while End is still valid preserves End', () => {
  assert.equal(endDateAfterStartChange('2026-08-30', '2026-08-31'), '2026-08-31')
})

test('manual invalid End cannot be submitted', () => {
  assert.equal(isStartOnOrBeforeEnd('2026-08-29', '2026-08-28'), false)
})

test('snapping End to Start refreshes Amount unless manually overridden', () => {
  const nextEnd = endDateAfterStartChange('2026-08-30', '2026-08-29')
  assert.equal(amountAfterDateChange(false, '2026-08-30', nextEnd, '1'), '1')
  assert.equal(amountAfterDateChange(true, '2026-08-30', nextEnd, '0.5'), '0.5')
})

test('preserving a later End refreshes inclusive Amount unless overridden', () => {
  const nextEnd = endDateAfterStartChange('2026-08-30', '2026-08-31')
  assert.equal(amountAfterDateChange(false, '2026-08-30', nextEnd, '3'), '2')
  assert.equal(amountAfterDateChange(true, '2026-08-30', nextEnd, '0.5'), '0.5')
})
