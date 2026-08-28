import assert from 'node:assert/strict'
import test from 'node:test'
import { APP_LANGUAGE_OPTIONS, languageNativeName } from './languages.ts'

test('supported languages keep native names for the selector', () => {
  assert.deepEqual(
    APP_LANGUAGE_OPTIONS.map((option) => option.code),
    ['tr', 'en', 'ru'],
  )
  assert.equal(languageNativeName('tr'), 'Türkçe')
  assert.equal(languageNativeName('en'), 'English')
  assert.equal(languageNativeName('ru'), 'Русский')
})
