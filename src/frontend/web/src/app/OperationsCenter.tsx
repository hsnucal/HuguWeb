import { StatusBadge } from '../ui/StatusBadge'
import {
  prototypeAttention,
  prototypeSnapshot,
  prototypeToday,
  prototypeUpcoming,
} from './operationsCenterPrototype'
import styles from './OperationsCenter.module.css'

export function OperationsCenter() {
  return (
    <div className={styles.page}>
      <section className={styles.section} aria-labelledby="today-heading">
        <h2 className={styles.sectionTitle} id="today-heading">
          Today
        </h2>
        <div className={styles.today}>
          {prototypeToday.map((item) => (
            <div key={item.id} className={styles.todayItem}>
              <p className={styles.todayLabel}>{item.label}</p>
              <p
                className={`${styles.todayValue} ${item.emphasis === 'warning' ? styles.todayWatch : ''}`}
              >
                {item.value}
              </p>
              <p className={styles.todayDetail}>{item.detail}</p>
            </div>
          ))}
        </div>
      </section>

      <div className={styles.columns}>
        <section className={styles.section} aria-labelledby="attention-heading">
          <h2 className={styles.sectionTitle} id="attention-heading">
            Requires attention
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
                    <p className={styles.location}>{item.location}</p>
                    <StatusBadge tone={item.urgency}>{item.urgencyLabel}</StatusBadge>
                  </div>
                  <p className={styles.summary}>{item.summary}</p>
                  <p className={styles.reason}>{item.reason}</p>
                </div>
              </article>
            ))}
          </div>
        </section>

        <div className={styles.rail}>
          <section className={styles.section} aria-labelledby="snapshot-heading">
            <h2 className={styles.sectionTitle} id="snapshot-heading">
              Room operations
            </h2>
            <div className={styles.snapshot}>
              {prototypeSnapshot.map((item) => (
                <div key={item.id} className={styles.snapshotItem}>
                  <span className={`${styles.dot} ${styles[item.tone]}`} aria-hidden="true" />
                  <span className={styles.snapshotCount}>{item.count}</span>
                  <span className={styles.snapshotLabel}>{item.label}</span>
                </div>
              ))}
            </div>
          </section>

          <section className={styles.section} aria-labelledby="upcoming-heading">
            <h2 className={styles.sectionTitle} id="upcoming-heading">
              Upcoming
            </h2>
            <div className={styles.upcoming}>
              {prototypeUpcoming.map((item) => (
                <div key={item.id} className={styles.upcomingItem}>
                  <span className={styles.time}>{item.time}</span>
                  <span>{item.detail}</span>
                </div>
              ))}
            </div>
          </section>
        </div>
      </div>
    </div>
  )
}
