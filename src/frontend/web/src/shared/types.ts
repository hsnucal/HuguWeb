export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  correlationId?: string
  code?: string
}

export type CurrentUser = {
  id: string
  email: string | null
  preferredLanguage: string | null
  permissions: string[]
}

export type SessionResponse = {
  authenticated: boolean
  user: CurrentUser | null
}

export type CsrfResponse = {
  token: string
}
