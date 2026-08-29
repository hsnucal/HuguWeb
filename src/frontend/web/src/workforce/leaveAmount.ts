import { toIsoDate } from '../ui/dateEntry.ts'

const MS_PER_DAY = 86_400_000
const HALF_DAY = /^-?\d+(?:\.0|\.5)?$/

export function suggestedLeaveAmountDays(startValue: string, endValue: string): number | null {
  const start = toIsoDate(startValue)
  const end = toIsoDate(endValue)
  if (!start || !end) {
    return null
  }

  const startUtc = utcDate(start)
  const endUtc = utcDate(end)
  if (startUtc === null || endUtc === null || endUtc < startUtc) {
    return null
  }

  return Math.round((endUtc - startUtc) / MS_PER_DAY) + 1
}

export function parseLeaveAmount(raw: string): number | null {
  const trimmed = raw.trim().replace(',', '.')
  if (!HALF_DAY.test(trimmed)) {
    return null
  }

  const value = Number(trimmed)
  return Number.isFinite(value) ? value : null
}

export function isPositiveHalfDayAmount(raw: string): boolean {
  const value = parseLeaveAmount(raw)
  return value !== null && value > 0
}

export function isNonZeroHalfDayAmount(raw: string): boolean {
  const value = parseLeaveAmount(raw)
  return value !== null && value !== 0
}

export function formatLeaveAmount(value: number): string {
  return String(value)
}

export function amountAfterDateChange(
  amountTouched: boolean,
  startValue: string,
  endValue: string,
  currentAmount: string,
): string {
  if (amountTouched) {
    return currentAmount
  }

  const suggested = suggestedLeaveAmountDays(startValue, endValue)
  return suggested === null ? currentAmount : formatLeaveAmount(suggested)
}

function utcDate(iso: string): number | null {
  const [year, month, day] = iso.split('-').map(Number)
  if (!year || !month || !day) {
    return null
  }

  return Date.UTC(year, month - 1, day)
}
