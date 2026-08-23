import { HgMonogram } from './HgMonogram'
import styles from './BrandMark.module.css'

export function BrandMark({ size = 'md' }: { size?: 'md' | 'lg' | 'xl' }) {
  const sizeClass = size === 'md' ? '' : styles[size]
  const markClass = `${styles.mark} ${sizeClass}`.trim()

  return (
    <span className={markClass} aria-hidden="true">
      <HgMonogram />
    </span>
  )
}
