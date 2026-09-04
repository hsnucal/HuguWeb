import { useEffect } from 'react'
import { createPortal } from 'react-dom'
import styles from './Toast.module.css'

const TOAST_MS = 4500

export function Toast({
  message,
  onDismiss,
}: {
  message: string | null
  onDismiss: () => void
}) {
  useEffect(() => {
    if (!message) {
      return
    }
    const handle = window.setTimeout(onDismiss, TOAST_MS)
    return () => window.clearTimeout(handle)
  }, [message, onDismiss])

  if (!message || typeof document === 'undefined') {
    return null
  }

  return createPortal(
    <div className={styles.host} data-toast="success">
      <p className={styles.toast} role="status" aria-live="polite">
        {message}
      </p>
    </div>,
    document.body,
  )
}
