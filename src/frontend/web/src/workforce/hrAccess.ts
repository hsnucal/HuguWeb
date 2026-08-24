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
