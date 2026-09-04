import assert from 'node:assert/strict'
import test from 'node:test'
import { buildPrimaryNav, buildSettingsNav, isPrimaryNavActive, resolveWorkforceNavTo } from './sidebarNav.ts'
import { resolveSidebarChrome } from './sidebarChrome.ts'

function destinationIds(options: Parameters<typeof buildPrimaryNav>[0]) {
  return buildPrimaryNav(options).map((item) => {
    if (item.destination.kind === 'link') {
      return `${item.id}:${item.destination.to}`
    }

    return `${item.id}:future`
  })
}

const permitted = {
  canReadRoomOperations: true,
  canReadMaintenance: true,
  canReadHrEmployees: true,
  canReadWorkforce: true,
}

test('permissions produce the same destinations regardless of collapsed chrome', () => {
  const expanded = destinationIds(permitted)
  const collapsed = destinationIds(permitted)
  const expandedChrome = resolveSidebarChrome({ collapsedPreference: false, isNarrowViewport: false })
  const collapsedChrome = resolveSidebarChrome({ collapsedPreference: true, isNarrowViewport: false })

  assert.deepEqual(expanded, collapsed)
  assert.notEqual(expandedChrome.railCollapsed, collapsedChrome.railCollapsed)
  assert.ok(expanded.includes('workforce:/app/workforce'))
  assert.ok(expanded.includes('room-operations:/app/room-operations'))
  assert.ok(expanded.includes('technical-service:/app/technical-service'))
})

test('workforce without HR read still appears and routes to departments', () => {
  const items = buildPrimaryNav({
    canReadRoomOperations: false,
    canReadMaintenance: false,
    canReadHrEmployees: false,
    canReadWorkforce: true,
  })
  const workforce = items.find((item) => item.id === 'workforce')
  assert.ok(workforce)
  assert.equal(workforce.destination.kind, 'link')
  if (workforce.destination.kind === 'link') {
    assert.equal(workforce.destination.to, '/app/workforce/departments')
  }
})

test('hr.leave.request shows My Leave nav destination', () => {
  assert.equal(
    resolveWorkforceNavTo({
      canReadHrEmployees: false,
      canReadWorkforce: false,
      canReadHrSchedule: true,
    }),
    '/app/workforce/shift-plan',
  )

  const items = buildPrimaryNav({
    canReadRoomOperations: false,
    canReadMaintenance: false,
    canReadHrEmployees: false,
    canReadWorkforce: false,
    canReadHrSchedule: true,
  })
  const workforce = items.find((item) => item.id === 'workforce')
  assert.ok(workforce)
  assert.equal(workforce.destination.kind, 'link')
  if (workforce.destination.kind === 'link') {
    assert.equal(workforce.destination.to, '/app/workforce/shift-plan')
  }
})

test('leave-only users get Workforce nav routed to leave management', () => {
  assert.equal(
    resolveWorkforceNavTo({
      canReadHrEmployees: false,
      canReadWorkforce: false,
      canReadHrLeave: true,
    }),
    '/app/workforce/leave-management',
  )
})

test('hr.leave.request shows My Leave nav destination', () => {
  const items = buildPrimaryNav({
    canReadRoomOperations: false,
    canReadMaintenance: false,
    canReadHrEmployees: false,
    canReadWorkforce: false,
    canRequestHrLeave: true,
  })
  const myLeave = items.find((item) => item.id === 'my-leave')
  assert.ok(myLeave)
  assert.equal(myLeave.destination.kind, 'link')
  if (myLeave.destination.kind === 'link') {
    assert.equal(myLeave.destination.to, '/app/my/leave')
  }
})

test('users without hr.leave.request do not see My Leave nav', () => {
  const items = buildPrimaryNav({
    canReadRoomOperations: false,
    canReadMaintenance: true,
    canReadHrEmployees: false,
    canReadWorkforce: false,
    canRequestHrLeave: false,
  })
  assert.equal(
    items.find((item) => item.id === 'my-leave'),
    undefined,
  )
})

test('modules without permission are omitted while future placeholders remain', () => {
  const items = buildPrimaryNav({
    canReadRoomOperations: false,
    canReadMaintenance: false,
    canReadHrEmployees: false,
    canReadWorkforce: false,
  })
  assert.deepEqual(
    items.map((item) => item.id),
    ['home', 'reservations', 'tasks'],
  )
})

test('settings availability follows the permission flag and stays independent of chrome', () => {
  assert.equal(buildSettingsNav(true).destination.kind, 'link')
  assert.equal(buildSettingsNav(false).destination.kind, 'future')
})

test('nav items always carry a label key so collapsed icons stay named', () => {
  for (const item of buildPrimaryNav(permitted)) {
    assert.match(item.labelKey, /^navigation\./)
  }
})

test('hr.attendance.read shows Puantaj as a top-level item after Personel', () => {
  const items = buildPrimaryNav({
    ...permitted,
    canReadHrAttendance: true,
  })
  const ids = items.map((item) => item.id)
  assert.ok(ids.includes('attendance'))
  assert.ok(ids.indexOf('workforce') < ids.indexOf('attendance'))
  const attendance = items.find((item) => item.id === 'attendance')
  assert.ok(attendance)
  assert.equal(attendance.labelKey, 'navigation.attendance')
  assert.equal(attendance.destination.kind, 'link')
  if (attendance.destination.kind === 'link') {
    assert.equal(attendance.destination.to, '/app/attendance')
  }
})

test('Puantaj sidebar is hidden without hr.attendance.read', () => {
  const items = buildPrimaryNav({
    ...permitted,
    canReadHrAttendance: false,
  })
  assert.equal(
    items.find((item) => item.id === 'attendance'),
    undefined,
  )
})

test('attendance-only users get a top-level Puantaj destination without Workforce', () => {
  const items = buildPrimaryNav({
    canReadRoomOperations: false,
    canReadMaintenance: false,
    canReadHrEmployees: false,
    canReadWorkforce: false,
    canReadHrAttendance: true,
  })
  assert.equal(
    items.find((item) => item.id === 'workforce'),
    undefined,
  )
  const attendance = items.find((item) => item.id === 'attendance')
  assert.ok(attendance)
  assert.equal(attendance.destination.kind, 'link')
  if (attendance.destination.kind === 'link') {
    assert.equal(attendance.destination.to, '/app/attendance')
  }
})

test('hr.movements.read shows Personel Hareketleri near Puantaj', () => {
  const items = buildPrimaryNav({
    ...permitted,
    canReadHrAttendance: true,
    canReadHrMovements: true,
  })
  const ids = items.map((item) => item.id)
  assert.ok(ids.includes('movements'))
  assert.ok(ids.indexOf('workforce') < ids.indexOf('movements'))
  const movements = items.find((item) => item.id === 'movements')
  assert.ok(movements)
  assert.equal(movements.icon, 'history')
  assert.equal(movements.labelKey, 'navigation.movements')
  assert.equal(movements.destination.kind, 'link')
  if (movements.destination.kind === 'link') {
    assert.equal(movements.destination.to, '/app/workforce/movements')
  }
})

test('Personel Hareketleri sidebar is hidden without hr.movements.read', () => {
  const items = buildPrimaryNav({
    ...permitted,
    canReadHrMovements: false,
  })
  assert.equal(
    items.find((item) => item.id === 'movements'),
    undefined,
  )
})

test('movements route highlights Personel Hareketleri not Personel', () => {
  const items = buildPrimaryNav({
    ...permitted,
    canReadHrAttendance: true,
    canReadHrMovements: true,
  })
  const workforce = items.find((item) => item.id === 'workforce')
  const movements = items.find((item) => item.id === 'movements')
  assert.ok(workforce)
  assert.ok(movements)
  assert.equal(isPrimaryNavActive(workforce, '/app/workforce/movements', true), false)
  assert.equal(isPrimaryNavActive(workforce, '/app/workforce', true), true)
  assert.equal(isPrimaryNavActive(workforce, '/app/workforce/departments', true), true)
  assert.equal(isPrimaryNavActive(movements, '/app/workforce/movements', true), true)
  assert.equal(isPrimaryNavActive(movements, '/app/workforce', false), false)
})
