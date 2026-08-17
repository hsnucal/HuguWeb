export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  correlationId?: string
}

export type CurrentUser = {
  id: string
  email: string | null
}

export type SessionResponse = {
  authenticated: boolean
  user: CurrentUser | null
}

export type CsrfResponse = {
  token: string
}
