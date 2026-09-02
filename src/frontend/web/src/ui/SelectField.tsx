import { useLayoutEffect, useRef, useState, type ClipboardEvent, type InputHTMLAttributes, type KeyboardEvent, type ReactNode, type SelectHTMLAttributes } from 'react'
import {
  constrainDateInput,
  DATE_DIGIT_MAX,
  DATE_DISPLAY_PLACEHOLDER,
  dateCaretFromDigitCount,
  dateDigitsOnly,
  isoToDisplayDate,
  pastedDateHasOversizedYear,
  toIsoDate,
} from './dateEntry'
import { CalendarIcon } from './icons'
import { FieldLabel } from './TextField'
import styles from './TextField.module.css'

type FieldChrome = {
  id: string
  label: string
  hint?: string
  error?: string
  required?: boolean
}

type SelectFieldProps = FieldChrome & {
  value: string
  onChange: (value: string) => void
  children: ReactNode
  placeholder?: string
} & Omit<SelectHTMLAttributes<HTMLSelectElement>, 'id' | 'value' | 'onChange'>

export function SelectField({
  id,
  label,
  value,
  onChange,
  children,
  hint,
  error,
  required,
  placeholder,
  ...selectProps
}: SelectFieldProps) {
  const hintId = hint ? `${id}-hint` : undefined
  const errorId = error ? `${id}-error` : undefined
  const describedBy = [selectProps['aria-describedby'], hintId, errorId].filter(Boolean).join(' ') || undefined
  const showingPlaceholder = Boolean(placeholder) && value === ''

  return (
    <div className={styles.field}>
      <FieldLabel id={id} label={label} required={required} />
      <select
        {...selectProps}
        id={id}
        className={`${styles.input} ${error ? styles.invalid : ''} ${showingPlaceholder ? styles.placeholderValue : ''}`}
        value={value}
        required={required}
        aria-required={required || undefined}
        onChange={(event) => onChange(event.target.value)}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
      >
        {placeholder ? (
          <option value="" hidden={required || undefined}>
            {placeholder}
          </option>
        ) : null}
        {children}
      </select>
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

type DateFieldProps = FieldChrome & {
  value: string
  onChange: (value: string) => void
  /** When true (default), show calendar icon that opens native date picker. Forced off for readOnly/disabled. */
  calendar?: boolean
  minDate?: string
  openCalendarLabel?: string
} & Omit<InputHTMLAttributes<HTMLInputElement>, 'id' | 'value' | 'onChange' | 'type' | 'min'>

export function DateField({
  id,
  label,
  value,
  onChange,
  hint,
  error,
  required,
  calendar = true,
  minDate,
  openCalendarLabel,
  onBlur,
  onFocus,
  ...inputProps
}: DateFieldProps) {
  const hintId = hint ? `${id}-hint` : undefined
  const errorId = error ? `${id}-error` : undefined
  const describedBy = [inputProps['aria-describedby'], hintId, errorId].filter(Boolean).join(' ') || undefined
  const inputRef = useRef<HTMLInputElement>(null)
  const pickerRef = useRef<HTMLInputElement>(null)
  const caretRef = useRef<number | null>(null)
  const [focused, setFocused] = useState(false)
  const [draft, setDraft] = useState('')
  const isoValue = toIsoDate(value)
  const minIso = toIsoDate(minDate ?? '')
  const displayed = focused ? draft : isoValue ? isoToDisplayDate(value) : value
  const empty = displayed === ''
  const interactive =
    calendar
    && !inputProps.disabled
    && !inputProps.readOnly
  const pickerLabel = openCalendarLabel?.trim() || `Open calendar for ${label}`

  useLayoutEffect(() => {
    const node = inputRef.current
    const caret = caretRef.current
    if (!node || caret === null) {
      return
    }

    node.setSelectionRange(caret, caret)
    caretRef.current = null
  }, [displayed])

  function displayFromValue(next: string): string {
    const iso = toIsoDate(next)
    return iso ? isoToDisplayDate(iso) : next
  }

  function commit(nextDisplay: string, payloadBefore: number) {
    const formatted = constrainDateInput(nextDisplay)
    caretRef.current = dateCaretFromDigitCount(formatted, payloadBefore)
    setDraft(formatted)
    const iso = toIsoDate(formatted)
    if (formatted === '') {
      onChange('')
      return
    }
    if (iso) {
      onChange(iso)
      return
    }
    onChange(formatted)
  }

  function applyIso(iso: string) {
    if (minIso && iso < minIso) {
      return
    }

    onChange(iso)
    if (focused) {
      setDraft(isoToDisplayDate(iso))
    }
  }

  function openCalendar() {
    const picker = pickerRef.current
    if (!picker || !interactive) {
      return
    }

    try {
      picker.showPicker()
    } catch {
      picker.click()
    }
  }

  const textInput = (
      <input
        {...inputProps}
        ref={inputRef}
        id={id}
        className={`${styles.input} ${error ? styles.invalid : ''} ${empty ? styles.placeholderValue : ''}`}
        type="text"
        inputMode="numeric"
        autoComplete="off"
        spellCheck={false}
        placeholder={DATE_DISPLAY_PLACEHOLDER}
        value={displayed}
        required={required}
        aria-required={required || undefined}
        onFocus={(event) => {
          setDraft(displayFromValue(value))
          setFocused(true)
          onFocus?.(event)
        }}
        onChange={(event) => {
          const next = event.target.value
          const caret = event.target.selectionStart ?? next.length
          commit(next, dateDigitsOnly(next.slice(0, caret)).length)
        }}
        onBlur={(event) => {
          const iso = toIsoDate(draft)
          if (iso) {
            onChange(iso)
          } else if (draft === '') {
            onChange('')
          } else {
            onChange(draft)
          }
          setFocused(false)
          onBlur?.(event)
        }}
        onKeyDown={(event: KeyboardEvent<HTMLInputElement>) => {
          const digits = dateDigitsOnly(displayed)
          const start = event.currentTarget.selectionStart ?? 0
          const end = event.currentTarget.selectionEnd ?? 0
          const hasSelection = start !== end

          if (event.key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
            if (event.key < '0' || event.key > '9') {
              event.preventDefault()
              return
            }
            if (!hasSelection && digits.length >= DATE_DIGIT_MAX) {
              event.preventDefault()
            }
          }
        }}
        onPaste={(event: ClipboardEvent<HTMLInputElement>) => {
          event.preventDefault()
          const text = event.clipboardData.getData('text')
          if (pastedDateHasOversizedYear(text)) {
            setDraft(text.trim())
            setFocused(true)
            onChange(text.trim())
            return
          }

          const node = event.currentTarget
          const start = node.selectionStart ?? 0
          const end = node.selectionEnd ?? node.value.length
          const before = dateDigitsOnly(node.value.slice(0, start))
          const after = dateDigitsOnly(node.value.slice(end))
          const inserted = dateDigitsOnly(text).slice(0, DATE_DIGIT_MAX - before.length - after.length)
          setFocused(true)
          commit(before + inserted + after, before.length + inserted.length)
        }}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
      />
  )

  return (
    <div className={styles.field}>
      <FieldLabel id={id} label={label} required={required} />
      {interactive ? (
        <div className={`${styles.dateControl} ${styles.dateControlHasPicker}`}>
          {textInput}
          <div className={styles.datePicker}>
            <button
              type="button"
              className={styles.datePickerButton}
              aria-label={pickerLabel}
              disabled={inputProps.disabled}
              onClick={(event) => {
                event.preventDefault()
                openCalendar()
              }}
            >
              <CalendarIcon />
            </button>
            <input
              ref={pickerRef}
              className={styles.datePickerNative}
              type="date"
              value={isoValue ?? ''}
              min={minIso ?? undefined}
              tabIndex={-1}
              aria-hidden="true"
              disabled={inputProps.disabled}
              onChange={(event) => {
                const iso = toIsoDate(event.target.value)
                if (iso) {
                  applyIso(iso)
                }
              }}
            />
          </div>
        </div>
      ) : (
        textInput
      )}
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
