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
