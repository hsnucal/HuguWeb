import assert from 'node:assert/strict'
import test from 'node:test'
import {
  amountAfterDateChange,
  isNonZeroHalfDayAmount,
  isPositiveHalfDayAmount,
  parseLeaveAmount,
  suggestedLeaveAmountDays,
} from './leaveAmount.ts'

test('same date suggests 1 calendar day', () => {
  assert.equal(suggestedLeaveAmountDays('2026-08-04', '2026-08-04'), 1)
})

test('three inclusive calendar dates suggest 3', () => {
  assert.equal(suggestedLeaveAmountDays('2026-08-03', '2026-08-05'), 3)
})

test('end before start is not a suggestion', () => {
  assert.equal(suggestedLeaveAmountDays('2026-08-05', '2026-08-03'), null)
})

test('untouched amount follows inclusive date suggestion', () => {
  assert.equal(amountAfterDateChange(false, '2026-08-29', '2026-08-29', '1'), '1')
  assert.equal(amountAfterDateChange(false, '2026-08-03', '2026-08-05', '1'), '3')
})

test('manual amount is kept after date changes once touched', () => {
  assert.equal(amountAfterDateChange(true, '2026-08-03', '2026-08-05', '0.5'), '0.5')
  assert.equal(amountAfterDateChange(true, '2026-08-29', '2026-08-29', '0.5'), '0.5')
})

test('accepts 0.5 increments and rejects other precision', () => {
  assert.equal(parseLeaveAmount('14.0'), 14)
  assert.equal(parseLeaveAmount('0.5'), 0.5)
  assert.equal(parseLeaveAmount('-1.0'), -1)
  assert.equal(parseLeaveAmount('0.2'), null)
  assert.equal(parseLeaveAmount('1.25'), null)
  assert.equal(isPositiveHalfDayAmount('0.5'), true)
  assert.equal(isPositiveHalfDayAmount('0'), false)
  assert.equal(isNonZeroHalfDayAmount('-1'), true)
  assert.equal(isNonZeroHalfDayAmount('0'), false)
})
