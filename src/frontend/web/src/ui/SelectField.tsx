import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes } from 'react'
import styles from './TextField.module.css'

type SelectFieldProps = {
  id: string
  label: string
  value: string
  onChange: (value: string) => void
  children: ReactNode
} & Omit<SelectHTMLAttributes<HTMLSelectElement>, 'id' | 'value' | 'onChange'>

export function SelectField({
  id,
  label,
  value,
  onChange,
  children,
  ...selectProps
}: SelectFieldProps) {
  return (
    <div className={styles.field}>
      <label className={styles.label} htmlFor={id}>
        {label}
      </label>
      <select
        id={id}
        className={styles.input}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        {...selectProps}
      >
        {children}
      </select>
    </div>
  )
}

type DateFieldProps = {
  id: string
  label: string
  value: string
  onChange: (value: string) => void
} & Omit<InputHTMLAttributes<HTMLInputElement>, 'id' | 'value' | 'onChange' | 'type'>

export function DateField({ id, label, value, onChange, ...inputProps }: DateFieldProps) {
  return (
    <div className={styles.field}>
      <label className={styles.label} htmlFor={id}>
        {label}
      </label>
      <input
        id={id}
        className={styles.input}
        type="date"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        {...inputProps}
      />
    </div>
  )
}
