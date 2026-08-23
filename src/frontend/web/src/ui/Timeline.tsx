import type { ReactNode } from 'react'
import styles from './Timeline.module.css'

export type TimelineMarker = 'neutral' | 'success' | 'warning' | 'danger' | 'info' | 'accent'

export function Timeline({ children, label }: { children: ReactNode; label: string }) {
  return (
    <ol className={styles.list} aria-label={label}>
      {children}
    </ol>
  )
}

export function TimelineItem({
  time,
  supporting,
  marker = 'neutral',
  children,
}: {
  time: string
  supporting?: string
  marker?: TimelineMarker
  children: ReactNode
}) {
  return (
    <li className={`${styles.item} ${styles[marker]}`}>
      <span className={styles.marker} aria-hidden="true" />
      <div className={styles.when}>
        <span className={styles.time}>{time}</span>
        {supporting ? <span className={styles.date}>{supporting}</span> : null}
      </div>
      <div className={styles.body}>{children}</div>
    </li>
  )
}
