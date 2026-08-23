import type { HTMLAttributes, ReactNode } from 'react'
import styles from './Surface.module.css'

type SurfaceTone = 'panel' | 'section' | 'inset' | 'raised' | 'interactive' | 'workspace'

type SurfaceProps = HTMLAttributes<HTMLDivElement> & {
  children: ReactNode
  tone?: SurfaceTone
  raised?: boolean
}

export function Surface({
  children,
  tone = 'panel',
  raised = false,
  className,
  ...props
}: SurfaceProps) {
  const classes = [
    styles.surface,
    styles[tone],
    raised || tone === 'raised' ? styles.raised : '',
    className,
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <div className={classes} {...props}>
      {children}
    </div>
  )
}
