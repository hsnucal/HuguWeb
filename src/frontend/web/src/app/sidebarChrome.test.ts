import assert from 'node:assert/strict'
import test from 'node:test'
import { resolveSidebarChrome } from './sidebarChrome.ts'

test('desktop uses the stored collapsed preference as a rail', () => {
  assert.deepEqual(
    resolveSidebarChrome({ collapsedPreference: false, isNarrowViewport: false }),
    { railCollapsed: false, isDrawer: false },
  )
  assert.deepEqual(
    resolveSidebarChrome({ collapsedPreference: true, isNarrowViewport: false }),
    { railCollapsed: true, isDrawer: false },
  )
})

test('narrow viewport uses a labeled drawer and ignores the collapsed rail preference', () => {
  assert.deepEqual(
    resolveSidebarChrome({ collapsedPreference: true, isNarrowViewport: true }),
    { railCollapsed: false, isDrawer: true },
  )
  assert.deepEqual(
    resolveSidebarChrome({ collapsedPreference: false, isNarrowViewport: true }),
    { railCollapsed: false, isDrawer: true },
  )
})
