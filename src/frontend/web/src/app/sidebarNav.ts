export type SidebarIconId =
  | 'home'
  | 'rooms'
  | 'wrench'
  | 'people'
  | 'reservations'
  | 'tasks'
  | 'settings'

export type SidebarNavDestination =
  | { kind: 'link'; to: string; end?: boolean }
  | { kind: 'future' }

export type SidebarNavItem = {
  id: string
  icon: SidebarIconId
  labelKey: string
  destination: SidebarNavDestination
}

const futureNav: ReadonlyArray<Pick<SidebarNavItem, 'id' | 'icon' | 'labelKey'>> = [
  { id: 'reservations', icon: 'reservations', labelKey: 'navigation.reservations' },
  { id: 'tasks', icon: 'tasks', labelKey: 'navigation.tasks' },
]

export function buildPrimaryNav(options: {
  canReadRoomOperations: boolean
  canReadMaintenance: boolean
  canReadHrEmployees: boolean
  canReadWorkforce: boolean
}): SidebarNavItem[] {
  const items: SidebarNavItem[] = [
    {
      id: 'home',
      icon: 'home',
      labelKey: 'navigation.home',
      destination: { kind: 'link', to: '/app', end: true },
    },
  ]

  if (options.canReadRoomOperations) {
    items.push({
      id: 'room-operations',
      icon: 'rooms',
      labelKey: 'navigation.roomOperations',
      destination: { kind: 'link', to: '/app/room-operations' },
    })
  }

  if (options.canReadMaintenance) {
    items.push({
      id: 'technical-service',
      icon: 'wrench',
      labelKey: 'navigation.technicalService',
      destination: { kind: 'link', to: '/app/technical-service' },
    })
  }

  if (options.canReadHrEmployees || options.canReadWorkforce) {
    items.push({
      id: 'workforce',
      icon: 'people',
      labelKey: 'navigation.workforce',
      destination: {
        kind: 'link',
        to: options.canReadHrEmployees ? '/app/workforce' : '/app/workforce/departments',
      },
    })
  }

  for (const item of futureNav) {
    items.push({
      ...item,
      destination: { kind: 'future' },
    })
  }

  return items
}

export function buildSettingsNav(canOpenSettings: boolean): SidebarNavItem {
  if (canOpenSettings) {
    return {
      id: 'settings',
      icon: 'settings',
      labelKey: 'navigation.settings',
      destination: { kind: 'link', to: '/app/settings/users' },
    }
  }

  return {
    id: 'settings',
    icon: 'settings',
    labelKey: 'navigation.settings',
    destination: { kind: 'future' },
  }
}
