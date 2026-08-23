import type { ReactNode } from 'react'
import styles from './StatusBadge.module.css'

export type StatusTone = 'success' | 'warning' | 'danger' | 'info' | 'neutral'
export type StatusBadgeVariant = 'fill' | 'outline'

export function StatusBadge({
  tone,
  variant = 'fill',
  className,
  title,
  children,
}: {
  tone: StatusTone
  variant?: StatusBadgeVariant
  className?: string
  title?: string
  children: ReactNode
}) {
  const classes = [styles.badge, styles[tone], styles[variant], className].filter(Boolean).join(' ')
  return (
    <span className={classes} title={title}>
      {children}
    </span>
  )
}
