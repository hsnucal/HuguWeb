import assert from 'node:assert/strict'
import test from 'node:test'
import type { CurrentUser } from '../shared/types.ts'
import { canManageHrAttendance, canReadHrAttendance } from './hrAccess.ts'
import { canShowAttendanceCorrectionForm } from './attendanceMonth.ts'

function userWith(...permissions: string[]): CurrentUser {
  return {
    id: 'u1',
    email: 'a@b.c',
    preferredLanguage: 'en',
    permissions,
  }
}

test('canReadHrAttendance accepts read or manage', () => {
  assert.equal(canReadHrAttendance(null), false)
  assert.equal(canReadHrAttendance(userWith()), false)
  assert.equal(canReadHrAttendance(userWith('hr.attendance.read')), true)
  assert.equal(canReadHrAttendance(userWith('hr.attendance.manage')), true)
  assert.equal(canReadHrAttendance(userWith('hr.schedule.read')), false)
  assert.equal(canReadHrAttendance(userWith('HR Manager')), false)
})

test('canManageHrAttendance requires manage only', () => {
  assert.equal(canManageHrAttendance(userWith('hr.attendance.read')), false)
  assert.equal(canManageHrAttendance(userWith('hr.attendance.manage')), true)
})

test('read-only attendance users can inspect but not correct', () => {
  const readOnly = userWith('hr.attendance.read')
  assert.equal(canReadHrAttendance(readOnly), true)
  assert.equal(canManageHrAttendance(readOnly), false)
  assert.equal(canShowAttendanceCorrectionForm(false, 'InEmployment'), false)
})

test('manage attendance users see correction controls', () => {
  const manager = userWith('hr.attendance.manage')
  assert.equal(canReadHrAttendance(manager), true)
  assert.equal(canManageHrAttendance(manager), true)
  assert.equal(canShowAttendanceCorrectionForm(true, 'InEmployment'), true)
  assert.equal(canShowAttendanceCorrectionForm(true, 'NotEmployed'), false)
})
