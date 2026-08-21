import type { CurrentUser } from '../shared/types'

export function workforcePermissions(user: CurrentUser | null): string[] {
  return user?.permissions ?? []
}

export function canReadWorkforce(user: CurrentUser | null): boolean {
  const permissions = workforcePermissions(user)
  return permissions.includes('workforce.read') || permissions.includes('workforce.manage')
}

export function canManageWorkforce(user: CurrentUser | null): boolean {
  return workforcePermissions(user).includes('workforce.manage')
}
