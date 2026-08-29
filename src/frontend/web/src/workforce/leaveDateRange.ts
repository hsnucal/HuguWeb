import { toIsoDate } from '../ui/dateEntry.ts'

export function endMinDate(startValue: string): string | undefined {
  return toIsoDate(startValue) ?? undefined
}

export function isStartOnOrBeforeEnd(startValue: string, endValue: string): boolean {
  const start = toIsoDate(startValue)
  const end = toIsoDate(endValue)
  return start !== null && end !== null && start <= end
}

export function endDateAfterStartChange(startValue: string, endValue: string): string {
  const start = toIsoDate(startValue)
  const end = toIsoDate(endValue)
  if (start === null) {
    return endValue
  }

  if (end === null || start > end) {
    return start
  }

  return end
}
