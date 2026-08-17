/*
  Operations Center prototype content.

  Isolated from domain logic and APIs. Labels are layout examples for the
  first product UI — not housekeeping state machines, room entities, or
  persisted hotel data. Replace this module when real operational queries exist.
*/

export type PrototypeSummaryCard = {
  id: string
  labelKey: string
  value: number
  detailKey: string
  detailTime?: { hours: number; minutes: number }
  detailCount?: number
  emphasis?: 'warning'
}

export type PrototypeAttentionItem = {
  id: string
  roomNumber: string
  summaryKey: string
  reasonKey: string
  reasonTime?: { hours: number; minutes: number }
  urgencyLabelKey: string
  urgency: 'warning' | 'info' | 'danger'
}

export type PrototypeSnapshotItem = {
  id: string
  labelKey: string
  count: number
  tone: 'neutral' | 'info' | 'warning' | 'success'
}

export type PrototypeUpcomingItem = {
  id: string
  hours: number
  minutes: number
  detailKey: string
  count?: number
  room?: string
}

export const prototypeToday: PrototypeSummaryCard[] = [
  {
    id: 'arrivals',
    labelKey: 'operations.arrivals',
    value: 12,
    detailKey: 'operations.arrivalsDetail',
    detailTime: { hours: 14, minutes: 0 },
  },
  {
    id: 'departures',
    labelKey: 'operations.departures',
    value: 9,
    detailKey: 'operations.departuresDetail',
    detailTime: { hours: 11, minutes: 0 },
  },
  {
    id: 'not-ready',
    labelKey: 'operations.roomsNotReady',
    value: 4,
    detailKey: 'operations.roomsNotReadyDetail',
    detailCount: 2,
    emphasis: 'warning',
  },
]

export const prototypeAttention: PrototypeAttentionItem[] = [
  {
    id: '214',
    roomNumber: '214',
    summaryKey: 'operations.arrivalNotReady',
    reasonKey: 'operations.arrivalNotReadyReason',
    reasonTime: { hours: 15, minutes: 0 },
    urgencyLabelKey: 'operations.timeSensitive',
    urgency: 'warning',
  },
  {
    id: '307',
    roomNumber: '307',
    summaryKey: 'operations.inspectionWaiting',
    reasonKey: 'operations.inspectionWaitingReason',
    urgencyLabelKey: 'operations.waiting',
    urgency: 'info',
  },
  {
    id: '118',
    roomNumber: '118',
    summaryKey: 'operations.maintenanceBlocking',
    reasonKey: 'operations.maintenanceBlockingReason',
    urgencyLabelKey: 'operations.blocking',
    urgency: 'danger',
  },
]

export const prototypeSnapshot: PrototypeSnapshotItem[] = [
  { id: 'dirty', labelKey: 'operations.dirty', count: 8, tone: 'neutral' },
  { id: 'cleaning', labelKey: 'operations.cleaning', count: 11, tone: 'info' },
  { id: 'inspection', labelKey: 'operations.inspection', count: 3, tone: 'warning' },
  { id: 'ready', labelKey: 'operations.ready', count: 42, tone: 'success' },
]

export const prototypeUpcoming: PrototypeUpcomingItem[] = [
  { id: 'group', hours: 16, minutes: 0, detailKey: 'operations.groupArrival', count: 18 },
  { id: 'vip', hours: 18, minutes: 0, detailKey: 'operations.vipArrival', room: '501' },
]
