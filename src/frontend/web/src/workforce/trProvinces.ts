import catalogue from './reference/tr-provinces.json' with { type: 'json' }
import type { SearchableOption } from '../ui/SearchableSelect'

export type TrProvince = {
  code: string
  name: string
  districts: string[]
}

type Catalogue = {
  provinceCount: number
  districtCount: number
  provinces: TrProvince[]
}

const provinces = (catalogue as Catalogue).provinces

export function turkishProvinceCount(): number {
  return provinces.length
}

export function turkishDistrictCount(): number {
  return provinces.reduce((total, item) => total + item.districts.length, 0)
}

export function listTurkishProvinces(): TrProvince[] {
  return provinces
}

export function findTurkishProvince(name: string): TrProvince | undefined {
  const trimmed = name.trim()
  if (trimmed === '') {
    return undefined
  }

  return provinces.find(
    (item) =>
      item.name === trimmed
      || item.code === trimmed
      || item.name.localeCompare(trimmed, 'tr', { sensitivity: 'accent' }) === 0,
  )
}

export function districtsForProvince(provinceName: string): string[] {
  return findTurkishProvince(provinceName)?.districts ?? []
}

export function isKnownProvinceDistrict(provinceName: string, districtName: string): boolean {
  const province = findTurkishProvince(provinceName)
  if (!province) {
    return false
  }

  return province.districts.some(
    (item) => item.localeCompare(districtName.trim(), 'tr', { sensitivity: 'accent' }) === 0,
  )
}

export function retainedDistrict(provinceName: string, districtName: string): string {
  if (districtName.trim() === '') {
    return ''
  }
  if (provinceName.trim() === '') {
    return ''
  }
  if (findTurkishProvince(provinceName) === undefined) {
    return districtName
  }
  return isKnownProvinceDistrict(provinceName, districtName) ? districtName : ''
}

export function provinceSelectOptions(current: string): SearchableOption[] {
  const options = provinces.map((item) => ({ value: item.name, label: item.name }))
  const trimmed = current.trim()
  if (trimmed !== '' && findTurkishProvince(trimmed) === undefined) {
    return [{ value: trimmed, label: trimmed }, ...options]
  }
  return options
}

export function districtSelectOptions(provinceName: string, currentDistrict: string): SearchableOption[] {
  const known = districtsForProvince(provinceName).map((item) => ({ value: item, label: item }))
  const trimmed = currentDistrict.trim()
  if (trimmed !== '' && !known.some((item) => item.value === trimmed)) {
    return [{ value: trimmed, label: trimmed }, ...known]
  }
  return known
}
