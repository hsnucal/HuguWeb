import assert from 'node:assert/strict'
import test from 'node:test'
import { compareShiftDefinitionsByStart, formatShiftClockRange } from './shiftDefinitionForm.ts'
import {
  cellCompactLabel,
  cellSelectionKey,
  cellVisibleContent,
  cellWouldOverwrite,
  copyWeekDialogState,
  countOverwriteCells,
  currentWeekStart,
  formatBulkShiftActionLabel,
  formatBulkShiftActionTooltip,
  formatScheduledCellDetail,
  formatShiftAssignMeta,
  groupEmployeesByDepartment,
  isCellEditable,
  matchesEmployeeSearch,
  mondayOfIsoWeek,
  parseCellSelectionKey,
  resolveDepartmentFilterValue,
  selectionHasOverwrite,
  shiftWeekStart,
  toggleCellSelection,
  weekDateRange,
} from './shiftPlanWeek.ts'
import { placeAnchoredMenu } from '../ui/placeAnchoredMenu.ts'

const labels = { restDay: 'OFF', unscheduled: '—', muted: '' }

test('mondayOfIsoWeek returns Monday for mid-week and Sunday dates', () => {
  assert.equal(mondayOfIsoWeek('2026-08-30'), '2026-08-24')
  assert.equal(mondayOfIsoWeek('2026-08-24'), '2026-08-24')
  assert.equal(mondayOfIsoWeek('2026-08-23'), '2026-08-17')
})

test('weekDateRange yields Mon–Sun ISO dates without UTC parsing', () => {
  assert.deepEqual(weekDateRange('2026-08-24'), [
    '2026-08-24',
    '2026-08-25',
    '2026-08-26',
    '2026-08-27',
    '2026-08-28',
    '2026-08-29',
    '2026-08-30',
  ])
})

test('shiftWeekStart navigates previous and next weeks', () => {
  assert.equal(shiftWeekStart('2026-08-24', 1), '2026-08-31')
  assert.equal(shiftWeekStart('2026-08-24', -1), '2026-08-17')
})

test('currentWeekStart is always a Monday', () => {
  const start = currentWeekStart()
  const [year, month, day] = start.split('-').map(Number)
  const local = new Date(year, month - 1, day)
  assert.equal(local.getDay(), 1)
})

test('selection helpers toggle and parse cell keys', () => {
  const key = cellSelectionKey('emp-1', '2026-08-24')
  assert.equal(key, 'emp-1|2026-08-24')
  assert.deepEqual(parseCellSelectionKey(key), { employeeId: 'emp-1', date: '2026-08-24' })

  const once = toggleCellSelection(new Set(), key)
  assert.equal(once.has(key), true)
  const twice = toggleCellSelection(once, key)
  assert.equal(twice.has(key), false)
})

test('overwrite detection covers Shift and RestDay only', () => {
  assert.equal(cellWouldOverwrite('Shift'), true)
  assert.equal(cellWouldOverwrite('RestDay'), true)
  assert.equal(cellWouldOverwrite('Unscheduled'), false)
  assert.equal(cellWouldOverwrite(null), false)
  assert.equal(selectionHasOverwrite([{ state: 'Unscheduled' }, { state: 'RestDay' }]), true)
  assert.equal(selectionHasOverwrite([{ state: 'Unscheduled' }]), false)
  assert.equal(
    countOverwriteCells([
      { state: 'Shift' },
      { state: 'Unscheduled' },
      { state: 'RestDay' },
      { state: 'Unscheduled' },
      { state: 'Shift' },
    ]),
    3,
  )
})

test('scheduled cell shows Name primary and time secondary, never Code', () => {
  const visible = cellVisibleContent(
    {
      eligibility: 'Editable',
      state: 'Shift',
      shiftName: 'Akşam',
      shiftCode: 'VRD200',
      startLocalTime: '16:00:00',
      endLocalTime: '00:00:00',
    },
    labels,
    formatShiftClockRange,
  )
  assert.equal(visible.primary, 'Akşam')
  assert.equal(visible.secondary, '16:00 – 00:00')
  assert.notEqual(visible.primary, 'VRD200')
  assert.doesNotMatch(`${visible.primary} ${visible.secondary}`, /VRD200|vrd200/i)

  const sabah = cellVisibleContent(
    {
      eligibility: 'Editable',
      state: 'Shift',
      shiftName: 'Sabah',
      shiftCode: 'VRD100',
      startLocalTime: '08:00:00',
      endLocalTime: '16:00:00',
    },
    labels,
    formatShiftClockRange,
  )
  assert.deepEqual(sabah, { primary: 'Sabah', secondary: '08:00 – 16:00' })

  // Honest midnight / 23:59 — never rewrite API values.
  assert.equal(formatShiftClockRange('16:00:00', '00:00:00'), '16:00 – 00:00')
  assert.equal(formatShiftClockRange('16:00:00', '23:59:00'), '16:00 – 23:59')
})

test('RestDay and Unscheduled have no time line', () => {
  assert.deepEqual(
    cellVisibleContent({ eligibility: 'Editable', state: 'RestDay' }, labels, formatShiftClockRange),
    { primary: 'OFF', secondary: null },
  )
  assert.deepEqual(
    cellVisibleContent(
      { eligibility: 'Editable', state: 'Unscheduled' },
      labels,
      formatShiftClockRange,
    ),
    { primary: '—', secondary: null },
  )
  assert.deepEqual(
    cellVisibleContent({ eligibility: 'OutOfScope', state: null }, labels, formatShiftClockRange),
    { primary: '', secondary: null },
  )
  assert.equal(cellCompactLabel({ eligibility: 'Editable', state: 'RestDay' }, labels), 'OFF')
  assert.equal(isCellEditable('Editable'), true)
  assert.equal(isCellEditable('OutOfScope'), false)
})

test('scheduled cell detail keeps Name primary and Code secondary', () => {
  const detail = formatScheduledCellDetail({
    name: 'Gece',
    code: 'VRD300',
    timeRange: '23:00–07:00',
    overnight: 'Ertesi gün',
    breakLabel: 'Mola: 30 dk',
    netLabel: 'Net: 7 sa 30 dk',
  })
  assert.match(detail, /^Gece\nVRD300/)
  assert.doesNotMatch(detail, /^VRD300/)
})

test('assignment menu meta keeps Code after Name as secondary line', () => {
  assert.equal(
    formatShiftAssignMeta({
      code: 'VRD100',
      timeRange: '08:00–16:00',
      overnight: null,
    }),
    'VRD100 · 08:00–16:00',
  )
})

test('copy week dialog state: overwrite warning, invalid, confirm enablement', () => {
  assert.deepEqual(copyWeekDialogState({ copyCount: 3, overwriteCount: 3, invalidCount: 0 }), {
    showOverwriteWarning: true,
    showInvalidState: false,
    canConfirm: true,
  })
  assert.deepEqual(copyWeekDialogState({ copyCount: 2, overwriteCount: 0, invalidCount: 1 }), {
    showOverwriteWarning: false,
    showInvalidState: true,
    canConfirm: false,
  })
  assert.deepEqual(copyWeekDialogState({ copyCount: 0, overwriteCount: 0, invalidCount: 0 }), {
    showOverwriteWarning: false,
    showInvalidState: false,
    canConfirm: false,
  })
})

test('bulk shift action labels are Name-first with time, never Code', () => {
  const sabah = {
    id: 's1',
    code: 'VRD100',
    name: 'Sabah',
    startLocalTime: '08:00:00',
    endLocalTime: '16:00:00',
    endsNextDay: false,
  }
  const aksam = {
    id: 's2',
    code: 'VRD200',
    name: 'Akşam',
    startLocalTime: '16:00:00',
    endLocalTime: '00:00:00',
    endsNextDay: true,
  }
  const gece = {
    id: 's3',
    code: 'VRD300',
    name: 'Gece',
    startLocalTime: '23:00:00',
    endLocalTime: '07:00:00',
    endsNextDay: true,
  }

  const sabahLabel = formatBulkShiftActionLabel({
    name: sabah.name,
    timeRange: formatShiftClockRange(sabah.startLocalTime, sabah.endLocalTime),
  })
  assert.equal(sabahLabel, 'Sabah · 08:00 – 16:00')
  assert.match(sabahLabel, /^Sabah/)
  assert.doesNotMatch(sabahLabel, /VRD100|vrd100/i)

  assert.equal(
    formatBulkShiftActionLabel({
      name: aksam.name,
      timeRange: formatShiftClockRange(aksam.startLocalTime, aksam.endLocalTime),
    }),
    'Akşam · 16:00 – 00:00',
  )

  const tip = formatBulkShiftActionTooltip({
    name: aksam.name,
    code: aksam.code,
    timeRange: formatShiftClockRange(aksam.startLocalTime, aksam.endLocalTime),
    overnight: 'Ertesi gün',
  })
  assert.equal(tip, ['Akşam', 'VRD200', '16:00 – 00:00', 'Ertesi gün'].join('\n'))

  const ordered = [aksam, gece, sabah].slice().sort(compareShiftDefinitionsByStart)
  assert.deepEqual(
    ordered.map((item) => item.name),
    ['Sabah', 'Akşam', 'Gece'],
  )
  // Clicking Sabah still maps to its definition id (not inferred from label).
  assert.equal(ordered[0]?.id, 's1')
})

test('anchored assign menu flips above and clamps near viewport edges', () => {
  const below = placeAnchoredMenu(
    { top: 40, left: 20, right: 80, bottom: 70, width: 60 },
    { width: 220, height: 180 },
    { width: 800, height: 600 },
  )
  assert.equal(below.top, 76)
  assert.equal(below.left, 20)

  const above = placeAnchoredMenu(
    { top: 500, left: 20, right: 80, bottom: 530, width: 60 },
    { width: 220, height: 180 },
    { width: 800, height: 600 },
  )
  assert.equal(above.top, 500 - 180 - 6)

  const nearRight = placeAnchoredMenu(
    { top: 40, left: 720, right: 780, bottom: 70, width: 60 },
    { width: 220, height: 120 },
    { width: 800, height: 600 },
  )
  assert.equal(nearRight.left, 780 - 220)
})

test('matchesEmployeeSearch looks at name and personnel number', () => {
  const person = { givenName: 'Ayşe', familyName: 'Yılmaz', personnelNumber: 'P-12' }
  assert.equal(matchesEmployeeSearch(person, 'yıl'), true)
  assert.equal(matchesEmployeeSearch(person, 'p-12'), true)
  assert.equal(matchesEmployeeSearch(person, 'nope'), false)
})

test('resolveDepartmentFilterValue auto-selects single department', () => {
  assert.equal(resolveDepartmentFilterValue(true, [{ id: 'd1' }], null), 'd1')
  assert.equal(resolveDepartmentFilterValue(true, [{ id: 'd1' }, { id: 'd2' }], null), '')
  assert.equal(resolveDepartmentFilterValue(false, [{ id: 'd1' }, { id: 'd2' }], null), null)
  assert.equal(resolveDepartmentFilterValue(false, [{ id: 'd1' }, { id: 'd2' }], 'd2'), 'd2')
})

test('groupEmployeesByDepartment sorts groups by name', () => {
  const groups = groupEmployeesByDepartment([
    {
      rowDepartmentId: 'b',
      rowDepartmentName: 'Housekeeping',
      employeeId: '1',
    },
    {
      rowDepartmentId: 'a',
      rowDepartmentName: 'Front Office',
      employeeId: '2',
    },
    {
      rowDepartmentId: 'b',
      rowDepartmentName: 'Housekeeping',
      employeeId: '3',
    },
  ])
  assert.equal(groups[0].name, 'Front Office')
  assert.equal(groups[1].employees.length, 2)
})
