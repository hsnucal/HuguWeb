import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '../ui/Button'
import { StatusBadge } from '../ui/StatusBadge'
import { TextField } from '../ui/TextField'
import styles from './Workforce.module.css'
import {
  type PositionRecord,
  createPosition,
  listPositions,
  updatePosition,
  workforceErrorKey,
} from './workforceApi'

export function PositionsPage() {
  const { t } = useTranslation()
  const [rows, setRows] = useState<PositionRecord[]>([])
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function loadPage() {
      try {
        const data = await listPositions()
        if (!cancelled) {
          setRows(data)
        }
      } catch (reason) {
        if (!cancelled) {
          setError(t(workforceErrorKey(reason)))
        }
      }
    }

    void loadPage()
    return () => {
      cancelled = true
    }
  }, [t])

  async function onCreate() {
    setError(null)
    try {
      await createPosition(name, code)
      setName('')
      setCode('')
      setRows(await listPositions())
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    }
  }

  async function onSave(row: PositionRecord) {
    setError(null)
    try {
      await updatePosition(row.id, { name: row.name, code: row.code })
      setEditingId(null)
      setRows(await listPositions())
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    }
  }

  async function onToggle(row: PositionRecord) {
    setError(null)
    try {
      await updatePosition(row.id, { isActive: !row.isActive })
      setRows(await listPositions())
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    }
  }

  return (
    <div className={styles.page}>
      <p className={styles.muted}>{t('workforce.positionsIntro')}</p>
      <form
        className={styles.panel}
        onSubmit={(event) => {
          event.preventDefault()
          void onCreate()
        }}
      >
        <div className={styles.formGrid}>
          <TextField
            id="position-name"
            label={t('workforce.name')}
            value={name}
            onChange={setName}
            required
          />
          <TextField id="position-code" label={t('workforce.code')} value={code} onChange={setCode} />
        </div>
        <Button type="submit" layout="inline">
          {t('workforce.createPosition')}
        </Button>
      </form>

      {error ? (
        <p className={styles.error} role="alert">
          {error}
        </p>
      ) : null}

      <section className={styles.list} aria-label={t('workforce.positions')}>
        {rows.length === 0 ? (
          <p className={styles.empty}>{t('workforce.emptyPositions')}</p>
        ) : (
          rows.map((row) => (
            <div key={row.id} className={`${styles.row} ${styles.structureRow}`}>
              {editingId === row.id ? (
                <TextField
                  id={`position-rename-${row.id}`}
                  label={t('workforce.name')}
                  value={row.name}
                  onChange={(value) =>
                    setRows((current) =>
                      current.map((item) => (item.id === row.id ? { ...item, name: value } : item)),
                    )
                  }
                />
              ) : (
                <span className={styles.personName}>{row.name}</span>
              )}
              <span className={styles.muted}>{row.code}</span>
              <StatusBadge tone={row.isActive ? 'success' : 'neutral'}>
                {row.isActive ? t('workforce.activeStatus') : t('workforce.inactive')}
              </StatusBadge>
              <div className={styles.actions}>
                {editingId === row.id ? (
                  <Button layout="inline" onClick={() => void onSave(row)}>
                    {t('workforce.save')}
                  </Button>
                ) : (
                  <Button variant="ghost" onClick={() => setEditingId(row.id)}>
                    {t('workforce.rename')}
                  </Button>
                )}
                <Button variant="ghost" onClick={() => void onToggle(row)}>
                  {row.isActive ? t('workforce.deactivate') : t('workforce.activate')}
                </Button>
              </div>
            </div>
          ))
        )}
      </section>
    </div>
  )
}
