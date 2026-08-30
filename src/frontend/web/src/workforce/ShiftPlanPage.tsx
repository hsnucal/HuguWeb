import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { formatDateOnly } from '../i18n/format'
import { DEFAULT_LANGUAGE, toAppLanguage } from '../i18n/languages'
import { Button } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { Notice } from '../ui/Notice'
import { placeAnchoredMenu } from '../ui/placeAnchoredMenu'
import { SelectField } from '../ui/SelectField'
import { Skeleton } from '../ui/Skeleton'
import { TextField } from '../ui/TextField'
import { WorkspaceDialog } from '../ui/WorkspaceDialog'
import { formatShiftClockRange, splitNetDuration, compareShiftDefinitionsByStart } from './shiftDefinitionForm'
import {
  listHrShiftDefinitions,
  bulkHrSchedule,
  clearHrEmployeeSchedule,
  copyHrScheduleWeek,
  getHrScheduleWeek,
  hrScheduleErrorKey,
  previewCopyHrScheduleWeek,
  upsertHrEmployeeSchedule,
  type BulkScheduleOperationInput,
  type CopyScheduleWeekPreview,
  type ScheduleWeekCell,
  type ScheduleWeekDto,
  type ScheduleWeekEmployee,
  type ScheduleWeekShiftDefinition,
  type ShiftDefinitionRecord,
} from './hrScheduleApi'
import { canManageHrSchedule, canReadHrSchedule, canReadHrShiftDefinitions } from './hrAccess'
import {
  cellCompactLabel,
  cellSelectionKey,
  cellVisibleContent,
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
  parseCellSelectionKey,
  resolveDepartmentFilterValue,
  selectionHasOverwrite,
  shiftWeekStart,
  toggleCellSelection,
} from './shiftPlanWeek'
import styles from './ShiftPlan.module.css'

type CellMenuState = {
  employeeId: string
  date: string
  anchorKey: string
}

type PendingBulkAction =
  | { type: 'shift'; shiftDefinitionId: string }
  | { type: 'rest' }
  | { type: 'clear' }

const WEEKDAY_KEYS = [
  'workforce.shiftPlanWeekdayMon',
  'workforce.shiftPlanWeekdayTue',
  'workforce.shiftPlanWeekdayWed',
  'workforce.shiftPlanWeekdayThu',
  'workforce.shiftPlanWeekdayFri',
  'workforce.shiftPlanWeekdaySat',
  'workforce.shiftPlanWeekdaySun',
] as const

function findCell(
  week: ScheduleWeekDto,
  employeeId: string,
  date: string,
): ScheduleWeekCell | null {
  const employee = week.employees.find((item) => item.employeeId === employeeId)
  return employee?.cells.find((cell) => cell.date === date) ?? null
}

function activeDefinitions(definitions: ReadonlyArray<ScheduleWeekShiftDefinition | ShiftDefinitionRecord>) {
  return definitions.filter((item) => item.isActive)
}

function formatNetLabel(
  plannedNetMinutes: number | null | undefined,
  t: (key: string, options?: Record<string, unknown>) => string,
): string | null {
  if (plannedNetMinutes == null) {
    return null
  }
  const { hours, minutes } = splitNetDuration(plannedNetMinutes)
  const duration =
    minutes === 0
      ? t('workforce.shiftNetHours', { hours })
      : t('workforce.shiftNetHoursMinutes', { hours, minutes })
  return `${t('workforce.shiftPlanNetPrefix')}: ${duration}`
}

function formatBreakLabel(
  breakMinutes: number | null | undefined,
  t: (key: string, options?: Record<string, unknown>) => string,
): string | null {
  if (breakMinutes == null) {
    return null
  }
  return `${t('workforce.shiftPlanBreakPrefix')}: ${t('workforce.shiftBreakValue', { minutes: breakMinutes })}`
}

function cellTooltip(
  cell: ScheduleWeekCell,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  if (cell.eligibility === 'OutOfScope') {
    return t('workforce.shiftPlanOutOfScopeDetail')
  }
  if (cell.eligibility === 'NotEmployed') {
    return t('workforce.shiftPlanNotEmployed')
  }
  if (cell.state === 'Shift') {
    const range =
      cell.startLocalTime && cell.endLocalTime
        ? formatShiftClockRange(cell.startLocalTime, cell.endLocalTime)
        : ''
    return formatScheduledCellDetail({
      name: cell.shiftName,
      code: cell.shiftCode,
      timeRange: range,
      overnight: cell.endsNextDay ? t('workforce.endsNextDayShort') : null,
      breakLabel: formatBreakLabel(cell.breakMinutes, t),
      netLabel: formatNetLabel(cell.plannedNetMinutes, t),
      note: cell.note,
      inactive: cell.shiftIsActive === false ? t('workforce.inactive') : null,
    })
  }
  if (cell.state === 'RestDay') {
    return [t('workforce.shiftPlanRestDay'), cell.note].filter(Boolean).join(' · ')
  }
  return t('workforce.shiftPlanUnscheduled')
}

function cellAriaLabel(
  employee: ScheduleWeekEmployee,
  cell: ScheduleWeekCell,
  label: string,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  let state = label
  if (cell.eligibility === 'OutOfScope') {
    state = t('workforce.shiftPlanOutOfScope')
  } else if (cell.eligibility === 'NotEmployed') {
    state = t('workforce.shiftPlanNotEmployed')
  } else if (cell.state === 'Shift') {
    state = cell.shiftName?.trim() || label || t('workforce.shiftPlanMutedCell')
  } else if (cell.state === 'RestDay') {
    state = t('workforce.shiftPlanRestDay')
  } else if (cell.state === 'Unscheduled' || cell.state === null) {
    state = t('workforce.shiftPlanUnscheduled')
  } else if (!state) {
    state = t('workforce.shiftPlanMutedCell')
  }

  return t('workforce.shiftPlanCellAria', {
    name: `${employee.givenName} ${employee.familyName}`.trim(),
    date: cell.date,
    state,
  })
}

function placeCellAssignMenu(trigger: HTMLElement, menu: HTMLElement) {
  const rect = trigger.getBoundingClientRect()
  const rem = Number.parseFloat(getComputedStyle(document.documentElement).fontSize) || 16
  const gap = 6
  const pad = 8
  const spaceBelow = Math.max(window.innerHeight - rect.bottom - gap - pad, 0)
  const spaceAbove = Math.max(rect.top - gap - pad, 0)
  const maxHeight = Math.max(Math.min(Math.max(spaceBelow, spaceAbove), window.innerHeight * 0.5), 8 * rem)
  const preferredWidth = Math.max(14 * rem, rect.width)
  const heightForPlacement = Math.min(menu.scrollHeight || menu.offsetHeight, maxHeight)
  const coords = placeAnchoredMenu(
    rect,
    { width: preferredWidth, height: heightForPlacement },
    { width: window.innerWidth, height: window.innerHeight },
    gap,
    pad,
  )
  return { ...coords, maxHeight }
}

export function ShiftPlanPage() {
  const { t, i18n } = useTranslation()
  const { user } = useAuthSession()
  const canRead = canReadHrSchedule(user)
  const canManage = canManageHrSchedule(user)
  const canReadDefinitions = canReadHrShiftDefinitions(user)
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE

  const [weekStart, setWeekStart] = useState(currentWeekStart)
  /** undefined = not initialized; '' = all departments; id = filter */
  const [departmentId, setDepartmentId] = useState<string | undefined>(undefined)
  const [week, setWeek] = useState<ScheduleWeekDto | null>(null)
  const [catalogShifts, setCatalogShifts] = useState<ShiftDefinitionRecord[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [query, setQuery] = useState('')
  const [bulkMode, setBulkMode] = useState(false)
  const [selected, setSelected] = useState<Set<string>>(() => new Set())
  const [menu, setMenu] = useState<CellMenuState | null>(null)
  const [pendingOverwrite, setPendingOverwrite] = useState<{
    action: PendingBulkAction
    keys: string[]
    overwriteCount: number
  } | null>(null)
  const [copyPreview, setCopyPreview] = useState<CopyScheduleWeekPreview | null>(null)
  const [copyLoading, setCopyLoading] = useState(false)
  const [busy, setBusy] = useState(false)
  const [menuCoords, setMenuCoords] = useState({ top: 0, left: 0, width: 0, maxHeight: 280 })
  const menuRef = useRef<HTMLDivElement>(null)
  const menuAnchorRef = useRef<HTMLButtonElement | null>(null)
  const filterBootstrapped = useRef(false)
  const propertyId = user?.propertyId

  useEffect(() => {
    if (!canRead || !propertyId) {
      return
    }

    let cancelled = false

    async function loadPage() {
      setLoading(true)
      setError(null)
      try {
        let dept = departmentId
        let data = await getHrScheduleWeek(weekStart, dept || null)

        if (!filterBootstrapped.current) {
          filterBootstrapped.current = true
          const resolved = resolveDepartmentFilterValue(
            data.propertyWide,
            data.filterDepartments,
            null,
          )
          if (resolved !== null && resolved !== '' && resolved !== dept) {
            dept = resolved
            setDepartmentId(resolved)
            data = await getHrScheduleWeek(weekStart, resolved)
          } else {
            setDepartmentId(resolved ?? '')
          }
        }

        if (!cancelled) {
          setWeek(data)
          setSelected(new Set())
          setMenu(null)
          menuAnchorRef.current = null
          setPendingOverwrite(null)
        }
      } catch (reason) {
        if (!cancelled) {
          setError(t(hrScheduleErrorKey(reason)))
          setWeek(null)
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void loadPage()
    return () => {
      cancelled = true
    }
  }, [canRead, weekStart, departmentId, propertyId, t])

  useEffect(() => {
    if (!propertyId || !canManage || !canReadDefinitions) {
      return
    }

    let cancelled = false

    async function loadDefinitions() {
      try {
        const rows = await listHrShiftDefinitions(true)
        if (!cancelled) {
          setCatalogShifts(rows)
        }
      } catch {
        if (!cancelled) {
          setCatalogShifts([])
        }
      }
    }

    void loadDefinitions()
    return () => {
      cancelled = true
    }
  }, [canManage, canReadDefinitions, propertyId])

  useLayoutEffect(() => {
    if (!menu) {
      return
    }

    const trigger = menuAnchorRef.current
    const menuEl = menuRef.current
    if (!trigger || !menuEl) {
      return
    }

    setMenuCoords(placeCellAssignMenu(trigger, menuEl))
    const firstItem = menuEl.querySelector<HTMLElement>('[role="menuitem"]')
    firstItem?.focus({ preventScroll: true })
  }, [menu, catalogShifts.length])

  useEffect(() => {
    if (!menu) {
      return
    }

    function closeMenu() {
      setMenu(null)
      menuAnchorRef.current = null
    }

    function onPointerDown(event: MouseEvent) {
      const target = event.target as Node
      if (menuRef.current?.contains(target) || menuAnchorRef.current?.contains(target)) {
        return
      }
      closeMenu()
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        event.preventDefault()
        closeMenu()
        menuAnchorRef.current?.focus()
      }
    }

    // Preferred MVP: close on scroll/resize so the menu never detaches from its cell.
    function onViewportChange() {
      closeMenu()
    }

    document.addEventListener('mousedown', onPointerDown)
    document.addEventListener('keydown', onKeyDown, true)
    window.addEventListener('resize', onViewportChange)
    window.addEventListener('scroll', onViewportChange, true)
    return () => {
      document.removeEventListener('mousedown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown, true)
      window.removeEventListener('resize', onViewportChange)
      window.removeEventListener('scroll', onViewportChange, true)
    }
  }, [menu])

  const visibleEmployees = useMemo(() => {
    const rows = week?.employees ?? []
    return rows.filter((employee) => matchesEmployeeSearch(employee, query))
  }, [week, query])

  const showDepartmentGroups = Boolean(week && week.propertyWide && departmentId === '')
  const grouped = useMemo(() => {
    if (!showDepartmentGroups) {
      return [{ key: 'all', name: '', employees: visibleEmployees }]
    }
    return groupEmployeesByDepartment(visibleEmployees)
  }, [showDepartmentGroups, visibleEmployees])

  const assignableShifts = useMemo(() => {
    const source = catalogShifts.length > 0 ? catalogShifts : (week?.shiftDefinitions ?? [])
    return activeDefinitions(source).slice().sort(compareShiftDefinitionsByStart)
  }, [catalogShifts, week?.shiftDefinitions])

  async function reload() {
    if (!propertyId) {
      return
    }
    const data = await getHrScheduleWeek(weekStart, departmentId || null)
    setWeek(data)
  }

  function changeWeekStart(next: string) {
    filterBootstrapped.current = true
    setSelected(new Set())
    setMenu(null)
    menuAnchorRef.current = null
    setPendingOverwrite(null)
    setWeekStart(next)
  }

  function changeDepartmentId(next: string) {
    filterBootstrapped.current = true
    setSelected(new Set())
    setMenu(null)
    menuAnchorRef.current = null
    setPendingOverwrite(null)
    setDepartmentId(next)
  }

  async function applyOperations(operations: BulkScheduleOperationInput[]) {
    if (operations.length === 0) {
      return
    }

    setBusy(true)
    setError(null)
    try {
      if (operations.length === 1) {
        const op = operations[0]
        if (op.clear) {
          await clearHrEmployeeSchedule(op.employeeId, op.date)
        } else {
          await upsertHrEmployeeSchedule(op.employeeId, op.date, {
            kind: op.kind!,
            shiftDefinitionId: op.shiftDefinitionId,
            note: op.note,
          })
        }
      } else {
        await bulkHrSchedule(operations)
      }
      setSelected(new Set())
      setMenu(null)
      menuAnchorRef.current = null
      setPendingOverwrite(null)
      await reload()
    } catch (reason) {
      setError(t(hrScheduleErrorKey(reason)))
    } finally {
      setBusy(false)
    }
  }

  function buildOperations(action: PendingBulkAction, keys: string[]): BulkScheduleOperationInput[] {
    return keys.map((key) => {
      const { employeeId, date } = parseCellSelectionKey(key)
      if (action.type === 'clear') {
        return { employeeId, date, clear: true }
      }
      if (action.type === 'rest') {
        return { employeeId, date, clear: false, kind: 'RestDay', shiftDefinitionId: null }
      }
      return {
        employeeId,
        date,
        clear: false,
        kind: 'Shift',
        shiftDefinitionId: action.shiftDefinitionId,
      }
    })
  }

  function requestAction(action: PendingBulkAction, keys: string[]) {
    if (!week || keys.length === 0) {
      return
    }

    const cells = keys
      .map((key) => {
        const { employeeId, date } = parseCellSelectionKey(key)
        return findCell(week, employeeId, date)
      })
      .filter((cell): cell is ScheduleWeekCell => Boolean(cell))

    if (selectionHasOverwrite(cells) && action.type !== 'clear') {
      setMenu(null)
      menuAnchorRef.current = null
      setPendingOverwrite({
        action,
        keys,
        overwriteCount: countOverwriteCells(cells),
      })
      return
    }

    void applyOperations(buildOperations(action, keys))
  }

  function onCellActivate(
    employee: ScheduleWeekEmployee,
    cell: ScheduleWeekCell,
    anchor: HTMLButtonElement,
  ) {
    if (!canManage || !isCellEditable(cell.eligibility) || busy) {
      return
    }

    const key = cellSelectionKey(employee.employeeId, cell.date)
    if (bulkMode) {
      setSelected((current) => toggleCellSelection(current, key))
      setMenu(null)
      menuAnchorRef.current = null
      return
    }

    if (menu?.anchorKey === key) {
      setMenu(null)
      menuAnchorRef.current = null
      return
    }

    menuAnchorRef.current = anchor
    const rect = anchor.getBoundingClientRect()
    const rem = Number.parseFloat(getComputedStyle(document.documentElement).fontSize) || 16
    setMenuCoords({
      top: rect.bottom + 6,
      left: rect.left,
      width: Math.max(14 * rem, rect.width),
      maxHeight: Math.min(window.innerHeight * 0.5, 18 * rem),
    })
    setMenu({
      employeeId: employee.employeeId,
      date: cell.date,
      anchorKey: key,
    })
  }

  async function onCopyPreview() {
    if (!canManage || departmentId === undefined) {
      return
    }

    setCopyLoading(true)
    setError(null)
    try {
      const preview = await previewCopyHrScheduleWeek({
        targetWeekStart: weekStart,
        departmentId: departmentId || null,
      })
      setCopyPreview(preview)
    } catch (reason) {
      setError(t(hrScheduleErrorKey(reason)))
    } finally {
      setCopyLoading(false)
    }
  }

  async function onCopyConfirm() {
    if (!copyPreview || copyPreview.invalidCount > 0) {
      return
    }

    setBusy(true)
    setError(null)
    try {
      await copyHrScheduleWeek({
        targetWeekStart: weekStart,
        departmentId: departmentId || null,
      })
      setCopyPreview(null)
      await reload()
    } catch (reason) {
      setError(t(hrScheduleErrorKey(reason)))
    } finally {
      setBusy(false)
    }
  }

  if (!canRead) {
    return <Notice tone="danger">{t('workforce.noAccess')}</Notice>
  }

  if (!propertyId) {
    return <Notice tone="warning">{t('common.propertySelectionRequired')}</Notice>
  }

  if (loading && !week) {
    return <Skeleton variant="list" rows={8} label={t('workforce.loading')} />
  }

  const dates = week?.dates ?? []
  const needsDepartment =
    week !== null &&
    !week.propertyWide &&
    week.filterDepartments.length > 1 &&
    (departmentId === undefined || departmentId === null || departmentId === '')

  const selectionKeys = [...selected].filter((key) => {
    if (!week) {
      return false
    }
    const { employeeId, date } = parseCellSelectionKey(key)
    const cell = findCell(week, employeeId, date)
    return cell ? isCellEditable(cell.eligibility) : false
  })

  return (
    <div className={styles.page}>
      <div className={styles.toolbar}>
        <div className={styles.weekNav} role="group" aria-label={t('workforce.shiftPlanWeekNav')}>
          <Button
            variant="secondary"
            size="sm"
            layout="inline"
            onClick={() => changeWeekStart(shiftWeekStart(weekStart, -1))}
            aria-label={t('workforce.shiftPlanPrevWeek')}
          >
            ‹
          </Button>
          <Button
            variant="ghost"
            size="sm"
            layout="inline"
            onClick={() => changeWeekStart(currentWeekStart())}
          >
            {t('workforce.shiftPlanThisWeek')}
          </Button>
          <Button
            variant="secondary"
            size="sm"
            layout="inline"
            onClick={() => changeWeekStart(shiftWeekStart(weekStart, 1))}
            aria-label={t('workforce.shiftPlanNextWeek')}
          >
            ›
          </Button>
          <span className={styles.weekLabel}>
            {week
              ? `${formatDateOnly(week.weekStart, language)} – ${formatDateOnly(week.weekEnd, language)}`
              : formatDateOnly(weekStart, language)}
          </span>
        </div>

        <div className={styles.filters}>
          <SelectField
            id="shift-plan-department"
            label={t('workforce.department')}
            value={departmentId ?? ''}
            required={!week?.propertyWide}
            placeholder={
              week?.propertyWide ? undefined : t('workforce.selectDepartment')
            }
            onChange={(value) => {
              changeDepartmentId(value)
            }}
          >
            {week?.propertyWide ? (
              <option value="">{t('workforce.allDepartments')}</option>
            ) : null}
            {(week?.filterDepartments ?? []).map((department) => (
              <option key={department.id} value={department.id}>
                {department.name}
              </option>
            ))}
          </SelectField>
          <TextField
            id="shift-plan-search"
            label={t('workforce.search')}
            value={query}
            onChange={setQuery}
            placeholder={t('workforce.searchPlaceholder')}
          />
          <div className={styles.toolbarActions}>
            {canManage ? (
              <>
                <Button
                  variant={bulkMode ? 'primary' : 'secondary'}
                  size="sm"
                  layout="inline"
                  onClick={() => {
                    setBulkMode((value) => !value)
                    setMenu(null)
                    menuAnchorRef.current = null
                    if (bulkMode) {
                      setSelected(new Set())
                    }
                  }}
                >
                  {t('workforce.shiftPlanBulkMode')}
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  layout="inline"
                  disabled={copyLoading || busy || departmentId === undefined || needsDepartment}
                  onClick={() => void onCopyPreview()}
                >
                  {t('workforce.shiftPlanCopyPreviousWeek')}
                </Button>
              </>
            ) : (
              <p className={styles.readOnlyHint}>{t('workforce.shiftPlanReadOnly')}</p>
            )}
          </div>
        </div>
      </div>

      {error ? <Notice tone="danger">{error}</Notice> : null}
      {needsDepartment ? (
        <Notice tone="info">{t('workforce.shiftPlanSelectDepartment')}</Notice>
      ) : null}

      {canManage && selectionKeys.length > 0 ? (
        <div className={styles.bulkBar} role="region" aria-label={t('workforce.shiftPlanBulkBar')}>
          <span className={styles.bulkCount}>
            {t('workforce.shiftPlanSelectedCount', { count: selectionKeys.length })}
          </span>
          <div className={styles.bulkActions}>
            {assignableShifts.map((shift) => {
              const timeRange = formatShiftClockRange(shift.startLocalTime, shift.endLocalTime)
              const label = formatBulkShiftActionLabel({ name: shift.name, timeRange })
              const title = formatBulkShiftActionTooltip({
                name: shift.name,
                code: shift.code,
                timeRange,
                overnight: shift.endsNextDay ? t('workforce.endsNextDayShort') : null,
              })
              return (
                <Button
                  key={shift.id}
                  variant="secondary"
                  size="sm"
                  layout="inline"
                  disabled={busy}
                  title={title}
                  aria-label={label}
                  onClick={() =>
                    requestAction({ type: 'shift', shiftDefinitionId: shift.id }, selectionKeys)
                  }
                >
                  {label}
                </Button>
              )
            })}
            <Button
              variant="secondary"
              size="sm"
              layout="inline"
              disabled={busy}
              onClick={() => requestAction({ type: 'rest' }, selectionKeys)}
            >
              {t('workforce.shiftPlanRestDay')}
            </Button>
            <Button
              variant="ghost"
              size="sm"
              layout="inline"
              disabled={busy}
              onClick={() => requestAction({ type: 'clear' }, selectionKeys)}
            >
              {t('workforce.shiftPlanClear')}
            </Button>
            <Button
              variant="ghost"
              size="sm"
              layout="inline"
              onClick={() => setSelected(new Set())}
            >
              {t('workforce.shiftPlanClearSelection')}
            </Button>
          </div>
        </div>
      ) : null}

      {!week || needsDepartment ? null : visibleEmployees.length === 0 ? (
        <EmptyState
          title={query.trim() ? t('workforce.emptySearch') : t('workforce.shiftPlanEmpty')}
          description={
            query.trim() ? t('workforce.emptySearchHint') : t('workforce.shiftPlanEmptyHint')
          }
        />
      ) : (
        <div className={styles.gridScroll}>
          <div
            className={styles.grid}
            role="grid"
            aria-label={t('workforce.shiftPlan')}
            aria-readonly={!canManage || undefined}
          >
            <div className={`${styles.headCell} ${styles.headCellEmployee}`} role="columnheader">
              {t('workforce.fullName')}
            </div>
            {dates.map((date, index) => (
              <div key={date} className={styles.headCell} role="columnheader">
                <span className={styles.headDow}>{t(WEEKDAY_KEYS[index])}</span>
                <span className={styles.headDate}>{formatDateOnly(date, language)}</span>
              </div>
            ))}

            {grouped.map((group) => (
              <div key={group.key} style={{ display: 'contents' }}>
                {showDepartmentGroups ? (
                  <div className={styles.groupRow} role="rowheader">
                    {group.name}
                  </div>
                ) : null}
                {group.employees.map((employee) => (
                  <div key={employee.employeeId} style={{ display: 'contents' }} role="row">
                    <div className={styles.employeeCell} role="rowheader">
                      <span className={styles.employeeName}>
                        {employee.givenName} {employee.familyName}
                      </span>
                      <span className={styles.employeeMeta}>{employee.personnelNumber}</span>
                    </div>
                    {dates.map((date) => {
                      const cell =
                        employee.cells.find((item) => item.date === date) ??
                        ({
                          date,
                          eligibility: 'OutOfScope',
                          state: null,
                        } as ScheduleWeekCell)
                      const editable = isCellEditable(cell.eligibility)
                      const interactive = canManage && editable && !busy
                      const key = cellSelectionKey(employee.employeeId, cell.date)
                      const selectedCell = selected.has(key)
                      const labels = {
                        restDay: t('workforce.shiftPlanRestDayShort'),
                        unscheduled: t('workforce.shiftPlanUnscheduledShort'),
                        muted: t('workforce.shiftPlanMutedCell'),
                      }
                      const visible = cellVisibleContent(cell, labels, formatShiftClockRange)
                      const label = cellCompactLabel(cell, labels)
                      const cellClass = [
                        styles.cellButton,
                        cell.eligibility !== 'Editable' ? styles.cellMuted : '',
                        cell.state === 'Shift' ? styles.cellShift : '',
                        cell.state === 'RestDay' ? styles.cellRest : '',
                        cell.state === 'Unscheduled' || cell.state === null
                          ? styles.cellEmpty
                          : '',
                        selectedCell ? styles.cellSelected : '',
                      ]
                        .filter(Boolean)
                        .join(' ')

                      return (
                        <div key={key} className={styles.dayCell} role="gridcell">
                          <button
                            type="button"
                            className={cellClass}
                            disabled={!editable}
                            aria-disabled={!interactive || undefined}
                            tabIndex={editable ? 0 : -1}
                            title={cellTooltip(cell, t)}
                            aria-label={cellAriaLabel(
                              employee,
                              cell,
                              visible.secondary
                                ? `${visible.primary}, ${visible.secondary}`
                                : label,
                              t,
                            )}
                            aria-haspopup={interactive && !bulkMode ? 'menu' : undefined}
                            aria-expanded={
                              interactive && !bulkMode ? menu?.anchorKey === key : undefined
                            }
                            aria-pressed={bulkMode && interactive ? selectedCell : undefined}
                            onClick={(event) => {
                              if (!interactive) {
                                return
                              }
                              onCellActivate(employee, cell, event.currentTarget)
                            }}
                          >
                            <span className={styles.cellPrimary}>{visible.primary || '\u00a0'}</span>
                            {visible.secondary ? (
                              <span className={styles.cellSecondary}>{visible.secondary}</span>
                            ) : null}
                          </button>
                        </div>
                      )
                    })}
                  </div>
                ))}
              </div>
            ))}
          </div>
        </div>
      )}

      {menu
        ? createPortal(
            <div
              ref={menuRef}
              className={styles.cellMenu}
              role="menu"
              aria-label={t('workforce.shiftPlanAssignMenu')}
              style={{
                top: menuCoords.top,
                left: menuCoords.left,
                width: menuCoords.width || undefined,
                maxHeight: menuCoords.maxHeight,
              }}
            >
              {assignableShifts.map((shift) => {
                const timeRange = formatShiftClockRange(shift.startLocalTime, shift.endLocalTime)
                const meta = formatShiftAssignMeta({
                  code: shift.code,
                  timeRange,
                  overnight: shift.endsNextDay ? t('workforce.endsNextDayShort') : null,
                })
                return (
                  <button
                    key={shift.id}
                    type="button"
                    className={styles.cellMenuItem}
                    role="menuitem"
                    onClick={() =>
                      requestAction({ type: 'shift', shiftDefinitionId: shift.id }, [
                        menu.anchorKey,
                      ])
                    }
                  >
                    <span className={styles.cellMenuTitle}>{shift.name}</span>
                    {meta ? <span className={styles.cellMenuMeta}>{meta}</span> : null}
                  </button>
                )
              })}
              <button
                type="button"
                className={styles.cellMenuItem}
                role="menuitem"
                onClick={() => requestAction({ type: 'rest' }, [menu.anchorKey])}
              >
                {t('workforce.shiftPlanRestDay')}
              </button>
              <button
                type="button"
                className={`${styles.cellMenuItem} ${styles.cellMenuItemDanger}`}
                role="menuitem"
                onClick={() => requestAction({ type: 'clear' }, [menu.anchorKey])}
              >
                {t('workforce.shiftPlanClear')}
              </button>
            </div>,
            document.body,
          )
        : null}

      {pendingOverwrite ? (
        <WorkspaceDialog
          title={t('workforce.shiftPlanOverwriteTitle')}
          subtitle={t('workforce.shiftPlanOverwriteBody', {
            selected: pendingOverwrite.keys.length,
            count: pendingOverwrite.overwriteCount,
          })}
          size="confirm"
          onRequestClose={() => setPendingOverwrite(null)}
          footer={
            <>
              <Button variant="ghost" onClick={() => setPendingOverwrite(null)}>
                {t('workforce.cancel')}
              </Button>
              <Button
                variant="primary"
                layout="inline"
                onClick={() => {
                  const { action, keys } = pendingOverwrite
                  setPendingOverwrite(null)
                  void applyOperations(buildOperations(action, keys))
                }}
              >
                {t('workforce.shiftPlanOverwriteConfirm')}
              </Button>
            </>
          }
        >
          <p className={styles.muted}>{t('workforce.shiftPlanOverwriteHint')}</p>
        </WorkspaceDialog>
      ) : null}

      {copyPreview ? (
        <WorkspaceDialog
          title={t('workforce.shiftPlanCopyTitle')}
          subtitle={t('workforce.shiftPlanCopySubtitle', {
            source: `${formatDateOnly(copyPreview.sourceWeekStart, language)} – ${formatDateOnly(copyPreview.sourceWeekEnd, language)}`,
            target: `${formatDateOnly(copyPreview.targetWeekStart, language)} – ${formatDateOnly(copyPreview.targetWeekEnd, language)}`,
          })}
          size="confirm"
          onRequestClose={() => setCopyPreview(null)}
          footer={
            <>
              <Button variant="ghost" layout="inline" onClick={() => setCopyPreview(null)}>
                {t('workforce.cancel')}
              </Button>
              <Button
                variant="primary"
                layout="inline"
                disabled={
                  busy || !copyWeekDialogState(copyPreview).canConfirm
                }
                onClick={() => void onCopyConfirm()}
              >
                {t('workforce.shiftPlanCopyConfirm')}
              </Button>
            </>
          }
        >
          <div className={styles.copySummary}>
            <ul className={styles.copyStats}>
              <li>{t('workforce.shiftPlanCopyCount', { count: copyPreview.copyCount })}</li>
              <li>
                {t('workforce.shiftPlanOverwriteCount', { count: copyPreview.overwriteCount })}
              </li>
              <li>{t('workforce.shiftPlanInvalidCount', { count: copyPreview.invalidCount })}</li>
            </ul>
            {copyWeekDialogState(copyPreview).showOverwriteWarning ? (
              <Notice tone="warning">
                {t('workforce.shiftPlanCopyOverwriteWarning', {
                  count: copyPreview.overwriteCount,
                })}
              </Notice>
            ) : null}
            {copyWeekDialogState(copyPreview).showInvalidState ? (
              <Notice tone="danger">{t('workforce.shiftPlanCopyBlocked')}</Notice>
            ) : null}
            {copyPreview.invalid.length > 0 ? (
              <ul className={styles.copyList}>
                {copyPreview.invalid.slice(0, 20).map((item) => (
                  <li key={`${item.employeeId}-${item.targetDate}`}>
                    {item.givenName} {item.familyName} · {item.targetDate}: {item.detail}
                  </li>
                ))}
              </ul>
            ) : null}
            {copyPreview.copyCount === 0 && copyPreview.invalidCount === 0 ? (
              <p className={styles.muted}>{t('workforce.scheduleErrors.copyWeekEmpty')}</p>
            ) : null}
          </div>
        </WorkspaceDialog>
      ) : null}
    </div>
  )
}
