import assert from 'node:assert/strict'
import test from 'node:test'
import { en as hrEn } from '../i18n/hr/en.ts'
import { ru as hrRu } from '../i18n/hr/ru.ts'
import { tr as hrTr } from '../i18n/hr/tr.ts'
import {
  asHrEmployeeList,
  isSuccessfulEmptyPersonnelList,
  personnelEmptyKind,
  selectedEmployeeIdAfterDirectoryLoad,
} from './personnelDirectory.ts'

test('empty personnel list payload maps to an empty array', () => {
  assert.deepEqual(asHrEmployeeList([]), [])
  assert.deepEqual(asHrEmployeeList({ items: [] }), [])
  assert.deepEqual(asHrEmployeeList(null), [])
  assert.deepEqual(asHrEmployeeList(undefined), [])
})

test('successful HTTP empty list is a valid empty collection, not a failure', () => {
  assert.equal(isSuccessfulEmptyPersonnelList([], true), true)
  assert.equal(isSuccessfulEmptyPersonnelList({ items: [] }, true), true)
  assert.equal(isSuccessfulEmptyPersonnelList([], false), false)
})

test('non-empty personnel list still maps through', () => {
  const rows = asHrEmployeeList<{ employeeId: string }>([
    { employeeId: 'e-1' },
    { employeeId: 'e-2' },
  ])
  assert.equal(rows.length, 2)
  assert.equal(rows[0]?.employeeId, 'e-1')
})

test('zero personnel is a dataset empty state, not a load error', () => {
  assert.equal(
    personnelEmptyKind({ loadFailed: false, totalCount: 0, visibleCount: 0 }),
    'dataset',
  )
  assert.equal(
    personnelEmptyKind({ loadFailed: true, totalCount: 0, visibleCount: 0 }),
    'none',
  )
})

test('filters on a non-empty directory keep filter-empty semantics', () => {
  assert.equal(
    personnelEmptyKind({ loadFailed: false, totalCount: 3, visibleCount: 0 }),
    'filter',
  )
  assert.equal(
    personnelEmptyKind({ loadFailed: false, totalCount: 3, visibleCount: 2 }),
    'none',
  )
})

test('directory load never auto-selects the first employee', () => {
  assert.equal(selectedEmployeeIdAfterDirectoryLoad([]), null)
  assert.equal(selectedEmployeeIdAfterDirectoryLoad([{ employeeId: 'e-1' }]), null)
})

test('empty-state copy is localized and does not reuse the mutation generic', () => {
  assert.equal(hrTr.personnel.emptyTitle, 'Henüz personel bulunmuyor')
  assert.equal(
    hrTr.personnel.emptyHint,
    'İlk personel kaydınızı oluşturarak başlayabilirsiniz.',
  )
  assert.equal(hrTr.personnel.addPersonnel, 'Personel Ekle')
  assert.equal(hrTr.personnel.errors.generic, 'Personel işlemi tamamlanamadı.')
  assert.notEqual(hrTr.personnel.emptyTitle, hrTr.personnel.errors.generic)
  assert.notEqual(hrTr.personnel.errors.listFailed, hrTr.personnel.errors.generic)

  assert.equal(hrEn.personnel.emptyTitle, 'No personnel yet')
  assert.equal(
    hrEn.personnel.emptyHint,
    'You can get started by creating your first personnel record.',
  )
  assert.equal(hrEn.personnel.addPersonnel, 'Add personnel')

  assert.equal(hrRu.personnel.emptyTitle, 'Сотрудников пока нет')
  assert.equal(
    hrRu.personnel.emptyHint,
    'Начните с создания первой записи сотрудника.',
  )
  assert.equal(hrRu.personnel.addPersonnel, 'Добавить сотрудника')
})
