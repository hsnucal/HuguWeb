import assert from 'node:assert/strict'
import test from 'node:test'
import { en as hrEn } from '../i18n/hr/en.ts'
import { ru as hrRu } from '../i18n/hr/ru.ts'
import { tr as hrTr } from '../i18n/hr/tr.ts'
import { en as workforceEn } from '../i18n/workforce/en.ts'
import { ru as workforceRu } from '../i18n/workforce/ru.ts'
import { tr as workforceTr } from '../i18n/workforce/tr.ts'
import {
  amountAfterLeaveTypeChange,
  amountAfterTypeOrPreview,
  leaveManagementFilterClassName,
  leaveRequestDateFieldUsesCalendar,
  leaveRequestSubmitUsesInlineButtons,
  personnelDirectoryFilterClassName,
  personnelDirectoryFilterControlIds,
} from './leaveRequestDefaults.ts'

test('leave request dates use shared DateField calendar', () => {
  assert.equal(leaveRequestDateFieldUsesCalendar(), true)
})

test('leave submit uses content-sized primary button', () => {
  assert.equal(leaveRequestSubmitUsesInlineButtons(), true)
  assert.equal(hrTr.personnel.leave.submitRequest, 'Talebi gönder')
  assert.equal(hrEn.personnel.leave.submitRequest, 'Submit request')
  assert.equal(hrRu.personnel.leave.submitRequest, 'Отправить запрос')
  assert.notEqual(hrTr.personnel.leave.submitRequest, hrTr.personnel.leave.newRequest)
})

test('type default wins over schedule suggestion until user edits', () => {
  assert.equal(amountAfterTypeOrPreview(false, 10, 5, ''), '10')
  assert.equal(amountAfterTypeOrPreview(false, null, 5, ''), '5')
  assert.equal(amountAfterTypeOrPreview(true, 10, 5, '7'), '7')
  assert.equal(amountAfterLeaveTypeChange(false, 3, ''), '3')
  assert.equal(amountAfterLeaveTypeChange(true, 3, '1.5'), '1.5')
})

test('preview never overwrites a manually edited amount even with type default', () => {
  assert.equal(amountAfterTypeOrPreview(true, 10, 2, '9'), '9')
})

test('personnel filter layout uses dedicated six-control grid class', () => {
  assert.equal(personnelDirectoryFilterClassName, 'hrFilters')
  assert.equal(leaveManagementFilterClassName, 'leaveMgmtFilters')
  assert.notEqual(personnelDirectoryFilterClassName, leaveManagementFilterClassName)
  assert.deepEqual([...personnelDirectoryFilterControlIds], [
    'workforce-search',
    'workforce-department-filter',
    'workforce-position-filter',
    'workforce-start-from',
    'workforce-start-to',
    'personnel-column-picker',
  ])
})

test('default request duration admin label is localized', () => {
  assert.equal(workforceTr.workforce.leaveTypeDefaultRequestAmount, 'Varsayılan talep süresi')
  assert.equal(workforceEn.workforce.leaveTypeDefaultRequestAmount, 'Default request duration')
  assert.equal(workforceRu.workforce.leaveTypeDefaultRequestAmount, 'Продолжительность запроса по умолчанию')
})
