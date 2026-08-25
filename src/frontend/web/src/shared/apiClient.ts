import type { ProblemDetails } from './types'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? ''

let csrfToken: string | null = null

export class ApiError extends Error {
  readonly status: number
  readonly problem?: ProblemDetails

  constructor(message: string, status: number, problem?: ProblemDetails) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

export function setCsrfToken(token: string | null) {
  csrfToken = token
}

export async function apiRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
  const method = (options.method ?? 'GET').toUpperCase()
  const headers = new Headers(options.headers)

  if (!headers.has('Accept-Language')) {
    headers.set('Accept-Language', document.documentElement.lang || 'tr')
  }

  if (options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  if (csrfToken && method !== 'GET' && method !== 'HEAD') {
    headers.set('X-XSRF-TOKEN', csrfToken)
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    method,
    headers,
    credentials: 'include',
  })

  if (response.status === 204) {
    return undefined as T
  }

  const contentType = response.headers.get('content-type') ?? ''
  const isJson =
    contentType.includes('application/json') ||
    contentType.includes('application/problem+json')
  const payload = isJson ? await response.json() : undefined

  if (!response.ok) {
    const problem = payload as ProblemDetails | undefined
    throw new ApiError(
      problem?.detail ?? problem?.title ?? 'Request failed',
      response.status,
      problem,
    )
  }

  return payload as T
}

export async function apiUpload<T>(path: string, body: FormData): Promise<T> {
  const headers = new Headers()
  headers.set('Accept', 'application/json')
  headers.set('Accept-Language', document.documentElement.lang || 'tr')
  if (csrfToken) {
    headers.set('X-XSRF-TOKEN', csrfToken)
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: 'POST',
    headers,
    body,
    credentials: 'include',
  })

  const contentType = response.headers.get('content-type') ?? ''
  const isJson =
    contentType.includes('application/json') ||
    contentType.includes('application/problem+json')
  const payload = isJson ? await response.json() : undefined

  if (!response.ok) {
    const problem = payload as ProblemDetails | undefined
    throw new ApiError(
      problem?.detail ?? problem?.title ?? 'Request failed',
      response.status,
      problem,
    )
  }

  return payload as T
}
