import styles from './TopBar.module.css'

export function TopBar({ title, subtitle }: { title: string; subtitle?: string }) {
  return (
    <header className={styles.topBar}>
      <h1 className={styles.title}>{title}</h1>
      {subtitle ? <p className={styles.subtitle}>{subtitle}</p> : null}
    </header>
  )
}
