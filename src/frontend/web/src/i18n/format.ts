import type { AppLanguage } from './languages'

export function formatDateOnly(value: string, language: AppLanguage): string {
  const [year, month, day] = value.split('-').map(Number)
  if (!year || !month || !day) {
    return value
  }

  return new Intl.DateTimeFormat(language, { dateStyle: 'medium' }).format(
    new Date(year, month - 1, day),
  )
}

export function todayIsoDate(): string {
  const now = new Date()
  const year = now.getFullYear()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function formatNumber(value: number, language: AppLanguage): string {
  return new Intl.NumberFormat(language).format(value)
}

export function formatTime(hours: number, minutes: number, language: AppLanguage): string {
  const value = new Date(1970, 0, 1, hours, minutes)
  return new Intl.DateTimeFormat(language, {
    hour: 'numeric',
    minute: '2-digit',
  }).format(value)
}

export function formatCurrency(
  value: number,
  language: AppLanguage,
  currency: string,
): string {
  return new Intl.NumberFormat(language, {
    style: 'currency',
    currency,
  }).format(value)
}
