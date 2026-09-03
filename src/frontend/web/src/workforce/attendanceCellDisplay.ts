import { formatShiftClockRange } from './shiftDefinitionForm.ts'
import type { AttendanceDayLeave, AttendanceDayResult } from './hrAttendanceApi.ts'

export type AttendanceCellTone =
  | 'worked'
  | 'leave'
  | 'rest'
  | 'absent'
  | 'unresolved'
  | 'notEmployed'
  | 'outOfScope'

export type AttendanceCellLabels = {
  restDay: string
  absent: string
  unresolved: string
  worked: string
  leave: string
  notEmployed: string
  leaveCell?: (leave: AttendanceDayLeave) => string
  leaveTooltip?: (leave: AttendanceDayLeave) => string
}

export type AttendanceCellVisible = {
  primary: string
  tone: AttendanceCellTone
  interactive: boolean
  isManual: boolean
}

export function formatAttendanceClockRange(startLocalTime: string, endLocalTime: string): string {
  return formatShiftClockRange(startLocalTime, endLocalTime).replace(' – ', '–')
}

export function scheduleClockRange(
  schedule: AttendanceDayResult['schedule'],
  formatRange: (start: string, end: string) => string = formatAttendanceClockRange,
): string | null {
  if (!schedule?.startLocalTime || !schedule.endLocalTime) {
    return null
  }

  const range = formatRange(schedule.startLocalTime, schedule.endLocalTime)
  return range || null
}

export function attendanceCellVisible(
  day: Pick<
    AttendanceDayResult,
    'coverage' | 'acceptedKind' | 'isManual' | 'schedule' | 'leave'
  >,
  labels: AttendanceCellLabels,
  formatRange: (start: string, end: string) => string = formatAttendanceClockRange,
): AttendanceCellVisible {
  if (day.coverage === 'NotEmployed') {
    return {
      primary: labels.notEmployed,
      tone: 'notEmployed',
      interactive: false,
      isManual: false,
    }
  }

  if (day.coverage === 'OutOfScope') {
    return {
      primary: labels.notEmployed,
      tone: 'outOfScope',
      interactive: false,
      isManual: false,
    }
  }

  if (day.acceptedKind === 'RestDay') {
    return {
      primary: labels.restDay,
      tone: 'rest',
      interactive: true,
      isManual: day.isManual,
    }
  }

  if (day.acceptedKind === 'Leave') {
    const leaveLabel =
      (day.leave ? labels.leaveCell?.(day.leave) : undefined)?.trim() ||
      day.leave?.leaveTypeName?.trim() ||
      labels.leave
    return {
      primary: leaveLabel,
      tone: 'leave',
      interactive: true,
      isManual: day.isManual,
    }
  }

  if (day.acceptedKind === 'Absent') {
    return {
      primary: labels.absent,
      tone: 'absent',
      interactive: true,
      isManual: day.isManual,
    }
  }

  if (day.acceptedKind === 'Worked') {
    return {
      primary: scheduleClockRange(day.schedule, formatRange) || labels.worked,
      tone: 'worked',
      interactive: true,
      isManual: day.isManual,
    }
  }

  return {
    primary: labels.unresolved,
    tone: 'unresolved',
    interactive: true,
    isManual: day.isManual,
  }
}

export function attendanceCellTooltipText(
  day: AttendanceDayResult,
  labels: AttendanceCellLabels & {
    notEmployedTooltip: string
    unresolvedTooltip: string
    outOfScopeTooltip: string
    leaveFallback: string
  },
  formatRange: (start: string, end: string) => string = formatAttendanceClockRange,
): string {
  if (day.coverage === 'NotEmployed') {
    return labels.notEmployedTooltip
  }

  if (day.coverage === 'OutOfScope') {
    return labels.outOfScopeTooltip
  }

  const visible = attendanceCellVisible(day, labels, formatRange)
  if (day.acceptedKind === 'Leave') {
    if (day.leave) {
      return labels.leaveTooltip?.(day.leave)?.trim() || day.leave.leaveTypeName?.trim() || labels.leaveFallback
    }
    return labels.leaveFallback
  }

  if (day.acceptedKind === 'Unresolved' || visible.tone === 'unresolved') {
    return labels.unresolvedTooltip
  }

  if (day.acceptedKind === 'Worked') {
    const range = scheduleClockRange(day.schedule, formatRange)
    const shift = day.schedule?.shiftName?.trim()
    return [range, shift, day.schedule?.shiftCode?.trim()].filter(Boolean).join(' · ') || visible.primary
  }

  return visible.primary
}

export function attendanceProvenanceStatus(day: Pick<AttendanceDayResult, 'source' | 'isProvisional' | 'isManual'>):
  | 'fromPlan'
  | 'manual'
  | 'fromLeave'
  | null {
  if (day.isManual || day.source === 'Manual') {
    return 'manual'
  }

  if (day.source === 'Leave') {
    return 'fromLeave'
  }

  if (day.source === 'Schedule' || day.isProvisional) {
    return 'fromPlan'
  }

  return null
}

export function attendanceKindLabelKey(kind: string | null | undefined): string | null {
  switch (kind) {
    case 'Worked':
      return 'attendance.kindWorked'
    case 'Leave':
      return 'attendance.kindLeave'
    case 'RestDay':
      return 'attendance.kindRestDay'
    case 'Absent':
      return 'attendance.kindAbsent'
    case 'Unresolved':
      return 'attendance.cellUnresolved'
    default:
      return null
  }
}

export function attendanceSourceLabelKey(source: string | null | undefined): string | null {
  switch (source) {
    case 'Schedule':
      return 'attendance.sourceSchedule'
    case 'Leave':
      return 'attendance.sourceLeave'
    case 'Manual':
      return 'attendance.sourceManual'
    default:
      return null
  }
}

export function attendanceScheduleStateLabelKey(state: string | null | undefined): string | null {
  switch (state) {
    case 'Shift':
      return 'attendance.scheduleShift'
    case 'RestDay':
      return 'attendance.scheduleRest'
    case 'Unscheduled':
      return 'attendance.scheduleUnscheduled'
    default:
      return null
  }
}

export function reverseChronological<T extends { changedAtUtc: string; id: string }>(items: readonly T[]): T[] {
  return [...items].sort((left, right) => {
    if (left.changedAtUtc !== right.changedAtUtc) {
      return left.changedAtUtc < right.changedAtUtc ? 1 : -1
    }

    return left.id < right.id ? 1 : -1
  })
}
