import { useTranslation } from 'react-i18next'
import { APP_LANGUAGE_OPTIONS, DEFAULT_LANGUAGE, toAppLanguage, type AppLanguage } from '../i18n/languages'
import styles from './LanguageSelect.module.css'

export function LanguageSelect({
  id,
  disabled,
  className,
  onChange,
}: {
  id: string
  disabled?: boolean
  className?: string
  onChange: (language: AppLanguage) => void
}) {
  const { t, i18n } = useTranslation()
  const value = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE

  return (
    <div className={[styles.wrap, className].filter(Boolean).join(' ')}>
      <label className="visually-hidden" htmlFor={id}>
        {t('common.language')}
      </label>
      <select
        id={id}
        className={styles.select}
        value={value}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value as AppLanguage)}
      >
        {APP_LANGUAGE_OPTIONS.map((option) => (
          <option key={option.code} value={option.code}>
            {option.nativeName}
          </option>
        ))}
      </select>
    </div>
  )
}
