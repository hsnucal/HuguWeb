import type { ChangeEvent, InputHTMLAttributes, Ref, TextareaHTMLAttributes } from 'react'
import styles from './TextField.module.css'

type FieldChrome = {
  id: string
  label: string
  hint?: string
  error?: string
  required?: boolean
}

export function FieldLabel({ id, label, required }: { id: string; label: string; required?: boolean }) {
  return (
    <label className={styles.label} htmlFor={id}>
      {label}
      {required ? (
        <span className={styles.requiredMark} aria-hidden="true">
          *
        </span>
      ) : null}
    </label>
  )
}

type TextFieldProps = FieldChrome & {
  value: string
  onChange: (value: string, event: ChangeEvent<HTMLInputElement>) => void
  ref?: Ref<HTMLInputElement>
} & Omit<InputHTMLAttributes<HTMLInputElement>, 'id' | 'value' | 'onChange' | 'size' | 'ref'>

export function TextField({
  id,
  label,
  value,
  onChange,
  type = 'text',
  hint,
  error,
  required,
  ref,
  ...inputProps
}: TextFieldProps) {
  const hintId = hint ? `${id}-hint` : undefined
  const errorId = error ? `${id}-error` : undefined
  const describedBy = [inputProps['aria-describedby'], hintId, errorId].filter(Boolean).join(' ') || undefined

  return (
    <div className={styles.field}>
      <FieldLabel id={id} label={label} required={required} />
      <input
        {...inputProps}
        ref={ref}
        id={id}
        className={`${styles.input} ${error ? styles.invalid : ''}`}
        type={type}
        value={value}
        required={required}
        aria-required={required || undefined}
        onChange={(event) => onChange(event.target.value, event)}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
      />
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

type TextAreaProps = FieldChrome & {
  value: string
  onChange: (value: string) => void
  ref?: Ref<HTMLTextAreaElement>
} & Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'id' | 'value' | 'onChange' | 'ref'>

export function TextArea({
  id,
  label,
  value,
  onChange,
  hint,
  error,
  required,
  rows = 3,
  ref,
  ...textAreaProps
}: TextAreaProps) {
  const hintId = hint ? `${id}-hint` : undefined
  const errorId = error ? `${id}-error` : undefined
  const describedBy = [textAreaProps['aria-describedby'], hintId, errorId].filter(Boolean).join(' ') || undefined

  return (
    <div className={styles.field}>
      <FieldLabel id={id} label={label} required={required} />
      <textarea
        {...textAreaProps}
        ref={ref}
        id={id}
        className={`${styles.input} ${styles.textarea} ${error ? styles.invalid : ''}`}
        value={value}
        rows={rows}
        required={required}
        aria-required={required || undefined}
        onChange={(event) => onChange(event.target.value)}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
      />
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
