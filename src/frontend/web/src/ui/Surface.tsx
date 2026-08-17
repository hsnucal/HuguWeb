import type { HTMLAttributes, ReactNode } from 'react'
import styles from './Surface.module.css'

type SurfaceProps = HTMLAttributes<HTMLDivElement> & {
  children: ReactNode
  raised?: boolean
}

export function Surface({ children, raised = false, className, ...props }: SurfaceProps) {
  const classes = [styles.surface, raised ? styles.raised : '', className].filter(Boolean).join(' ')

  return (
    <div className={classes} {...props}>
      {children}
    </div>
  )
}
