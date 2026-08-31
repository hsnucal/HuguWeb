import assert from 'node:assert/strict'
import test from 'node:test'
import { buildPrimaryNav, buildSettingsNav, resolveWorkforceNavTo } from './sidebarNav.ts'
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

test('schedule-only users get Workforce nav routed to shift plan', () => {
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
