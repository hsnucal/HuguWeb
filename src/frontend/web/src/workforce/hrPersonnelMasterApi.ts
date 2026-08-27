import { apiRequest, apiUpload, ApiError } from '../shared/apiClient'
import type { EmploymentHistoryRecord } from './workforceApi'
import type { PersonnelColumnId } from './personnelColumns'

export type EmployeePaymentProfileRead = {
  iban: string
  bankName: string | null
}

export type EmployeeErpAccountSummary = {
  hasAccount: boolean
  email: string | null
  isLocked: boolean | null
}

export type PersonnelProfileChangeRecord = {
  id: string
  fieldCode: string
  oldValue: string | null
  newValue: string | null
  changedAtUtc: string
  changedByUserId: string
  changedByEmployeeId: string | null
  changeSource: string | null
}

export type PersonnelHistoryResponse = {
  profileChanges: PersonnelProfileChangeRecord[]
  employments: EmploymentHistoryRecord[]
}

export type PersonnelImportRowPreview = {
  rowNumber: number
  action: 'Create' | 'Update'
  personnelNumber: string | null
  givenName: string
  familyName: string
  departmentLabel: string
  positionLabel: string
  employmentStartDate: string
  changedFields: string[]
  errors: Array<{ field: string; code: string; message: string }>
}

export type PersonnelImportPreviewResult = {
  previewToken: string
  totalRows: number
  createCount: number
  updateCount: number
  invalidCount: number
  rows: PersonnelImportRowPreview[]
  canConfirm: boolean
}

export type PersonnelImportConfirmResult = {
  createdCount: number
  updatedCount: number
  failedCount: number
  rows: PersonnelImportRowPreview[]
}

export type PersonnelExportFilters = {
  search?: string
  departmentId?: string
  positionId?: string
  status?: string
  startFrom?: string
  startTo?: string
  columns?: PersonnelColumnId[]
}

function buildExportQuery(filters: PersonnelExportFilters): string {
  const params = new URLSearchParams()
  if (filters.search) params.set('search', filters.search)
  if (filters.departmentId) params.set('departmentId', filters.departmentId)
  if (filters.positionId) params.set('positionId', filters.positionId)
  if (filters.status) params.set('status', filters.status)
  if (filters.startFrom) params.set('startFrom', filters.startFrom)
  if (filters.startTo) params.set('startTo', filters.startTo)
  if (filters.columns?.length) params.set('columns', filters.columns.join(','))
  const query = params.toString()
  return query === '' ? '/api/hr/employees/export' : `/api/hr/employees/export?${query}`
}

export async function exportHrEmployees(filters: PersonnelExportFilters) {
  const response = await fetch(`${import.meta.env.VITE_API_BASE_URL ?? ''}${buildExportQuery(filters)}`, {
    credentials: 'include',
  })
  if (!response.ok) {
    const problem = response.headers.get('content-type')?.includes('json')
      ? await response.json()
      : undefined
    throw new ApiError(
      problem?.detail ?? problem?.title ?? 'Export failed',
      response.status,
      problem,
    )
  }
  return response.blob()
}

export async function downloadHrImportTemplate() {
  const response = await fetch(`${import.meta.env.VITE_API_BASE_URL ?? ''}/api/hr/employees/import/template`, {
    credentials: 'include',
  })
  if (!response.ok) {
    throw new ApiError('Template download failed', response.status)
  }
  return response.blob()
}

export async function previewHrImport(file: File) {
  const body = new FormData()
  body.append('file', file)
  return apiUpload<PersonnelImportPreviewResult>('/api/hr/employees/import/preview', body)
}

export async function confirmHrImport(previewToken: string) {
  return apiRequest<PersonnelImportConfirmResult>('/api/hr/employees/import/confirm', {
    method: 'POST',
    body: JSON.stringify({ previewToken }),
  })
}

export async function getHrPersonnelHistory(employeeId: string) {
  return apiRequest<PersonnelHistoryResponse>(`/api/hr/employees/${employeeId}/profile-history`)
}

export async function getHrEmployeeErpAccount(employeeId: string) {
  return apiRequest<EmployeeErpAccountSummary>(`/api/hr/employees/${employeeId}/erp-account`)
}

export async function saveHrPaymentProfile(employeeId: string, iban: string, bankName: string | null) {
  return apiRequest(`/api/hr/employees/${employeeId}/payment-profile`, {
    method: 'PUT',
    body: JSON.stringify({ iban, bankName }),
  })
}

export function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.click()
  URL.revokeObjectURL(url)
}
