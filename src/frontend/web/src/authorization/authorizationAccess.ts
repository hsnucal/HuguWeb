import type { CurrentUser } from '../shared/types'

export function canManageAuthorizationUsers(user: CurrentUser | null): boolean {
  return (user?.permissions ?? []).includes('authorization.users.manage')
}

export function canManageAuthorizationRoles(user: CurrentUser | null): boolean {
  return (user?.permissions ?? []).includes('authorization.roles.manage')
}

export function canOpenSettings(user: CurrentUser | null): boolean {
  return canManageAuthorizationUsers(user) || canManageAuthorizationRoles(user)
}
