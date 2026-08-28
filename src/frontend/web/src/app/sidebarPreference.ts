export const SIDEBAR_COLLAPSED_STORAGE_KEY = 'hugu.sidebar.collapsed'

type StorageLike = Pick<Storage, 'getItem' | 'setItem'>

export function parseSidebarCollapsed(value: string | null | undefined): boolean {
  return value === 'true'
}

export function readSidebarCollapsed(storage?: StorageLike | null): boolean {
  try {
    const source = storage ?? (typeof window === 'undefined' ? null : window.localStorage)
    if (!source) {
      return false
    }

    return parseSidebarCollapsed(source.getItem(SIDEBAR_COLLAPSED_STORAGE_KEY))
  } catch {
    return false
  }
}

export function persistSidebarCollapsed(collapsed: boolean, storage?: StorageLike | null) {
  try {
    const source = storage ?? (typeof window === 'undefined' ? null : window.localStorage)
    if (!source) {
      return
    }

    source.setItem(SIDEBAR_COLLAPSED_STORAGE_KEY, collapsed ? 'true' : 'false')
  } catch {
    // Storage may be unavailable (private mode). Keep the in-memory UI preference.
  }
}
