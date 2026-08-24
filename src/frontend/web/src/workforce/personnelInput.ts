export const MOBILE_DIGIT_MAX = 10
export const TCKN_DIGIT_MAX = 11
export const MOBILE_PLACEHOLDER = '0(___) ___ __ __'

export function digitsOnly(value: string): string {
  let digits = ''
  for (const character of value) {
    if (character >= '0' && character <= '9') {
      digits += character
    }
  }
  return digits
}

export function normalizeTurkishMobileInput(input: string): string {
  let digits = digitsOnly(input)
  if (
    digits.startsWith('90')
    && (digits.length >= 12 || (digits.length >= 3 && digits[2] === '5'))
  ) {
    digits = digits.slice(2)
  }

  if (digits.startsWith('0')) {
    digits = digits.slice(1)
  }

  return digits.slice(0, MOBILE_DIGIT_MAX)
}

export function formatTurkishMobile(rawDigits: string): string {
  const digits = digitsOnly(rawDigits).slice(0, MOBILE_DIGIT_MAX)
  if (digits.length === 0) {
    return ''
  }

  const padded = digits.padEnd(MOBILE_DIGIT_MAX, '_')
  return `0(${padded.slice(0, 3)}) ${padded.slice(3, 6)} ${padded.slice(6, 8)} ${padded.slice(8, 10)}`
}

export const normalizeMobileDigits = normalizeTurkishMobileInput
export const formatMobile = formatTurkishMobile

export function mobileCaretFromDigitCount(formatted: string, digitCount: number): number {
  if (formatted.length === 0) {
    return 0
  }

  const open = formatted.indexOf('(')
  if (digitCount <= 0) {
    return Math.max(open + 1, 0)
  }

  let seen = 0
  for (let index = open + 1; index < formatted.length; index += 1) {
    const character = formatted[index]
    if (character >= '0' && character <= '9') {
      seen += 1
      if (seen >= digitCount) {
        return index + 1
      }
    }
  }

  for (let index = open + 1; index < formatted.length; index += 1) {
    if (formatted[index] === '_') {
      return index
    }
  }

  return formatted.length
}

export function isPayloadDigitChar(formatted: string, index: number): boolean {
  if (index < 0 || index >= formatted.length) {
    return false
  }

  const character = formatted[index]
  if (character < '0' || character > '9') {
    return false
  }

  return index > formatted.indexOf('(')
}

export function isUnsafePhonePaste(text: string): boolean {
  for (const character of text) {
    if ((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z')) {
      return true
    }
  }
  return false
}

export function restrictIdentityInput(scheme: string, value: string): string {
  if (scheme === 'Tckn' || scheme === 'Ykn') {
    return digitsOnly(value).slice(0, TCKN_DIGIT_MAX)
  }

  return value
}

export function formatMobileForDisplay(value: string | null | undefined): string | null {
  if (!value) {
    return null
  }

  const digits = normalizeTurkishMobileInput(value)
  return digits.length === MOBILE_DIGIT_MAX ? formatTurkishMobile(digits) : value
}
