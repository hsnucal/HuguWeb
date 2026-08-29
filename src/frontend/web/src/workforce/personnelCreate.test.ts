import assert from 'node:assert/strict'
import test from 'node:test'
import { emptyPersonnelForm, toHrWrite } from './personnelForm.ts'
import {
  firstInvalidTarget,
  invalidPersonnelTabs,
  revalidateKnownErrors,
  validatePersonnelField,
  validatePersonnelForm,
} from './personnelValidation.ts'

test('create write payload keeps department and position while omitting blank seniority', () => {
  const form = emptyPersonnelForm('2026-08-28')
  form.givenName = 'Ayşe'
  form.familyName = 'Yılmaz'
  form.departmentId = 'dept-1'
  form.positionId = 'pos-1'
  form.seniorityStartDate = ''
  form.contractType = 'Indefinite'

  const body = toHrWrite(form, true)
  assert.equal(body.employmentStartDate, '2026-08-28')
  assert.equal(body.departmentId, 'dept-1')
  assert.equal(body.positionId, 'pos-1')
  assert.equal(body.seniorityStartDate, null)
  assert.equal(body.workforceTerms.contractType, 'Indefinite')
})

test('create validation for missing department and position belongs to the work tab', () => {
  const form = emptyPersonnelForm('2026-08-28')
  form.givenName = 'Ayşe'
  form.familyName = 'Yılmaz'
  const errors = validatePersonnelForm(form, { createMode: true, today: '2026-08-28' })
  assert.equal(errors.departmentId, 'department-required')
  assert.equal(errors.positionId, 'position-required')
  assert.equal(errors.seniorityStartDate, undefined)

  const target = firstInvalidTarget(errors, form, true)
  assert.equal(target?.tab, 'work')
  assert.equal(target?.controlId, 'hr-work-department')
  assert.deepEqual([...invalidPersonnelTabs(errors, form, true)], ['work'])
})

test('blank seniority is valid and a later seniority date is rejected', () => {
  const form = emptyPersonnelForm('2026-08-28')
  form.givenName = 'Ayşe'
  form.familyName = 'Yılmaz'
  form.departmentId = 'dept-1'
  form.positionId = 'pos-1'
  form.seniorityStartDate = ''
  const ok = validatePersonnelForm(form, { createMode: true, today: '2026-08-28' })
  assert.equal(ok.seniorityStartDate, undefined)
  assert.equal(ok.paymentIban, undefined)

  form.seniorityStartDate = '2026-08-29'
  const invalid = validatePersonnelForm(form, { createMode: true, today: '2026-08-28' })
  assert.equal(invalid.seniorityStartDate, 'seniority-start-date-invalid')
  assert.equal(firstInvalidTarget(invalid, form, true)?.tab, 'work')
})

test('malformed and impossible dates are rejected before save', () => {
  const form = emptyPersonnelForm('2026-08-28')
  form.givenName = 'Ayşe'
  form.familyName = 'Yılmaz'
  form.departmentId = 'dept-1'
  form.positionId = 'pos-1'
  form.birthDate = '18.04.201991'
  const oversized = validatePersonnelForm(form, { createMode: true, today: '2026-08-28' })
  assert.equal(oversized.birthDate, 'date-invalid')

  form.birthDate = '31.02.2020'
  const impossible = validatePersonnelForm(form, { createMode: true, today: '2026-08-28' })
  assert.equal(impossible.birthDate, 'date-invalid')

  form.birthDate = '29.02.2024'
  const leap = validatePersonnelForm(form, { createMode: true, today: '2026-08-28' })
  assert.equal(leap.birthDate, undefined)
})

test('district must belong to the selected province while unknown text does not crash', () => {
  const form = emptyPersonnelForm('2026-08-28')
  form.givenName = 'Ayşe'
  form.familyName = 'Yılmaz'
  form.departmentId = 'dept-1'
  form.positionId = 'pos-1'
  form.residenceCity = 'İstanbul'
  form.residenceDistrict = 'Kadıköy'
  const ok = validatePersonnelForm(form, { createMode: true, today: '2026-08-28' })
  assert.equal(ok.residenceDistrict, undefined)

  form.residenceDistrict = 'Çankaya'
  const mismatch = validatePersonnelForm(form, { createMode: true, today: '2026-08-28' })
  assert.equal(mismatch.residenceDistrict, 'district-not-in-province')
  assert.equal(firstInvalidTarget(mismatch, form, true)?.tab, 'identity')

  form.residenceCity = 'Eski Serbest Metin'
  form.residenceDistrict = 'Bilinmeyen İlçe'
  const legacy = validatePersonnelForm(form, { createMode: true, today: '2026-08-28' })
  assert.equal(legacy.residenceDistrict, undefined)
})

test('bank name without IBAN is owned by the payment tab', () => {
  const form = emptyPersonnelForm('2026-08-28')
  form.givenName = 'Ayşe'
  form.familyName = 'Yılmaz'
  form.departmentId = 'dept-1'
  form.positionId = 'pos-1'
  form.paymentBankName = 'Ziraat'
  const errors = validatePersonnelForm(form, { createMode: true, today: '2026-08-28' })
  assert.equal(errors.paymentIban, 'payment-iban-required')
  assert.equal(firstInvalidTarget(errors, form, true)?.tab, 'payment')
  assert.equal(firstInvalidTarget(errors, form, true)?.controlId, 'hr-payment-iban')
})

test('incomplete IBAN fails blur/field validation and save; complete and empty pass', () => {
  const form = emptyPersonnelForm('2026-08-28')
  form.givenName = 'Ayşe'
  form.familyName = 'Yılmaz'
  form.departmentId = 'dept-1'
  form.positionId = 'pos-1'

  assert.equal(
    validatePersonnelField(form, 'paymentIban', { createMode: true, today: '2026-08-28' }),
    undefined,
  )

  form.paymentIban = 'TR1'
  assert.equal(
    validatePersonnelField(form, 'paymentIban', { createMode: true, today: '2026-08-28' }),
    'payment-profile-invalid-iban',
  )

  form.paymentIban = 'TR12 3123'
  assert.equal(
    validatePersonnelField(form, 'paymentIban', { createMode: true, today: '2026-08-28' }),
    'payment-profile-invalid-iban',
  )
  assert.equal(
    validatePersonnelForm(form, { createMode: true, today: '2026-08-28' }).paymentIban,
    'payment-profile-invalid-iban',
  )

  form.paymentIban = 'TR33000610051978645784132'
  assert.equal(
    validatePersonnelField(form, 'paymentIban', { createMode: true, today: '2026-08-28' }),
    'payment-profile-invalid-iban',
  )

  form.paymentIban = 'TR33 0006 1005 1978 6457 8413 26'
  assert.equal(
    validatePersonnelField(form, 'paymentIban', { createMode: true, today: '2026-08-28' }),
    undefined,
  )
  assert.equal(validatePersonnelForm(form, { createMode: true, today: '2026-08-28' }).paymentIban, undefined)

  form.paymentIban = 'TR 12 3123 1231 2312 3213 2131 32'
  assert.equal(
    validatePersonnelField(form, 'paymentIban', { createMode: true, today: '2026-08-28' }),
    undefined,
  )
  assert.equal(validatePersonnelForm(form, { createMode: true, today: '2026-08-28' }).paymentIban, undefined)

  form.paymentIban = ''
  assert.equal(
    validatePersonnelField(form, 'paymentIban', { createMode: true, today: '2026-08-28' }),
    undefined,
  )
  assert.equal(validatePersonnelForm(form, { createMode: true, today: '2026-08-28' }).paymentIban, undefined)
})

test('incomplete IBAN does not appear until blur, then clears when completed', () => {
  const form = emptyPersonnelForm('2026-08-28')
  form.paymentIban = 'TR12 3123'
  const context = { createMode: true, today: '2026-08-28' }

  // Live typing with no prior field error: do not introduce IBAN error yet.
  assert.deepEqual(revalidateKnownErrors({}, form, context, []), {})

  // After blur/save has marked the field, revalidation keeps the error until complete.
  const afterBlur = revalidateKnownErrors({ paymentIban: 'payment-profile-invalid-iban' }, form, context, [])
  assert.equal(afterBlur.paymentIban, 'payment-profile-invalid-iban')

  form.paymentIban = 'TR33 0006 1005 1978 6457 8413 26'
  const afterComplete = revalidateKnownErrors({ paymentIban: 'payment-profile-invalid-iban' }, form, context, [])
  assert.equal(afterComplete.paymentIban, undefined)
})
