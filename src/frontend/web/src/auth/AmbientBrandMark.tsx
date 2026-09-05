import { HUGUWEB_MARK_SRC } from '../ui/BrandMark'
import styles from './AmbientBrandMark.module.css'

export function AmbientBrandMark() {
  return (
    <div className={styles.ambient} aria-hidden="true">
      <div className={styles.stage}>
        <span className={styles.emblem}>
          <img src={HUGUWEB_MARK_SRC} alt="" width={500} height={500} draggable={false} />
        </span>
        <span className={styles.wordmark}>
          <span className={styles.letterH}>H</span>
          <span className={styles.letterU1}>u</span>
          <span className={styles.letterG}>G</span>
          <span className={styles.letterU2}>u</span>
        </span>
      </div>
    </div>
  )
}
