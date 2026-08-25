import { ApiError, apiRequest } from '../shared/apiClient'

export type MembershipSummary = {
  id: string
  organizationId: string
  organizationName?: string | null
  propertyId: string | null
  propertyName?: string | null
  isActive: boolean
  scopeType: string
  roleIds: string[]
}

export type AuthorizationUser = {
  id: string
  email: string | null
  lockedOut: boolean
  employeeId: string | null
  memberships: MembershipSummary[]
  effectivePermissions: string[]
}

export type AuthorizationRole = {
  id: string
  organizationId: string
  name: string
  code: string
  scopeType: string
  isSystemTemplate: boolean
  isActive: boolean
  permissionCodes: string[]
}

export type PermissionCatalogItem = {
  code: string
  domain: string
}

export function listAuthorizationUsers() {
  return apiRequest<AuthorizationUser[]>('/api/authorization/users')
}

export function createAuthorizationUser(email: string, password: string, employeeId?: string) {
  return apiRequest<{ id: string; email: string }>(`/api/authorization/users`, {
    method: 'POST',
    body: JSON.stringify({
      email,
      password,
      employeeId: employeeId || null,
    }),
  })
}

export function createMembership(userId: string, organizationId: string, propertyId: string | null) {
  return apiRequest<MembershipSummary>(`/api/authorization/users/${userId}/memberships`, {
    method: 'POST',
    body: JSON.stringify({ organizationId, propertyId }),
  })
}

export function setMembershipActive(membershipId: string, isActive: boolean) {
  return apiRequest<void>(`/api/authorization/users/memberships/${membershipId}`, {
    method: 'PATCH',
    body: JSON.stringify({ isActive }),
  })
}

export function assignRole(membershipId: string, roleId: string) {
  return apiRequest<void>(`/api/authorization/users/memberships/${membershipId}/roles/${roleId}`, {
    method: 'POST',
  })
}

export function removeRole(membershipId: string, roleId: string) {
  return apiRequest<void>(`/api/authorization/users/memberships/${membershipId}/roles/${roleId}`, {
    method: 'DELETE',
  })
}

export function listAuthorizationRoles() {
  return apiRequest<AuthorizationRole[]>('/api/authorization/roles')
}

export function listPermissionCatalog() {
  return apiRequest<PermissionCatalogItem[]>('/api/authorization/roles/permissions')
}

export function replaceRolePermissions(roleId: string, permissionCodes: string[]) {
  return apiRequest<void>(`/api/authorization/roles/${roleId}/permissions`, {
    method: 'PUT',
    body: JSON.stringify({ permissionCodes }),
  })
}

export function setRoleActive(roleId: string, isActive: boolean) {
  return apiRequest<void>(`/api/authorization/roles/${roleId}`, {
    method: 'PATCH',
    body: JSON.stringify({ isActive }),
  })
}

export function createRole(body: { name: string; code: string; scopeType: string; organizationId: string }) {
  return apiRequest<AuthorizationRole>('/api/authorization/roles', {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

export function authorizationErrorKey(error: unknown): string {
  if (error instanceof ApiError && error.problem?.code === 'email-in-use') {
    return 'authorization.errors.emailInUse'
  }
  if (error instanceof ApiError && error.problem?.code === 'invalid-password') {
    return 'authorization.errors.invalidPassword'
  }
  if (error instanceof ApiError && error.problem?.code === 'last-administrator') {
    return 'authorization.errors.lastAdministrator'
  }
  if (error instanceof ApiError && error.problem?.code === 'scope-mismatch') {
    return 'authorization.errors.scopeMismatch'
  }
  if (error instanceof ApiError && error.problem?.code === 'property-context-required') {
    return 'authorization.errors.propertyContextRequired'
  }
  return 'authorization.errors.generic'
}
