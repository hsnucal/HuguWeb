import type { InputHTMLAttributes } from 'react'
import styles from './TextField.module.css'

type TextFieldProps = {
  id: string
  label: string
  value: string
  onChange: (value: string) => void
} & Omit<InputHTMLAttributes<HTMLInputElement>, 'id' | 'value' | 'onChange' | 'size'>

export function TextField({
  id,
  label,
  value,
  onChange,
  type = 'text',
  ...inputProps
}: TextFieldProps) {
  return (
    <div className={styles.field}>
      <label className={styles.label} htmlFor={id}>
        {label}
      </label>
      <input
        id={id}
        className={styles.input}
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        {...inputProps}
      />
    </div>
  )
}
