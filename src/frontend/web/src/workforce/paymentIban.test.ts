import assert from 'node:assert/strict'
import test from 'node:test'
import {
  applyTurkishIbanBodyEdit,
  formatTurkishIban,
  formatTurkishIbanBody,
  isCompleteTurkishIban,
  isEmptyTurkishIban,
  isIncompleteTurkishIban,
  normalizeTurkishIbanDigits,
  toCanonicalTurkishIban,
  toPersistedIban,
  turkishIbanBody,
  validatePaymentIban,
  TR_IBAN_MAX_LENGTH,
  TR_IBAN_PREFIX,
} from './paymentIban.ts'

const FULL_DIGITS = '330006100519786457841326'
const FULL_CANONICAL = 'TR330006100519786457841326'
const FULL_DISPLAY = 'TR33 0006 1005 1978 6457 8413 26'
const FULL_BODY_DISPLAY = '33 0006 1005 1978 6457 8413 26'

test('EMPTY: empty logical value and TR-only serialize empty, not TR', () => {
  assert.equal(isEmptyTurkishIban(''), true)
  assert.equal(isEmptyTurkishIban('TR'), true)
  assert.equal(isEmptyTurkishIban('tr'), true)
  assert.equal(isEmptyTurkishIban('  TR  '), true)
  assert.equal(toPersistedIban(''), '')
  assert.equal(toPersistedIban('TR'), '')
  assert.equal(toPersistedIban('  tr  '), '')
  assert.equal(toCanonicalTurkishIban(''), '')
  assert.equal(normalizeTurkishIbanDigits('TR'), '')
  assert.equal(formatTurkishIban(''), TR_IBAN_PREFIX)
  assert.equal(formatTurkishIbanBody(''), '')
})

test('INPUT: digits accepted; letters and punctuation rejected from payload', () => {
  assert.equal(normalizeTurkishIbanDigits('33ASD123'), '33123')
  assert.equal(applyTurkishIbanBodyEdit('33ASD123'), 'TR33123')
  assert.equal(normalizeTurkishIbanDigits('33-00 06!'), '330006')
  assert.equal(applyTurkishIbanBodyEdit('33-00 06!'), 'TR330006')
  assert.equal(normalizeTurkishIbanDigits('abc'), '')
})

test('INPUT: max 24 digits after TR', () => {
  const twentyFive = FULL_DIGITS + '9'
  assert.equal(normalizeTurkishIbanDigits(twentyFive), FULL_DIGITS)
  assert.equal(applyTurkishIbanBodyEdit(twentyFive), FULL_CANONICAL)
  assert.equal(toCanonicalTurkishIban(TR_IBAN_PREFIX + twentyFive).length, TR_IBAN_MAX_LENGTH)
})

test('FORMAT: Turkish grouping while typing', () => {
  assert.equal(formatTurkishIban('3'), 'TR3')
  assert.equal(formatTurkishIban('33'), 'TR33')
  assert.equal(formatTurkishIban('330006'), 'TR33 0006')
  assert.equal(formatTurkishIban('3300061005'), 'TR33 0006 1005')
  assert.equal(formatTurkishIban(FULL_DIGITS), FULL_DISPLAY)
  assert.equal(formatTurkishIbanBody(FULL_DIGITS), FULL_BODY_DISPLAY)
  assert.equal(formatTurkishIbanBody('33'), '33')
  assert.equal(formatTurkishIbanBody('330006'), '33 0006')
})

test('PASTE: canonical, spaced, lowercase, no TRTR, invalid chars stripped', () => {
  assert.equal(toCanonicalTurkishIban(FULL_CANONICAL), FULL_CANONICAL)
  assert.equal(toCanonicalTurkishIban(FULL_DISPLAY), FULL_CANONICAL)
  assert.equal(toCanonicalTurkishIban('tr33 0006 1005 1978 6457 8413 26'), FULL_CANONICAL)
  assert.equal(toCanonicalTurkishIban(FULL_DIGITS), FULL_CANONICAL)
  assert.equal(applyTurkishIbanBodyEdit(FULL_CANONICAL), FULL_CANONICAL)
  assert.equal(applyTurkishIbanBodyEdit('TRTR' + FULL_DIGITS), FULL_CANONICAL)
  assert.equal(toCanonicalTurkishIban('tr33-0006-abc1005'), 'TR3300061005')
  assert.equal(formatTurkishIban('tr33-0006-abc1005'), 'TR33 0006 1005')
})

test('EDIT: TR cannot be removed from canonical payload; clearing digits yields empty', () => {
  assert.equal(applyTurkishIbanBodyEdit(''), '')
  assert.equal(turkishIbanBody(FULL_CANONICAL), FULL_DIGITS)
  assert.equal(turkishIbanBody(''), '')
  assert.equal(toPersistedIban(FULL_CANONICAL), FULL_CANONICAL)
})

test('VALIDATION: empty optional; incomplete invalid; complete passes', () => {
  assert.equal(validatePaymentIban('', ''), undefined)
  assert.equal(validatePaymentIban('TR', ''), undefined)
  assert.equal(validatePaymentIban('', 'Test Bank'), 'payment-iban-required')
  assert.equal(validatePaymentIban('TR', 'Test Bank'), 'payment-iban-required')

  assert.equal(isIncompleteTurkishIban('TR33'), true)
  assert.equal(isCompleteTurkishIban('TR33'), false)
  assert.equal(validatePaymentIban('TR1', ''), 'payment-profile-invalid-iban')
  assert.equal(validatePaymentIban('TR12 3123', ''), 'payment-profile-invalid-iban')
  assert.equal(validatePaymentIban('TR33 0006 1005 1978 6457 8413 2', ''), 'payment-profile-invalid-iban')
  assert.equal(validatePaymentIban(FULL_DISPLAY, ''), undefined)
  assert.equal(validatePaymentIban(FULL_DISPLAY, 'Test Bank'), undefined)
  assert.equal(isCompleteTurkishIban(FULL_CANONICAL), true)
})

test('PO regression: spaced display TR + 24 digits is structurally valid and canonical', () => {
  const display = 'TR 12 3123 1231 2312 3213 2131 32'
  const expected = 'TR123123123123123213213132'
  assert.equal(normalizeTurkishIbanDigits(display).length, 24)
  assert.equal(toCanonicalTurkishIban(display), expected)
  assert.equal(toPersistedIban(display), expected)
  assert.equal(expected.length, TR_IBAN_MAX_LENGTH)
  assert.equal(validatePaymentIban(display, ''), undefined)
  assert.equal(validatePaymentIban(expected, ''), undefined)
  assert.equal(validatePaymentIban('12 3123 1231 2312 3213 2131 32', ''), undefined)
  assert.equal(formatTurkishIbanBody(expected), '12 3123 1231 2312 3213 2131 32')
})
