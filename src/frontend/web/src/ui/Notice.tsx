import type { ReactNode } from 'react'
import styles from './Notice.module.css'

export type NoticeTone = 'danger' | 'warning' | 'info' | 'success'

export function Notice({
  tone,
  children,
  className,
}: {
  tone: NoticeTone
  children: ReactNode
  className?: string
}) {
  const classes = [styles.notice, styles[tone], className].filter(Boolean).join(' ')
  return (
    <p className={classes} role={tone === 'danger' || tone === 'warning' ? 'alert' : 'status'}>
      {children}
    </p>
  )
}
