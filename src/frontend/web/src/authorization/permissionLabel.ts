export function permissionLabel(
  t: (key: string, options?: Record<string, unknown>) => unknown,
  code: string,
): string {
  const labels = t('authorization.permissionLabels', { returnObjects: true })
  if (labels && typeof labels === 'object' && code in labels) {
    return String((labels as Record<string, string>)[code])
  }

  return code
}
