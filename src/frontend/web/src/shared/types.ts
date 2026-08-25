export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  correlationId?: string
  code?: string
  errors?: Record<string, string[]>
}

export type AccessibleProperty = {
  id: string
  name: string
  timeZoneId: string
}

export type CurrentUser = {
  id: string
  email: string | null
  preferredLanguage: string | null
  permissions: string[]
  membershipId?: string | null
  organizationId?: string | null
  propertyId?: string | null
  scopeType?: string | null
  employeeId?: string | null
  accessibleProperties?: AccessibleProperty[]
  propertySelectionRequired?: boolean
}

export type SessionResponse = {
  authenticated: boolean
  user: CurrentUser | null
}

export type CsrfResponse = {
  token: string
}
