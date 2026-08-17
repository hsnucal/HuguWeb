import styles from './BrandMark.module.css'

export function BrandMark({ size = 'md' }: { size?: 'md' | 'lg' }) {
  return (
    <span className={`${styles.mark} ${size === 'lg' ? styles.lg : ''}`} aria-hidden="true">
      HG
    </span>
  )
}
