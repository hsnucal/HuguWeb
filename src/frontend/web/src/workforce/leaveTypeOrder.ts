import type { LeaveTypeRecord } from './hrLeaveApi.ts'

export function orderActiveLeaveTypes<T extends Pick<LeaveTypeRecord, 'isActive' | 'systemKind' | 'name'>>(
  types: readonly T[],
): T[] {
  return types
    .filter((item) => item.isActive)
    .slice()
    .sort((left, right) => {
      const leftRank = left.systemKind === 'Annual' ? 0 : 1
      const rightRank = right.systemKind === 'Annual' ? 0 : 1
      if (leftRank !== rightRank) {
        return leftRank - rightRank
      }

      return left.name.localeCompare(right.name)
    })
}
