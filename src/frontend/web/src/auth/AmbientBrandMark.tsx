import { HUGUWEB_MARK_SRC } from '../ui/BrandMark'
import styles from './AmbientBrandMark.module.css'

export function AmbientBrandMark() {
  return (
    <div className={styles.ambient} aria-hidden="true">
      <div className={styles.clip}>
        <span className={styles.compact}>
          <img src={HUGUWEB_MARK_SRC} alt="" width={500} height={500} draggable={false} />
        </span>
        <span className={styles.expanded}>HuGu</span>
      </div>
    </div>
  )
}
