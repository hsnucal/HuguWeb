import type { StatusBadgeVariant, StatusTone } from '../ui/StatusBadge'
import type {
  HousekeepingWorkState,
  NeededAction,
  RoomReadiness,
  RoomTechnicalServiceability,
  TaskPriority,
} from './roomOperationsApi'

export function readinessTone(readiness: RoomReadiness): StatusTone {
  switch (readiness) {
    case 'Dirty':
      return 'warning'
    case 'Clean':
      return 'info'
    case 'Inspected':
      return 'accent'
    case 'Ready':
      return 'success'
    default:
      return 'neutral'
  }
}

export function readinessMarker(readiness: RoomReadiness): 'warning' | 'info' | 'accent' | 'success' {
  return readinessTone(readiness) as 'warning' | 'info' | 'accent' | 'success'
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
  if (priority === 'Urgent') {
    return 'fill'
  }

  if (priority === 'High') {
    return 'priority'
  }

  return 'outline'
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

export function isTechnicallyUnusable(serviceability: RoomTechnicalServiceability): boolean {
  return serviceability !== 'Serviceable'
}

export function serviceabilityLabelKey(serviceability: RoomTechnicalServiceability): string {
  return `roomOperations.serviceability.${serviceability}`
}

export function serviceabilityTone(serviceability: RoomTechnicalServiceability): StatusTone {
  switch (serviceability) {
    case 'OutOfService':
      return 'danger'
    case 'OutOfOrder':
      return 'warning'
    default:
      return 'neutral'
  }
}

export function priorityLabelKey(priority: TaskPriority): string {
  return `roomOperations.priority.${priority}`
}

export function neededActionLabelKey(action: NeededAction): string {
  return `roomOperations.needed.${action}`
}
