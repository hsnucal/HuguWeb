import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { useTranslation } from 'react-i18next'
import { formatDateOnly } from '../i18n/format'
import { toAppLanguage } from '../i18n/languages'
import { EmptyState } from '../ui/EmptyState'
import { StatusBadge } from '../ui/StatusBadge'
import { listHrMovements, type PersonnelMovementListItem } from './hrMovementsApi'
import {
  movementDiffSummary,
  movementLifecycleLabelKey,
  movementLifecycleTone,
  movementTypeLabelKey,
} from './movementDisplay'
import styles from './Workforce.module.css'

export function PersonnelCardMovementHistory({
  employeeId,
  canRead,
}: {
  employeeId: string | null
  canRead: boolean
}) {
  const { t, i18n } = useTranslation()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? 'tr'
  const [items, setItems] = useState<PersonnelMovementListItem[] | null>(canRead && employeeId ? null : [])

  useEffect(() => {
    if (!employeeId || !canRead) {
      return
    }
    let cancelled = false
    void listHrMovements({ employeeId })
      .then((rows) => {
        if (!cancelled) {
          setItems(rows.slice(0, 8))
        }
      })
      .catch(() => {
        if (!cancelled) {
          setItems([])
        }
      })
    return () => {
      cancelled = true
    }
  }, [canRead, employeeId])

  if (!canRead) {
    return null
  }

  return (
    <fieldset className={styles.section} data-movement-history="readonly">
      <legend className={styles.sectionTitle}>{t('movements.card.title')}</legend>
      {items === null || items.length === 0 ? (
        <EmptyState compact title={t('movements.card.empty')} />
      ) : (
        <ul className={styles.scheduleDayList}>
          {items.map((item) => {
            const diff = movementDiffSummary(item)
            return (
              <li key={item.id}>
                <strong>{formatDateOnly(item.effectiveDate, language)}</strong>
                <span>
                  {t(movementTypeLabelKey(item.type))} · {diff.previous} → {diff.next}
                </span>
                <StatusBadge tone={movementLifecycleTone(item.lifecycle)} variant="outline">
                  {t(movementLifecycleLabelKey(item.lifecycle))}
                </StatusBadge>
              </li>
            )
          })}
        </ul>
      )}
      {employeeId ? (
        <Link className={styles.backLink} to={`/app/workforce/movements?employeeId=${employeeId}`}>
          {t('movements.card.viewAll')}
        </Link>
      ) : null}
    </fieldset>
  )
}
