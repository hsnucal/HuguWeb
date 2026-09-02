import assert from 'node:assert/strict'
import test from 'node:test'
import { canLoadPropertyStructure, workplaceLabelsFromUser } from './workforceWorkplaceLabels.ts'
import type { CurrentUser } from '../shared/types.ts'

test('property structure load is gated on an explicit active property id', () => {
  assert.equal(canLoadPropertyStructure(null), false)
  assert.equal(canLoadPropertyStructure(undefined), false)
  assert.equal(canLoadPropertyStructure(''), false)
  assert.equal(canLoadPropertyStructure('prop-ankara'), true)
})

test('workplace labels resolve organization and selected property from session user', () => {
  const user: CurrentUser = {
    id: 'u-1',
    email: 'hr.manager@localhost',
    preferredLanguage: 'tr',
    permissions: [],
    organizationId: 'org-1',
    organizationName: 'Demo Hotel Group',
    propertyId: 'prop-ankara',
    accessibleProperties: [
      { id: 'prop-ankara', name: 'Ankara Hotel', timeZoneId: 'Europe/Istanbul' },
      { id: 'prop-izmir', name: 'Izmir Hotel', timeZoneId: 'Europe/Istanbul' },
    ],
  }

  const labels = workplaceLabelsFromUser(user)
  assert.equal(labels.organizationName, 'Demo Hotel Group')
  assert.equal(labels.propertyName, 'Ankara Hotel')
  assert.equal(labels.hasProperty, true)
})

test('workplace labels expose property selection requirement without inventing a property', () => {
  const user: CurrentUser = {
    id: 'u-2',
    email: 'hr.corporate@localhost',
    preferredLanguage: 'tr',
    permissions: [],
    organizationId: 'org-1',
    organizationName: 'Demo Hotel Group',
    propertySelectionRequired: true,
    accessibleProperties: [
      { id: 'prop-ankara', name: 'Ankara Hotel', timeZoneId: 'Europe/Istanbul' },
    ],
  }

  const labels = workplaceLabelsFromUser(user)
  assert.equal(labels.organizationName, 'Demo Hotel Group')
  assert.equal(labels.propertyName, 'Ankara Hotel')
  assert.equal(labels.hasProperty, false)
  assert.equal(labels.propertySelectionRequired, true)
})
