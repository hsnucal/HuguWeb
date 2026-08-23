import type { ButtonHTMLAttributes } from 'react'
import styles from './Button.module.css'

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
  layout?: 'block' | 'inline'
  size?: 'md' | 'sm'
  loading?: boolean
}

export function Button({
  variant = 'primary',
  layout = variant === 'primary' ? 'block' : 'inline',
  size = 'md',
  loading = false,
  type = 'button',
  className,
  disabled,
  children,
  ...props
}: ButtonProps) {
  const classes = [styles.button, styles[variant], styles[layout], styles[size], className]
    .filter(Boolean)
    .join(' ')

  return (
    <button
      type={type}
      className={classes}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...props}
    >
      {children}
    </button>
  )
}
