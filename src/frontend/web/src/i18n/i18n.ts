import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import { APP_LANGUAGES, DEFAULT_LANGUAGE, toAppLanguage } from './languages'
import { en } from './locales/en'
import { ru } from './locales/ru'
import { tr } from './locales/tr'
import { resolveUnauthenticatedLanguage } from './storage'

const initialLanguage = resolveUnauthenticatedLanguage()

void i18n.use(initReactI18next).init({
  resources: {
    tr: { translation: tr },
    en: { translation: en },
    ru: { translation: ru },
  },
  lng: initialLanguage,
  fallbackLng: DEFAULT_LANGUAGE,
  supportedLngs: [...APP_LANGUAGES],
  nonExplicitSupportedLngs: false,
  interpolation: {
    escapeValue: false,
  },
  react: {
    useSuspense: false,
  },
})

function syncDocumentLanguage(language: string) {
  document.documentElement.lang = toAppLanguage(language) ?? DEFAULT_LANGUAGE
}

syncDocumentLanguage(i18n.resolvedLanguage ?? initialLanguage)

i18n.on('languageChanged', syncDocumentLanguage)

export default i18n
