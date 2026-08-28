import { useCallback, useState, useSyncExternalStore } from 'react'
import { SIDEBAR_DRAWER_MEDIA } from './sidebarChrome'
import { persistSidebarCollapsed, readSidebarCollapsed } from './sidebarPreference'

export function useNarrowViewport() {
  return useSyncExternalStore(subscribeNarrowViewport, getNarrowViewportSnapshot, () => false)
}

export function useSidebarCollapsedPreference() {
  const [collapsed, setCollapsed] = useState(() => readSidebarCollapsed())

  const setSidebarCollapsed = useCallback((next: boolean) => {
    setCollapsed(next)
    persistSidebarCollapsed(next)
  }, [])

  const toggleSidebarCollapsed = useCallback(() => {
    setCollapsed((current) => {
      const next = !current
      persistSidebarCollapsed(next)
      return next
    })
  }, [])

  return { collapsed, setSidebarCollapsed, toggleSidebarCollapsed }
}

function subscribeNarrowViewport(onChange: () => void) {
  const media = window.matchMedia(SIDEBAR_DRAWER_MEDIA)
  media.addEventListener('change', onChange)
  return () => media.removeEventListener('change', onChange)
}

function getNarrowViewportSnapshot() {
  return window.matchMedia(SIDEBAR_DRAWER_MEDIA).matches
}
