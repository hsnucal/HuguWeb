import assert from 'node:assert/strict'
import test from 'node:test'
import type { CurrentUser } from '../shared/types.ts'
import {
  canManageHrSchedule,
  canManageHrShiftDefinitions,
  canReadHrSchedule,
  canReadHrShiftDefinitions,
} from './hrAccess.ts'

function userWith(...permissions: string[]): CurrentUser {
  return {
    id: 'u1',
    email: 'a@b.c',
    preferredLanguage: 'en',
    permissions,
  }
}

test('canReadHrSchedule accepts read or manage', () => {
  assert.equal(canReadHrSchedule(null), false)
  assert.equal(canReadHrSchedule(userWith()), false)
  assert.equal(canReadHrSchedule(userWith('hr.schedule.read')), true)
  assert.equal(canReadHrSchedule(userWith('hr.schedule.manage')), true)
  assert.equal(canReadHrSchedule(userWith('hr.leave.read')), false)
})

test('canManageHrSchedule requires manage only', () => {
  assert.equal(canManageHrSchedule(userWith('hr.schedule.read')), false)
  assert.equal(canManageHrSchedule(userWith('hr.schedule.manage')), true)
})

test('shift definition nav uses shift-definition permissions not schedule manage', () => {
  assert.equal(canReadHrShiftDefinitions(userWith('hr.schedule.manage')), false)
  assert.equal(canReadHrShiftDefinitions(userWith('hr.shift-definition.read')), true)
  assert.equal(canReadHrShiftDefinitions(userWith('hr.shift-definition.manage')), true)
  assert.equal(canManageHrShiftDefinitions(userWith('hr.shift-definition.read')), false)
  assert.equal(canManageHrShiftDefinitions(userWith('hr.shift-definition.manage')), true)
})
