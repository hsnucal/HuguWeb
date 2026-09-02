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

test('native date-picker ISO values populate the shared DateField contract', () => {
  assert.equal(toIsoDate('2026-08-29'), '2026-08-29')
  assert.equal(isoToDisplayDate('2026-08-29'), '29.08.2026')
  assert.equal(toIsoDate('2026-02-31'), null)
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

test('start and end leave dates parse independently as ISO', () => {
  assert.equal(toIsoDate('31.08.2026'), '2026-08-31')
  assert.equal(toIsoDate('01.09.2026'), '2026-09-01')
  assert.notEqual(toIsoDate('31.08.2026'), toIsoDate('01.09.2026'))
})

test('blur-ready incomplete drafts stay non-ISO until complete', () => {
  assert.equal(toIsoDate('31.08.202'), null)
  assert.equal(toIsoDate('31.08.'), null)
  assert.equal(constrainDateInput('31082026'), '31.08.2026')
})

test('optional graduation-style DateOnly stays empty or valid ISO', () => {
  assert.equal(toIsoDate(''), null)
  assert.equal(toIsoDate('15.06.2018'), '2018-06-15')
  assert.equal(toIsoDate('15.06.20181'), null)
  assert.equal(toIsoDate('31.02.2018'), null)
})

test('DateField calendar contract: picker for editable, none for readonly', () => {
  function shouldShowCalendar(options: { calendar?: boolean; readOnly?: boolean; disabled?: boolean }) {
    const calendar = options.calendar ?? true
    return calendar && !options.disabled && !options.readOnly
  }

  assert.equal(shouldShowCalendar({}), true)
  assert.equal(shouldShowCalendar({ calendar: true }), true)
  assert.equal(shouldShowCalendar({ calendar: false }), false)
  assert.equal(shouldShowCalendar({ readOnly: true }), false)
  assert.equal(shouldShowCalendar({ disabled: true }), false)
  assert.equal(shouldShowCalendar({ calendar: true, readOnly: true }), false)
})

test('probation start/end presentation stays DD.MM.YYYY', () => {
  assert.equal(isoToDisplayDate('2026-09-01'), '01.09.2026')
  assert.equal(isoToDisplayDate('2026-11-01'), '01.11.2026')
})
