import type { StatusBadgeVariant, StatusTone } from '../ui/StatusBadge'
import type { HousekeepingWorkState, NeededAction, RoomReadiness, TaskPriority } from './roomOperationsApi'

export function readinessTone(readiness: RoomReadiness): StatusTone {
  switch (readiness) {
    case 'Dirty':
      return 'danger'
    case 'Clean':
      return 'warning'
    case 'Inspected':
      return 'info'
    case 'Ready':
      return 'success'
    default:
      return 'neutral'
  }
}

export function priorityTone(priority: TaskPriority): StatusTone {
  switch (priority) {
    case 'Urgent':
      return 'danger'
    case 'High':
      return 'warning'
    default:
      return 'neutral'
  }
}

export function priorityVariant(priority: TaskPriority): StatusBadgeVariant {
  return priority === 'Urgent' ? 'fill' : 'outline'
}

export function workStateTone(state: HousekeepingWorkState | null): StatusTone {
  if (state === 'Open') {
    return 'info'
  }

  return 'neutral'
}

export function neededActionFromState(
  readiness: RoomReadiness,
  workState: HousekeepingWorkState | null,
  isActive = true,
): NeededAction {
  if (!isActive) {
    return 'none'
  }

  if (readiness === 'Dirty' && workState === 'Open') {
    return 'complete-cleaning'
  }

  if (readiness === 'Dirty') {
    return 'needs-cleaning'
  }

  if (readiness === 'Clean') {
    return 'inspect'
  }

  return 'none'
}

export function readinessLabelKey(readiness: RoomReadiness): string {
  return `roomOperations.readiness.${readiness}`
}

export function priorityLabelKey(priority: TaskPriority): string {
  return `roomOperations.priority.${priority}`
}

export function neededActionLabelKey(action: NeededAction): string {
  return `roomOperations.needed.${action}`
}
