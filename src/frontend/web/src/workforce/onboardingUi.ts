import type { OnboardingChecklist, OnboardingRequirementCatalogItem } from './onboardingApi.ts'

export type OnboardingDraft = Record<string, boolean>

export type OnboardingDocumentDraftFields = {
  givenName: string
  familyName: string
  employmentStartDate: string
  departmentName?: string | null
  positionName?: string | null
}

export function shouldShowOnboardingTab(): boolean {
  return true
}

export function onboardingProgressText(completed: number, total: number): string {
  return `${completed} / ${total}`
}

export function countSelectedTemplates(selectedIds: Iterable<string>): number {
  return [...selectedIds].length
}

export function emptyOnboardingDraft(requirements: OnboardingRequirementCatalogItem[]): OnboardingDraft {
  return Object.fromEntries(requirements.map((item) => [item.id, false]))
}

export function completedRequirementIds(draft: OnboardingDraft): string[] {
  return Object.entries(draft).filter(([, completed]) => completed).map(([id]) => id)
}

export function countDraftCompleted(draft: OnboardingDraft): number {
  return completedRequirementIds(draft).length
}

export function toggleDraftItem(draft: OnboardingDraft, requirementId: string, next: boolean): OnboardingDraft {
  return { ...draft, [requirementId]: next }
}

export function toggleSelectedTemplate(selectedIds: string[], templateId: string): string[] {
  return selectedIds.includes(templateId)
    ? selectedIds.filter((id) => id !== templateId)
    : [...selectedIds, templateId]
}

export function draftItemsFromCatalog(
  requirements: OnboardingRequirementCatalogItem[],
  draft: OnboardingDraft,
) {
  return requirements.map((requirement) => ({
    requirementId: requirement.id,
    code: requirement.code,
    name: requirement.name,
    isRequiredByDefault: requirement.isRequiredByDefault,
    isCompleted: draft[requirement.id] ?? false,
  }))
}

export function isOnboardingDocumentDraftReady(fields: OnboardingDocumentDraftFields): boolean {
  return (
    fields.givenName.trim().length > 0 &&
    fields.familyName.trim().length > 0 &&
    fields.employmentStartDate.trim().length > 0
  )
}

export function canEditOnboarding(checklist: OnboardingChecklist | null, canManage: boolean): boolean {
  return Boolean(canManage && checklist?.canEditChecklist)
}

export function canGenerateOnboardingDocuments(
  checklist: OnboardingChecklist | null,
  canManage: boolean,
): boolean {
  return Boolean(canManage && checklist?.canGenerateDocuments)
}

export function showOnboardingTemplateActions(
  mode: 'create' | 'edit',
  checklist: OnboardingChecklist | null,
  canManage: boolean,
): boolean {
  if (!canManage) {
    return false
  }

  if (mode === 'create') {
    return true
  }

  return canGenerateOnboardingDocuments(checklist, canManage)
}

export function canSelectOnboardingTemplates(
  mode: 'create' | 'edit',
  checklist: OnboardingChecklist | null,
  canManage: boolean,
): boolean {
  return showOnboardingTemplateActions(mode, checklist, canManage)
}

export function canPreviewOnboardingDraft(
  mode: 'create' | 'edit',
  checklist: OnboardingChecklist | null,
  canManage: boolean,
  draftFields: OnboardingDocumentDraftFields,
): boolean {
  if (mode === 'create') {
    return canManage && isOnboardingDocumentDraftReady(draftFields)
  }

  return canGenerateOnboardingDocuments(checklist, canManage)
}

export function canGenerateOnboardingDraft(
  mode: 'create' | 'edit',
  checklist: OnboardingChecklist | null,
  canManage: boolean,
  draftFields: OnboardingDocumentDraftFields,
): boolean {
  return canPreviewOnboardingDraft(mode, checklist, canManage, draftFields)
}

export function canExecutePersistedOnboardingTemplateActions(
  mode: 'create' | 'edit',
  checklist: OnboardingChecklist | null,
  canManage: boolean,
): boolean {
  if (mode === 'create') {
    return false
  }

  return canGenerateOnboardingDocuments(checklist, canManage)
}
