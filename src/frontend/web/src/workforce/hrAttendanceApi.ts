import { ApiError, apiRequest } from '../shared/apiClient'
import {
  ATTENDANCE_CORRECTION_KINDS,
  attendanceCorrectionPath,
  attendanceHistoryPath,
  buildAttendanceMonthPath,
  hrAttendanceErrorKeyFromCode,
  isAttendanceCorrectionKind,
  type AttendanceCorrectionKind,
  type AttendanceMonthQuery,
} from './attendancePaths.ts'

export type AttendanceCoverage = 'InEmployment' | 'NotEmployed' | 'OutOfScope'
export type AttendanceAcceptedKind = 'Worked' | 'Leave' | 'RestDay' | 'Absent' | 'Unresolved'
export type AttendanceSource = 'Schedule' | 'Leave' | 'Manual'
export type AttendanceChangeType = 'Set' | 'Clear'
export type AttendanceScheduleState = 'Shift' | 'RestDay' | 'Unscheduled'

export {
  ATTENDANCE_CORRECTION_KINDS,
  attendanceCorrectionPath,
  attendanceHistoryPath,
  buildAttendanceMonthPath,
  isAttendanceCorrectionKind,
}
export type { AttendanceCorrectionKind, AttendanceMonthQuery }

export type AttendanceDaySchedule = {
  state: AttendanceScheduleState | string
  scheduleEntryId: string | null
  shiftDefinitionId: string | null
  shiftCode: string | null
  shiftName: string | null
  startLocalTime: string | null
  endLocalTime: string | null
  endsNextDay: boolean | null
}

export type AttendanceDayLeave = {
  leaveRecordId: string
  leaveTypeId: string
  leaveTypeCode: string | null
  leaveTypeName: string | null
  startDate: string
  endDate: string
  amount: number
}

export type AttendanceDayResult = {
  localDate: string
  coverage: AttendanceCoverage | string
  acceptedKind: AttendanceAcceptedKind | string | null
  source: AttendanceSource | string | null
  isProvisional: boolean
  isManual: boolean
  isUnresolved: boolean
  correctionReason: string | null
  employmentId: string | null
  assignmentId: string | null
  departmentId: string | null
  departmentName: string | null
  schedule: AttendanceDaySchedule | null
  leave: AttendanceDayLeave | null
  plannedMinutes: number | null
  acceptedWorkedMinutes: number | null
}

export type AttendanceMonthTotals = {
  workedDays: number
  leaveDays: number
  restDays: number
  absentDays: number
  unresolvedDays: number
  plannedMinutes: number
}

export type AttendanceMonthDepartment = {
  id: string
  name: string
  isActive: boolean
}

export type AttendanceMonthEmployee = {
  employeeId: string
  employmentId: string | null
  givenName: string
  familyName: string
  personnelNumber: string
  rowDepartmentId: string | null
  rowDepartmentName: string | null
  days: AttendanceDayResult[]
  totals: AttendanceMonthTotals
}

export type AttendanceMonthDto = {
  year: number
  month: number
  monthStart: string
  monthEnd: string
  dates: string[]
  propertyId: string
  propertyWide: boolean
  selectedDepartmentId: string | null
  filterDepartments: AttendanceMonthDepartment[]
  employees: AttendanceMonthEmployee[]
}

export type AttendanceCorrectionHistoryItem = {
  id: string
  changeType: AttendanceChangeType | string
  previousKind: AttendanceCorrectionKind | string | null
  newKind: AttendanceCorrectionKind | string | null
  previousReason: string | null
  newReason: string | null
  changedByUserId: string
  changedAtUtc: string
}

export type AttendanceCorrectionHistoryDto = {
  employmentId: string
  localDate: string
  changes: AttendanceCorrectionHistoryItem[]
}

export type SetAttendanceCorrectionInput = {
  kind: AttendanceCorrectionKind
  reason: string
}

export function hrAttendanceErrorKey(error: unknown): string {
  if (error instanceof ApiError) {
    return hrAttendanceErrorKeyFromCode(error.problem?.code)
  }

  return hrAttendanceErrorKeyFromCode(undefined)
}

export async function getHrAttendanceMonth(query: AttendanceMonthQuery) {
  return apiRequest<AttendanceMonthDto>(buildAttendanceMonthPath(query))
}

export async function getHrAttendanceHistory(employmentId: string, date: string) {
  return apiRequest<AttendanceCorrectionHistoryDto>(attendanceHistoryPath(employmentId, date))
}

export async function setHrAttendanceCorrection(
  employmentId: string,
  date: string,
  input: SetAttendanceCorrectionInput,
) {
  return apiRequest<AttendanceDayResult>(attendanceCorrectionPath(employmentId, date), {
    method: 'PUT',
    body: JSON.stringify({
      kind: input.kind,
      reason: input.reason,
    }),
  })
}

export async function clearHrAttendanceCorrection(employmentId: string, date: string) {
  return apiRequest<AttendanceDayResult>(attendanceCorrectionPath(employmentId, date), {
    method: 'DELETE',
  })
}
