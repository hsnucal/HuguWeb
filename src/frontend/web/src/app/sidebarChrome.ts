export const SIDEBAR_DRAWER_MEDIA = '(max-width: 768px)'

export function resolveSidebarChrome(options: {
  collapsedPreference: boolean
  isNarrowViewport: boolean
}): { railCollapsed: boolean; isDrawer: boolean } {
  if (options.isNarrowViewport) {
    return { railCollapsed: false, isDrawer: true }
  }

  return { railCollapsed: options.collapsedPreference, isDrawer: false }
}
