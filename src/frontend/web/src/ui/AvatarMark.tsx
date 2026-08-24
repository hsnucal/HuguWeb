import styles from './AvatarMark.module.css'

function initialsFromName(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) {
    return '·'
  }

  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase()
  }

  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase()
}

export function AvatarMark({
  name,
  size = 'md',
  src,
  alt,
  tone = 'brand',
}: {
  name: string
  size?: 'sm' | 'md' | 'lg' | 'xl'
  src?: string | null
  alt?: string
  tone?: 'brand' | 'onBrand'
}) {
  const markClass = `${styles.mark} ${styles[size]} ${tone === 'onBrand' ? styles.onBrand : ''}`

  if (src) {
    return (
      <img
        className={`${markClass} ${styles.photo}`}
        src={src}
        alt={alt ?? ''}
      />
    )
  }

  return (
    <span className={markClass} aria-hidden="true">
      {initialsFromName(name)}
    </span>
  )
}
