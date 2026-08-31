import { formatLeaveAmount } from './leaveAmount.ts'

/**
 * RequestedAmount precedence for new leave request:
 * 1. User-edited amount always wins
 * 2. LeaveType.DefaultRequestAmount is the product default
 * 3. Schedule SuggestedAmount is advisory only when no type default exists
 */
export function amountAfterTypeOrPreview(
  amountTouched: boolean,
  defaultRequestAmount: number | null | undefined,
  suggestedAmount: number | null | undefined,
  currentAmount: string,
): string {
  if (amountTouched) {
    return currentAmount
  }

  if (defaultRequestAmount != null && defaultRequestAmount > 0) {
    return formatLeaveAmount(defaultRequestAmount)
  }

  if (suggestedAmount != null && suggestedAmount > 0) {
    return formatLeaveAmount(suggestedAmount)
  }

  return currentAmount
}

export function amountAfterLeaveTypeChange(
  amountTouched: boolean,
  defaultRequestAmount: number | null | undefined,
  currentAmount: string,
): string {
  return amountAfterTypeOrPreview(amountTouched, defaultRequestAmount, null, currentAmount)
}

/** Shared DateField calendar is opt-in; leave request dates must enable it. */
export function leaveRequestDateFieldUsesCalendar(): boolean {
  return true
}

export function leaveRequestSubmitUsesInlineButtons(): boolean {
  return true
}

export const personnelDirectoryFilterControlIds = [
  'workforce-search',
  'workforce-department-filter',
  'workforce-position-filter',
  'workforce-start-from',
  'workforce-start-to',
  'personnel-column-picker',
] as const

export const personnelDirectoryFilterClassName = 'hrFilters'
export const leaveManagementFilterClassName = 'leaveMgmtFilters'
