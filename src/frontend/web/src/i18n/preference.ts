import { updatePreferredLanguage as persistPreferredLanguage } from '../auth/sessionApi'
import type { CurrentUser } from '../shared/types'
import i18n from './i18n'
import { DEFAULT_LANGUAGE, toAppLanguage, type AppLanguage } from './languages'
import { persistBrowserLanguage } from './storage'

let explicitLanguage: AppLanguage | null = null

export function currentLanguage(): AppLanguage {
  return toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
}

export async function applyLanguage(language: AppLanguage) {
  persistBrowserLanguage(language)
  document.documentElement.lang = language

  if (i18n.resolvedLanguage !== language && i18n.language !== language) {
    await i18n.changeLanguage(language)
  }
}

export function selectLanguageLocal(language: AppLanguage) {
  explicitLanguage = language
  void applyLanguage(language)
}

export function consumeExplicitLanguage(): AppLanguage | null {
  const selected = explicitLanguage
  explicitLanguage = null
  return selected
}

export async function persistAuthenticatedLanguage(language: AppLanguage): Promise<CurrentUser> {
  await applyLanguage(language)
  return persistPreferredLanguage(language)
}

export async function reconcileAuthenticatedLanguage(
  persisted: string | null,
  options: { consumeExplicit: boolean },
): Promise<{ preferredLanguage: string | null; saveFailed: boolean; user?: CurrentUser }> {
  const saved = toAppLanguage(persisted)
  const explicit = options.consumeExplicit ? consumeExplicitLanguage() : null

  if (explicit) {
    await applyLanguage(explicit)
    if (explicit === saved) {
      return { preferredLanguage: saved, saveFailed: false }
    }

    try {
      const user = await persistPreferredLanguage(explicit)
      return { preferredLanguage: user.preferredLanguage, saveFailed: false, user }
    } catch {
      return { preferredLanguage: saved, saveFailed: true }
    }
  }

  if (saved) {
    await applyLanguage(saved)
    return { preferredLanguage: saved, saveFailed: false }
  }

  const current = currentLanguage()
  await applyLanguage(current)

  try {
    const user = await persistPreferredLanguage(current)
    return { preferredLanguage: user.preferredLanguage, saveFailed: false, user }
  } catch {
    return { preferredLanguage: null, saveFailed: true }
  }
}
