import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import languageStyles from '../ui/LanguageSelect.module.css'
import styles from './PropertySelect.module.css'

export function PropertySelect({
  id,
  className,
  tone = 'default',
}: {
  id: string
  className?: string
  tone?: 'default' | 'onBrand'
}) {
  const { t } = useTranslation()
  const { user, selectProperty } = useAuthSession()
  const properties = user?.accessibleProperties ?? []
  if (properties.length === 0) {
    return null
  }

  const value = user?.propertyId ?? ''
  const showSelect = properties.length > 1 || user?.propertySelectionRequired

  if (!showSelect) {
    return (
      <p className={[styles.current, className].filter(Boolean).join(' ')}>
        <span className={styles.label}>{t('common.currentProperty')}</span>
        <strong>{properties[0]?.name}</strong>
      </p>
    )
  }

  return (
    <div className={[languageStyles.wrap, className].filter(Boolean).join(' ')}>
      <label className="visually-hidden" htmlFor={id}>
        {t('common.currentProperty')}
      </label>
      <select
        id={id}
        className={[languageStyles.select, tone === 'onBrand' ? languageStyles.onBrand : '', styles.select]
          .filter(Boolean)
          .join(' ')}
        value={value}
        onChange={(event) => {
          const next = event.target.value
          if (next) {
            void selectProperty(next)
          }
        }}
      >
        {user?.propertySelectionRequired && !value ? (
          <option value="">{t('common.selectProperty')}</option>
        ) : null}
        {properties.map((property) => (
          <option key={property.id} value={property.id}>
            {property.name}
          </option>
        ))}
      </select>
    </div>
  )
}
