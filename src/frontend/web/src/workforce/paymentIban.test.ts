import assert from 'node:assert/strict'
import test from 'node:test'
import { compactIban, isValidIbanChecksum, validatePaymentIban } from './paymentIban.ts'

test('valid IBAN compact and checksum', () => {
  const raw = 'TR33 0006 1005 1978 6457 8413 26'
  assert.equal(compactIban(raw), 'TR330006100519786457841326')
  assert.equal(isValidIbanChecksum(compactIban(raw)), true)
  assert.equal(validatePaymentIban(raw, 'Test Bank'), undefined)
})

test('payment is optional until bank name requires IBAN', () => {
  assert.equal(validatePaymentIban('', ''), undefined)
  assert.equal(validatePaymentIban('', 'Test Bank'), 'payment-iban-required')
  assert.equal(validatePaymentIban('BAD', ''), 'payment-profile-invalid-iban')
})
