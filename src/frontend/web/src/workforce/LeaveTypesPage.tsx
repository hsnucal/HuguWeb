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
import { canManageHrLeave } from './hrAccess'
import { formatLeaveAmount, isPositiveHalfDayAmount, parseLeaveAmount } from './leaveAmount'
import {
  createHrLeaveType,
  hrLeaveErrorKey,
  listHrLeaveTypes,
  updateHrLeaveType,
  type LeaveTypeRecord,
} from './hrLeaveApi'

function sortedTypes(rows: LeaveTypeRecord[]) {
  return [...rows].sort((left, right) => {
    if (left.isActive !== right.isActive) {
      return left.isActive ? -1 : 1
    }

    return left.name.localeCompare(right.name)
  })
}

export function LeaveTypesPage() {
  const { t } = useTranslation()
  const { user } = useAuthSession()
  const canManage = canManageHrLeave(user)
  const [rows, setRows] = useState<LeaveTypeRecord[] | null>(null)
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [tracksBalance, setTracksBalance] = useState(false)
  const [defaultRequestAmount, setDefaultRequestAmount] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function loadPage() {
      try {
        const data = await listHrLeaveTypes()
        if (!cancelled) {
          setRows(sortedTypes(data))
        }
      } catch (reason) {
        if (!cancelled) {
          setError(t(hrLeaveErrorKey(reason)))
          setRows([])
        }
      }
    }

    void loadPage()
    return () => {
      cancelled = true
    }
  }, [t])

  async function reload() {
    setRows(sortedTypes(await listHrLeaveTypes()))
  }

  async function onCreate() {
    setError(null)
    const trimmedDefault = defaultRequestAmount.trim()
    if (trimmedDefault && !isPositiveHalfDayAmount(trimmedDefault)) {
      setError(t('personnel.leave.errors.invalidAmount'))
      return
    }
    try {
      await createHrLeaveType({
        code,
        name,
        tracksBalance,
        defaultRequestAmount: trimmedDefault ? parseLeaveAmount(trimmedDefault) : null,
      })
      setCode('')
      setName('')
      setTracksBalance(false)
      setDefaultRequestAmount('')
      await reload()
    } catch (reason) {
      setError(t(hrLeaveErrorKey(reason)))
    }
  }

  async function onSave(row: LeaveTypeRecord) {
    setError(null)
    try {
      await updateHrLeaveType(row.id, {
        name: row.name,
        tracksBalance: row.tracksBalance,
        defaultRequestAmount: row.defaultRequestAmount,
      })
      setEditingId(null)
      await reload()
    } catch (reason) {
      setError(t(hrLeaveErrorKey(reason)))
    }
  }

  async function onToggle(row: LeaveTypeRecord) {
    setError(null)
    try {
      await updateHrLeaveType(row.id, { isActive: !row.isActive })
      await reload()
    } catch (reason) {
      setError(t(hrLeaveErrorKey(reason)))
    }
  }

  if (rows === null && error === null) {
    return <Skeleton variant="list" rows={6} label={t('workforce.loading')} />
  }

  const list = rows ?? []

  return (
    <div className={styles.page}>
      <p className={styles.muted}>{t('workforce.leaveTypesIntro')}</p>
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
              id="leave-type-code"
              label={t('workforce.leaveTypeCode')}
              value={code}
              onChange={setCode}
              required
            />
            <TextField
              id="leave-type-name"
              label={t('workforce.leaveTypeName')}
              value={name}
              onChange={setName}
              required
            />
            <TextField
              id="leave-type-default-amount"
              label={t('workforce.leaveTypeDefaultRequestAmount')}
              value={defaultRequestAmount}
              onChange={setDefaultRequestAmount}
              hint={t('workforce.leaveTypeDefaultRequestAmountHint')}
            />
          </div>
          <label className={styles.choiceOption} htmlFor="leave-type-tracks">
            <input
              id="leave-type-tracks"
              type="checkbox"
              checked={tracksBalance}
              onChange={(event) => setTracksBalance(event.target.checked)}
            />
            {t('workforce.leaveTracksBalance')}
          </label>
          <Button type="submit" layout="inline">
            {t('workforce.createLeaveType')}
          </Button>
        </form>
      ) : null}
      {error ? <Notice tone="danger">{error}</Notice> : null}
      {list.length === 0 ? (
        <EmptyState title={t('workforce.emptyLeaveTypes')} description={t('workforce.emptyLeaveTypesHint')} />
      ) : (
        <div className={styles.list}>
          {list.map((row) => (
            <div key={row.id} className={`${styles.row} ${styles.structureRow}`}>
              {editingId === row.id ? (
                <>
                  <TextField
                    id={`leave-type-name-${row.id}`}
                    label={t('workforce.leaveTypeName')}
                    value={row.name}
                    onChange={(value) =>
                      setRows((current) =>
                        sortedTypes(
                          (current ?? []).map((item) => (item.id === row.id ? { ...item, name: value } : item)),
                        ),
                      )
                    }
                  />
                  <TextField
                    id={`leave-type-default-${row.id}`}
                    label={t('workforce.leaveTypeDefaultRequestAmount')}
                    value={
                      row.defaultRequestAmount == null ? '' : formatLeaveAmount(row.defaultRequestAmount)
                    }
                    onChange={(value) =>
                      setRows((current) =>
                        sortedTypes(
                          (current ?? []).map((item) =>
                            item.id === row.id
                              ? {
                                  ...item,
                                  defaultRequestAmount:
                                    value.trim() === '' ? null : parseLeaveAmount(value),
                                }
                              : item,
                          ),
                        ),
                      )
                    }
                    hint={t('workforce.leaveTypeDefaultRequestAmountHint')}
                  />
                  <label className={styles.choiceOption} htmlFor={`leave-type-tracks-${row.id}`}>
                    <input
                      id={`leave-type-tracks-${row.id}`}
                      type="checkbox"
                      checked={row.tracksBalance}
                      onChange={(event) =>
                        setRows((current) =>
                          sortedTypes(
                            (current ?? []).map((item) =>
                              item.id === row.id ? { ...item, tracksBalance: event.target.checked } : item,
                            ),
                          ),
                        )
                      }
                    />
                    {t('workforce.leaveTracksBalance')}
                  </label>
                </>
              ) : (
                <>
                  <span className={styles.personName}>{row.name}</span>
                  <span className={styles.muted}>{row.code}</span>
                  <span className={styles.muted}>
                    {row.tracksBalance ? t('workforce.leaveTracksBalanceYes') : t('workforce.leaveTracksBalanceNo')}
                    {row.defaultRequestAmount != null
                      ? ` · ${formatLeaveAmount(row.defaultRequestAmount)} ${t('personnel.leave.dayUnit')}`
                      : ''}
                  </span>
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
