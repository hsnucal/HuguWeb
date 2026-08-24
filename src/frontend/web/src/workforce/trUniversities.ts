import catalogue from './reference/tr-universities.json'
import type { SearchableOption } from '../ui/SearchableSelect'

export type TrUniversity = {
  id: string
  name: string
  officialName: string
  city: string
  kind: 'state' | 'foundation'
}

type Catalogue = {
  universities: TrUniversity[]
}

const universities = (catalogue as Catalogue).universities

export function usesUniversitySchoolField(educationLevel: string): boolean {
  return educationLevel === 'Bachelor' || educationLevel === 'Master' || educationLevel === 'Doctorate'
}

export function resolveUniversityName(schoolName: string): string {
  const trimmed = schoolName.trim()
  if (trimmed === '') {
    return ''
  }

  const match = universities.find(
    (item) => item.name === trimmed || item.officialName === trimmed || item.id === trimmed,
  )
  return match?.name ?? trimmed
}

export function universitySelectOptions(currentSchoolName: string): SearchableOption[] {
  const options = universities.map((item) => ({ value: item.name, label: item.name }))
  const resolved = resolveUniversityName(currentSchoolName)
  if (resolved !== '' && !options.some((item) => item.value === resolved)) {
    return [{ value: resolved, label: resolved }, ...options]
  }

  return options
}
