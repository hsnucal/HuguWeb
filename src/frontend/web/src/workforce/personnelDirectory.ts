export function asCollection<T>(payload: unknown): T[] {
  if (Array.isArray(payload)) {
    return payload as T[]
  }

  if (payload && typeof payload === 'object') {
    const items = (payload as { items?: unknown }).items
    if (Array.isArray(items)) {
      return items as T[]
    }
  }

  return []
}

export function asHrEmployeeList<T>(payload: unknown): T[] {
  return asCollection<T>(payload)
}

export type PersonnelEmptyKind = 'none' | 'dataset' | 'filter'

export function personnelEmptyKind(input: {
  loadFailed: boolean
  totalCount: number
  visibleCount: number
}): PersonnelEmptyKind {
  if (input.loadFailed) {
    return 'none'
  }

  if (input.totalCount === 0) {
    return 'dataset'
  }

  if (input.visibleCount === 0) {
    return 'filter'
  }

  return 'none'
}

export function selectedEmployeeIdAfterDirectoryLoad(
  items: ReadonlyArray<{ employeeId: string }>,
): string | null {
  if (items.length === 0) {
    return null
  }

  return null
}

export function isSuccessfulEmptyPersonnelList(payload: unknown, httpOk: boolean): boolean {
  return httpOk && asHrEmployeeList(payload).length === 0
}
