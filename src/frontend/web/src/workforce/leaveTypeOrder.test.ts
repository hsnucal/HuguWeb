import assert from 'node:assert/strict'
import test from 'node:test'
import type { LeaveTypeRecord } from './hrLeaveApi.ts'
import { orderActiveLeaveTypes } from './leaveTypeOrder.ts'

function type(partial: Partial<LeaveTypeRecord> & Pick<LeaveTypeRecord, 'id' | 'name'>): LeaveTypeRecord {
  return {
    code: partial.code ?? partial.id,
    systemKind: partial.systemKind ?? null,
    tracksBalance: partial.tracksBalance ?? false,
    isActive: partial.isActive ?? true,
    ...partial,
  }
}

test('active Annual SystemKind appears first regardless of stored name', () => {
  const ordered = orderActiveLeaveTypes([
    type({ id: 'unpaid', name: 'AAA Unpaid', systemKind: 'Unpaid' }),
    type({ id: 'sick', name: 'BBB Sick', systemKind: 'Sick' }),
    type({ id: 'annual', name: 'ZZZ renamed annual', systemKind: 'Annual' }),
  ])

  assert.deepEqual(
    ordered.map((item) => item.id),
    ['annual', 'unpaid', 'sick'],
  )
})

test('remaining active types keep name order after Annual', () => {
  const ordered = orderActiveLeaveTypes([
    type({ id: 'custom-z', name: 'Study leave', systemKind: null }),
    type({ id: 'excuse', name: 'Excuse', systemKind: 'Excuse' }),
    type({ id: 'annual', name: 'Annual', systemKind: 'Annual' }),
    type({ id: 'admin', name: 'Administrative', systemKind: 'Administrative' }),
  ])

  assert.deepEqual(
    ordered.map((item) => item.id),
    ['annual', 'admin', 'excuse', 'custom-z'],
  )
})

test('renamed Annual type remains first', () => {
  const ordered = orderActiveLeaveTypes([
    type({ id: 'custom', name: 'A custom type', systemKind: null }),
    type({ id: 'annual', name: 'Company holiday bank', systemKind: 'Annual' }),
  ])

  assert.equal(ordered[0]?.id, 'annual')
  assert.equal(ordered[0]?.name, 'Company holiday bank')
})

test('localized display names are not used for ordering', () => {
  const turkish = orderActiveLeaveTypes([
    type({ id: 'unpaid', name: 'Ücretsiz İzin', systemKind: 'Unpaid' }),
    type({ id: 'annual', name: 'Yıllık İzin', systemKind: 'Annual' }),
  ])
  const english = orderActiveLeaveTypes([
    type({ id: 'unpaid', name: 'Unpaid Leave', systemKind: 'Unpaid' }),
    type({ id: 'annual', name: 'Annual Leave', systemKind: 'Annual' }),
  ])
  const russian = orderActiveLeaveTypes([
    type({ id: 'unpaid', name: 'Отпуск без сохранения', systemKind: 'Unpaid' }),
    type({ id: 'annual', name: 'Ежегодный отпуск', systemKind: 'Annual' }),
  ])

  assert.equal(turkish[0]?.id, 'annual')
  assert.equal(english[0]?.id, 'annual')
  assert.equal(russian[0]?.id, 'annual')
})

test('inactive types including inactive Annual are omitted', () => {
  const ordered = orderActiveLeaveTypes([
    type({ id: 'annual', name: 'Annual', systemKind: 'Annual', isActive: false }),
    type({ id: 'sick', name: 'Sick', systemKind: 'Sick' }),
    type({ id: 'old', name: 'Old custom', systemKind: null, isActive: false }),
  ])

  assert.deepEqual(
    ordered.map((item) => item.id),
    ['sick'],
  )
})
