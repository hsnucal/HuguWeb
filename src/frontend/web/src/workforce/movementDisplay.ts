import { toIsoDate } from '../ui/dateEntry.ts'
import { isEligiblePromotionTarget } from './assignmentOptions.ts'
import type { AssignmentHistoryRecord, EmploymentHistoryRecord, PositionRecord } from './workforceApi.ts'
import {
  CREATABLE_MOVEMENT_TYPES,
  MOVEMENT_REASON_MAX,
  type CreatableMovementType,
  type CreateMovementRequest,
  type MovementAssignmentSummary,
  type MovementReportingLineSummary,
} from './hrMovementsPaths.ts'

type PositionOption = {
  id: string
  isActive: boolean
  applicableDepartmentIds: string[]
}

function positionsForDepartment(positions: PositionOption[], departmentId: string): PositionOption[] {
  if (departmentId === '') {
    return []
  }
  return positions.filter((item) => item.isActive && item.applicableDepartmentIds.includes(departmentId))
}

export function movementTypeLabelKey(type: string): string {
  switch (type) {
    case 'DepartmentChange':
      return 'movements.types.DepartmentChange'
    case 'PositionChange':
      return 'movements.types.PositionChange'
    case 'Promotion':
      return 'movements.types.Promotion'
    case 'PropertyTransfer':
      return 'movements.types.PropertyTransfer'
    case 'ManagerChange':
      return 'movements.types.ManagerChange'
    case 'AssignmentChange':
      return 'movements.types.AssignmentChange'
    default:
      return 'movements.types.unknown'
  }
}

export function movementLifecycleLabelKey(lifecycle: string): string {
  switch (lifecycle) {
    case 'Scheduled':
      return 'movements.lifecycle.Scheduled'
    case 'Effective':
      return 'movements.lifecycle.Effective'
    case 'Cancelled':
      return 'movements.lifecycle.Cancelled'
    default:
      return 'movements.lifecycle.unknown'
  }
}

export function movementLifecycleTone(lifecycle: string): 'info' | 'success' | 'neutral' {
  if (lifecycle === 'Scheduled') {
    return 'info'
  }
  if (lifecycle === 'Effective') {
    return 'success'
  }
  return 'neutral'
}

export function managerDisplayName(line: MovementReportingLineSummary | null): string {
  if (!line) {
    return '—'
  }
  return `${line.managerGivenName} ${line.managerFamilyName}`.trim()
}

export function assignmentDepartmentLine(assignment: MovementAssignmentSummary | null): string {
  return assignment?.departmentName?.trim() || '—'
}

export function assignmentPositionLine(assignment: MovementAssignmentSummary | null): string {
  return assignment?.positionName?.trim() || '—'
}

export function assignmentPropertyLine(assignment: MovementAssignmentSummary | null): string {
  return assignment?.propertyName?.trim() || '—'
}

export function movementDiffSummary(
  item: {
    type: string
    previousAssignment: MovementAssignmentSummary | null
    newAssignment: MovementAssignmentSummary | null
    previousReportingLine: MovementReportingLineSummary | null
    newReportingLine: MovementReportingLineSummary | null
  },
): { previous: string; next: string } {
  if (item.type === 'ManagerChange') {
    return {
      previous: managerDisplayName(item.previousReportingLine),
      next: managerDisplayName(item.newReportingLine),
    }
  }

  if (item.type === 'PropertyTransfer') {
    return {
      previous: compactAssignment(item.previousAssignment, ['property', 'department', 'position']),
      next: compactAssignment(item.newAssignment, ['property', 'department', 'position']),
    }
  }

  if (item.type === 'DepartmentChange') {
    return {
      previous: compactAssignment(item.previousAssignment, ['department', 'position']),
      next: compactAssignment(item.newAssignment, ['department', 'position']),
    }
  }

  if (item.type === 'PositionChange' || item.type === 'Promotion') {
    return {
      previous: assignmentPositionLine(item.previousAssignment),
      next: assignmentPositionLine(item.newAssignment),
    }
  }

  return {
    previous: compactAssignment(item.previousAssignment, ['department', 'position']),
    next: compactAssignment(item.newAssignment, ['department', 'position']),
  }
}

function compactAssignment(
  assignment: MovementAssignmentSummary | null,
  parts: Array<'property' | 'department' | 'position'>,
): string {
  if (!assignment) {
    return '—'
  }
  const values = parts
    .map((part) => {
      if (part === 'property') {
        return assignment.propertyName
      }
      if (part === 'department') {
        return assignment.departmentName
      }
      return assignment.positionName
    })
    .map((value) => value.trim())
    .filter((value) => value !== '')
  return values.length === 0 ? '—' : values.join(' · ')
}

export type MovementWizardDraft = {
  employmentId: string
  employeeId: string
  type: CreatableMovementType | ''
  effectiveDate: string
  targetPropertyId: string
  targetDepartmentId: string
  targetPositionId: string
  targetManagerEmploymentId: string
  reason: string
  note: string
}

export function emptyMovementWizardDraft(): MovementWizardDraft {
  return {
    employmentId: '',
    employeeId: '',
    type: '',
    effectiveDate: '',
    targetPropertyId: '',
    targetDepartmentId: '',
    targetPositionId: '',
    targetManagerEmploymentId: '',
    reason: '',
    note: '',
  }
}

export function isMovementWizardDirty(draft: MovementWizardDraft, search = ''): boolean {
  const empty = emptyMovementWizardDraft()
  return (
    search.trim() !== ''
    || draft.employeeId !== empty.employeeId
    || draft.employmentId !== empty.employmentId
    || draft.type !== empty.type
    || draft.effectiveDate.trim() !== ''
    || draft.targetPropertyId !== empty.targetPropertyId
    || draft.targetDepartmentId !== empty.targetDepartmentId
    || draft.targetPositionId !== empty.targetPositionId
    || draft.targetManagerEmploymentId !== empty.targetManagerEmploymentId
    || draft.reason.trim() !== ''
    || draft.note.trim() !== ''
  )
}

export function earliestAssignmentMovementDate(assignmentStartIso: string | null | undefined): string | null {
  if (!assignmentStartIso || !/^\d{4}-\d{2}-\d{2}$/.test(assignmentStartIso)) {
    return null
  }
  const [year, month, day] = assignmentStartIso.split('-').map(Number)
  const next = new Date(year, month - 1, day + 1)
  const y = next.getFullYear()
  const m = String(next.getMonth() + 1).padStart(2, '0')
  const d = String(next.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

export function assignmentMovementDateTooEarly(
  effectiveIso: string | null,
  assignmentStartIso: string | null | undefined,
  type: string,
): boolean {
  if (!effectiveIso || type === '' || type === 'ManagerChange') {
    return false
  }
  const earliest = earliestAssignmentMovementDate(assignmentStartIso)
  return earliest !== null && effectiveIso < earliest
}

export function departmentChangeNeedsTargetPosition(
  positions: PositionOption[],
  targetDepartmentId: string,
  currentPositionId: string,
): boolean {
  if (targetDepartmentId === '' || currentPositionId === '') {
    return true
  }
  return !positionsForDepartment(positions, targetDepartmentId).some((item) => item.id === currentPositionId)
}

export function assignmentCoversDate(startDate: string, endDate: string | null, date: string): boolean {
  return startDate <= date && (endDate === null || endDate === '' || endDate >= date)
}

export function coveringPrimaryAssignment(
  assignments: AssignmentHistoryRecord[] | undefined,
  date: string | null,
): AssignmentHistoryRecord | null {
  if (!date || !assignments || assignments.length === 0) {
    return null
  }

  return (
    [...assignments]
      .filter(
        (item) =>
          item.kind !== 'Temporary' && assignmentCoversDate(item.startDate, item.endDate, date),
      )
      .sort((left, right) => right.startDate.localeCompare(left.startDate))[0] ?? null
  )
}

export function sourceAssignmentAsOf(
  card: {
    currentEmployment: EmploymentHistoryRecord | null
    currentPrimaryAssignment: AssignmentHistoryRecord | null
    employments: EmploymentHistoryRecord[]
  } | null,
  employmentId: string,
  date: string | null,
): AssignmentHistoryRecord | null {
  const employment =
    card?.employments.find((item) => item.id === employmentId) ??
    (card?.currentEmployment?.id === employmentId ? card.currentEmployment : null)
  const fromHistory = coveringPrimaryAssignment(employment?.primaryAssignments, date)
  if (fromHistory) {
    return fromHistory
  }

  const current = card?.currentPrimaryAssignment
  if (current && date && assignmentCoversDate(current.startDate, current.endDate, date)) {
    return current
  }

  return current ?? null
}

export function sourceOrganizationalLevel(
  positions: PositionRecord[],
  sourcePositionId: string,
): number | undefined {
  if (sourcePositionId === '') {
    return undefined
  }

  return positions.find((item) => item.id === sourcePositionId)?.organizationalLevel
}

export function buildCreateMovementRequest(
  draft: MovementWizardDraft,
  current: { departmentId: string; positionId: string },
  catalogue?: { positions: PositionRecord[]; sourceOrganizationalLevel?: number },
): CreateMovementRequest | { error: 'type' | 'date' | 'reason' | 'target' } {
  if (!draft.type || !CREATABLE_MOVEMENT_TYPES.includes(draft.type)) {
    return { error: 'type' }
  }
  const effectiveDate = toIsoDate(draft.effectiveDate)
  if (!effectiveDate) {
    return { error: 'date' }
  }
  const reason = draft.reason.trim()
  if (reason === '' || reason.length > MOVEMENT_REASON_MAX) {
    return { error: 'reason' }
  }

  const request: CreateMovementRequest = {
    employmentId: draft.employmentId,
    type: draft.type,
    effectiveDate,
    clearManager: false,
    reason,
  }
  const note = draft.note.trim()
  if (note !== '') {
    request.note = note
  }

  switch (draft.type) {
    case 'DepartmentChange': {
      if (draft.targetDepartmentId === '' || draft.targetDepartmentId === current.departmentId) {
        return { error: 'target' }
      }
      request.targetDepartmentId = draft.targetDepartmentId
      if (draft.targetPositionId !== '' && draft.targetPositionId !== current.positionId) {
        request.targetPositionId = draft.targetPositionId
      }
      return request
    }
    case 'PositionChange': {
      if (draft.targetPositionId === '' || draft.targetPositionId === current.positionId) {
        return { error: 'target' }
      }
      request.targetPositionId = draft.targetPositionId
      return request
    }
    case 'Promotion': {
      if (
        !isEligiblePromotionTarget(
          catalogue?.positions ?? [],
          current.departmentId,
          current.positionId,
          catalogue?.sourceOrganizationalLevel,
          draft.targetPositionId,
        )
      ) {
        return { error: 'target' }
      }
      request.targetPositionId = draft.targetPositionId
      return request
    }
    case 'PropertyTransfer': {
      if (
        draft.targetPropertyId === ''
        || draft.targetDepartmentId === ''
        || draft.targetPositionId === ''
      ) {
        return { error: 'target' }
      }
      request.targetPropertyId = draft.targetPropertyId
      request.targetDepartmentId = draft.targetDepartmentId
      request.targetPositionId = draft.targetPositionId
      return request
    }
    case 'ManagerChange': {
      if (draft.targetManagerEmploymentId === '' || draft.targetManagerEmploymentId === draft.employmentId) {
        return { error: 'target' }
      }
      request.targetManagerEmploymentId = draft.targetManagerEmploymentId
      return request
    }
  }
}

export function isScheduledCancellable(lifecycle: string, canManage: boolean): boolean {
  return canManage && lifecycle === 'Scheduled'
}

export const MOVEMENT_WIZARD_STEPS = ['personnel', 'type', 'date', 'target', 'reason', 'review'] as const
export type MovementWizardStep = (typeof MOVEMENT_WIZARD_STEPS)[number]

export function movementWizardStepStatus(
  id: MovementWizardStep,
  current: MovementWizardStep,
): 'complete' | 'current' | 'upcoming' {
  const index = MOVEMENT_WIZARD_STEPS.indexOf(id)
  const currentIndex = MOVEMENT_WIZARD_STEPS.indexOf(current)
  if (index < currentIndex) {
    return 'complete'
  }
  if (index === currentIndex) {
    return 'current'
  }
  return 'upcoming'
}

export function movementWizardShowsPicker(hasSelection: boolean, replacing: boolean): boolean {
  return !hasSelection || replacing
}

export function adjacentWizardStep(current: MovementWizardStep, direction: -1 | 1): MovementWizardStep | null {
  const next = MOVEMENT_WIZARD_STEPS[MOVEMENT_WIZARD_STEPS.indexOf(current) + direction]
  return next ?? null
}

const guidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export function looksLikeRawUserId(value: string | null | undefined): boolean {
  return typeof value === 'string' && guidPattern.test(value.trim())
}

export type MovementActorView = {
  id?: string | null
  displayName?: string | null
}

export function movementActorLabel(
  actor: MovementActorView | null | undefined,
  createdByUserId: string | null | undefined,
  copy: { system: string; unknown: string },
): string {
  const displayName = actor?.displayName?.trim()
  if (displayName && !looksLikeRawUserId(displayName)) {
    return displayName
  }

  const id = (actor?.id ?? createdByUserId)?.trim() ?? ''
  if (id === '') {
    return copy.system
  }

  return copy.unknown
}

export function matchesEmployeeSearch(
  item: { givenName: string; familyName: string; personnelNumber: string },
  needle: string,
): boolean {
  const query = needle.trim().toLocaleLowerCase()
  if (query === '') {
    return true
  }
  const fullName = `${item.givenName} ${item.familyName}`.toLocaleLowerCase()
  return (
    fullName.includes(query)
    || item.givenName.toLocaleLowerCase().includes(query)
    || item.familyName.toLocaleLowerCase().includes(query)
    || item.personnelNumber.toLocaleLowerCase().includes(query)
  )
}

export function creatableTypesExcludeAssignmentChange(): boolean {
  return !(CREATABLE_MOVEMENT_TYPES as readonly string[]).includes('AssignmentChange')
}

export function authorizedDestinationProperties<T extends { id: string }>(
  accessibleProperties: readonly T[],
  sourcePropertyId: string | null | undefined,
): T[] {
  if (!sourcePropertyId) {
    return []
  }

  return accessibleProperties.filter((item) => item.id !== sourcePropertyId)
}

export function selectableCreatableMovementTypes(
  accessibleProperties: readonly { id: string }[],
  sourcePropertyId: string | null | undefined,
): CreatableMovementType[] {
  const canTransfer = authorizedDestinationProperties(accessibleProperties, sourcePropertyId).length > 0
  return CREATABLE_MOVEMENT_TYPES.filter((type) => type !== 'PropertyTransfer' || canTransfer)
}

export function retainedPropertyTransferTarget(
  targetPropertyId: string,
  destinations: readonly { id: string }[],
): string {
  return destinations.some((item) => item.id === targetPropertyId) ? targetPropertyId : ''
}

export function reconcileMovementWizardDraft(
  draft: MovementWizardDraft,
  context: {
    selectableTypes: readonly CreatableMovementType[]
    destinationProperties: readonly { id: string }[]
    positions: PositionRecord[]
    sourceDepartmentId: string
    sourcePositionId: string
    sourceOrganizationalLevel: number | undefined
  },
): MovementWizardDraft {
  const type =
    draft.type !== '' && !context.selectableTypes.includes(draft.type)
      ? ''
      : draft.type
  const targetPropertyId = retainedPropertyTransferTarget(draft.targetPropertyId, context.destinationProperties)
  const propertyChanged = targetPropertyId !== draft.targetPropertyId
  const targetDepartmentId = propertyChanged ? '' : draft.targetDepartmentId
  let targetPositionId = propertyChanged ? '' : draft.targetPositionId
  if (
    type === 'Promotion'
    && targetPositionId !== ''
    && !isEligiblePromotionTarget(
      context.positions,
      context.sourceDepartmentId,
      context.sourcePositionId,
      context.sourceOrganizationalLevel,
      targetPositionId,
    )
  ) {
    targetPositionId = ''
  }

  if (
    type === draft.type
    && targetPropertyId === draft.targetPropertyId
    && targetDepartmentId === draft.targetDepartmentId
    && targetPositionId === draft.targetPositionId
  ) {
    return draft
  }

  return {
    ...draft,
    type,
    targetPropertyId,
    targetDepartmentId,
    targetPositionId,
  }
}
