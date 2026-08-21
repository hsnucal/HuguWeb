import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { Button } from '../ui/Button'
import { StatusBadge } from '../ui/StatusBadge'
import { TextField } from '../ui/TextField'
import styles from './Workforce.module.css'
import { canManageWorkforce } from './workforceAccess'
import {
  type PositionRecord,
  createPosition,
  listPositions,
  updatePosition,
  workforceErrorKey,
} from './workforceApi'

function sortedRecords<T extends { isActive: boolean; name: string }>(rows: T[]) {
  return [...rows].sort((left, right) => {
    if (left.isActive !== right.isActive) {
      return left.isActive ? -1 : 1
    }

    return left.name.localeCompare(right.name)
  })
}

export function PositionsPage() {
  const { t } = useTranslation()
  const { user } = useAuthSession()
  const canManage = canManageWorkforce(user)
  const [rows, setRows] = useState<PositionRecord[] | null>(null)
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
          setRows(sortedRecords(data))
        }
      } catch (reason) {
        if (!cancelled) {
          setError(t(workforceErrorKey(reason)))
          setRows([])
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
      setRows(sortedRecords(await listPositions()))
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    }
  }

  async function onSave(row: PositionRecord) {
    setError(null)
    try {
      await updatePosition(row.id, { name: row.name, code: row.code })
      setEditingId(null)
      setRows(sortedRecords(await listPositions()))
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    }
  }

  async function onToggle(row: PositionRecord) {
    setError(null)
    try {
      await updatePosition(row.id, { isActive: !row.isActive })
      setRows(sortedRecords(await listPositions()))
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    }
  }

  if (rows === null && error === null) {
    return (
      <p className={styles.muted} role="status">
        {t('workforce.loading')}
      </p>
    )
  }

  const list = rows ?? []

  return (
    <div className={styles.page}>
      <p className={styles.muted}>{t('workforce.positionsIntro')}</p>
      {canManage ? (
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
      ) : null}

      {error ? (
        <p className={styles.error} role="alert">
          {error}
        </p>
      ) : null}

      <section className={styles.list} aria-label={t('workforce.positions')}>
        {list.length === 0 ? (
          <p className={styles.empty}>{t('workforce.emptyPositions')}</p>
        ) : (
          list.map((row) => (
            <div key={row.id} className={`${styles.row} ${styles.structureRow}`}>
              {editingId === row.id ? (
                <TextField
                  id={`position-rename-${row.id}`}
                  label={t('workforce.name')}
                  value={row.name}
                  onChange={(value) =>
                    setRows((current) =>
                      sortedRecords(
                        (current ?? []).map((item) =>
                          item.id === row.id ? { ...item, name: value } : item,
                        ),
                      ),
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
              {canManage ? (
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
              ) : null}
            </div>
          ))
        )}
      </section>
    </div>
  )
}
