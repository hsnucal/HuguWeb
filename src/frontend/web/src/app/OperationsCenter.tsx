import { useTranslation } from 'react-i18next'
import { formatNumber, formatTime } from '../i18n/format'
import { DEFAULT_LANGUAGE, toAppLanguage } from '../i18n/languages'
import { DistributionBar } from '../ui/DistributionBar'
import { StatusBadge } from '../ui/StatusBadge'
import { Timeline, TimelineItem } from '../ui/Timeline'
import {
  prototypeAttention,
  prototypeSnapshot,
  prototypeToday,
  prototypeUpcoming,
} from './operationsCenterPrototype'
import styles from './OperationsCenter.module.css'

const snapshotTone = {
  dirty: 'dirty',
  cleaning: 'clean',
  inspection: 'inspected',
  ready: 'ready',
} as const

export function OperationsCenter() {
  const { t, i18n } = useTranslation()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const snapshotTotal = prototypeSnapshot.reduce((sum, item) => sum + item.count, 0)

  return (
    <div className={styles.page}>
      <section className={styles.today} aria-labelledby="today-heading">
        <div className={styles.todayHead}>
          <h2 className={styles.sectionTitle} id="today-heading">
            {t('operations.today')}
          </h2>
        </div>
        <div className={styles.todayMetrics}>
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

      <div className={styles.stage}>
        <section className={styles.attentionPanel} aria-labelledby="attention-heading">
          <h2 className={styles.sectionTitle} id="attention-heading">
            {t('operations.requiresAttention')}
          </h2>
          <Timeline label={t('operations.requiresAttention')}>
            {prototypeAttention.map((item) => (
              <TimelineItem
                key={item.id}
                time={t('operations.room', { number: item.roomNumber })}
                supporting={
                  item.reasonTime
                    ? formatTime(item.reasonTime.hours, item.reasonTime.minutes, language)
                    : undefined
                }
                marker={item.urgency === 'danger' ? 'danger' : item.urgency}
              >
                <div className={styles.attentionBody}>
                  <div className={styles.attentionHead}>
                    <p className={styles.summary}>{t(item.summaryKey)}</p>
                    <StatusBadge tone={item.urgency}>{t(item.urgencyLabelKey)}</StatusBadge>
                  </div>
                  <p className={styles.reason}>
                    {t(item.reasonKey, {
                      time: item.reasonTime
                        ? formatTime(item.reasonTime.hours, item.reasonTime.minutes, language)
                        : undefined,
                    })}
                  </p>
                </div>
              </TimelineItem>
            ))}
          </Timeline>
        </section>

        <section className={styles.snapshotPanel} aria-labelledby="snapshot-heading">
          <div className={styles.snapshotHead}>
            <h2 className={styles.sectionTitle} id="snapshot-heading">
              {t('operations.roomOperations')}
            </h2>
            <p className={styles.snapshotCount}>
              {t('operations.roomsInSnapshot', { value: formatNumber(snapshotTotal, language) })}
            </p>
          </div>
          <DistributionBar
            ariaLabel={t('operations.distribution')}
            segments={prototypeSnapshot.map((item) => ({
              id: item.id,
              label: t(item.labelKey),
              count: item.count,
              tone: snapshotTone[item.id as keyof typeof snapshotTone] ?? item.tone,
            }))}
          />
        </section>
      </div>

      <section className={styles.upcoming} aria-labelledby="upcoming-heading">
        <h2 className={styles.sectionTitle} id="upcoming-heading">
          {t('operations.upcoming')}
        </h2>
        <ol className={styles.upcomingTrail}>
          {prototypeUpcoming.map((item) => (
            <li key={item.id} className={styles.upcomingItem}>
              <span className={styles.upcomingTime}>
                {formatTime(item.hours, item.minutes, language)}
              </span>
              <span className={styles.upcomingDetail}>
                {t(item.detailKey, {
                  count: item.count,
                  room: item.room,
                })}
              </span>
            </li>
          ))}
        </ol>
      </section>
    </div>
  )
}
