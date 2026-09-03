import type { AppLanguage } from '../i18n/languages.ts'
import type { CurrentUser } from '../shared/types.ts'

export const ATTENDANCE_SEARCH_DEBOUNCE_MS = 300
export const ATTENDANCE_REASON_MAX_LENGTH = 500

export type YearMonth = {
  year: number
  month: number
}

export function resolvePropertyTimeZoneId(user: CurrentUser | null): string | null {
  if (!user?.propertyId) {
    return null
  }

  return user.accessibleProperties?.find((item) => item.id === user.propertyId)?.timeZoneId ?? null
}

export function yearMonthFromTimeZone(timeZoneId: string, now: Date): YearMonth {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: timeZoneId,
    year: 'numeric',
    month: 'numeric',
  }).formatToParts(now)
  const year = Number(parts.find((part) => part.type === 'year')?.value)
  const month = Number(parts.find((part) => part.type === 'month')?.value)
  if (!Number.isInteger(year) || !Number.isInteger(month) || month < 1 || month > 12) {
    throw new RangeError(`Invalid calendar parts for time zone ${timeZoneId}`)
  }

  return { year, month }
}

export function currentYearMonth(options: { timeZoneId: string | null; now?: Date }): YearMonth {
  const now = options.now ?? new Date()
  if (options.timeZoneId) {
    try {
      return yearMonthFromTimeZone(options.timeZoneId, now)
    } catch {
      // Fall through to UTC calendar of the same instant.
    }
  }

  return { year: now.getUTCFullYear(), month: now.getUTCMonth() + 1 }
}

export function shiftYearMonth(value: YearMonth, deltaMonths: number): YearMonth {
  const index = value.year * 12 + (value.month - 1) + deltaMonths
  const year = Math.floor(index / 12)
  const month = (index % 12) + 1
  return { year, month }
}

export function compareYearMonth(left: YearMonth, right: YearMonth): number {
  if (left.year !== right.year) {
    return left.year - right.year
  }

  return left.month - right.month
}

export function isPastYearMonth(selected: YearMonth, current: YearMonth): boolean {
  return compareYearMonth(selected, current) < 0
}

export function yearMonthKey(value: YearMonth): string {
  return `${value.year}-${String(value.month).padStart(2, '0')}`
}

export function weekdayFromIsoDate(isoDate: string): number {
  const [year, month, day] = isoDate.split('-').map(Number)
  if (!year || !month || !day) {
    return 0
  }

  return new Date(year, month - 1, day).getDay()
}

export function isWeekendIsoDate(isoDate: string): boolean {
  const weekday = weekdayFromIsoDate(isoDate)
  return weekday === 0 || weekday === 6
}

export function weekdayShort(isoDate: string, language: AppLanguage): string {
  const [year, month, day] = isoDate.split('-').map(Number)
  if (!year || !month || !day) {
    return isoDate
  }

  return new Intl.DateTimeFormat(language, { weekday: 'short' }).format(new Date(year, month - 1, day))
}

export function dayNumberFromIso(isoDate: string): string {
  const day = isoDate.split('-')[2]
  return day ? String(Number(day)) : isoDate
}

export function monthName(year: number, month: number, language: AppLanguage): string {
  return new Intl.DateTimeFormat(language, { month: 'long', year: 'numeric' }).format(
    new Date(year, month - 1, 1),
  )
}

export function monthOptionLabel(month: number, language: AppLanguage): string {
  return new Intl.DateTimeFormat(language, { month: 'long' }).format(new Date(2026, month - 1, 1))
}

export function yearOptions(current: YearMonth, spanBefore = 4, spanAfter = 1): number[] {
  const years: number[] = []
  for (let year = current.year - spanBefore; year <= current.year + spanAfter; year += 1) {
    years.push(year)
  }

  if (!years.includes(current.year)) {
    years.push(current.year)
    years.sort((left, right) => left - right)
  }

  return years
}

export function formatPlannedHours(minutes: number, language: AppLanguage): string {
  const hours = Math.max(0, minutes) / 60
  return new Intl.NumberFormat(language, { maximumFractionDigits: 1 }).format(hours)
}

export function attendanceMonthSummary(
  employees: ReadonlyArray<{ totals: { unresolvedDays: number; absentDays: number } }>,
): {
  employeeCount: number
  unresolvedDays: number
  absentDays: number
} {
  return {
    employeeCount: employees.length,
    unresolvedDays: employees.reduce((total, row) => total + row.totals.unresolvedDays, 0),
    absentDays: employees.reduce((total, row) => total + row.totals.absentDays, 0),
  }
}

export function canOpenAttendancePanel(coverage: string | null | undefined): boolean {
  return coverage === 'InEmployment'
}

export function canShowAttendanceCorrectionForm(
  canManage: boolean,
  coverage: string | null | undefined,
): boolean {
  return canManage && coverage === 'InEmployment'
}

export function canClearAttendanceCorrection(
  canManage: boolean,
  day: { coverage: string; isManual: boolean },
): boolean {
  return canManage && day.coverage === 'InEmployment' && day.isManual
}

export function shouldShowPastMonthWarning(options: {
  canManage: boolean
  formVisible: boolean
  selected: YearMonth
  current: YearMonth
}): boolean {
  return options.canManage && options.formVisible && isPastYearMonth(options.selected, options.current)
}

export function validateAttendanceReason(reason: string): 'required' | 'tooLong' | null {
  const trimmed = reason.trim()
  if (!trimmed) {
    return 'required'
  }

  if (trimmed.length > ATTENDANCE_REASON_MAX_LENGTH) {
    return 'tooLong'
  }

  return null
}
