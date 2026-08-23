import styles from './TopBar.module.css'

export function TopBar({
  kicker,
  title,
  subtitle,
}: {
  kicker?: string
  title: string
  subtitle?: string
}) {
  return (
    <header className={styles.topBar}>
      {kicker ? <p className={`kicker ${styles.kicker}`}>{kicker}</p> : null}
      <h1 className={styles.title}>{title}</h1>
      {subtitle ? <p className={styles.subtitle}>{subtitle}</p> : null}
    </header>
  )
}
