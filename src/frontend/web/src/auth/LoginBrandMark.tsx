import styles from './LoginBrandMark.module.css'

export function LoginBrandMark() {
  return (
    <span className={styles.mark} aria-hidden="true">
      <span className={styles.lockup}>
        <span className={styles.h}>H</span>
        <span className={styles.g}>G</span>
      </span>
    </span>
  )
}
