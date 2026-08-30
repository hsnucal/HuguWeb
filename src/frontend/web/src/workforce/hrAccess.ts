import type { CurrentUser } from '../shared/types'

export function canReadHrEmployees(user: CurrentUser | null): boolean {
  const permissions = user?.permissions ?? []
  return permissions.includes('hr.employee.read') || permissions.includes('hr.employee.manage')
}

export function canManageHrEmployees(user: CurrentUser | null): boolean {
  return (user?.permissions ?? []).includes('hr.employee.manage')
}

export function canReadHrSensitive(user: CurrentUser | null): boolean {
  return (user?.permissions ?? []).includes('hr.employee.sensitive.read')
}

export function canReadHrLeave(user: CurrentUser | null): boolean {
  const permissions = user?.permissions ?? []
  return permissions.includes('hr.leave.read') || permissions.includes('hr.leave.manage')
}

export function canManageHrLeave(user: CurrentUser | null): boolean {
  return (user?.permissions ?? []).includes('hr.leave.manage')
}

export function canReadHrSchedule(user: CurrentUser | null): boolean {
  const permissions = user?.permissions ?? []
  return permissions.includes('hr.schedule.read') || permissions.includes('hr.schedule.manage')
}

export function canManageHrSchedule(user: CurrentUser | null): boolean {
  return (user?.permissions ?? []).includes('hr.schedule.manage')
}

export function canReadHrShiftDefinitions(user: CurrentUser | null): boolean {
  const permissions = user?.permissions ?? []
  return (
    permissions.includes('hr.shift-definition.read') || permissions.includes('hr.shift-definition.manage')
  )
}

export function canManageHrShiftDefinitions(user: CurrentUser | null): boolean {
  return (user?.permissions ?? []).includes('hr.shift-definition.manage')
}
