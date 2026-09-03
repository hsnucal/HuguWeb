import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'
import { resolveAttendanceCorrectionEmploymentId } from './attendanceMonth.ts'

const css = readFileSync(new URL('./AttendancePage.module.css', import.meta.url), 'utf8')
const page = readFileSync(new URL('./AttendancePage.tsx', import.meta.url), 'utf8')
const panel = readFileSync(new URL('./AttendanceDayPanel.tsx', import.meta.url), 'utf8')

test('drawer is an overlay and does not take width from the Puantaj grid', () => {
  assert.match(css, /\.drawer\s*\{[\s\S]*?position:\s*fixed/)
  assert.match(css, /\.drawer\s*\{[\s\S]*?right:\s*0\.85rem/)
  assert.doesNotMatch(css, /\.drawer\s*\{[\s\S]*?flex:\s*0 0/)
  assert.match(page, /data-attendance-grid-layout="full"/)
  assert.match(panel, /data-attendance-drawer="overlay"/)
  assert.match(page, /drawerScrim/)
})

test('cell leave labels do not wrap', () => {
  assert.match(css, /\.cellLabel\s*\{[^}]*white-space:\s*nowrap/)
  assert.doesNotMatch(css, /\.cellButton,\s*\.cellStatic\s*\{[^}]*overflow-wrap/)
})

test('correction writes use the day employment id, never the employee id', () => {
  const employmentId = 'a1e1c0de-0003-4000-8000-000000000422'
  const employeeId = 'a1e1c0de-0003-4000-8000-000000000421'
  assert.equal(
    resolveAttendanceCorrectionEmploymentId({ employmentId }, { employmentId: employeeId }),
    employmentId,
  )
  assert.notEqual(
    resolveAttendanceCorrectionEmploymentId({ employmentId }, { employmentId: employeeId }),
    employeeId,
  )
})

test('Escape close remains wired on the attendance page', () => {
  assert.match(page, /event\.key === 'Escape'/)
  assert.match(panel, /onClose/)
})
