export const SYSTEM_LEAVE_KIND_CODES = {
  annual: 'Annual',
  unpaid: 'Unpaid',
  sick: 'Sick',
  marriage: 'Marriage',
  paternity: 'Paternity',
  maternity: 'Maternity',
  bereavement: 'Bereavement',
  excuse: 'Excuse',
  administrative: 'Administrative',
  other: 'Other',
} as const

export type LeaveSystemKind = (typeof SYSTEM_LEAVE_KIND_CODES)[keyof typeof SYSTEM_LEAVE_KIND_CODES]

export type LeaveDisplaySource = {
  systemKind?: string | null
  leaveTypeCode?: string | null
  leaveTypeName?: string | null
}

const SYSTEM_KINDS = new Set<string>(Object.values(SYSTEM_LEAVE_KIND_CODES))

export function resolveLeaveSystemKind(source: LeaveDisplaySource): LeaveSystemKind | null {
  const kind = source.systemKind?.trim()
  if (kind && SYSTEM_KINDS.has(kind)) {
    return kind as LeaveSystemKind
  }

  const code = source.leaveTypeCode?.trim().toLowerCase()
  if (code && code in SYSTEM_LEAVE_KIND_CODES) {
    return SYSTEM_LEAVE_KIND_CODES[code as keyof typeof SYSTEM_LEAVE_KIND_CODES]
  }

  return null
}

export function attendanceLeaveCellLabel(
  source: LeaveDisplaySource,
  t: (key: string) => string,
): string {
  const kind = resolveLeaveSystemKind(source)
  if (kind) {
    return t(`attendance.leaveShort.${kind}`)
  }

  return compactLeaveText(source.leaveTypeName) || compactLeaveText(source.leaveTypeCode) || t('attendance.kindLeave')
}

export function attendanceLeaveDetailLabel(
  source: LeaveDisplaySource,
  t: (key: string) => string,
): string {
  const kind = resolveLeaveSystemKind(source)
  if (kind) {
    return t(`attendance.leaveFull.${kind}`)
  }

  return source.leaveTypeName?.trim() || source.leaveTypeCode?.trim() || t('attendance.kindLeave')
}

function compactLeaveText(value: string | null | undefined): string {
  return value?.trim() ?? ''
}
