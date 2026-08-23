const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export function displayEmployeeName(value: string | null | undefined): string | null {
  const name = value?.replace(/\s+/g, ' ').trim() ?? ''
  if (name === '' || GUID_PATTERN.test(name)) {
    return null
  }

  return name
}
