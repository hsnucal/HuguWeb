import assert from 'node:assert/strict'
import test from 'node:test'
import { placeAnchoredMenu } from './placeAnchoredMenu.ts'

test('menu stays aligned with the trigger when it fits', () => {
  assert.deepEqual(
    placeAnchoredMenu(
      { top: 80, left: 24, right: 188, bottom: 112, width: 164 },
      { width: 164, height: 120 },
      { width: 1280, height: 800 },
    ),
    { top: 118, left: 24, width: 164 },
  )
})

test('menu flips above the trigger and shifts left to stay in the viewport', () => {
  assert.deepEqual(
    placeAnchoredMenu(
      { top: 700, left: 1140, right: 1174, bottom: 732, width: 34 },
      { width: 164, height: 120 },
      { width: 1280, height: 800 },
    ),
    { top: 574, left: 1010, width: 164 },
  )
})
