import { HgMonogram } from '../ui/HgMonogram'
import styles from './AmbientBrandMark.module.css'

export function AmbientBrandMark() {
  return (
    <div className={styles.ambient} aria-hidden="true">
      <div className={styles.clip}>
        <span className={styles.compact}>
          <HgMonogram />
        </span>
        <span className={styles.expanded}>HuGu</span>
      </div>
    </div>
  )
}
