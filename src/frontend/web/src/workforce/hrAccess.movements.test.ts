import assert from 'node:assert/strict'
import test from 'node:test'
import type { CurrentUser } from '../shared/types.ts'
import { canManageHrMovements, canReadHrMovements } from './hrAccess.ts'

function userWith(...permissions: string[]): CurrentUser {
  return {
    id: 'u1',
    email: 'a@b.c',
    preferredLanguage: 'en',
    permissions,
  }
}

test('canReadHrMovements accepts read or manage and ignores role names', () => {
  assert.equal(canReadHrMovements(null), false)
  assert.equal(canReadHrMovements(userWith()), false)
  assert.equal(canReadHrMovements(userWith('hr.movements.read')), true)
  assert.equal(canReadHrMovements(userWith('hr.movements.manage')), true)
  assert.equal(canReadHrMovements(userWith('hr.employee.read')), false)
  assert.equal(canReadHrMovements(userWith('HR Manager')), false)
})

test('canManageHrMovements requires manage only', () => {
  assert.equal(canManageHrMovements(userWith('hr.movements.read')), false)
  assert.equal(canManageHrMovements(userWith('hr.movements.manage')), true)
})
