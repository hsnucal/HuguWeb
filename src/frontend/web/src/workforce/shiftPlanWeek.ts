import { addDaysIso, todayIsoDate } from '../i18n/format.ts'

export type ScheduleCellEligibility = 'Editable' | 'OutOfScope' | 'NotEmployed'
export type ScheduleCellState = 'Unscheduled' | 'Shift' | 'RestDay'

export type ShiftPlanCellLike = {
  eligibility: ScheduleCellEligibility | string
  state: ScheduleCellState | string | null
  shiftName?: string | null
  shiftCode?: string | null
  startLocalTime?: string | null
  endLocalTime?: string | null
}

export type ShiftPlanCellVisibleContent = {
  primary: string
  /** Time range for Shift cells only; never Code. */
  secondary: string | null
}

export function mondayOfIsoWeek(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number)
  if (!year || !month || !day) {
    return isoDate
  }

  const local = new Date(year, month - 1, day)
  const weekday = local.getDay()
  const offset = weekday === 0 ? -6 : 1 - weekday
  return addDaysIso(isoDate, offset)
}

export function currentWeekStart(): string {
  return mondayOfIsoWeek(todayIsoDate())
}

export function shiftWeekStart(weekStart: string, weeks: number): string {
  return addDaysIso(weekStart, weeks * 7)
}

export function weekDateRange(weekStart: string): string[] {
  return Array.from({ length: 7 }, (_, index) => addDaysIso(weekStart, index))
}

export function cellSelectionKey(employeeId: string, date: string): string {
  return `${employeeId}|${date}`
}

export function parseCellSelectionKey(key: string): { employeeId: string; date: string } {
  const separator = key.indexOf('|')
  if (separator <= 0) {
    return { employeeId: key, date: '' }
  }

  return {
    employeeId: key.slice(0, separator),
    date: key.slice(separator + 1),
  }
}

export function toggleCellSelection(selected: ReadonlySet<string>, key: string): Set<string> {
  const next = new Set(selected)
  if (next.has(key)) {
    next.delete(key)
  } else {
    next.add(key)
  }
  return next
}

export function cellWouldOverwrite(state: string | null | undefined): boolean {
  return state === 'Shift' || state === 'RestDay'
}

export function selectionHasOverwrite(
  cells: ReadonlyArray<{ state: string | null | undefined }>,
): boolean {
  return cells.some((cell) => cellWouldOverwrite(cell.state))
}

export function countOverwriteCells(
  cells: ReadonlyArray<{ state: string | null | undefined }>,
): number {
  return cells.reduce((total, cell) => total + (cellWouldOverwrite(cell.state) ? 1 : 0), 0)
}

export function isCellEditable(eligibility: string | null | undefined): boolean {
  return eligibility === 'Editable'
}

export function cellCompactLabel(
  cell: ShiftPlanCellLike,
  labels: {
    restDay: string
    unscheduled: string
    muted: string
  },
): string {
  return cellVisibleContent(cell, labels, () => '').primary
}

/**
 * Grid cell visible content: Name primary + wall-clock range secondary.
 * Code is never part of the normal cell surface.
 */
export function cellVisibleContent(
  cell: ShiftPlanCellLike,
  labels: {
    restDay: string
    unscheduled: string
    muted: string
  },
  formatTimeRange: (startLocalTime: string, endLocalTime: string) => string,
): ShiftPlanCellVisibleContent {
  if (cell.eligibility === 'OutOfScope' || cell.eligibility === 'NotEmployed') {
    return { primary: labels.muted, secondary: null }
  }

  if (cell.state === 'Shift') {
    const primary = cell.shiftName?.trim() || '—'
    const start = cell.startLocalTime?.trim()
    const end = cell.endLocalTime?.trim()
    const secondary =
      start && end ? formatTimeRange(start, end).trim() || null : null
    return { primary, secondary }
  }

  if (cell.state === 'RestDay') {
    return { primary: labels.restDay, secondary: null }
  }

  return { primary: labels.unscheduled, secondary: null }
}

/** Multiline scheduled-cell detail for tooltip/title. Name first; Code secondary. */
export function formatScheduledCellDetail(parts: {
  name: string | null | undefined
  code: string | null | undefined
  timeRange: string | null | undefined
  overnight: string | null | undefined
  breakLabel: string | null | undefined
  netLabel: string | null | undefined
  note?: string | null | undefined
  inactive?: string | null | undefined
}): string {
  const lines: string[] = []
  const name = parts.name?.trim()
  const code = parts.code?.trim()
  if (name) {
    lines.push(name)
  }
  if (code) {
    lines.push(code)
  }

  const timeLine = [parts.timeRange?.trim(), parts.overnight?.trim()].filter(Boolean).join(' · ')
  if (timeLine) {
    lines.push(timeLine)
  }
  if (parts.breakLabel?.trim()) {
    lines.push(parts.breakLabel.trim())
  }
  if (parts.netLabel?.trim()) {
    lines.push(parts.netLabel.trim())
  }
  if (parts.note?.trim()) {
    lines.push(parts.note.trim())
  }
  if (parts.inactive?.trim()) {
    lines.push(parts.inactive.trim())
  }
  return lines.join('\n')
}

/** Secondary line under shift Name in the assignment menu. */
export function formatShiftAssignMeta(parts: {
  code: string | null | undefined
  timeRange: string | null | undefined
  overnight: string | null | undefined
}): string {
  return [parts.code?.trim(), parts.timeRange?.trim(), parts.overnight?.trim()]
    .filter(Boolean)
    .join(' · ')
}

/** Pure UI flags for the copy-week confirmation dialog. */
export function copyWeekDialogState(preview: {
  copyCount: number
  overwriteCount: number
  invalidCount: number
}): {
  showOverwriteWarning: boolean
  showInvalidState: boolean
  canConfirm: boolean
} {
  return {
    showOverwriteWarning: preview.overwriteCount > 0 && preview.invalidCount === 0,
    showInvalidState: preview.invalidCount > 0,
    canConfirm: preview.invalidCount === 0 && preview.copyCount > 0,
  }
}

/**
 * Bulk toolbar shift action: Name primary + time secondary.
 * Code is never part of the visible label.
 */
export function formatBulkShiftActionLabel(parts: {
  name: string | null | undefined
  timeRange: string | null | undefined
}): string {
  const name = parts.name?.trim() || '—'
  const time = parts.timeRange?.trim()
  return time ? `${name} · ${time}` : name
}

/** Tooltip/title for bulk shift actions — Code allowed as secondary detail. */
export function formatBulkShiftActionTooltip(parts: {
  name: string | null | undefined
  code: string | null | undefined
  timeRange: string | null | undefined
  overnight: string | null | undefined
}): string {
  return [parts.name?.trim(), parts.code?.trim(), parts.timeRange?.trim(), parts.overnight?.trim()]
    .filter(Boolean)
    .join('\n')
}

export function matchesEmployeeSearch(
  employee: {
    givenName: string
    familyName: string
    personnelNumber: string
  },
  query: string,
): boolean {
  const needle = query.trim().toLocaleLowerCase()
  if (needle === '') {
    return true
  }

  const haystack =
    `${employee.givenName} ${employee.familyName} ${employee.personnelNumber}`.toLocaleLowerCase()
  return haystack.includes(needle)
}

/** Empty string means all departments (property-wide only). */
export function resolveDepartmentFilterValue(
  propertyWide: boolean,
  filterDepartments: ReadonlyArray<{ id: string }>,
  current: string | null,
): string | null {
  if (filterDepartments.length === 1) {
    return filterDepartments[0].id
  }

  if (current !== null && current !== '') {
    if (filterDepartments.some((item) => item.id === current)) {
      return current
    }
  }

  if (propertyWide) {
    return ''
  }

  if (current === '') {
    return null
  }

  return current
}

export function groupEmployeesByDepartment<
  T extends { rowDepartmentId: string | null; rowDepartmentName: string | null },
>(employees: readonly T[]): Array<{ key: string; name: string; employees: T[] }> {
  const groups = new Map<string, { key: string; name: string; employees: T[] }>()

  for (const employee of employees) {
    const key = employee.rowDepartmentId ?? '__none__'
    const name = employee.rowDepartmentName?.trim() || '—'
    const existing = groups.get(key)
    if (existing) {
      existing.employees.push(employee)
    } else {
      groups.set(key, { key, name, employees: [employee] })
    }
  }

  return [...groups.values()].sort((left, right) =>
    left.name.localeCompare(right.name, undefined, { sensitivity: 'base' }),
  )
}
