import { HgMonogram } from './HgMonogram'
import styles from './BrandMark.module.css'

export function BrandMark({
  size = 'md',
  tone = 'brand',
  label,
}: {
  size?: 'sm' | 'md' | 'lg'
  tone?: 'brand' | 'inverse'
  label?: string
}) {
  const markClass = `${styles.mark} ${styles[size]} ${tone === 'inverse' ? styles.inverse : ''}`.trim()

  if (label) {
    return (
      <span className={markClass} role="img" aria-label={label}>
        <HgMonogram />
      </span>
    )
  }

  return (
    <span className={markClass} aria-hidden="true">
      <HgMonogram />
    </span>
  )
}
