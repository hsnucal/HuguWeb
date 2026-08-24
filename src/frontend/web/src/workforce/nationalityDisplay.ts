import type { AppLanguage } from '../i18n/languages'

const localeFor = (language: AppLanguage) => (language === 'tr' ? 'tr' : language === 'ru' ? 'ru' : 'en')

export function nationalityLabel(code: string, language: AppLanguage): string {
  if (!code) {
    return ''
  }

  try {
    const name = new Intl.DisplayNames([localeFor(language)], { type: 'region' }).of(code)
    return name && name !== code ? `${code} — ${name}` : code
  } catch {
    return code
  }
}
