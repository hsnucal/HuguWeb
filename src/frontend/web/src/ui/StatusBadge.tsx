import type { ReactNode } from 'react'
import styles from './StatusBadge.module.css'

export type StatusTone = 'success' | 'warning' | 'danger' | 'info' | 'neutral'

export function StatusBadge({ tone, children }: { tone: StatusTone; children: ReactNode }) {
  return <span className={`${styles.badge} ${styles[tone]}`}>{children}</span>
}
