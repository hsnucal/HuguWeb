import type { ReactNode } from 'react'
import styles from './SessionNotice.module.css'

export function SessionNotice({ children }: { children: ReactNode }) {
  return (
    <div className={styles.notice} role="status">
      <p>{children}</p>
    </div>
  )
}
