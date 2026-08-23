import type { ReactNode } from 'react'
import styles from './EmptyState.module.css'

export function EmptyState({
  title,
  description,
  action,
  compact = false,
}: {
  title: string
  description?: string
  action?: ReactNode
  compact?: boolean
}) {
  return (
    <div className={`${styles.empty} ${compact ? styles.compact : ''}`}>
      <p className={styles.title}>{title}</p>
      {description ? <p className={styles.description}>{description}</p> : null}
      {action ? <div className={styles.action}>{action}</div> : null}
    </div>
  )
}
