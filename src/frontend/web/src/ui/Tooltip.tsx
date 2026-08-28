import { useRef, useState, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import styles from './Tooltip.module.css'

export function Tooltip({
  label,
  enabled = true,
  fit = false,
  className,
  children,
}: {
  label: string
  enabled?: boolean
  fit?: boolean
  className?: string
  children: ReactNode
}) {
  const anchorRef = useRef<HTMLSpanElement>(null)
  const [open, setOpen] = useState(false)
  const [coords, setCoords] = useState({ top: 0, left: 0 })

  if (!enabled) {
    return children
  }

  function show() {
    const rect = anchorRef.current?.getBoundingClientRect()
    if (!rect) {
      return
    }

    setCoords({ top: rect.top + rect.height / 2, left: rect.right + 10 })
    setOpen(true)
  }

  function hide() {
    setOpen(false)
  }

  return (
    <span
      ref={anchorRef}
      className={[styles.anchor, fit ? '' : styles.full, className].filter(Boolean).join(' ')}
      onMouseEnter={show}
      onMouseLeave={hide}
      onFocusCapture={show}
      onBlurCapture={hide}
    >
      {children}
      {open
        ? createPortal(
            <span className={styles.tip} role="tooltip" aria-hidden="true" style={{ top: coords.top, left: coords.left }}>
              {label}
            </span>,
            document.body,
          )
        : null}
    </span>
  )
}
