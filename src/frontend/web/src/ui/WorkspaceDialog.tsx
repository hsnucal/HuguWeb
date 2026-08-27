import { useEffect, useEffectEvent, useId, useLayoutEffect, useRef, useState, type ReactNode, type RefObject } from 'react'
import styles from './Dialog.module.css'

const CLOSE_FALLBACK_MS = 280

export function WorkspaceDialog({
  title,
  subtitle,
  onRequestClose,
  children,
  footer,
  size = 'workspace',
  initialFocusRef,
  stacked = false,
  inert = false,
  hideHeader = false,
  closing = false,
  onCloseAnimationComplete,
  bodyOverflow = 'auto',
}: {
  title: string
  subtitle?: string
  onRequestClose: () => void
  children: ReactNode
  footer?: ReactNode
  size?: 'workspace' | 'confirm' | 'compact'
  initialFocusRef?: RefObject<HTMLElement | null>
  stacked?: boolean
  inert?: boolean
  hideHeader?: boolean
  closing?: boolean
  onCloseAnimationComplete?: () => void
  bodyOverflow?: 'auto' | 'hidden'
}) {
  const titleId = useId()
  const dialogRef = useRef<HTMLDivElement>(null)
  const previouslyFocused = useRef<Element | null>(null)
  const closeFinished = useRef(false)
  const requestClose = useEffectEvent(onRequestClose)
  const finishClose = useEffectEvent(() => {
    if (closeFinished.current) {
      return
    }

    closeFinished.current = true
    onCloseAnimationComplete?.()
  })
  const [entered, setEntered] = useState(false)

  useLayoutEffect(() => {
    const frame = requestAnimationFrame(() => {
      setEntered(true)
    })
    return () => cancelAnimationFrame(frame)
  }, [])

  useEffect(() => {
    previouslyFocused.current = document.activeElement
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const node = dialogRef.current
    const initial = initialFocusRef?.current ?? node
    initial?.focus()

    return () => {
      document.body.style.overflow = previousOverflow
      if (previouslyFocused.current instanceof HTMLElement) {
        previouslyFocused.current.focus()
      }
    }
  }, [initialFocusRef])

  useEffect(() => {
    if (inert || closing) {
      return
    }

    const node = dialogRef.current

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
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [inert, closing])

  useEffect(() => {
    if (!closing) {
      return
    }

    const node = dialogRef.current

    function onEnd(event: Event) {
      const transition = event as TransitionEvent
      if (transition.target !== node) {
        return
      }

      if (transition.propertyName === 'opacity' || transition.propertyName === 'transform') {
        finishClose()
      }
    }

    node?.addEventListener('transitionend', onEnd)
    const timeout = window.setTimeout(() => finishClose(), CLOSE_FALLBACK_MS)
    return () => {
      node?.removeEventListener('transitionend', onEnd)
      window.clearTimeout(timeout)
    }
  }, [closing])

  const panelClass = [
    size === 'confirm' ? styles.confirm : size === 'compact' ? styles.compact : styles.workspace,
    entered ? styles.panelEntered : styles.panelEnter,
    closing ? styles.panelClosing : '',
  ]
    .filter(Boolean)
    .join(' ')

  const scrimClass = [
    styles.scrim,
    stacked ? styles.scrimStacked : '',
    entered ? styles.scrimEntered : styles.scrimEnter,
    closing ? styles.scrimClosing : '',
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <div
      className={scrimClass}
      inert={inert || undefined}
      onMouseDown={(event) => {
        if (inert || closing) {
          return
        }
        if (event.target === event.currentTarget) {
          onRequestClose()
        }
      }}
    >
      <div
        ref={dialogRef}
        className={panelClass}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
      >
        {hideHeader ? (
          <h2 id={titleId} className={styles.visuallyHidden}>
            {title}
          </h2>
        ) : (
          <div className={styles.header}>
            <h2 id={titleId} className={styles.title}>
              {title}
            </h2>
            {subtitle ? <p className={styles.subtitle}>{subtitle}</p> : null}
          </div>
        )}
        <div className={`${styles.body} ${bodyOverflow === 'hidden' ? styles.bodyContained : ''}`}>{children}</div>
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
