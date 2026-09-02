import { ApiError, apiRequest, appendCsrfHeader } from '../shared/apiClient'

export type RecruitmentSourceListItem = {
  id: string
  code: string
  name: string
  isActive: boolean
  sortOrder: number
}

export type OnboardingChecklistItem = {
  requirementId: string
  code: string
  name: string
  isRequiredByDefault: boolean
  isCompleted: boolean
  completedAtUtc: string | null
  completedByUserId: string | null
}

export type OnboardingChecklist = {
  employmentId: string
  onboardingStatus: 'InProgress' | 'Completed' | string
  canEditChecklist: boolean
  canGenerateDocuments: boolean
  items: OnboardingChecklistItem[]
  totalCount: number
  completedCount: number
  documentTemplates: HrDocumentTemplateListItem[]
}

export type OnboardingCatalog = {
  requirements: OnboardingRequirementCatalogItem[]
  documentTemplates: HrDocumentTemplateListItem[]
}

export type OnboardingRequirementCatalogItem = {
  id: string
  code: string
  name: string
  isRequiredByDefault: boolean
}

export type HrDocumentTemplateCategory = 'Onboarding' | 'Employment' | 'Other'

export type HrDocumentTemplateListItem = {
  id: string
  code: string
  name: string
  description: string | null
  category: HrDocumentTemplateCategory
  version: string
  sortOrder: number
  hasDocxAsset: boolean
}

export type HrDocumentTemplatePreview = {
  templateId: string
  code: string
  name: string
  version: string
  renderedContent: string
}

export type OnboardingDocumentDraftPayload = {
  givenName: string
  familyName: string
  employmentStartDate: string
  departmentName?: string | null
  positionName?: string | null
}

export async function listRecruitmentSources() {
  return apiRequest<RecruitmentSourceListItem[]>('/api/hr/recruitment-sources')
}

export async function getOnboardingCatalog() {
  return apiRequest<OnboardingCatalog>('/api/hr/onboarding/catalog')
}

export async function listOnboardingDocumentRequirements() {
  return apiRequest<OnboardingRequirementCatalogItem[]>('/api/hr/onboarding-document-requirements')
}

export async function getEmployeeOnboardingDocuments(employeeId: string) {
  return apiRequest<OnboardingChecklist>(`/api/hr/employees/${employeeId}/onboarding-documents`)
}

export async function setEmployeeOnboardingDocument(
  employeeId: string,
  requirementId: string,
  isCompleted: boolean,
) {
  return apiRequest<OnboardingChecklistItem>(
    `/api/hr/employees/${employeeId}/onboarding-documents/${requirementId}`,
    {
      method: 'PUT',
      body: JSON.stringify({ isCompleted }),
    },
  )
}

export async function syncEmployeeOnboardingDocuments(
  employeeId: string,
  completedRequirementIds: string[],
) {
  return apiRequest<OnboardingChecklist>(`/api/hr/employees/${employeeId}/onboarding-documents/sync`, {
    method: 'POST',
    body: JSON.stringify({ completedRequirementIds }),
  })
}

export async function completeEmployeeOnboarding(employeeId: string) {
  return apiRequest<OnboardingChecklist>(`/api/hr/employees/${employeeId}/onboarding-documents/complete`, {
    method: 'POST',
    body: '{}',
  })
}

export function isOnboardingAlreadyCompleted(error: unknown): boolean {
  return error instanceof ApiError && error.problem?.code === 'onboarding-already-completed'
}

export async function listHrDocumentTemplates(category?: HrDocumentTemplateCategory) {
  const query = category ? `?category=${encodeURIComponent(category)}` : ''
  return apiRequest<HrDocumentTemplateListItem[]>(`/api/hr/document-templates${query}`)
}

export async function previewHrDocumentTemplateDraft(
  templateId: string,
  draft: OnboardingDocumentDraftPayload,
) {
  return apiRequest<HrDocumentTemplatePreview>(
    `/api/hr/onboarding/document-templates/${templateId}/preview-draft`,
    {
      method: 'POST',
      body: JSON.stringify(draft),
    },
  )
}

export async function downloadHrDocumentDraftDocx(
  templateId: string,
  draft: OnboardingDocumentDraftPayload,
) {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? ''
  const headers = new Headers({
    Accept: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    'Accept-Language': document.documentElement.lang || 'tr',
    'Content-Type': 'application/json',
  })
  appendCsrfHeader(headers, 'POST')
  const response = await fetch(
    `${apiBaseUrl}/api/hr/onboarding/document-templates/${templateId}/generate-draft`,
    {
      method: 'POST',
      credentials: 'include',
      headers,
      body: JSON.stringify(draft),
    },
  )

  if (!response.ok) {
    const contentType = response.headers.get('content-type') ?? ''
    const problem = contentType.includes('json') ? await response.json() : undefined
    throw new ApiError(
      problem?.detail ?? problem?.title ?? 'Request failed',
      response.status,
      problem,
    )
  }

  const blob = await response.blob()
  const disposition = response.headers.get('Content-Disposition') ?? ''
  const match = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(disposition)
  const fileName = match ? decodeURIComponent(match[1].replace(/"/g, '')) : 'document.docx'
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.click()
  URL.revokeObjectURL(url)
}

export async function previewHrDocumentTemplate(employeeId: string, templateId: string) {
  return apiRequest<HrDocumentTemplatePreview>(
    `/api/hr/employees/${employeeId}/document-templates/${templateId}/preview`,
  )
}

export async function downloadHrDocumentDocx(employeeId: string, templateId: string) {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? ''
  const response = await fetch(
    `${apiBaseUrl}/api/hr/employees/${employeeId}/document-templates/${templateId}/docx`,
    {
      credentials: 'include',
      headers: {
        Accept: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
        'Accept-Language': document.documentElement.lang || 'tr',
      },
    },
  )

  if (!response.ok) {
    const contentType = response.headers.get('content-type') ?? ''
    const problem = contentType.includes('json') ? await response.json() : undefined
    throw new ApiError(
      problem?.detail ?? problem?.title ?? 'Request failed',
      response.status,
      problem,
    )
  }

  const blob = await response.blob()
  const disposition = response.headers.get('Content-Disposition') ?? ''
  const match = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(disposition)
  const fileName = match ? decodeURIComponent(match[1].replace(/"/g, '')) : 'document.docx'
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.click()
  URL.revokeObjectURL(url)
}

const errorKeys: Record<string, string> = {
  'onboarding-requirement-not-found': 'personnel.onboarding.errors.requirementNotFound',
  'onboarding-completed': 'personnel.onboarding.errors.readOnly',
  'onboarding-documents-read-only': 'personnel.onboarding.errors.readOnly',
  'onboarding-document-generation-closed': 'personnel.onboarding.errors.generationClosed',
  'onboarding-already-completed': 'personnel.onboarding.errors.alreadyCompleted',
  'document-template-not-found': 'personnel.onboarding.errors.templateNotFound',
  'document-template-unknown-placeholder': 'personnel.onboarding.errors.templateInvalid',
  'document-template-asset-missing': 'personnel.onboarding.errors.docxMissing',
  'document-template-docx-unavailable': 'personnel.onboarding.errors.docxUnavailable',
  'employee-not-found': 'personnel.errors.generic',
}

export function onboardingErrorKey(error: unknown): string {
  if (error instanceof ApiError && error.problem?.code && errorKeys[error.problem.code]) {
    return errorKeys[error.problem.code]
  }

  return 'personnel.onboarding.errors.generic'
}
