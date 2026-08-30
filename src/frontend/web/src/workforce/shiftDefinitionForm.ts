/** Normalize API TimeOnly ("HH:mm:ss", "HH:mm", or fractional seconds) for display / `<input type="time">`. */
export function formatTimeForInput(value: string): string {
  const match = /^(\d{1,2}):(\d{2})(?::\d{2}(?:\.\d+)?)?$/.exec(value.trim())
  if (!match) {
    return ''
  }

  return `${match[1]!.padStart(2, '0')}:${match[2]}`
}

/** Parse a time input value into ASP.NET TimeOnly JSON form "HH:mm:ss". */
export function parseTimeInput(value: string): string | null {
  const match = /^(\d{1,2}):(\d{2})(?::(\d{2}))?$/.exec(value.trim())
  if (!match) {
    return null
  }

  const hours = Number(match[1])
  const minutes = Number(match[2])
  const seconds = match[3] === undefined ? 0 : Number(match[3])
  if (
    !Number.isInteger(hours) ||
    !Number.isInteger(minutes) ||
    !Number.isInteger(seconds) ||
    hours > 23 ||
    minutes > 59 ||
    seconds > 59
  ) {
    return null
  }

  return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
}

/**
 * True when end is at or before start on the clock and EndsNextDay is not set
 * (same rule as domain ShiftDuration.TryValidateTimes).
 */
export function isOvernightInconsistent(start: string, end: string, endsNextDay: boolean): boolean {
  const startTime = parseTimeInput(start)
  const endTime = parseTimeInput(end)
  if (!startTime || !endTime) {
    return false
  }

  return endTime <= startTime && !endsNextDay
}

/** Display persisted wall-clock times as-is (no midnight → 23:59 conversion). */
export function formatShiftClockRange(startLocalTime: string, endLocalTime: string): string {
  const start = formatTimeForInput(startLocalTime)
  const end = formatTimeForInput(endLocalTime)
  if (!start || !end) {
    return ''
  }

  return `${start} – ${end}`
}

/** Parts for human-readable planned net duration from API `plannedNetMinutes`. */
export function splitNetDuration(plannedNetMinutes: number): { hours: number; minutes: number } {
  const safe = Number.isFinite(plannedNetMinutes) ? Math.max(0, Math.trunc(plannedNetMinutes)) : 0
  return {
    hours: Math.floor(safe / 60),
    minutes: safe % 60,
  }
}

/** Chronological list order: StartLocalTime ascending, then code, then id. */
export function compareShiftDefinitionsByStart(
  left: { startLocalTime: string; code: string; id: string },
  right: { startLocalTime: string; code: string; id: string },
): number {
  const byStart = formatTimeForInput(left.startLocalTime).localeCompare(
    formatTimeForInput(right.startLocalTime),
  )
  if (byStart !== 0) {
    return byStart
  }

  const byCode = left.code.localeCompare(right.code)
  if (byCode !== 0) {
    return byCode
  }

  return left.id.localeCompare(right.id)
}
