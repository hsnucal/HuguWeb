import { useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { Button } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { Notice } from '../ui/Notice'
import { SelectField } from '../ui/SelectField'
import { Skeleton } from '../ui/Skeleton'
import { TextField } from '../ui/TextField'
import { AttendanceDayPanel } from './AttendanceDayPanel'
import {
  attendanceCellTooltipText,
  attendanceCellVisible,
} from './attendanceCellDisplay'
import {
  ATTENDANCE_SEARCH_DEBOUNCE_MS,
  attendanceMonthSummary,
  canOpenAttendancePanel,
  currentYearMonth,
  dayNumberFromIso,
  formatPlannedHours,
  isWeekendIsoDate,
  monthOptionLabel,
  resolvePropertyTimeZoneId,
  shiftYearMonth,
  weekdayShort,
  yearOptions,
  type YearMonth,
} from './attendanceMonth'
import { canManageHrAttendance, canReadHrAttendance } from './hrAccess'
import {
  getHrAttendanceMonth,
  hrAttendanceErrorMessage,
  type AttendanceDayLeave,
  type AttendanceDayResult,
  type AttendanceMonthDto,
  type AttendanceMonthEmployee,
} from './hrAttendanceApi'
import { attendanceLeaveCellLabel, attendanceLeaveDetailLabel } from './leaveDisplay'
import { resolveDepartmentFilterValue } from './shiftPlanWeek'
import { workplaceLabelsFromUser } from './workforceWorkplaceLabels'
import { DEFAULT_LANGUAGE, toAppLanguage } from '../i18n/languages'
import styles from './AttendancePage.module.css'

type Selection = {
  employeeId: string
  date: string
}

function findDay(
  employee: AttendanceMonthEmployee,
  date: string,
): AttendanceDayResult | undefined {
  return employee.days.find((item) => item.localDate === date)
}

export function AttendancePage() {
  const { t, i18n } = useTranslation()
  const { user } = useAuthSession()
  const canRead = canReadHrAttendance(user)
  const canManage = canManageHrAttendance(user)
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const workplace = workplaceLabelsFromUser(user)
  const propertyId = user?.propertyId
  const timeZoneId = resolvePropertyTimeZoneId(user)
  const clockMonth = currentYearMonth({ timeZoneId })

  const [month, setMonth] = useState<YearMonth>(clockMonth)
  const [departmentId, setDepartmentId] = useState<string | undefined>(undefined)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [data, setData] = useState<AttendanceMonthDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selection, setSelection] = useState<Selection | null>(null)
  const filterBootstrapped = useRef(false)

  useEffect(() => {
    const handle = window.setTimeout(() => {
      setSearch(searchInput.trim())
    }, ATTENDANCE_SEARCH_DEBOUNCE_MS)
    return () => window.clearTimeout(handle)
  }, [searchInput])

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
        let payload = await getHrAttendanceMonth({
          year: month.year,
          month: month.month,
          departmentId: dept || null,
          search,
        })

        if (!filterBootstrapped.current) {
          filterBootstrapped.current = true
          const resolved = resolveDepartmentFilterValue(
            payload.propertyWide,
            payload.filterDepartments,
            null,
          )
          if (resolved !== null && resolved !== '' && resolved !== dept) {
            dept = resolved
            setDepartmentId(resolved)
            payload = await getHrAttendanceMonth({
              year: month.year,
              month: month.month,
              departmentId: resolved,
              search,
            })
          } else {
            setDepartmentId(resolved ?? '')
          }
        }

        if (!cancelled) {
          setData(payload)
        }
      } catch (reason) {
        if (!cancelled) {
          setError(hrAttendanceErrorMessage(reason, t))
          setData(null)
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
  }, [canRead, departmentId, month.month, month.year, propertyId, search, t])

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape' && selection) {
        setSelection(null)
      }
    }

    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [selection])

  const selectedEmployee = data?.employees.find((item) => item.employeeId === selection?.employeeId) ?? null
  const selectedDay =
    selectedEmployee && selection ? findDay(selectedEmployee, selection.date) ?? null : null

  const summary = useMemo(
    () => attendanceMonthSummary(data?.employees ?? []),
    [data?.employees],
  )

  const years = yearOptions(clockMonth)
  const departmentLocked = (data?.filterDepartments.length ?? 0) === 1
  const dates = data?.dates ?? []
  const dayCount = Math.max(dates.length, 28)
  const gridTemplate = `minmax(11rem, 14rem) repeat(${dayCount}, minmax(2.7rem, 1fr)) repeat(5, minmax(2.3rem, 2.6rem))`

  const needsDepartment =
    data !== null &&
    !data.propertyWide &&
    data.filterDepartments.length > 1 &&
    (departmentId === undefined || departmentId === '')

  function changeMonth(next: YearMonth) {
    filterBootstrapped.current = true
    setSelection(null)
    setMonth(next)
  }

  function changeDepartment(next: string) {
    filterBootstrapped.current = true
    setSelection(null)
    setDepartmentId(next)
  }

  async function reload() {
    const payload = await getHrAttendanceMonth({
      year: month.year,
      month: month.month,
      departmentId: departmentId || null,
      search,
    })
    setData(payload)
  }

  if (!canRead) {
    return <Notice tone="danger">{t('attendance.noAccess')}</Notice>
  }

  if (!propertyId) {
    return <Notice tone="warning">{t('common.propertySelectionRequired')}</Notice>
  }

  if (loading && !data) {
    return <Skeleton variant="list" rows={8} label={t('attendance.loading')} />
  }

  const cellLabels = {
    restDay: t('attendance.cellRest'),
    absent: t('attendance.cellAbsent'),
    unresolved: t('attendance.cellUnresolved'),
    worked: t('attendance.cellWorked'),
    leave: t('attendance.kindLeave'),
    notEmployed: '',
    leaveCell: (leave: AttendanceDayLeave) => attendanceLeaveCellLabel(leave, t),
    leaveTooltip: (leave: AttendanceDayLeave) => attendanceLeaveDetailLabel(leave, t),
    notEmployedTooltip: t('attendance.notEmployedTooltip'),
    unresolvedTooltip: t('attendance.unresolvedTooltip'),
    outOfScopeTooltip: t('attendance.outOfScopeTooltip'),
    leaveFallback: t('attendance.kindLeave'),
  }

  return (
    <div className={styles.page}>
      {workplace.propertyName ? (
        <p className={styles.propertyLabel}>
          {t('attendance.propertyContext', { name: workplace.propertyName })}
        </p>
      ) : null}

      <div className={styles.toolbar}>
        <div className={styles.monthNav} role="group" aria-label={t('attendance.monthNav')}>
          <Button
            variant="secondary"
            size="sm"
            layout="inline"
            aria-label={t('attendance.prevMonth')}
            onClick={() => changeMonth(shiftYearMonth(month, -1))}
          >
            ‹
          </Button>
          <div className={styles.monthFields}>
            <SelectField
              id="attendance-month"
              label={t('attendance.monthLabel')}
              value={String(month.month)}
              onChange={(value) => changeMonth({ year: month.year, month: Number(value) })}
            >
              {Array.from({ length: 12 }, (_, index) => index + 1).map((value) => (
                <option key={value} value={value}>
                  {monthOptionLabel(value, language)}
                </option>
              ))}
            </SelectField>
            <SelectField
              id="attendance-year"
              label={t('attendance.yearLabel')}
              value={String(month.year)}
              onChange={(value) => changeMonth({ year: Number(value), month: month.month })}
            >
              {(years.includes(month.year) ? years : [...years, month.year].sort((left, right) => left - right)).map(
                (year) => (
                  <option key={year} value={year}>
                    {year}
                  </option>
                ),
              )}
            </SelectField>
          </div>
          <Button
            variant="ghost"
            size="sm"
            layout="inline"
            onClick={() => changeMonth(clockMonth)}
          >
            {t('attendance.thisMonth')}
          </Button>
          <Button
            variant="secondary"
            size="sm"
            layout="inline"
            aria-label={t('attendance.nextMonth')}
            onClick={() => changeMonth(shiftYearMonth(month, 1))}
          >
            ›
          </Button>
        </div>

        <div className={styles.filters}>
          <SelectField
            id="attendance-department"
            label={t('attendance.department')}
            value={departmentId ?? ''}
            required={!data?.propertyWide}
            disabled={departmentLocked}
            placeholder={data?.propertyWide ? undefined : t('attendance.selectDepartment')}
            onChange={changeDepartment}
          >
            {data?.propertyWide ? <option value="">{t('attendance.allDepartments')}</option> : null}
            {(data?.filterDepartments ?? []).map((department) => (
              <option key={department.id} value={department.id}>
                {department.name}
              </option>
            ))}
          </SelectField>
          <TextField
            id="attendance-search"
            label={t('attendance.search')}
            value={searchInput}
            onChange={setSearchInput}
            placeholder={t('attendance.searchPlaceholder')}
          />
        </div>
      </div>

      {error ? (
        <Notice tone="danger">
          {error}{' '}
          <Button variant="ghost" size="sm" layout="inline" onClick={() => void reload()}>
            {t('attendance.retry')}
          </Button>
        </Notice>
      ) : null}
      {needsDepartment ? <Notice tone="info">{t('attendance.needsDepartment')}</Notice> : null}

      {data && !needsDepartment ? (
        <ul className={styles.summary}>
          <li>
            <strong>{t('attendance.summaryEmployees', { count: summary.employeeCount })}</strong>
          </li>
          <li>{t('attendance.summaryUnresolved', { count: summary.unresolvedDays })}</li>
          <li>{t('attendance.summaryAbsent', { count: summary.absentDays })}</li>
        </ul>
      ) : null}

      <div className={styles.workspace} data-attendance-grid-layout="full">
        <div className={styles.gridColumn}>
          {!data || needsDepartment ? null : data.employees.length === 0 ? (
            <EmptyState
              title={search ? t('attendance.emptySearch') : t('attendance.empty')}
              description={search ? t('attendance.emptySearchHint') : t('attendance.emptyHint')}
              action={
                error ? (
                  <Button variant="secondary" onClick={() => void reload()}>
                    {t('attendance.retry')}
                  </Button>
                ) : undefined
              }
            />
          ) : (
            <div className={styles.gridScroll}>
              <div
                className={styles.grid}
                role="grid"
                aria-label={t('attendance.title')}
                aria-readonly={!canManage || undefined}
                style={{ gridTemplateColumns: gridTemplate }}
              >
                <div className={`${styles.headCell} ${styles.headCellEmployee}`} role="columnheader">
                  {t('attendance.colEmployee')}
                </div>
                {dates.map((date) => (
                  <div
                    key={date}
                    className={`${styles.headCell} ${isWeekendIsoDate(date) ? styles.headWeekend : ''}`}
                    role="columnheader"
                  >
                    <span className={styles.headDow}>{dayNumberFromIso(date)}</span>
                    <span className={styles.headDate}>{weekdayShort(date, language)}</span>
                  </div>
                ))}
                <div className={`${styles.headCell} ${styles.headCellTotals}`} role="columnheader">
                  {t('attendance.totalsWorked')}
                </div>
                <div className={`${styles.headCell} ${styles.headCellTotals}`} role="columnheader">
                  {t('attendance.totalsLeave')}
                </div>
                <div className={`${styles.headCell} ${styles.headCellTotals}`} role="columnheader">
                  {t('attendance.totalsRest')}
                </div>
                <div className={`${styles.headCell} ${styles.headCellTotals}`} role="columnheader">
                  {t('attendance.totalsAbsent')}
                </div>
                <div className={`${styles.headCell} ${styles.headCellTotals}`} role="columnheader">
                  {t('attendance.totalsUnresolved')}
                </div>

                {data.employees.map((employee) => {
                  const name = `${employee.givenName} ${employee.familyName}`.trim()
                  return (
                    <div key={employee.employeeId} style={{ display: 'contents' }} role="row">
                      <div className={styles.employeeCell} role="rowheader">
                        <span className={styles.employeeName}>{name}</span>
                        {employee.personnelNumber ? (
                          <span className={styles.employeeMeta}>{employee.personnelNumber}</span>
                        ) : null}
                      </div>
                      {dates.map((date) => {
                        const day = findDay(employee, date) ?? {
                          localDate: date,
                          coverage: 'OutOfScope',
                          acceptedKind: null,
                          source: null,
                          isProvisional: false,
                          isManual: false,
                          isUnresolved: false,
                          correctionReason: null,
                          employmentId: null,
                          assignmentId: null,
                          departmentId: null,
                          departmentName: null,
                          schedule: null,
                          leave: null,
                          plannedMinutes: null,
                          acceptedWorkedMinutes: null,
                        }
                        const visible = attendanceCellVisible(day, cellLabels)
                        const interactive = canOpenAttendancePanel(day.coverage)
                        const selectedCell =
                          selection?.employeeId === employee.employeeId && selection.date === date
                        const className = [
                          interactive ? styles.cellButton : styles.cellStatic,
                          visible.tone === 'worked' ? styles.cellWorked : '',
                          visible.tone === 'leave' ? styles.cellLeave : '',
                          visible.tone === 'rest' ? styles.cellRest : '',
                          visible.tone === 'absent' ? styles.cellAbsent : '',
                          visible.tone === 'unresolved' ? styles.cellUnresolved : '',
                          visible.tone === 'notEmployed' || visible.tone === 'outOfScope'
                            ? styles.cellMuted
                            : '',
                          visible.isManual ? styles.cellManual : '',
                          selectedCell ? styles.cellSelected : '',
                        ]
                          .filter(Boolean)
                          .join(' ')
                        const tooltip = attendanceCellTooltipText(day, cellLabels)
                        const provenance =
                          day.source === 'Schedule'
                            ? t('attendance.sourceSchedule')
                            : day.source === 'Manual'
                              ? t('attendance.sourceManual')
                              : day.source === 'Leave'
                                ? t('attendance.sourceLeave')
                                : ''
                        const aria = t('attendance.cellAria', {
                          name,
                          date: `${dayNumberFromIso(date)} ${weekdayShort(date, language)}`,
                          state: [visible.primary || tooltip, provenance].filter(Boolean).join(', '),
                        })

                        return (
                          <div key={`${employee.employeeId}-${date}`} className={styles.dayCell} role="gridcell">
                            {interactive ? (
                              <button
                                type="button"
                                className={className}
                                title={tooltip}
                                aria-label={aria}
                                onClick={() => setSelection({ employeeId: employee.employeeId, date })}
                              >
                                {visible.primary ? <span className={styles.cellLabel}>{visible.primary}</span> : '\u00a0'}
                              </button>
                            ) : (
                              <span className={className} title={tooltip} aria-label={aria}>
                                {visible.primary ? <span className={styles.cellLabel}>{visible.primary}</span> : '\u00a0'}
                              </span>
                            )}
                          </div>
                        )
                      })}
                      <div className={styles.totalsCell}>{employee.totals.workedDays}</div>
                      <div className={styles.totalsCell}>{employee.totals.leaveDays}</div>
                      <div className={styles.totalsCell}>{employee.totals.restDays}</div>
                      <div className={styles.totalsCell}>{employee.totals.absentDays}</div>
                      <div
                        className={styles.totalsCell}
                        title={t('attendance.plannedHours', {
                          hours: formatPlannedHours(employee.totals.plannedMinutes, language),
                        })}
                      >
                        {employee.totals.unresolvedDays}
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>
          )}
        </div>

        {selectedEmployee && selectedDay ? (
          <>
            <button
              type="button"
              className={styles.drawerScrim}
              aria-label={t('attendance.closePanel')}
              onClick={() => setSelection(null)}
            />
            <AttendanceDayPanel
              key={`${selectedEmployee.employeeId}-${selectedDay.localDate}`}
              employee={selectedEmployee}
              day={selectedDay}
              canManage={canManage}
              selectedMonth={month}
              currentMonth={clockMonth}
              onClose={() => setSelection(null)}
              onMutated={reload}
            />
          </>
        ) : null}
      </div>
    </div>
  )
}
