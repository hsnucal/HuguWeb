import assert from 'node:assert/strict'
import test from 'node:test'
import { nationalityLabel } from './nationalityDisplay.ts'

test('nationality labels use Intl with a code fallback', () => {
  const tr = nationalityLabel('TR', 'tr')
  assert.match(tr, /^TR/)
  assert.ok(tr.includes('Türkiye') || tr === 'TR')
  assert.equal(nationalityLabel('', 'en'), '')
  assert.match(nationalityLabel('DE', 'en'), /^DE/)
  assert.match(nationalityLabel('RU', 'ru'), /^RU/)
})
