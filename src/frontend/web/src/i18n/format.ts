import type { AppLanguage } from './languages'

export function formatDate(value: Date, language: AppLanguage): string {
  return new Intl.DateTimeFormat(language, { dateStyle: 'medium' }).format(value)
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
