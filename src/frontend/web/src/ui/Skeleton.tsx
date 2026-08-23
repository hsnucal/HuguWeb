import styles from './Skeleton.module.css'

export function Skeleton({
  variant = 'block',
  rows = 5,
  label,
}: {
  variant?: 'list' | 'block'
  rows?: number
  label: string
}) {
  const count = variant === 'list' ? rows : 3

  return (
    <div className={styles.wrap} role="status" aria-live="polite">
      <span className="visually-hidden">{label}</span>
      <div className={variant === 'list' ? styles.list : styles.block} aria-hidden="true">
        {Array.from({ length: count }, (_, index) => (
          <span key={index} className={variant === 'list' ? styles.row : styles.line} />
        ))}
      </div>
    </div>
  )
}
