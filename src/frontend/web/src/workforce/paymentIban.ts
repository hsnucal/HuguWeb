export const TR_IBAN_PREFIX = 'TR'
export const TR_IBAN_MAX_LENGTH = 26
export const TR_IBAN_BODY_MAX_LENGTH = 24

/** @deprecated Prefer TR_IBAN_MAX_LENGTH for Turkish payment IBAN. */
export const IBAN_MAX_LENGTH = TR_IBAN_MAX_LENGTH

const BODY_GROUP_SIZES = [2, 4, 4, 4, 4, 4, 2] as const
const CANONICAL_PATTERN = /^TR[0-9]{24}$/

/**
 * Digits after the fixed TR prefix only (max 24).
 * Strips spaces/punctuation/letters and leading TR prefixes so paste never yields TRTR.
 */
export function normalizeTurkishIbanDigits(input: string): string {
  let compact = ''
  for (const character of input) {
    if (character === ' ' || character === '\t' || character === '\n' || character === '\r') {
      continue
    }

    const upper = character.toUpperCase()
    if (upper >= '0' && upper <= '9') {
      compact += upper
      continue
    }

    if (upper >= 'A' && upper <= 'Z') {
      compact += upper
    }
  }

  while (compact.startsWith(TR_IBAN_PREFIX)) {
    compact = compact.slice(TR_IBAN_PREFIX.length)
  }

  let digits = ''
  for (const character of compact) {
    if (character >= '0' && character <= '9') {
      digits += character
      if (digits.length >= TR_IBAN_BODY_MAX_LENGTH) {
        break
      }
    }
  }

  return digits
}

/** Format body digits as `33 0006 1005 1978 6457 8413 26` (2-4-4-4-4-4-2). */
export function formatTurkishIbanBody(digits: string): string {
  const body = normalizeTurkishIbanDigits(digits)
  if (body === '') {
    return ''
  }

  const parts: string[] = []
  let index = 0
  for (const size of BODY_GROUP_SIZES) {
    if (index >= body.length) {
      break
    }
    parts.push(body.slice(index, index + size))
    index += size
  }

  return parts.join(' ')
}

/** Display form including fixed TR prefix, e.g. `TR33 0006 1005 1978 6457 8413 26`. */
export function formatTurkishIban(input: string): string {
  const body = formatTurkishIbanBody(input)
  return body === '' ? TR_IBAN_PREFIX : `${TR_IBAN_PREFIX}${body}`
}

/** Canonical API/storage value: `TR` + up to 24 digits, or empty when no digits. */
export function toCanonicalTurkishIban(input: string): string {
  const digits = normalizeTurkishIbanDigits(input)
  return digits === '' ? '' : TR_IBAN_PREFIX + digits
}

export function isEmptyTurkishIban(value: string): boolean {
  return normalizeTurkishIbanDigits(value) === ''
}

export function isCompleteTurkishIban(value: string): boolean {
  return CANONICAL_PATTERN.test(toCanonicalTurkishIban(value))
}

export function isIncompleteTurkishIban(value: string): boolean {
  const digits = normalizeTurkishIbanDigits(value)
  return digits.length > 0 && digits.length < TR_IBAN_BODY_MAX_LENGTH
}

/** @deprecated Prefer toCanonicalTurkishIban. */
export function compactIban(value: string): string {
  return toCanonicalTurkishIban(value)
}

/** @deprecated Prefer toCanonicalTurkishIban. */
export function normalizeTurkishIban(value: string): string {
  return toCanonicalTurkishIban(value)
}

/** Canonical value for API/storage. Untouched visual `TR` becomes empty. */
export function toPersistedIban(value: string): string {
  return toCanonicalTurkishIban(value)
}

export function turkishIbanBody(value: string): string {
  return normalizeTurkishIbanDigits(value)
}

/**
 * Apply body/paste input while keeping a single TR prefix and max 26 chars.
 * Pasting a full IBAN that already starts with TR does not produce TRTR.
 */
export function applyTurkishIbanBodyEdit(rawBody: string): string {
  return toCanonicalTurkishIban(rawBody)
}

export function isValidTurkishIbanStructure(iban: string): boolean {
  return isCompleteTurkishIban(iban)
}

/**
 * @deprecated Structural Turkish IBAN only — checksum is out of scope for this sprint.
 * Prefer isValidTurkishIbanStructure / isCompleteTurkishIban.
 */
export function isValidIbanChecksum(iban: string): boolean {
  return isValidTurkishIbanStructure(iban)
}

export function validatePaymentIban(iban: string, bankName: string): string | undefined {
  // Always validate the canonical no-space form — never display length / presentation spaces.
  const canonical = toCanonicalTurkishIban(iban)
  const bank = bankName.trim()

  if (canonical === '' && bank === '') {
    return undefined
  }

  if (canonical === '') {
    return 'payment-iban-required'
  }

  return CANONICAL_PATTERN.test(canonical) ? undefined : 'payment-profile-invalid-iban'
}

/** Map a digit index (0..24) to caret position in a formatted body string. */
export function caretIndexForDigitCount(formattedBody: string, digitCount: number): number {
  if (digitCount <= 0) {
    return 0
  }

  let seen = 0
  for (let index = 0; index < formattedBody.length; index += 1) {
    if (formattedBody[index] >= '0' && formattedBody[index] <= '9') {
      seen += 1
      if (seen >= digitCount) {
        return index + 1
      }
    }
  }

  return formattedBody.length
}

export function countDigitsBefore(value: string, caret: number): number {
  let count = 0
  const end = Math.max(0, Math.min(caret, value.length))
  for (let index = 0; index < end; index += 1) {
    const character = value[index]
    if (character >= '0' && character <= '9') {
      count += 1
    }
  }
  return count
}
