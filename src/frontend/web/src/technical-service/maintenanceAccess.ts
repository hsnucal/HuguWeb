import type { CurrentUser } from '../shared/types'

function permissions(user: CurrentUser | null): string[] {
  return user?.permissions ?? []
}

export function canReadMaintenance(user: CurrentUser | null): boolean {
  return permissions(user).includes('maintenance.read')
}

export function canManageMaintenance(user: CurrentUser | null): boolean {
  return permissions(user).includes('maintenance.manage')
}

export function canResolveMaintenance(user: CurrentUser | null): boolean {
  return permissions(user).includes('maintenance.resolve')
}
