import { useLayoutEffect, useRef, type InputHTMLAttributes, type KeyboardEvent, type MouseEvent } from 'react'
import { FieldLabel } from '../ui/TextField'
import styles from '../ui/TextField.module.css'
import {
  applyTurkishIbanBodyEdit,
  caretIndexForDigitCount,
  countDigitsBefore,
  formatTurkishIbanBody,
  TR_IBAN_PREFIX,
  turkishIbanBody,
} from './paymentIban'

type TurkishIbanFieldProps = {
  id: string
  label: string
  value: string
  onChange: (value: string) => void
  hint?: string
  error?: string
  required?: boolean
} & Omit<InputHTMLAttributes<HTMLInputElement>, 'id' | 'value' | 'onChange' | 'type' | 'maxLength' | 'inputMode'>

export function TurkishIbanField({
  id,
  label,
  value,
  onChange,
  hint,
  error,
  required,
  disabled,
  onBlur,
  ...inputProps
}: TurkishIbanFieldProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const pendingDigitCaret = useRef<number | null>(null)
  const hintId = hint ? `${id}-hint` : undefined
  const errorId = error ? `${id}-error` : undefined
  const describedBy = [inputProps['aria-describedby'], hintId, errorId].filter(Boolean).join(' ') || undefined
  const digits = turkishIbanBody(value)
  const formattedBody = formatTurkishIbanBody(digits)

  useLayoutEffect(() => {
    const input = inputRef.current
    const digitCaret = pendingDigitCaret.current
    if (!input || digitCaret === null) {
      return
    }

    const nextCaret = caretIndexForDigitCount(formattedBody, digitCaret)
    input.setSelectionRange(nextCaret, nextCaret)
    pendingDigitCaret.current = null
  }, [formattedBody])

  function focusDigitInput() {
    if (disabled) {
      return
    }
    inputRef.current?.focus()
  }

  function handleControlMouseDown(event: MouseEvent<HTMLDivElement>) {
    if (disabled || event.target === inputRef.current) {
      return
    }
    event.preventDefault()
    focusDigitInput()
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    // Block non-digit printable keys; allow navigation/editing shortcuts.
    if (event.ctrlKey || event.metaKey || event.altKey) {
      return
    }

    if (event.key.length === 1 && (event.key < '0' || event.key > '9')) {
      event.preventDefault()
    }
  }

  return (
    <div className={styles.field}>
      <FieldLabel id={id} label={label} required={required} />
      <div
        className={`${styles.ibanControl} ${error ? styles.ibanInvalid : ''} ${disabled ? styles.ibanDisabled : ''}`.trim()}
        onMouseDown={handleControlMouseDown}
      >
        <span className={styles.ibanPrefix} aria-hidden="true">
          {TR_IBAN_PREFIX}
        </span>
        <input
          {...inputProps}
          ref={inputRef}
          id={id}
          className={styles.ibanInput}
          type="text"
          inputMode="numeric"
          autoComplete="off"
          spellCheck={false}
          value={formattedBody}
          disabled={disabled}
          required={required}
          aria-required={required || undefined}
          onKeyDown={handleKeyDown}
          onChange={(event) => {
            const caret = event.target.selectionStart ?? event.target.value.length
            pendingDigitCaret.current = countDigitsBefore(event.target.value, caret)
            onChange(applyTurkishIbanBodyEdit(event.target.value))
          }}
          onBlur={(event) => {
            // Re-commit from the visible input so parent blur validation sees the latest digits.
            onChange(applyTurkishIbanBodyEdit(event.target.value))
            onBlur?.(event)
          }}
          aria-invalid={error ? true : undefined}
          aria-describedby={describedBy}
        />
      </div>
      {hint ? (
        <p className={styles.hint} id={hintId}>
          {hint}
        </p>
      ) : null}
      {error ? (
        <p className={styles.error} id={errorId} role="alert">
          {error}
        </p>
      ) : null}
    </div>
  )
}
