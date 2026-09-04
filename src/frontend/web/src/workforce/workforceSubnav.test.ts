import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'
import { buildWorkforceSubnav } from './workforceSubnav.ts'

const allAccess = {
  canReadHrEmployees: true,
  canReadWorkforce: true,
  canReadHrLeave: true,
  canReadHrShiftDefinitions: true,
  canReadHrSchedule: true,
}

test('Personel submenu no longer contains Personel Hareketleri', () => {
  const items = buildWorkforceSubnav(allAccess)
  assert.deepEqual(
    items.map((item) => item.to),
    [
      '/app/workforce',
      '/app/workforce/departments',
      '/app/workforce/positions',
      '/app/workforce/official-settings',
      '/app/workforce/leave-management',
      '/app/workforce/leave-types',
      '/app/workforce/shift-definitions',
      '/app/workforce/shift-plan',
    ],
  )
  assert.equal(
    items.some((item) => item.to === '/app/workforce/movements' || item.labelKey.includes('movements')),
    false,
  )
})

test('WorkforceLayout uses the Personel submenu without a movements link', () => {
  const layout = readFileSync(new URL('./WorkforceLayout.tsx', import.meta.url), 'utf8')
  assert.match(layout, /buildWorkforceSubnav/)
  assert.match(layout, /className=\{styles\.subnav\}/)
  assert.doesNotMatch(layout, /<NavLink to="\/app\/workforce\/movements"/)
})

test('WorkforceLayout evaluates shift-definition read as a boolean', () => {
  const layout = readFileSync(new URL('./WorkforceLayout.tsx', import.meta.url), 'utf8')
  assert.match(layout, /const canReadShiftDefinitions = canReadHrShiftDefinitions\(user\)/)
  assert.match(layout, /canReadHrShiftDefinitions: canReadShiftDefinitions/)
  assert.doesNotMatch(
    layout,
    /buildWorkforceSubnav\(\{[\s\S]*canReadHrShiftDefinitions,/,
  )
})

test('Personel submenu is not composed into the movements route', () => {
  const app = readFileSync(new URL('../app/App.tsx', import.meta.url), 'utf8')
  const layout = readFileSync(new URL('./WorkforceLayout.tsx', import.meta.url), 'utf8')
  assert.match(app, /path="workforce\/movements" element=\{<PersonnelMovementsPage \/>\}/)
  assert.doesNotMatch(app, /path="workforce"[\s\S]*path="movements"/)
  assert.doesNotMatch(layout, /pathname\.startsWith\('\/app\/workforce\/movements'\)/)
})
