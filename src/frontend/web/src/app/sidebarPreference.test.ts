import assert from 'node:assert/strict'
import test from 'node:test'
import {
  parseSidebarCollapsed,
  persistSidebarCollapsed,
  readSidebarCollapsed,
  SIDEBAR_COLLAPSED_STORAGE_KEY,
} from './sidebarPreference.ts'

function memoryStorage(initial: Record<string, string> = {}): Storage {
  const data = { ...initial }
  return {
    get length() {
      return Object.keys(data).length
    },
    clear() {
      for (const key of Object.keys(data)) {
        delete data[key]
      }
    },
    getItem(key: string) {
      return Object.hasOwn(data, key) ? data[key] : null
    },
    key() {
      return null
    },
    removeItem(key: string) {
      delete data[key]
    },
    setItem(key: string, value: string) {
      data[key] = value
    },
  }
}

test('default is expanded when no preference exists', () => {
  assert.equal(parseSidebarCollapsed(null), false)
  assert.equal(readSidebarCollapsed(memoryStorage()), false)
})

test('collapse preference is stored locally as true', () => {
  const storage = memoryStorage()
  persistSidebarCollapsed(true, storage)
  assert.equal(storage.getItem(SIDEBAR_COLLAPSED_STORAGE_KEY), 'true')
  assert.equal(readSidebarCollapsed(storage), true)
})

test('expand preference is stored locally as false', () => {
  const storage = memoryStorage({ [SIDEBAR_COLLAPSED_STORAGE_KEY]: 'true' })
  persistSidebarCollapsed(false, storage)
  assert.equal(storage.getItem(SIDEBAR_COLLAPSED_STORAGE_KEY), 'false')
  assert.equal(readSidebarCollapsed(storage), false)
})

test('unknown stored values keep the sidebar expanded', () => {
  assert.equal(parseSidebarCollapsed('yes'), false)
  assert.equal(parseSidebarCollapsed(''), false)
})
