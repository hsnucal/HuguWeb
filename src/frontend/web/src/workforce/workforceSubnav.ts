export type WorkforceSubnavItem = {
  to: string
  labelKey: string
  end?: boolean
}

export function buildWorkforceSubnav(options: {
  canReadHrEmployees: boolean
  canReadWorkforce: boolean
  canReadHrLeave: boolean
  canReadHrShiftDefinitions: boolean
  canReadHrSchedule: boolean
}): WorkforceSubnavItem[] {
  const items: WorkforceSubnavItem[] = []

  if (options.canReadHrEmployees) {
    items.push({ to: '/app/workforce', labelKey: 'workforce.directory', end: true })
  }

  if (options.canReadWorkforce) {
    items.push(
      { to: '/app/workforce/departments', labelKey: 'workforce.departments' },
      { to: '/app/workforce/positions', labelKey: 'workforce.positions' },
      { to: '/app/workforce/official-settings', labelKey: 'workforce.officialSettings' },
    )
  }

  if (options.canReadHrLeave) {
    items.push(
      { to: '/app/workforce/leave-management', labelKey: 'workforce.leaveManagement' },
      { to: '/app/workforce/leave-types', labelKey: 'workforce.leaveTypes' },
    )
  }

  if (options.canReadHrShiftDefinitions) {
    items.push({ to: '/app/workforce/shift-definitions', labelKey: 'workforce.shiftDefinitions' })
  }

  if (options.canReadHrSchedule) {
    items.push({ to: '/app/workforce/shift-plan', labelKey: 'workforce.shiftPlan' })
  }

  return items
}
