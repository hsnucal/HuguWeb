import styles from './DistributionBar.module.css'

export type DistributionSegment = {
  id: string
  label: string
  count: number
  tone: 'dirty' | 'clean' | 'inspected' | 'ready' | 'neutral' | 'info' | 'warning' | 'success' | 'accent'
}

export function DistributionBar({
  segments,
  ariaLabel,
}: {
  segments: DistributionSegment[]
  ariaLabel: string
}) {
  const total = segments.reduce((sum, item) => sum + item.count, 0)
  const max = Math.max(...segments.map((item) => item.count), 1)

  return (
    <div className={styles.wrap}>
      <div className={styles.bar} role="img" aria-label={ariaLabel}>
        {segments.map((item) => (
          <span
            key={item.id}
            className={`${styles.seg} ${styles[item.tone]}`}
            style={{ flexGrow: Math.max(item.count, 0) }}
            title={`${item.label}: ${item.count}`}
          />
        ))}
      </div>
      <ul className={styles.legend}>
        {segments.map((item) => (
          <li key={item.id} className={styles.legendItem}>
            <span className={styles.legendCopy}>
              <span className={`${styles.swatch} ${styles[item.tone]}`} aria-hidden="true" />
              <span className={styles.legendLabel}>{item.label}</span>
              <span className={styles.legendCount}>{item.count}</span>
            </span>
            <span className={styles.track} aria-hidden="true">
              <span
                className={`${styles.fill} ${styles[item.tone]}`}
                style={{ width: `${Math.round((item.count / max) * 100)}%` }}
              />
            </span>
          </li>
        ))}
      </ul>
      {total === 0 ? null : <span className="visually-hidden">{total}</span>}
    </div>
  )
}
