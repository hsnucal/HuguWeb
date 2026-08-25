import { type ReactNode } from 'react'
import styles from './TopBar.module.css'

export function TopBar({
  kicker,
  title,
  subtitle,
  actions,
}: {
  kicker?: string
  title: string
  subtitle?: string
  actions?: ReactNode
}) {
  return (
    <header className={styles.topBar}>
      <div className={styles.heading}>
        {kicker ? <p className={`kicker ${styles.kicker}`}>{kicker}</p> : null}
        <h1 className={styles.title}>{title}</h1>
        {subtitle ? <p className={styles.subtitle}>{subtitle}</p> : null}
      </div>
      {actions ? <div className={styles.actions}>{actions}</div> : null}
    </header>
  )
}
