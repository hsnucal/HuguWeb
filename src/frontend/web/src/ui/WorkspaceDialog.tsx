import { useEffect, useEffectEvent, useId, useRef, type ReactNode, type RefObject } from 'react'
import styles from './Dialog.module.css'

export function WorkspaceDialog({
  title,
  onRequestClose,
  children,
  footer,
  size = 'workspace',
  initialFocusRef,
  stacked = false,
  inert = false,
}: {
  title: string
  onRequestClose: () => void
  children: ReactNode
  footer?: ReactNode
  size?: 'workspace' | 'confirm'
  initialFocusRef?: RefObject<HTMLElement | null>
  stacked?: boolean
  inert?: boolean
}) {
  const titleId = useId()
  const dialogRef = useRef<HTMLDivElement>(null)
  const previouslyFocused = useRef<Element | null>(null)
  const requestClose = useEffectEvent(onRequestClose)

  useEffect(() => {
    if (inert) {
      return
    }

    previouslyFocused.current = document.activeElement
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const node = dialogRef.current
    const initial = initialFocusRef?.current ?? node
    initial?.focus()

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        event.preventDefault()
        requestClose()
        return
      }

      if (event.key !== 'Tab' || !node) {
        return
      }

      const items = getFocusable(node)
      if (items.length === 0) {
        return
      }

      const first = items[0]
      const last = items[items.length - 1]
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first.focus()
      }
    }

    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('keydown', onKeyDown)
      document.body.style.overflow = previousOverflow
      if (previouslyFocused.current instanceof HTMLElement) {
        previouslyFocused.current.focus()
      }
    }
  }, [initialFocusRef, inert])

  return (
    <div
      className={`${styles.scrim} ${stacked ? styles.scrimStacked : ''}`}
      inert={inert || undefined}
      onMouseDown={(event) => {
        if (inert) {
          return
        }
        if (event.target === event.currentTarget) {
          onRequestClose()
        }
      }}
    >
      <div
        ref={dialogRef}
        className={size === 'confirm' ? styles.confirm : styles.workspace}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
      >
        <div className={styles.header}>
          <h2 id={titleId} className={styles.title}>
            {title}
          </h2>
        </div>
        <div className={styles.body}>{children}</div>
        {footer ? <div className={styles.footer}>{footer}</div> : null}
      </div>
    </div>
  )
}

function getFocusable(root: HTMLElement): HTMLElement[] {
  return [
    ...root.querySelectorAll<HTMLElement>(
      'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
    ),
  ].filter((item) => !item.hasAttribute('disabled') && item.getAttribute('aria-hidden') !== 'true')
}
