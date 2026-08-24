import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes } from 'react'
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
} & Omit<InputHTMLAttributes<HTMLInputElement>, 'id' | 'value' | 'onChange' | 'type'>

export function DateField({
  id,
  label,
  value,
  onChange,
  hint,
  error,
  required,
  ...inputProps
}: DateFieldProps) {
  const hintId = hint ? `${id}-hint` : undefined
  const errorId = error ? `${id}-error` : undefined
  const describedBy = [inputProps['aria-describedby'], hintId, errorId].filter(Boolean).join(' ') || undefined

  return (
    <div className={styles.field}>
      <FieldLabel id={id} label={label} required={required} />
      <input
        {...inputProps}
        id={id}
        className={`${styles.input} ${error ? styles.invalid : ''} ${value === '' ? styles.placeholderValue : ''}`}
        type="date"
        value={value}
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
