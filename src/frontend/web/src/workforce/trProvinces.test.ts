import assert from 'node:assert/strict'
import test from 'node:test'
import {
  districtsForProvince,
  findTurkishProvince,
  isKnownProvinceDistrict,
  retainedDistrict,
  turkishDistrictCount,
  turkishProvinceCount,
} from './trProvinces.ts'

test('catalogue contains all 81 provinces and their districts', () => {
  assert.equal(turkishProvinceCount(), 81)
  assert.ok(turkishDistrictCount() >= 900)
  assert.ok(findTurkishProvince('Ankara'))
  assert.ok(findTurkishProvince('İstanbul'))
  assert.ok(findTurkishProvince('34'))
})

test('district list filters by province and changing province clears incompatible district', () => {
  const kadikoy = 'Kadıköy'
  assert.equal(isKnownProvinceDistrict('İstanbul', kadikoy), true)
  assert.equal(isKnownProvinceDistrict('Ankara', kadikoy), false)
  assert.ok(districtsForProvince('İstanbul').includes(kadikoy))
  assert.equal(retainedDistrict('Ankara', kadikoy), '')
  assert.equal(retainedDistrict('İstanbul', kadikoy), kadikoy)
})

test('unknown free-text values do not crash and are retained for compatibility', () => {
  assert.equal(findTurkishProvince('Eski Serbest Metin'), undefined)
  assert.equal(retainedDistrict('Eski Serbest Metin', 'Bilinmeyen İlçe'), 'Bilinmeyen İlçe')
  assert.equal(isKnownProvinceDistrict('Eski Serbest Metin', 'Bilinmeyen İlçe'), false)
})
