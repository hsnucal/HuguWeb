import type { StatusTone } from '../ui/StatusBadge'
import type { EmployeeDirectoryItem } from './workforceApi'

export type WorkforceView = 'active' | 'scheduled' | 'former'

export function employmentStatusTone(status: string | undefined): StatusTone {
  if (status === 'Active') {
    return 'success'
  }

  if (status === 'Scheduled') {
    return 'info'
  }

  return 'neutral'
}

export function matchesWorkforceSearch(person: EmployeeDirectoryItem, query: string): boolean {
  const needle = query.trim().toLocaleLowerCase()
  if (needle === '') {
    return true
  }

  const haystack = `${person.givenName} ${person.familyName} ${person.personnelNumber}`.toLocaleLowerCase()
  return haystack.includes(needle)
}

export function inWorkforceView(person: EmployeeDirectoryItem, view: WorkforceView): boolean {
  if (view === 'active') {
    return person.employmentStatus === 'Active'
  }

  if (view === 'scheduled') {
    return person.employmentStatus === 'Scheduled'
  }

  return person.employmentStatus === 'Ended'
}
