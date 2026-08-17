import {
  DEFAULT_LANGUAGE,
  LANGUAGE_STORAGE_KEY,
  toAppLanguage,
  type AppLanguage,
} from './languages'

export function readBrowserLanguage(): AppLanguage | null {
  try {
    return toAppLanguage(window.localStorage.getItem(LANGUAGE_STORAGE_KEY))
  } catch {
    return null
  }
}

export function persistBrowserLanguage(language: AppLanguage) {
  try {
    window.localStorage.setItem(LANGUAGE_STORAGE_KEY, language)
  } catch {
    // Storage may be unavailable (private mode). Keep the in-memory UI language.
  }
}

export function detectSupportedBrowserLanguage(): AppLanguage | null {
  const candidates = [...(navigator.languages ?? []), navigator.language]

  for (const candidate of candidates) {
    if (!candidate) {
      continue
    }

    const primary = candidate.trim().toLowerCase().split('-')[0]
    const language = toAppLanguage(primary)
    if (language) {
      return language
    }
  }

  return null
}

export function resolveUnauthenticatedLanguage(): AppLanguage {
  return readBrowserLanguage() ?? detectSupportedBrowserLanguage() ?? DEFAULT_LANGUAGE
}
