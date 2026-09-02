import type { CurrentUser } from '../shared/types.ts'

export type WorkplaceLabels = {
  organizationName: string
  propertyName: string
  hasProperty: boolean
  propertySelectionRequired: boolean
}

export function canLoadPropertyStructure(propertyId: string | null | undefined): boolean {
  return Boolean(propertyId)
}

export function workplaceLabelsFromUser(user: CurrentUser | null): WorkplaceLabels {
  if (!user) {
    return {
      organizationName: '',
      propertyName: '',
      hasProperty: false,
      propertySelectionRequired: false,
    }
  }

  const selectedProperty = user.propertyId
    ? user.accessibleProperties?.find((item) => item.id === user.propertyId)
    : undefined
  const soleProperty =
    user.accessibleProperties?.length === 1 ? user.accessibleProperties[0] : undefined

  return {
    organizationName: user.organizationName?.trim() ?? '',
    propertyName: selectedProperty?.name ?? soleProperty?.name ?? '',
    hasProperty: Boolean(user.propertyId),
    propertySelectionRequired: Boolean(user.propertySelectionRequired),
  }
}
