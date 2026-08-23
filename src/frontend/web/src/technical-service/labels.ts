import type { StatusTone } from '../ui/StatusBadge'
import type {
  MaintenanceHistoryEvent,
  MaintenanceIssueStatus,
  MaintenanceNeededAction,
  MaintenancePriority,
  OutageClassification,
  PreparationImpact,
  RoomServiceabilityState,
} from './maintenanceApi'

export function statusLabelKey(status: MaintenanceIssueStatus) {
  return `maintenance.status.${status}` as const
}

export function statusTone(status: MaintenanceIssueStatus): StatusTone {
  switch (status) {
    case 'Open':
      return 'info'
    case 'InProgress':
      return 'warning'
    case 'UnableToResolve':
      return 'danger'
    case 'Resolved':
      return 'success'
    default:
      return 'neutral'
  }
}

export function priorityLabelKey(priority: MaintenancePriority) {
  return `maintenance.priority.${priority}` as const
}

export function priorityTone(priority: MaintenancePriority): StatusTone {
  switch (priority) {
    case 'Urgent':
      return 'danger'
    case 'High':
      return 'warning'
    default:
      return 'neutral'
  }
}

export function neededActionLabelKey(action: MaintenanceNeededAction) {
  return `maintenance.needed.${action}` as const
}

export function serviceabilityLabelKey(state: RoomServiceabilityState) {
  return `maintenance.serviceability.${state}` as const
}

export function serviceabilityTone(state: RoomServiceabilityState): StatusTone {
  switch (state) {
    case 'OutOfService':
      return 'danger'
    case 'OutOfOrder':
      return 'warning'
    default:
      return 'success'
  }
}

export function outageLabelKey(value: OutageClassification) {
  return `maintenance.outage.${value}` as const
}

export function historyLabelKey(event: MaintenanceHistoryEvent) {
  return `maintenance.historyEvent.${event}` as const
}

export function historyMarker(
  event: MaintenanceHistoryEvent,
): 'neutral' | 'success' | 'warning' | 'danger' | 'info' | 'accent' {
  switch (event) {
    case 'Resolved':
      return 'success'
    case 'UnableToResolve':
      return 'danger'
    case 'Started':
    case 'Resumed':
      return 'warning'
    case 'Created':
    case 'Assigned':
      return 'info'
    default:
      return 'neutral'
  }
}

export function impactLabelKey(value: PreparationImpact) {
  return `maintenance.impact.${value}` as const
}
