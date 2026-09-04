import styles from './BrandMark.module.css'

export const HUGUWEB_MARK_SRC = '/huguweb.svg'

export type BrandMarkSize = 'login' | 'sidebar' | 'sidebarCollapsed' | 'mobile'

export function BrandMark({
  size = 'sidebar',
  tone = 'brand',
  label,
}: {
  size?: BrandMarkSize
  tone?: 'brand' | 'inverse'
  label?: string
}) {
  const markClass = `${styles.mark} ${styles[size]} ${tone === 'inverse' ? styles.inverse : ''}`.trim()
  const image = (
    <img
      className={styles.image}
      src={HUGUWEB_MARK_SRC}
      alt={label ?? ''}
      width={500}
      height={500}
      draggable={false}
    />
  )

  if (label) {
    return <span className={markClass}>{image}</span>
  }

  return (
    <span className={markClass} aria-hidden="true">
      {image}
    </span>
  )
}
