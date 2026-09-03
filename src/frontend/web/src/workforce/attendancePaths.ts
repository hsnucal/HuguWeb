export type AttendanceMonthQuery = {
  year: number
  month: number
  departmentId?: string | null
  search?: string | null
}

export const ATTENDANCE_CORRECTION_KINDS = ['Worked', 'Leave', 'RestDay', 'Absent'] as const

export type AttendanceCorrectionKind = (typeof ATTENDANCE_CORRECTION_KINDS)[number]

export function isAttendanceCorrectionKind(value: string | null | undefined): value is AttendanceCorrectionKind {
  return ATTENDANCE_CORRECTION_KINDS.includes(value as AttendanceCorrectionKind)
}

export const attendanceErrorKeys: Record<string, string> = {
  'attendance-invalid-month': 'attendance.errors.invalidMonth',
  'attendance-outside-employment': 'attendance.errors.outsideEmployment',
  'attendance-correction-reason-required': 'attendance.errors.reasonRequired',
  'attendance-correction-reason-too-long': 'attendance.errors.reasonTooLong',
  'attendance-correction-kind-invalid': 'attendance.errors.kindInvalid',
  'attendance-employment-not-found': 'attendance.errors.employmentNotFound',
  'attendance-department-scope-denied': 'attendance.errors.departmentScopeDenied',
  'attendance-property-access-denied': 'attendance.errors.propertyAccessDenied',
  'attendance-assignment-not-found': 'attendance.errors.assignmentNotFound',
  'attendance-department-filter-denied': 'attendance.errors.departmentFilterDenied',
}

export function buildAttendanceMonthPath(query: AttendanceMonthQuery): string {
  const params = new URLSearchParams()
  params.set('year', String(query.year))
  params.set('month', String(query.month))
  if (query.departmentId) {
    params.set('departmentId', query.departmentId)
  }
  const search = query.search?.trim()
  if (search) {
    params.set('search', search)
  }
  return `/api/hr/attendance/monthly?${params.toString()}`
}

export function attendanceCorrectionPath(employmentId: string, date: string): string {
  return `/api/hr/attendance/${employmentId}/${date}/correction`
}

export function attendanceHistoryPath(employmentId: string, date: string): string {
  return `/api/hr/attendance/${employmentId}/${date}/history`
}

export function attendanceCorrectionBody(kind: string, reason: string): { kind: string; reason: string } {
  return { kind, reason }
}

export function hrAttendanceErrorKeyFromCode(code: string | undefined): string {
  if (code && attendanceErrorKeys[code]) {
    return attendanceErrorKeys[code]
  }

  return 'attendance.errors.generic'
}
