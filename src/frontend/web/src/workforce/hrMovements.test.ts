import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'
import {
  CREATABLE_MOVEMENT_TYPES,
  buildMovementsListPath,
  hrMovementErrorKeyFromCode,
  hrMovementErrorMessage,
  hrMovementErrorStep,
} from './hrMovementsPaths.ts'
import {
  adjacentWizardStep,
  assignmentMovementDateTooEarly,
  authorizedDestinationProperties,
  buildCreateMovementRequest,
  coveringPrimaryAssignment,
  creatableTypesExcludeAssignmentChange,
  departmentChangeNeedsTargetPosition,
  earliestAssignmentMovementDate,
  emptyMovementWizardDraft,
  isMovementWizardDirty,
  isScheduledCancellable,
  looksLikeRawUserId,
  matchesEmployeeSearch,
  movementActorLabel,
  movementDiffSummary,
  movementLifecycleLabelKey,
  movementTypeLabelKey,
  movementWizardShowsPicker,
  movementWizardStepStatus,
  reconcileMovementWizardDraft,
  retainedPropertyTransferTarget,
  selectableCreatableMovementTypes,
  sourceAssignmentAsOf,
} from './movementDisplay.ts'
import { workplacePropertyBannerRequired } from '../app/workplacePropertyBanner.ts'
import { isEligiblePromotionTarget, promotionTargetPositions } from './assignmentOptions.ts'
import { tr as hrTr } from '../i18n/hr/tr.ts'
import { en as hrEn } from '../i18n/hr/en.ts'
import { ru as hrRu } from '../i18n/hr/ru.ts'

test('list query sends backend filters without organization id', () => {
  const path = buildMovementsListPath({
    dateFrom: '2026-09-01',
    dateTo: '2026-09-30',
    type: 'Promotion',
    departmentId: 'dep-1',
    employeeId: 'emp-1',
    propertyId: 'prop-1',
    search: 'Ayşe',
  })
  assert.match(path, /^\/api\/hr\/movements\?/)
  assert.match(path, /dateFrom=2026-09-01/)
  assert.match(path, /type=Promotion/)
  assert.match(path, /search=Ay%C5%9Fe/)
  assert.doesNotMatch(path, /organizationId/i)
})

test('blank filters are omitted from the movements query', () => {
  assert.equal(buildMovementsListPath({ search: '  ' }), '/api/hr/movements')
})

test('movement types and lifecycle are localized without enum dump', () => {
  assert.equal(hrTr.movements.types.DepartmentChange, 'Departman Değişikliği')
  assert.equal(hrTr.movements.types.AssignmentChange, 'Organizasyon Değişikliği')
  assert.equal(hrTr.movements.lifecycle.Scheduled, 'Planlandı')
  assert.equal(movementTypeLabelKey('DutyChange'), 'movements.types.unknown')
  assert.equal(movementLifecycleLabelKey('Scheduled'), 'movements.lifecycle.Scheduled')
})

test('AssignmentChange is not a selectable create type', () => {
  assert.equal(CREATABLE_MOVEMENT_TYPES.includes('AssignmentChange' as never), false)
  assert.equal(creatableTypesExcludeAssignmentChange(), true)
})

test('PropertyTransfer is omitted when the user has no authorized destination property', () => {
  const sourceOnly = [{ id: 'ankara', name: 'Ankara Hotel', timeZoneId: 'Europe/Istanbul' }]
  assert.deepEqual(authorizedDestinationProperties(sourceOnly, 'ankara'), [])
  assert.equal(selectableCreatableMovementTypes(sourceOnly, 'ankara').includes('PropertyTransfer'), false)
  const wizard = readFileSync(new URL('./PersonnelMovementWizard.tsx', import.meta.url), 'utf8')
  assert.match(wizard, /selectableCreatableMovementTypes/)
  assert.match(wizard, /selectableTypes\.map/)
  assert.doesNotMatch(wizard, /CREATABLE_MOVEMENT_TYPES\.map/)
})

test('PropertyTransfer is selectable when another accessible property exists', () => {
  const accessible = [
    { id: 'ankara', name: 'Ankara Hotel', timeZoneId: 'Europe/Istanbul' },
    { id: 'antalya', name: 'Antalya Hotel', timeZoneId: 'Europe/Istanbul' },
  ]
  const destinations = authorizedDestinationProperties(accessible, 'ankara')
  assert.deepEqual(
    destinations.map((item) => item.id),
    ['antalya'],
  )
  assert.equal(selectableCreatableMovementTypes(accessible, 'ankara').includes('PropertyTransfer'), true)
})

test('target property list excludes source and unauthorized properties', () => {
  const accessible = [
    { id: 'ankara', name: 'Ankara Hotel', timeZoneId: 'Europe/Istanbul' },
    { id: 'antalya', name: 'Antalya Hotel', timeZoneId: 'Europe/Istanbul' },
  ]
  const destinations = authorizedDestinationProperties(accessible, 'ankara')
  assert.equal(
    destinations.some((item) => item.id === 'ankara'),
    false,
  )
  assert.equal(
    destinations.some((item) => item.id === 'unauthorized'),
    false,
  )
  const wizard = readFileSync(new URL('./PersonnelMovementWizard.tsx', import.meta.url), 'utf8')
  assert.match(wizard, /destinationProperties\.map/)
  assert.doesNotMatch(
    wizard,
    /accessibleProperties\s*\.filter\(\(item\) => item\.id !== structure\?\.propertyId\)/,
  )
})

test('stale property-transfer target is cleared when source or destinations change', () => {
  const antalyaOnly = [{ id: 'antalya', name: 'Antalya Hotel', timeZoneId: 'Europe/Istanbul' }]
  assert.equal(retainedPropertyTransferTarget('antalya', antalyaOnly), 'antalya')
  assert.equal(retainedPropertyTransferTarget('antalya', []), '')
  assert.equal(retainedPropertyTransferTarget('ankara', antalyaOnly), '')
  const draft = {
    ...emptyMovementWizardDraft(),
    type: 'PropertyTransfer' as const,
    targetPropertyId: 'antalya',
    targetDepartmentId: 'dep-1',
    targetPositionId: 'pos-1',
  }
  const cleared = reconcileMovementWizardDraft(draft, {
    selectableTypes: selectableCreatableMovementTypes([{ id: 'ankara' }], 'ankara'),
    destinationProperties: [],
    positions: [],
    sourceDepartmentId: 'dep-0',
    sourcePositionId: 'pos-0',
    sourceOrganizationalLevel: 100,
  })
  assert.equal(cleared.type, '')
  assert.equal(cleared.targetPropertyId, '')
  assert.equal(cleared.targetDepartmentId, '')
  assert.equal(cleared.targetPositionId, '')
  const wizard = readFileSync(new URL('./PersonnelMovementWizard.tsx', import.meta.url), 'utf8')
  assert.match(wizard, /reconcileMovementWizardDraft/)
})

test('Personnel Movements without an active property stays top-aligned', () => {
  const shell = readFileSync(new URL('../app/AppShell.tsx', import.meta.url), 'utf8')
  const css = readFileSync(new URL('../app/AppShell.module.css', import.meta.url), 'utf8')
  const page = readFileSync(new URL('./PersonnelMovementsPage.tsx', import.meta.url), 'utf8')
  const pageCss = readFileSync(new URL('./PersonnelMovementsPage.module.css', import.meta.url), 'utf8')
  assert.equal(workplacePropertyBannerRequired('/app/workforce/movements'), false)
  assert.match(shell, /workplacePropertyBannerRequired\(location\.pathname\)/)
  assert.match(shell, /className=\{styles\.workplaceNotice\}/)
  assert.doesNotMatch(
    shell,
    /user\?\.propertySelectionRequired \? \(\s*<div className=\{styles\.main\}>/,
  )
  const noticeBlock = css.match(/\.workplaceNotice\s*\{[^}]*\}/)?.[0] ?? ''
  assert.match(noticeBlock, /flex:\s*none/)
  assert.doesNotMatch(noticeBlock, /flex:\s*1/)
  assert.doesNotMatch(noticeBlock, /min-height/)
  assert.doesNotMatch(pageCss, /\.page\s*\{[\s\S]*?min-height:\s*100/)
  assert.doesNotMatch(page, /propertySelectionRequired/)
})

test('Room Operations workplace warning is not rendered on Personnel Movements', () => {
  assert.equal(workplacePropertyBannerRequired('/app/room-operations'), true)
  assert.equal(workplacePropertyBannerRequired('/app/technical-service/new'), true)
  assert.equal(workplacePropertyBannerRequired('/app/workforce/movements'), false)
  assert.equal(workplacePropertyBannerRequired('/app/workforce'), false)
  const shell = readFileSync(new URL('../app/AppShell.tsx', import.meta.url), 'utf8')
  assert.match(shell, /t\('common\.propertySelectionRequired'\)/)
  assert.match(shell, /workplacePropertyBannerRequired/)
})

test('empty list copy does not imply a system error', () => {
  assert.match(hrTr.movements.empty, /Henüz personel hareketi/)
  assert.match(hrTr.movements.emptyFiltered, /filtrelere uygun/)
})

test('manager hierarchy copy and error codes stay localized', () => {
  assert.equal(hrTr.movements.wizard.noManagerCandidates, 'Bu personel için uygun bir üst yönetici bulunmuyor.')
  assert.equal(
    hrTr.movements.wizard.noPromotionTargets,
    'Bu personel için uygun bir terfi pozisyonu bulunmuyor.',
  )
  assert.equal(
    hrTr.movements.wizard.managerLevelHint,
    'Doğrudan yönetici, bir üst organizasyon seviyesinde olmalıdır.',
  )
  assert.equal(hrMovementErrorKeyFromCode('movement-manager-level-invalid'), 'movements.errors.managerLevelInvalid')
  assert.equal(hrMovementErrorKeyFromCode('movement-target-not-promotion'), 'movements.errors.targetNotPromotion')
  assert.match(hrEn.movements.errors.managerLevelInvalid, /next organizational level/)
  assert.match(hrRu.movements.errors.managerLevelInvalid, /следующем/)
  assert.match(hrTr.movements.errors.targetNotPromotion, /daha yüksek/)
  assert.match(hrEn.movements.errors.targetNotPromotion, /higher organizational level/)
  assert.match(hrRu.movements.errors.targetNotPromotion, /более высоком/)
})

test('department change keeps applicable position and requires otherwise', () => {
  const positions = [
    {
      id: 'pos-1',
      propertyId: 'p1',
      name: 'Kat Görevlisi',
      code: null,
      isActive: true,
      applicableDepartmentIds: ['dep-a', 'dep-b'],
      organizationalLevel: 100,
      canManageEmployees: false,
    },
    {
      id: 'pos-2',
      propertyId: 'p1',
      name: 'Kat Şefi',
      code: null,
      isActive: true,
      applicableDepartmentIds: ['dep-b'],
      organizationalLevel: 200,
      canManageEmployees: true,
    },
  ]
  assert.equal(departmentChangeNeedsTargetPosition(positions, 'dep-b', 'pos-1'), false)
  assert.equal(departmentChangeNeedsTargetPosition(positions, 'dep-b', 'pos-2'), false)
  assert.equal(departmentChangeNeedsTargetPosition(positions, 'dep-a', 'pos-2'), true)
})

test('create payload sends only required conditional fields', () => {
  const draft = {
    ...emptyMovementWizardDraft(),
    employmentId: 'emp-1',
    type: 'DepartmentChange' as const,
    effectiveDate: '2026-09-04',
    targetDepartmentId: 'dep-2',
    reason: 'Reorg',
  }
  const body = buildCreateMovementRequest(draft, { departmentId: 'dep-1', positionId: 'pos-1' })
  assert.equal('error' in body, false)
  if ('error' in body) {
    return
  }
  assert.equal(body.employmentId, 'emp-1')
  assert.equal(body.type, 'DepartmentChange')
  assert.equal(body.targetDepartmentId, 'dep-2')
  assert.equal(body.targetPositionId, undefined)
  assert.equal(body.clearManager, false)
  assert.doesNotMatch(JSON.stringify(body), /organizationId/i)
})

test('promotion selector keeps only higher-level positions and rejects stale targets', () => {
  const positions = [
    {
      id: 'hk-att',
      propertyId: 'p1',
      name: 'Kat Görevlisi',
      code: 'HK-ATT',
      isActive: true,
      applicableDepartmentIds: ['hk'],
      organizationalLevel: 100,
      canManageEmployees: false,
    },
    {
      id: 'hk-sup',
      propertyId: 'p1',
      name: 'Kat Hizmetleri Sorumlusu',
      code: 'HK-SUP',
      isActive: true,
      applicableDepartmentIds: ['hk'],
      organizationalLevel: 200,
      canManageEmployees: true,
    },
    {
      id: 'fo-mgr',
      propertyId: 'p1',
      name: 'Ön Büro Müdürü',
      code: 'FO-MGR',
      isActive: true,
      applicableDepartmentIds: ['fo'],
      organizationalLevel: 300,
      canManageEmployees: true,
    },
    {
      id: 'hk-peer',
      propertyId: 'p1',
      name: 'Eşit seviye',
      code: 'HK-PEER',
      isActive: true,
      applicableDepartmentIds: ['hk'],
      organizationalLevel: 200,
      canManageEmployees: false,
    },
  ]
  const fromAttendant = promotionTargetPositions(positions, 'hk', 'hk-att', 100).map((item) => item.id)
  assert.deepEqual(fromAttendant, ['hk-sup', 'hk-peer'])
  const fromSupervisor = promotionTargetPositions(positions, 'hk', 'hk-sup', 200)
  assert.equal(fromSupervisor.length, 0)
  assert.equal(isEligiblePromotionTarget(positions, 'hk', 'hk-sup', 200, 'hk-att'), false)
  assert.equal(isEligiblePromotionTarget(positions, 'hk', 'hk-sup', 200, 'hk-peer'), false)
  assert.equal(isEligiblePromotionTarget(positions, 'hk', 'hk-att', 100, 'hk-sup'), true)

  const draft = {
    ...emptyMovementWizardDraft(),
    employmentId: 'emp-1',
    type: 'Promotion' as const,
    effectiveDate: '2026-09-05',
    targetPositionId: 'hk-att',
    reason: 'Terfi',
  }
  const stale = buildCreateMovementRequest(
    draft,
    { departmentId: 'hk', positionId: 'hk-sup' },
    { positions, sourceOrganizationalLevel: 200 },
  )
  assert.deepEqual(stale, { error: 'target' })
  const higher = buildCreateMovementRequest(
    { ...draft, targetPositionId: 'hk-sup' },
    { departmentId: 'hk', positionId: 'hk-att' },
    { positions, sourceOrganizationalLevel: 100 },
  )
  assert.equal('error' in higher, false)
})

test('covering assignment as of effective date is used as promotion source', () => {
  const assignments = [
    {
      id: 'a-sup',
      departmentId: 'hk',
      departmentName: 'Kat Hizmetleri',
      positionId: 'hk-sup',
      positionName: 'Kat Hizmetleri Sorumlusu',
      startDate: '2026-01-01',
      endDate: '2026-09-04',
      kind: 'Primary' as const,
    },
    {
      id: 'a-att',
      departmentId: 'hk',
      departmentName: 'Kat Hizmetleri',
      positionId: 'hk-att',
      positionName: 'Kat Görevlisi',
      startDate: '2026-09-05',
      endDate: null,
      kind: 'Primary' as const,
    },
  ]
  assert.equal(coveringPrimaryAssignment(assignments, '2026-09-04')?.positionId, 'hk-sup')
  assert.equal(coveringPrimaryAssignment(assignments, '2026-09-05')?.positionId, 'hk-att')
  const card = {
    currentEmployment: {
      id: 'emp-1',
      startDate: '2026-01-01',
      endDate: null,
      status: 'Active' as const,
      seniorityStartDate: null,
      terminationReason: null,
      primaryAssignments: assignments,
    },
    currentPrimaryAssignment: assignments[0],
    employments: [
      {
        id: 'emp-1',
        startDate: '2026-01-01',
        endDate: null,
        status: 'Active' as const,
        seniorityStartDate: null,
        terminationReason: null,
        primaryAssignments: assignments,
      },
    ],
  }
  assert.equal(sourceAssignmentAsOf(card, 'emp-1', '2026-09-04')?.positionId, 'hk-sup')
  assert.equal(sourceAssignmentAsOf(card, 'emp-1', '2026-09-05')?.positionId, 'hk-att')
})

test('promotion payload requires a higher-level position', () => {
  const positions = [
    {
      id: 'pos-1',
      propertyId: 'p1',
      name: 'Kat Görevlisi',
      code: null,
      isActive: true,
      applicableDepartmentIds: ['dep-1'],
      organizationalLevel: 100,
      canManageEmployees: false,
    },
    {
      id: 'pos-2',
      propertyId: 'p1',
      name: 'Kat Şefi',
      code: null,
      isActive: true,
      applicableDepartmentIds: ['dep-1'],
      organizationalLevel: 200,
      canManageEmployees: true,
    },
  ]
  const draft = {
    ...emptyMovementWizardDraft(),
    employmentId: 'emp-1',
    type: 'Promotion' as const,
    effectiveDate: '2026-09-04',
    targetPositionId: 'pos-1',
    reason: 'Promotion',
  }
  const same = buildCreateMovementRequest(
    draft,
    { departmentId: 'dep-1', positionId: 'pos-1' },
    { positions, sourceOrganizationalLevel: 100 },
  )
  assert.deepEqual(same, { error: 'target' })
  const next = buildCreateMovementRequest(
    { ...draft, targetPositionId: 'pos-2' },
    { departmentId: 'dep-1', positionId: 'pos-1' },
    { positions, sourceOrganizationalLevel: 100 },
  )
  assert.equal('error' in next, false)
  if ('error' in next) {
    return
  }
  assert.equal(next.type, 'Promotion')
  assert.equal(next.targetPositionId, 'pos-2')
})

test('position change payload still allows a lower-level target', () => {
  const draft = {
    ...emptyMovementWizardDraft(),
    employmentId: 'emp-1',
    type: 'PositionChange' as const,
    effectiveDate: '2026-09-04',
    targetPositionId: 'hk-att',
    reason: 'Role change',
  }
  const body = buildCreateMovementRequest(draft, { departmentId: 'hk', positionId: 'hk-sup' })
  assert.equal('error' in body, false)
  if ('error' in body) {
    return
  }
  assert.equal(body.type, 'PositionChange')
  assert.equal(body.targetPositionId, 'hk-att')
})

test('property transfer payload requires destination triple', () => {
  const draft = {
    ...emptyMovementWizardDraft(),
    employmentId: 'emp-1',
    type: 'PropertyTransfer' as const,
    effectiveDate: '04.09.2026',
    targetPropertyId: 'prop-2',
    targetDepartmentId: 'dep-9',
    targetPositionId: 'pos-9',
    reason: 'Season',
  }
  const body = buildCreateMovementRequest(draft, { departmentId: 'dep-1', positionId: 'pos-1' })
  assert.equal('error' in body, false)
  if ('error' in body) {
    return
  }
  assert.equal(body.targetPropertyId, 'prop-2')
  assert.equal(body.targetDepartmentId, 'dep-9')
  assert.equal(body.targetPositionId, 'pos-9')
})

test('manager change uses employment id and rejects self', () => {
  const draft = {
    ...emptyMovementWizardDraft(),
    employmentId: 'emp-1',
    type: 'ManagerChange' as const,
    effectiveDate: '2026-09-04',
    targetManagerEmploymentId: 'emp-1',
    reason: 'Reporting',
  }
  assert.deepEqual(buildCreateMovementRequest(draft, { departmentId: 'd', positionId: 'p' }), { error: 'target' })
  const ok = buildCreateMovementRequest(
    { ...draft, targetManagerEmploymentId: 'mgr-1' },
    { departmentId: 'd', positionId: 'p' },
  )
  assert.equal('error' in ok, false)
  if ('error' in ok) {
    return
  }
  assert.equal(ok.targetManagerEmploymentId, 'mgr-1')
})

test('reason is required and date must be valid', () => {
  const draft = {
    ...emptyMovementWizardDraft(),
    employmentId: 'emp-1',
    type: 'PositionChange' as const,
    effectiveDate: '32.13.2026',
    targetPositionId: 'pos-2',
    reason: '',
  }
  assert.deepEqual(buildCreateMovementRequest(draft, { departmentId: 'd', positionId: 'pos-1' }), { error: 'date' })
  assert.deepEqual(
    buildCreateMovementRequest({ ...draft, effectiveDate: '2026-09-04' }, { departmentId: 'd', positionId: 'pos-1' }),
    { error: 'reason' },
  )
})

test('cancel is only available for scheduled movements with manage permission', () => {
  assert.equal(isScheduledCancellable('Scheduled', true), true)
  assert.equal(isScheduledCancellable('Scheduled', false), false)
  assert.equal(isScheduledCancellable('Effective', true), false)
  assert.equal(isScheduledCancellable('Cancelled', true), false)
})

test('conflict problem details map to actionable copy without raw codes', () => {
  assert.equal(
    hrMovementErrorKeyFromCode('movement-schedule-conflict'),
    'movements.errors.scheduleConflict',
  )
  const message = hrMovementErrorMessage(
    { message: 'movement-schedule-conflict', problem: { code: 'movement-schedule-conflict', detail: 'raw' } },
    (key) => (key === 'movements.errors.scheduleConflict' ? hrTr.movements.errors.scheduleConflict : key),
  )
  assert.equal(message, hrTr.movements.errors.scheduleConflict)
  assert.doesNotMatch(message, /movement-schedule-conflict/)
  assert.doesNotMatch(
    hrMovementErrorMessage(
      { message: 'x', problem: { code: 'reporting-line-cycle', detail: 'cycle' } },
      (key) => (key === 'movements.errors.reportingCycle' ? hrTr.movements.errors.reportingCycle : key),
    ),
    /reporting-line-cycle/,
  )
})

test('employee search matches name and personnel number', () => {
  const item = { givenName: 'Ayşe', familyName: 'Yılmaz', personnelNumber: 'P-1001' }
  assert.equal(matchesEmployeeSearch(item, 'yıl'), true)
  assert.equal(matchesEmployeeSearch(item, '1001'), true)
  assert.equal(matchesEmployeeSearch(item, 'mehmet'), false)
})

test('before/after summaries use names not guids', () => {
  const diff = movementDiffSummary({
    type: 'DepartmentChange',
    previousAssignment: {
      id: 'a',
      departmentId: 'guid-1',
      departmentName: 'Kat Hizmetleri',
      positionId: 'guid-2',
      positionName: 'Kat Görevlisi',
      propertyId: 'guid-3',
      propertyName: 'Ankara',
      startDate: '2026-01-01',
      endDate: null,
    },
    newAssignment: {
      id: 'b',
      departmentId: 'guid-4',
      departmentName: 'İnsan Kaynakları',
      positionId: 'guid-2',
      positionName: 'Kat Görevlisi',
      propertyId: 'guid-3',
      propertyName: 'Ankara',
      startDate: '2026-09-04',
      endDate: null,
    },
    previousReportingLine: null,
    newReportingLine: null,
  })
  assert.match(diff.previous, /Kat Hizmetleri/)
  assert.match(diff.next, /İnsan Kaynakları/)
  assert.doesNotMatch(diff.previous, /guid/)
})

test('drawer overlay does not take width from the movement list', () => {
  const css = readFileSync(new URL('./PersonnelMovementsPage.module.css', import.meta.url), 'utf8')
  const page = readFileSync(new URL('./PersonnelMovementsPage.tsx', import.meta.url), 'utf8')
  assert.match(css, /\.drawer\s*\{[\s\S]*?position:\s*fixed/)
  assert.doesNotMatch(css, /\.drawer\s*\{[\s\S]*?flex:\s*0 0/)
  assert.match(page, /data-movements-grid-layout="full"/)
  assert.match(page, /data-movements-drawer="overlay"/)
  assert.match(page, /<aside className=\{styles\.drawer\}/)
  assert.match(page, /drawerScrim/)
  assert.match(page, /event\.key === 'Escape'/)
  assert.doesNotMatch(page, /detail \?[\s\S]*?<WorkspaceDialog[\s\S]*movements\.detail\.title/)
  assert.doesNotMatch(page, /role="dialog"/)
})

test('movements page keeps the title description once and uses a toast after create', () => {
  const page = readFileSync(new URL('./PersonnelMovementsPage.tsx', import.meta.url), 'utf8')
  const wizard = readFileSync(new URL('./PersonnelMovementWizard.tsx', import.meta.url), 'utf8')
  const shell = readFileSync(new URL('../app/AppShell.tsx', import.meta.url), 'utf8')
  const toast = readFileSync(new URL('../ui/Toast.tsx', import.meta.url), 'utf8')
  assert.match(shell, /subtitle: t\('movements\.intro'\)/)
  assert.doesNotMatch(page, /t\('movements\.intro'\)/)
  assert.match(page, /<Toast /)
  assert.match(toast, /data-toast="success"/)
  assert.match(toast, /role="status"/)
  assert.match(toast, /aria-live="polite"/)
  assert.doesNotMatch(page, /Notice tone="success"/)
  assert.match(page, /setDetail\(created\)/)
  assert.match(page, /setToast\(t\('movements\.wizard\.success'\)\)/)
  assert.match(wizard, /onCreated\(created\)/)
  assert.doesNotMatch(wizard, /onCreated\(created\.id\)/)
  assert.equal(hrTr.movements.wizard.success, 'Hareket kaydedildi.')
  assert.equal(hrEn.movements.wizard.success, 'Movement saved.')
  assert.equal(hrRu.movements.wizard.success, 'Перемещение сохранено.')
})

test('scheduled cancel stays a compact drawer action', () => {
  const page = readFileSync(new URL('./PersonnelMovementsPage.tsx', import.meta.url), 'utf8')
  const css = readFileSync(new URL('./PersonnelMovementsPage.module.css', import.meta.url), 'utf8')
  assert.equal(isScheduledCancellable('Scheduled', true), true)
  assert.equal(isScheduledCancellable('Effective', true), false)
  assert.equal(isScheduledCancellable('Cancelled', true), false)
  assert.match(page, /isScheduledCancellable\(detail\.lifecycle, canManage\)/)
  assert.match(page, /data-drawer-cancel/)
  assert.match(page, /drawerActions/)
  assert.match(css, /\.drawerActions\s*\{[\s\S]*?align-self:\s*flex-start/)
  assert.doesNotMatch(page, /variant="danger" layout="block"/)
})

test('personnel card history is read-only and deep-links by employee', () => {
  const card = readFileSync(new URL('./PersonnelCardMovementHistory.tsx', import.meta.url), 'utf8')
  const wizard = readFileSync(new URL('./PersonnelMovementWizard.tsx', import.meta.url), 'utf8')
  assert.match(card, /data-movement-history="readonly"/)
  assert.match(card, /employeeId=\$\{employeeId\}/)
  assert.doesNotMatch(card, /Yeni Hareket|createHrMovement|setWorkMode\('transfer'\)/)
  assert.doesNotMatch(wizard, /AssignmentChange/)
})

test('legacy personnel card transfer remains a department/position action', () => {
  const page = readFileSync(new URL('./PersonnelCard.tsx', import.meta.url), 'utf8')
  assert.match(page, /transferEmployee/)
  assert.match(page, /workforce\.transfer/)
  assert.equal(hrTr.personnel.transferAction.includes('Görev değişikliği'), false)
})

test('movements route remains a top-level sibling of Personel layout', () => {
  const app = readFileSync(new URL('../app/App.tsx', import.meta.url), 'utf8')
  assert.match(app, /path="workforce\/movements" element=\{<PersonnelMovementsPage \/>\}/)
  assert.match(app, /path="workforce" element=\{<WorkforceLayout \/>\}/)
  assert.doesNotMatch(app, /<Route path="movements" element=\{<PersonnelMovementsPage \/>\} \/>/)
})

test('actor labels prefer display name and never show a raw GUID', () => {
  const guid = '8528d29a-b042-4c3a-8dcf-b22255877825'
  const copy = { system: 'Sistem', unknown: 'Bilinmeyen kullanıcı' }
  assert.equal(looksLikeRawUserId(guid), true)
  assert.equal(
    movementActorLabel({ id: guid, displayName: 'Ayşe Yılmaz' }, guid, copy),
    'Ayşe Yılmaz',
  )
  assert.equal(
    movementActorLabel({ id: guid, displayName: 'hr@hugu.local' }, guid, copy),
    'hr@hugu.local',
  )
  const unresolved = movementActorLabel({ id: guid, displayName: null }, guid, copy)
  assert.equal(unresolved, copy.unknown)
  assert.doesNotMatch(unresolved, /8528d29a/i)
  assert.equal(movementActorLabel({ id: null, displayName: null }, '', copy), copy.system)
  assert.equal(movementActorLabel(null, guid, copy), copy.unknown)
  const page = readFileSync(new URL('./PersonnelMovementsPage.tsx', import.meta.url), 'utf8')
  const endpoints = readFileSync(
    new URL('../../../../backend/HuGuWeb.Api/Endpoints/HrMovementEndpoints.cs', import.meta.url),
    'utf8',
  )
  const actors = readFileSync(
    new URL('../../../../backend/HuGuWeb.Api/Authorization/MovementActorDisplayService.cs', import.meta.url),
    'utf8',
  )
  assert.match(page, /setDetail\(created\)/)
  assert.match(endpoints, /CreateMovement[\s\S]*EnrichDetailAsync/)
  assert.match(endpoints, /GetMovement[\s\S]*EnrichDetailAsync/)
  assert.match(endpoints, /ListMovements[\s\S]*EnrichListAsync/)
  assert.match(actors, /FindByIdAsync/)
  assert.match(actors, /FindByNameAsync/)
  assert.match(actors, /FindLinkByUserAsync/)
  assert.doesNotMatch(actors, /userManager\.Users/)
})

test('table and detail drawer render the actor helper instead of the raw user id', () => {
  const page = readFileSync(new URL('./PersonnelMovementsPage.tsx', import.meta.url), 'utf8')
  assert.match(page, /movementActorLabel\(item\.actor, item\.createdByUserId/)
  assert.match(page, /movementActorLabel\(detail\.actor, detail\.createdByUserId/)
  assert.doesNotMatch(page, /title=\{item\.createdByUserId\}/)
  assert.doesNotMatch(page, />\{item\.createdByUserId\}</)
  assert.doesNotMatch(page, /value=\{detail\.createdByUserId\}/)
})

test('wizard is a compact dialog with attached footer and capped picker', () => {
  const wizard = readFileSync(new URL('./PersonnelMovementWizard.tsx', import.meta.url), 'utf8')
  const css = readFileSync(new URL('./PersonnelMovementsPage.module.css', import.meta.url), 'utf8')
  const dialog = readFileSync(new URL('../ui/Dialog.module.css', import.meta.url), 'utf8')
  assert.match(wizard, /size="dialog"/)
  assert.match(wizard, /data-movement-wizard="compact"/)
  assert.match(wizard, /data-wizard-selected-employee/)
  assert.match(wizard, /data-wizard-employee-picker/)
  assert.match(wizard, /movements\.wizard\.changeSelection/)
  assert.match(wizard, /wizardFooter/)
  assert.match(wizard, /personnel\.cancel/)
  assert.match(wizard, /movements\.wizard\.next/)
  assert.doesNotMatch(wizard, /size="workspace"/)
  assert.match(dialog, /\.dialog\s*\{[\s\S]*?width:\s*min\(36rem/)
  assert.doesNotMatch(dialog, /\.dialog\s*\{[\s\S]*?height:\s*min\(860px/)
  assert.match(css, /\.pickerList\s*\{[\s\S]*?max-height:\s*11\.5rem/)
  assert.doesNotMatch(css, /\.pickerList\s*\{[\s\S]*?min-height/)
  assert.match(css, /box-shadow:\s*inset 2px 0 0/)
})

test('wizard stepper and picker collapse after selection', () => {
  assert.equal(movementWizardStepStatus('personnel', 'target'), 'complete')
  assert.equal(movementWizardStepStatus('target', 'target'), 'current')
  assert.equal(movementWizardStepStatus('review', 'target'), 'upcoming')
  assert.equal(adjacentWizardStep('personnel', 1), 'type')
  assert.equal(adjacentWizardStep('personnel', -1), null)
  assert.equal(adjacentWizardStep('review', 1), null)
  assert.equal(movementWizardShowsPicker(false, false), true)
  assert.equal(movementWizardShowsPicker(true, false), false)
  assert.equal(movementWizardShowsPicker(true, true), true)
  assert.equal(hrTr.movements.wizard.changeSelection, 'Değiştir')
})

test('assignment overlap uses localized movement copy instead of English domain text', () => {
  const english =
    'Primary assignments cannot overlap. The previous primary must end the day before the new primary starts.'
  const mapped = hrMovementErrorMessage(
    { message: english, problem: { code: 'overlapping-primary-assignment', detail: english } },
    (key, options) => {
      if (key === 'movements.errors.dateConflict') {
        return hrTr.movements.errors.dateConflict
      }
      if (key === 'movements.errors.dateConflictWithBound') {
        return hrTr.movements.errors.dateConflictWithBound.replace('{{date}}', options?.date ?? '')
      }
      return key
    },
  )
  assert.equal(mapped, hrTr.movements.errors.dateConflict)
  assert.doesNotMatch(mapped, /Primary assignments cannot overlap/)
  assert.doesNotMatch(hrTr.movements.errors.dateConflict, /Primary assignments cannot overlap/)
  assert.match(hrTr.movements.errors.dateConflict, /çalışma geçmişiyle çakışıyor/)
  assert.match(hrEn.movements.errors.dateConflict, /work history/)
  assert.match(hrRu.movements.errors.dateConflict, /трудовой историей/)
  assert.equal(hrMovementErrorKeyFromCode('overlapping-primary-assignment'), 'movements.errors.dateConflict')
  assert.equal(hrMovementErrorKeyFromCode('invalid-transfer-date'), 'movements.errors.dateConflict')
  assert.equal(hrMovementErrorStep('overlapping-primary-assignment'), 'date')
  const withBound = hrMovementErrorMessage(
    { message: english, problem: { code: 'invalid-transfer-date', detail: english } },
    (key, options) =>
      key === 'movements.errors.dateConflictWithBound'
        ? `bound:${options?.date}`
        : key === 'movements.errors.dateConflict'
          ? 'conflict'
          : key,
    { earliestEffectiveDateLabel: '05.09.2026' },
  )
  assert.equal(withBound, 'bound:05.09.2026')
})

test('current assignment start can block the date step without duplicating backend schedule rules', () => {
  assert.equal(earliestAssignmentMovementDate('2026-09-04'), '2026-09-05')
  assert.equal(assignmentMovementDateTooEarly('2026-09-04', '2026-09-04', 'DepartmentChange'), true)
  assert.equal(assignmentMovementDateTooEarly('2026-09-05', '2026-09-04', 'DepartmentChange'), false)
  assert.equal(assignmentMovementDateTooEarly('2026-09-01', '2026-09-04', 'ManagerChange'), false)
})

test('wizard close control uses one dirty confirmation path', () => {
  const wizard = readFileSync(new URL('./PersonnelMovementWizard.tsx', import.meta.url), 'utf8')
  const dialog = readFileSync(new URL('../ui/WorkspaceDialog.tsx', import.meta.url), 'utf8')
  assert.match(wizard, /showClose/)
  assert.match(wizard, /personnel\.close/)
  assert.match(wizard, /onRequestClose=\{requestClose\}/)
  assert.match(wizard, /step === 'personnel' \? requestClose : goBack/)
  assert.match(wizard, /if \(dirty\) \{\s*setConfirmingClose\(true\)/)
  assert.match(wizard, /variant="danger" onClick=\{onClose\}/)
  assert.match(wizard, /personnel\.dirtyTitle/)
  assert.match(wizard, /personnel\.dirtyDiscard/)
  assert.match(wizard, /personnel\.dirtyContinue/)
  assert.match(wizard, /initialFocusRef=\{continueEditingRef\}/)
  assert.match(wizard, /editEffectiveDate/)
  assert.match(wizard, /setStep\('date'\)/)
  assert.match(wizard, /hrMovementErrorStep/)
  assert.match(dialog, /showClose/)
  assert.match(dialog, /className=\{styles\.close\}/)
  assert.match(dialog, /event\.key === 'Escape'/)
  assert.equal(hrTr.movements.errors.editEffectiveDate, 'Geçerlilik tarihini düzenle')
  assert.equal(hrTr.personnel.dirtyTitle, 'Kaydedilmemiş değişiklikler')
  assert.equal(hrTr.personnel.dirtyDiscard, 'Kaydetmeden Çık')
  assert.equal(hrTr.personnel.dirtyContinue, 'Düzenlemeye Devam Et')
  assert.equal(isMovementWizardDirty(emptyMovementWizardDraft()), false)
  assert.equal(isMovementWizardDirty({ ...emptyMovementWizardDraft(), reason: 'Reorg' }), true)
  assert.equal(isMovementWizardDirty(emptyMovementWizardDraft(), 'Ayşe'), true)
})
