import type { PositionRecord } from './workforceApi'

export function positionsForDepartment(
  positions: PositionRecord[],
  departmentId: string,
): PositionRecord[] {
  if (departmentId === '') {
    return []
  }

  return positions.filter(
    (item) => item.isActive && (item.applicableDepartmentIds ?? []).includes(departmentId),
  )
}

export function retainedPositionId(
  positions: PositionRecord[],
  departmentId: string,
  positionId: string,
): string {
  if (positionId === '') {
    return ''
  }

  return positionsForDepartment(positions, departmentId).some((item) => item.id === positionId)
    ? positionId
    : ''
}

export function promotionTargetPositions(
  positions: PositionRecord[],
  departmentId: string,
  sourcePositionId: string,
  sourceOrganizationalLevel: number | undefined,
): PositionRecord[] {
  if (departmentId === '' || sourceOrganizationalLevel === undefined) {
    return []
  }

  return positionsForDepartment(positions, departmentId).filter(
    (item) => item.id !== sourcePositionId && item.organizationalLevel > sourceOrganizationalLevel,
  )
}

export function isEligiblePromotionTarget(
  positions: PositionRecord[],
  departmentId: string,
  sourcePositionId: string,
  sourceOrganizationalLevel: number | undefined,
  targetPositionId: string,
): boolean {
  if (targetPositionId === '') {
    return false
  }

  return promotionTargetPositions(
    positions,
    departmentId,
    sourcePositionId,
    sourceOrganizationalLevel,
  ).some((item) => item.id === targetPositionId)
}
