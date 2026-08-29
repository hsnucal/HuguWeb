export const DATE_DIGIT_MAX = 8
export const DATE_DISPLAY_PLACEHOLDER = 'DD.MM.YYYY'
const ISO_DATE = /^(\d{4})-(\d{2})-(\d{2})$/
const DISPLAY_DATE = /^(\d{2})\.(\d{2})\.(\d{4})$/

export function dateDigitsOnly(value: string): string {
  let digits = ''
  for (const character of value) {
    if (character >= '0' && character <= '9') {
      digits += character
    }
  }
  return digits
}

export function isValidCalendarDate(year: number, month: number, day: number): boolean {
  if (!Number.isInteger(year) || !Number.isInteger(month) || !Number.isInteger(day)) {
    return false
  }
  if (year < 1000 || year > 9999 || month < 1 || month > 12 || day < 1 || day > 31) {
    return false
  }

  const date = new Date(year, month - 1, day)
  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day
}

export function toIsoDate(value: string): string | null {
  const trimmed = value.trim()
  if (trimmed === '') {
    return null
  }

  const iso = ISO_DATE.exec(trimmed)
  if (iso) {
    const year = Number(iso[1])
    const month = Number(iso[2])
    const day = Number(iso[3])
    return isValidCalendarDate(year, month, day) ? `${iso[1]}-${iso[2]}-${iso[3]}` : null
  }

  const display = DISPLAY_DATE.exec(trimmed)
  if (!display) {
    return null
  }

  const day = Number(display[1])
  const month = Number(display[2])
  const year = Number(display[3])
  if (!isValidCalendarDate(year, month, day)) {
    return null
  }

  return `${String(year).padStart(4, '0')}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
}

export function isValidIsoDate(value: string): boolean {
  return toIsoDate(value) === value.trim()
}

export function isoToDisplayDate(value: string): string {
  const iso = toIsoDate(value)
  if (!iso) {
    return value.trim()
  }

  const [year, month, day] = iso.split('-')
  return `${day}.${month}.${year}`
}

export function formatDateDigits(rawDigits: string): string {
  const digits = dateDigitsOnly(rawDigits).slice(0, DATE_DIGIT_MAX)
  if (digits.length === 0) {
    return ''
  }
  if (digits.length <= 2) {
    return digits
  }
  if (digits.length <= 4) {
    return `${digits.slice(0, 2)}.${digits.slice(2)}`
  }
  return `${digits.slice(0, 2)}.${digits.slice(2, 4)}.${digits.slice(4)}`
}

export function pastedDateHasOversizedYear(text: string): boolean {
  const digits = dateDigitsOnly(text)
  if (digits.length > DATE_DIGIT_MAX) {
    return true
  }

  const yearMatch = /\.(\d{5,})\s*$/.exec(text.trim())
  return yearMatch !== null
}

export function constrainDateInput(input: string): string {
  return formatDateDigits(dateDigitsOnly(input).slice(0, DATE_DIGIT_MAX))
}

export function dateCaretFromDigitCount(formatted: string, digitCount: number): number {
  if (formatted.length === 0 || digitCount <= 0) {
    return 0
  }

  let seen = 0
  for (let index = 0; index < formatted.length; index += 1) {
    const character = formatted[index]
    if (character >= '0' && character <= '9') {
      seen += 1
      if (seen >= digitCount) {
        return index + 1
      }
    }
  }

  return formatted.length
}
