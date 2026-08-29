export const IBAN_MAX_LENGTH = 34

export function compactIban(value: string): string {
  let compact = ''
  for (const character of value) {
    if (character === ' ' || character === '\t' || character === '\n' || character === '\r') {
      continue
    }
    compact += character.toUpperCase()
  }
  return compact
}

export function isValidIbanChecksum(iban: string): boolean {
  if (iban.length < 15 || iban.length > IBAN_MAX_LENGTH) {
    return false
  }

  for (const character of iban) {
    const isDigit = character >= '0' && character <= '9'
    const isLetter = character >= 'A' && character <= 'Z'
    if (!isDigit && !isLetter) {
      return false
    }
  }

  const rearranged = iban.slice(4) + iban.slice(0, 4)
  let remainder = 0
  for (const character of rearranged) {
    const expanded =
      character >= '0' && character <= '9' ? character : String(character.charCodeAt(0) - 55)
    for (const digit of expanded) {
      remainder = (remainder * 10 + (digit.charCodeAt(0) - 48)) % 97
    }
  }

  return remainder === 1
}

export function validatePaymentIban(iban: string, bankName: string): string | undefined {
  const compact = compactIban(iban)
  const bank = bankName.trim()
  if (compact === '' && bank === '') {
    return undefined
  }
  if (compact === '') {
    return 'payment-iban-required'
  }
  return isValidIbanChecksum(compact) ? undefined : 'payment-profile-invalid-iban'
}
