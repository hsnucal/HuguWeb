import assert from 'node:assert/strict'
import test from 'node:test'
import {
  formatTurkishMobile,
  normalizeTurkishMobileInput,
} from './personnelInput.ts'

test('formats 10 raw digits with a presentation zero', () => {
  assert.equal(formatTurkishMobile('5555555555'), '0(555) 555 55 55')
})

test('strips a trunk zero from 11 digits', () => {
  assert.equal(normalizeTurkishMobileInput('05555555555'), '5555555555')
})

test('strips +90 country code', () => {
  assert.equal(normalizeTurkishMobileInput('+90 555 555 55 55'), '5555555555')
})

test('does not keep the presentation zero in raw state', () => {
  assert.equal(normalizeTurkishMobileInput('0(555) 555 55 55'), '5555555555')
  assert.equal(normalizeTurkishMobileInput('0(5__) ___ __ __'), '5')
  assert.equal(formatTurkishMobile(normalizeTurkishMobileInput('0(5__) ___ __ __')), '0(5__) ___ __ __')
})

test('backspacing formatted input stays stable', () => {
  const raw = normalizeTurkishMobileInput('0(555) 555 55 55')
  const after = normalizeTurkishMobileInput('0(555) 555 55 5_')
  assert.equal(raw, '5555555555')
  assert.equal(after, '555555555')
  assert.equal(formatTurkishMobile(after), '0(555) 555 55 5_')
})

test('ignores extra digits beyond 10', () => {
  assert.equal(normalizeTurkishMobileInput('5555555555999'), '5555555555')
})
