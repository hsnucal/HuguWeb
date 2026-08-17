/*
  Operations Center prototype content.

  Isolated from domain logic and APIs. Labels are layout examples for the
  first product UI — not housekeeping state machines, room entities, or
  persisted hotel data. Replace this module when real operational queries exist.
*/

export type PrototypeSummaryCard = {
  id: string
  label: string
  value: string
  detail: string
  emphasis?: 'warning'
}

export type PrototypeAttentionItem = {
  id: string
  location: string
  summary: string
  reason: string
  urgencyLabel: string
  urgency: 'warning' | 'info' | 'danger'
}

export type PrototypeSnapshotItem = {
  id: string
  label: string
  count: string
  tone: 'neutral' | 'info' | 'warning' | 'success'
}

export type PrototypeUpcomingItem = {
  id: string
  time: string
  detail: string
}

export const prototypeToday: PrototypeSummaryCard[] = [
  { id: 'arrivals', label: 'Arrivals', value: '12', detail: 'Peak around 14:00' },
  { id: 'departures', label: 'Departures', value: '9', detail: 'Until 11:00' },
  { id: 'not-ready', label: 'Rooms not ready', value: '4', detail: '2 due before next arrival', emphasis: 'warning' },
]

export const prototypeAttention: PrototypeAttentionItem[] = [
  {
    id: '214',
    location: 'Room 214',
    summary: 'Arrival approaching — room not ready',
    reason: 'Guest arrival at 15:00 · cleaning delayed',
    urgencyLabel: 'Time-sensitive',
    urgency: 'warning',
  },
  {
    id: '307',
    location: 'Room 307',
    summary: 'Supervisor inspection waiting',
    reason: 'Cleaning finished · waiting on supervisor',
    urgencyLabel: 'Waiting',
    urgency: 'info',
  },
  {
    id: '118',
    location: 'Room 118',
    summary: 'Maintenance issue blocking readiness',
    reason: 'Open fault · room cannot be prepared',
    urgencyLabel: 'Blocking',
    urgency: 'danger',
  },
]

export const prototypeSnapshot: PrototypeSnapshotItem[] = [
  { id: 'dirty', label: 'Dirty', count: '8', tone: 'neutral' },
  { id: 'cleaning', label: 'Cleaning', count: '11', tone: 'info' },
  { id: 'inspection', label: 'Inspection', count: '3', tone: 'warning' },
  { id: 'ready', label: 'Ready', count: '42', tone: 'success' },
]

export const prototypeUpcoming: PrototypeUpcomingItem[] = [
  { id: 'group', time: '16:00', detail: 'Group arrival · 18 rooms' },
  { id: 'vip', time: '18:00', detail: 'VIP arrival · 501' },
]
