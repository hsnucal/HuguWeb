export const PERSONNEL_COLUMN_IDS = [
  'photo',
  'name',
  'personnelNumber',
  'department',
  'position',
  'startDate',
  'status',
  'educationLevel',
  'mobilePhone',
  'email',
  'bloodType',
  'nationalIdentity',
] as const

export type PersonnelColumnId = (typeof PERSONNEL_COLUMN_IDS)[number]

export const DEFAULT_PERSONNEL_COLUMNS: PersonnelColumnId[] = [
  'photo',
  'name',
  'personnelNumber',
  'department',
  'position',
  'startDate',
  'status',
]

const STORAGE_KEY = 'huguweb.personnelList.columns.v1'

export function requiredPersonnelColumns(): PersonnelColumnId[] {
  return ['photo', 'name']
}

export function availablePersonnelColumns(canReadSensitive: boolean): PersonnelColumnId[] {
  return PERSONNEL_COLUMN_IDS.filter((id) => canReadSensitive || id !== 'nationalIdentity')
}

export function loadPersonnelColumns(canReadSensitive: boolean): PersonnelColumnId[] {
  const allowed = availablePersonnelColumns(canReadSensitive)
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return DEFAULT_PERSONNEL_COLUMNS.filter((id) => allowed.includes(id))
    }

    const parsed = JSON.parse(raw) as unknown
    if (!Array.isArray(parsed)) {
      return DEFAULT_PERSONNEL_COLUMNS
    }

    const selected = parsed.filter((item): item is PersonnelColumnId =>
      allowed.includes(item as PersonnelColumnId),
    )
    const required = requiredPersonnelColumns()
    const merged = [...required, ...selected.filter((id) => !required.includes(id))]
    return merged.length > 0 ? merged : DEFAULT_PERSONNEL_COLUMNS
  } catch {
    return DEFAULT_PERSONNEL_COLUMNS
  }
}

export function savePersonnelColumns(columns: PersonnelColumnId[]) {
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(columns))
}
