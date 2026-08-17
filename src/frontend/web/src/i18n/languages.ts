export const APP_LANGUAGES = ['tr', 'en', 'ru'] as const

export type AppLanguage = (typeof APP_LANGUAGES)[number]

export const DEFAULT_LANGUAGE: AppLanguage = 'tr'

export const LANGUAGE_STORAGE_KEY = 'huguweb.preferredLanguage'

export const APP_LANGUAGE_OPTIONS: ReadonlyArray<{ code: AppLanguage; nativeName: string }> = [
  { code: 'tr', nativeName: 'Türkçe' },
  { code: 'en', nativeName: 'English' },
  { code: 'ru', nativeName: 'Русский' },
]

export function isAppLanguage(value: string | null | undefined): value is AppLanguage {
  return value === 'tr' || value === 'en' || value === 'ru'
}

export function toAppLanguage(value: string | null | undefined): AppLanguage | null {
  if (!value) {
    return null
  }

  const normalized = value.trim().toLowerCase()
  return isAppLanguage(normalized) ? normalized : null
}
