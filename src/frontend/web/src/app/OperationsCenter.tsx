import { useTranslation } from 'react-i18next'
import { formatNumber, formatTime } from '../i18n/format'
import { DEFAULT_LANGUAGE, toAppLanguage } from '../i18n/languages'
import { StatusBadge } from '../ui/StatusBadge'
import {
  prototypeAttention,
  prototypeSnapshot,
  prototypeToday,
  prototypeUpcoming,
} from './operationsCenterPrototype'
import styles from './OperationsCenter.module.css'

export function OperationsCenter() {
  const { t, i18n } = useTranslation()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE

  return (
    <div className={styles.page}>
      <section className={styles.section} aria-labelledby="today-heading">
        <h2 className={styles.sectionTitle} id="today-heading">
          {t('operations.today')}
        </h2>
        <div className={styles.today}>
          {prototypeToday.map((item) => (
            <div key={item.id} className={styles.todayItem}>
              <p className={styles.todayLabel}>{t(item.labelKey)}</p>
              <p
                className={`${styles.todayValue} ${item.emphasis === 'warning' ? styles.todayWatch : ''}`}
              >
                {formatNumber(item.value, language)}
              </p>
              <p className={styles.todayDetail}>
                {t(item.detailKey, {
                  time: item.detailTime
                    ? formatTime(item.detailTime.hours, item.detailTime.minutes, language)
                    : undefined,
                  count: item.detailCount,
                })}
              </p>
            </div>
          ))}
        </div>
      </section>

      <div className={styles.columns}>
        <section className={styles.section} aria-labelledby="attention-heading">
          <h2 className={styles.sectionTitle} id="attention-heading">
            {t('operations.requiresAttention')}
          </h2>
          <div className={styles.attention}>
            {prototypeAttention.map((item) => (
              <article
                key={item.id}
                className={`${styles.attentionItem} ${item.urgency === 'danger' ? styles.blocking : ''}`}
              >
                <span className={`${styles.marker} ${styles[item.urgency]}`} aria-hidden="true" />
                <div className={styles.attentionBody}>
                  <div className={styles.attentionHead}>
                    <p className={styles.location}>{t('operations.room', { number: item.roomNumber })}</p>
                    <StatusBadge tone={item.urgency}>{t(item.urgencyLabelKey)}</StatusBadge>
                  </div>
                  <p className={styles.summary}>{t(item.summaryKey)}</p>
                  <p className={styles.reason}>
                    {t(item.reasonKey, {
                      time: item.reasonTime
                        ? formatTime(item.reasonTime.hours, item.reasonTime.minutes, language)
                        : undefined,
                    })}
                  </p>
                </div>
              </article>
            ))}
          </div>
        </section>

        <div className={styles.rail}>
          <section className={styles.section} aria-labelledby="snapshot-heading">
            <h2 className={styles.sectionTitle} id="snapshot-heading">
              {t('operations.roomOperations')}
            </h2>
            <div className={styles.snapshot}>
              {prototypeSnapshot.map((item) => (
                <div key={item.id} className={styles.snapshotItem}>
                  <span className={`${styles.dot} ${styles[item.tone]}`} aria-hidden="true" />
                  <span className={styles.snapshotCount}>{formatNumber(item.count, language)}</span>
                  <span className={styles.snapshotLabel}>{t(item.labelKey)}</span>
                </div>
              ))}
            </div>
          </section>

          <section className={styles.section} aria-labelledby="upcoming-heading">
            <h2 className={styles.sectionTitle} id="upcoming-heading">
              {t('operations.upcoming')}
            </h2>
            <div className={styles.upcoming}>
              {prototypeUpcoming.map((item) => (
                <div key={item.id} className={styles.upcomingItem}>
                  <span className={styles.time}>
                    {formatTime(item.hours, item.minutes, language)}
                  </span>
                  <span>
                    {t(item.detailKey, {
                      count: item.count,
                      room: item.room,
                    })}
                  </span>
                </div>
              ))}
            </div>
          </section>
        </div>
      </div>
    </div>
  )
}
