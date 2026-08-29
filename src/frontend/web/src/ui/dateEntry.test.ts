import assert from 'node:assert/strict'
import test from 'node:test'
import {
  constrainDateInput,
  DATE_DIGIT_MAX,
  isoToDisplayDate,
  pastedDateHasOversizedYear,
  toIsoDate,
} from './dateEntry.ts'

test('valid 18.04.2019 maps to ISO and back', () => {
  assert.equal(toIsoDate('18.04.2019'), '2019-04-18')
  assert.equal(isoToDisplayDate('2019-04-18'), '18.04.2019')
  assert.equal(toIsoDate('2019-04-18'), '2019-04-18')
})

test('rejects oversized years such as 18.04.201991', () => {
  assert.equal(toIsoDate('18.04.201991'), null)
  assert.equal(pastedDateHasOversizedYear('18.04.201991'), true)
  assert.equal(constrainDateInput('18.04.201991'), '18.04.2019')
  assert.equal(DATE_DIGIT_MAX, 8)
})

test('rejects impossible calendar dates and accepts leap days', () => {
  assert.equal(toIsoDate('31.02.2020'), null)
  assert.equal(toIsoDate('29.02.2024'), '2024-02-29')
  assert.equal(toIsoDate('29.02.2023'), null)
  assert.equal(toIsoDate('00.12.2020'), null)
  assert.equal(toIsoDate('12.13.2020'), null)
  assert.equal(toIsoDate('18.4.2019'), null)
  assert.equal(toIsoDate('18.04.19'), null)
})

test('typing constraint keeps a four-digit year', () => {
  assert.equal(constrainDateInput('1804201999'), '18.04.2019')
  assert.equal(constrainDateInput('18.04.2019'), '18.04.2019')
})
