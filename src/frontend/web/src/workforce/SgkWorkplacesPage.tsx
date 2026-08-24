import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { Button } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { Notice } from '../ui/Notice'
import { Skeleton } from '../ui/Skeleton'
import { StatusBadge } from '../ui/StatusBadge'
import { TextField } from '../ui/TextField'
import styles from './Workforce.module.css'
import type { SgkWorkplaceRecord } from './hrApi'
import { canManageWorkforce } from './workforceAccess'
import {
  createSgkWorkplace,
  listSgkWorkplaces,
  updateSgkWorkplace,
  workforceErrorKey,
} from './workforceApi'

function sortedRecords(rows: SgkWorkplaceRecord[]) {
  return [...rows].sort((left, right) => {
    if (left.isActive !== right.isActive) {
      return left.isActive ? -1 : 1
    }

    return left.pickerLabel.localeCompare(right.pickerLabel)
  })
}

export function SgkWorkplacesPage() {
  const { t } = useTranslation()
  const { user } = useAuthSession()
  const canManage = canManageWorkforce(user)
  const [rows, setRows] = useState<SgkWorkplaceRecord[] | null>(null)
  const [registrationNumber, setRegistrationNumber] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function loadPage() {
      try {
        const data = await listSgkWorkplaces()
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
      await createSgkWorkplace(registrationNumber, displayName)
      setRegistrationNumber('')
      setDisplayName('')
      setRows(sortedRecords(await listSgkWorkplaces()))
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    }
  }

  async function onSave(row: SgkWorkplaceRecord) {
    setError(null)
    try {
      await updateSgkWorkplace(row.id, {
        registrationNumber: row.registrationNumber ?? '',
        displayName: row.displayName,
      })
      setEditingId(null)
      setRows(sortedRecords(await listSgkWorkplaces()))
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    }
  }

  async function onToggle(row: SgkWorkplaceRecord) {
    setError(null)
    try {
      await updateSgkWorkplace(row.id, { isActive: !row.isActive })
      setRows(sortedRecords(await listSgkWorkplaces()))
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    }
  }

  if (rows === null && error === null) {
    return <Skeleton variant="list" rows={5} label={t('workforce.loading')} />
  }

  const list = rows ?? []

  return (
    <div className={styles.page}>
      <p className={styles.muted}>{t('workforce.officialSettingsIntro')}</p>
      <p className={styles.muted}>{t('workforce.sgkWorkplaceHint')}</p>
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
              id="sgk-display-name"
              label={t('workforce.displayName')}
              value={displayName}
              onChange={setDisplayName}
              placeholder={t('workforce.displayName')}
            />
            <TextField
              id="sgk-registration-number"
              label={t('workforce.registrationNumber')}
              value={registrationNumber}
              onChange={setRegistrationNumber}
              required
            />
          </div>
          <Button type="submit" layout="inline">
            {t('workforce.createSgkWorkplace')}
          </Button>
        </form>
      ) : null}
      {error ? <Notice tone="danger">{error}</Notice> : null}
      {list.length === 0 ? (
        <EmptyState title={t('workforce.emptySgkWorkplaces')} description={t('workforce.emptySgkWorkplacesHint')} />
      ) : (
        <div className={styles.list}>
          {list.map((row) => (
            <div key={row.id} className={`${styles.row} ${styles.structureRow}`}>
              {editingId === row.id ? (
                <>
                  <TextField
                    id={`sgk-rename-${row.id}`}
                    label={t('workforce.displayName')}
                    value={row.displayName ?? ''}
                    onChange={(value) =>
                      setRows((current) =>
                        sortedRecords(
                          (current ?? []).map((item) =>
                            item.id === row.id ? { ...item, displayName: value } : item,
                          ),
                        ),
                      )
                    }
                  />
                  <TextField
                    id={`sgk-number-${row.id}`}
                    label={t('workforce.registrationNumber')}
                    value={row.registrationNumber ?? ''}
                    onChange={(value) =>
                      setRows((current) =>
                        sortedRecords(
                          (current ?? []).map((item) =>
                            item.id === row.id ? { ...item, registrationNumber: value } : item,
                          ),
                        ),
                      )
                    }
                  />
                </>
              ) : (
                <>
                  <span className={styles.personName}>{row.displayName || row.pickerLabel}</span>
                  <span className={styles.muted}>{row.registrationNumber}</span>
                </>
              )}
              <StatusBadge tone={row.isActive ? 'success' : 'neutral'}>
                {row.isActive ? t('workforce.activeStatus') : t('workforce.inactive')}
              </StatusBadge>
              {canManage ? (
                <div className={styles.actions}>
                  {editingId === row.id ? (
                    <Button variant="secondary" size="sm" layout="inline" onClick={() => void onSave(row)}>
                      {t('workforce.save')}
                    </Button>
                  ) : (
                    <Button variant="ghost" size="sm" onClick={() => setEditingId(row.id)}>
                      {t('workforce.rename')}
                    </Button>
                  )}
                  <Button variant="ghost" size="sm" onClick={() => void onToggle(row)}>
                    {row.isActive ? t('workforce.deactivate') : t('workforce.activate')}
                  </Button>
                </div>
              ) : null}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
