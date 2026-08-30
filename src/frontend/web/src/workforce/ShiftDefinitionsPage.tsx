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
import { canManageHrShiftDefinitions } from './hrAccess'
import {
  createHrShiftDefinition,
  hrScheduleErrorKey,
  listHrShiftDefinitions,
  updateHrShiftDefinition,
  type ShiftDefinitionRecord,
} from './hrScheduleApi'
import {
  compareShiftDefinitionsByStart,
  formatShiftClockRange,
  formatTimeForInput,
  isOvernightInconsistent,
  parseTimeInput,
  splitNetDuration,
} from './shiftDefinitionForm'

type DraftRow = ShiftDefinitionRecord & {
  startLocalTimeInput: string
  endLocalTimeInput: string
  breakMinutesInput: string
}

function toDraft(row: ShiftDefinitionRecord): DraftRow {
  return {
    ...row,
    startLocalTimeInput: formatTimeForInput(row.startLocalTime),
    endLocalTimeInput: formatTimeForInput(row.endLocalTime),
    breakMinutesInput: String(row.breakMinutes),
  }
}

function sortedDefinitions(rows: ShiftDefinitionRecord[]) {
  return [...rows].sort(compareShiftDefinitionsByStart)
}

export function ShiftDefinitionsPage() {
  const { t } = useTranslation()
  const { user } = useAuthSession()
  const canManage = canManageHrShiftDefinitions(user)
  const [rows, setRows] = useState<DraftRow[] | null>(null)
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [startLocalTime, setStartLocalTime] = useState('08:00')
  const [endLocalTime, setEndLocalTime] = useState('16:00')
  const [endsNextDay, setEndsNextDay] = useState(false)
  const [breakMinutes, setBreakMinutes] = useState('0')
  const [createActive, setCreateActive] = useState(true)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const createOvernightInvalid = isOvernightInconsistent(startLocalTime, endLocalTime, endsNextDay)

  useEffect(() => {
    let cancelled = false

    async function loadPage() {
      try {
        const data = await listHrShiftDefinitions()
        if (!cancelled) {
          setRows(sortedDefinitions(data).map(toDraft))
        }
      } catch (reason) {
        if (!cancelled) {
          setError(t(hrScheduleErrorKey(reason)))
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
    setRows(sortedDefinitions(await listHrShiftDefinitions()).map(toDraft))
  }

  function formatNetLabel(plannedNetMinutes: number) {
    const { hours, minutes } = splitNetDuration(plannedNetMinutes)
    if (hours > 0 && minutes === 0) {
      return t('workforce.shiftNetHours', { hours })
    }
    if (hours > 0) {
      return t('workforce.shiftNetHoursMinutes', { hours, minutes })
    }
    return t('workforce.shiftNetMinutes', { minutes })
  }

  function formatBreakLabel(minutes: number) {
    if (minutes <= 0) {
      return t('workforce.shiftBreakNone')
    }
    return t('workforce.shiftBreakValue', { minutes })
  }

  async function onCreate() {
    setError(null)
    setFormError(null)

    if (createOvernightInvalid) {
      setFormError(t('workforce.overnightInconsistent'))
      return
    }

    const start = parseTimeInput(startLocalTime)
    const end = parseTimeInput(endLocalTime)
    const breakValue = Number(breakMinutes)
    if (!start || !end || !Number.isInteger(breakValue) || breakValue < 0) {
      setFormError(t('workforce.scheduleErrors.invalidTime'))
      return
    }

    try {
      const created = await createHrShiftDefinition({
        code,
        name,
        startLocalTime: start,
        endLocalTime: end,
        endsNextDay,
        breakMinutes: breakValue,
      })
      if (!createActive) {
        await updateHrShiftDefinition(created.id, { isActive: false })
      }
      setCode('')
      setName('')
      setStartLocalTime('08:00')
      setEndLocalTime('16:00')
      setEndsNextDay(false)
      setBreakMinutes('0')
      setCreateActive(true)
      await reload()
    } catch (reason) {
      setError(t(hrScheduleErrorKey(reason)))
    }
  }

  async function onSave(row: DraftRow) {
    setError(null)
    setFormError(null)

    const overnightInvalid =
      !row.semanticFieldsLocked &&
      isOvernightInconsistent(row.startLocalTimeInput, row.endLocalTimeInput, row.endsNextDay)
    if (overnightInvalid) {
      setFormError(t('workforce.overnightInconsistent'))
      return
    }

    const patch: Parameters<typeof updateHrShiftDefinition>[1] = { name: row.name }

    if (!row.semanticFieldsLocked) {
      const start = parseTimeInput(row.startLocalTimeInput)
      const end = parseTimeInput(row.endLocalTimeInput)
      const breakValue = Number(row.breakMinutesInput)
      if (!start || !end || !Number.isInteger(breakValue) || breakValue < 0) {
        setFormError(t('workforce.scheduleErrors.invalidTime'))
        return
      }
      patch.startLocalTime = start
      patch.endLocalTime = end
      patch.endsNextDay = row.endsNextDay
      patch.breakMinutes = breakValue
    }

    try {
      await updateHrShiftDefinition(row.id, patch)
      setEditingId(null)
      await reload()
    } catch (reason) {
      setError(t(hrScheduleErrorKey(reason)))
    }
  }

  async function onToggle(row: DraftRow) {
    setError(null)
    try {
      await updateHrShiftDefinition(row.id, { isActive: !row.isActive })
      await reload()
    } catch (reason) {
      setError(t(hrScheduleErrorKey(reason)))
    }
  }

  function patchDraft(id: string, patch: Partial<DraftRow>) {
    setRows((current) =>
      (current ?? []).map((item) => (item.id === id ? { ...item, ...patch } : item)),
    )
  }

  if (rows === null && error === null) {
    return <Skeleton variant="list" rows={6} label={t('workforce.loading')} />
  }

  const list = rows ?? []

  return (
    <div className={styles.page}>
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
              id="shift-definition-code"
              label={t('workforce.shiftCode')}
              value={code}
              onChange={setCode}
              required
            />
            <TextField
              id="shift-definition-name"
              label={t('workforce.shiftName')}
              value={name}
              onChange={setName}
              required
            />
            <TextField
              id="shift-definition-start"
              label={t('workforce.shiftStartTime')}
              type="time"
              value={startLocalTime}
              onChange={setStartLocalTime}
              required
              error={createOvernightInvalid ? t('workforce.overnightInconsistent') : undefined}
            />
            <TextField
              id="shift-definition-end"
              label={t('workforce.shiftEndTime')}
              type="time"
              value={endLocalTime}
              onChange={setEndLocalTime}
              required
            />
            <TextField
              id="shift-definition-break"
              label={t('workforce.shiftBreakMinutes')}
              type="number"
              min={0}
              step={1}
              value={breakMinutes}
              onChange={setBreakMinutes}
              required
            />
            <div className={styles.formOptions}>
              <label className={styles.choiceOption} htmlFor="shift-definition-ends-next-day">
                <input
                  id="shift-definition-ends-next-day"
                  type="checkbox"
                  checked={endsNextDay}
                  onChange={(event) => setEndsNextDay(event.target.checked)}
                />
                {t('workforce.endsNextDay')}
              </label>
              <label className={styles.choiceOption} htmlFor="shift-definition-active">
                <input
                  id="shift-definition-active"
                  type="checkbox"
                  checked={createActive}
                  onChange={(event) => setCreateActive(event.target.checked)}
                />
                {t('workforce.activeStatus')}
              </label>
            </div>
          </div>
          <div className={`${styles.formFooter} ${styles.formFooterEnd}`}>
            <Button type="submit" layout="inline" disabled={createOvernightInvalid}>
              {t('workforce.createShiftDefinition')}
            </Button>
          </div>
        </form>
      ) : null}
      {formError ? <Notice tone="danger">{formError}</Notice> : null}
      {error ? <Notice tone="danger">{error}</Notice> : null}

      <section className={styles.shiftListSection} aria-labelledby="shift-definitions-heading">
        <h2 id="shift-definitions-heading" className={styles.sectionTitle}>
          {t('workforce.definedShifts')}
        </h2>
        {list.length === 0 ? (
          <EmptyState
            title={t('workforce.emptyShiftDefinitions')}
            description={t('workforce.emptyShiftDefinitionsHint')}
          />
        ) : (
          <div className={styles.list} role="table" aria-label={t('workforce.definedShifts')}>
            <div className={`${styles.row} ${styles.head} ${styles.shiftDefinitionRow}`} role="row">
              <span role="columnheader">{t('workforce.shiftColumnName')}</span>
              <span role="columnheader">{t('workforce.shiftCode')}</span>
              <span role="columnheader">{t('workforce.shiftColumnHours')}</span>
              <span role="columnheader">{t('workforce.shiftBreakMinutes')}</span>
              <span role="columnheader">{t('workforce.shiftColumnNet')}</span>
              <span role="columnheader">{t('workforce.status')}</span>
              <span role="columnheader">{t('workforce.shiftColumnActions')}</span>
            </div>
            {list.map((row) => {
              const editOvernightInvalid =
                editingId === row.id &&
                !row.semanticFieldsLocked &&
                isOvernightInconsistent(
                  row.startLocalTimeInput,
                  row.endLocalTimeInput,
                  row.endsNextDay,
                )
              const statusLabel = row.isActive ? t('workforce.activeStatus') : t('workforce.inactive')
              const clockRange = formatShiftClockRange(row.startLocalTime, row.endLocalTime)
              const toggleLabel = row.isActive
                ? `${t('workforce.deactivate')}: ${row.name}`
                : `${t('workforce.activate')}: ${row.name}`

              if (editingId === row.id) {
                return (
                  <div key={row.id} className={`${styles.row} ${styles.shiftDefinitionEdit}`}>
                    <div className={styles.formStack}>
                      <TextField
                        id={`shift-definition-name-${row.id}`}
                        label={t('workforce.shiftName')}
                        value={row.name}
                        onChange={(value) => patchDraft(row.id, { name: value })}
                      />
                      <span className={styles.muted}>
                        {t('workforce.shiftCode')}: {row.code}
                      </span>
                      {row.semanticFieldsLocked ? (
                        <Notice tone="info">{t('workforce.semanticFieldsLockedHint')}</Notice>
                      ) : null}
                      <div className={styles.formGrid}>
                        <TextField
                          id={`shift-definition-start-${row.id}`}
                          label={t('workforce.shiftStartTime')}
                          type="time"
                          value={row.startLocalTimeInput}
                          onChange={(value) => patchDraft(row.id, { startLocalTimeInput: value })}
                          readOnly={row.semanticFieldsLocked}
                          disabled={row.semanticFieldsLocked}
                          error={editOvernightInvalid ? t('workforce.overnightInconsistent') : undefined}
                        />
                        <TextField
                          id={`shift-definition-end-${row.id}`}
                          label={t('workforce.shiftEndTime')}
                          type="time"
                          value={row.endLocalTimeInput}
                          onChange={(value) => patchDraft(row.id, { endLocalTimeInput: value })}
                          readOnly={row.semanticFieldsLocked}
                          disabled={row.semanticFieldsLocked}
                        />
                        <TextField
                          id={`shift-definition-break-${row.id}`}
                          label={t('workforce.shiftBreakMinutes')}
                          type="number"
                          min={0}
                          step={1}
                          value={row.breakMinutesInput}
                          onChange={(value) => patchDraft(row.id, { breakMinutesInput: value })}
                          readOnly={row.semanticFieldsLocked}
                          disabled={row.semanticFieldsLocked}
                        />
                        <div className={styles.formOptions}>
                          <label
                            className={styles.choiceOption}
                            htmlFor={`shift-definition-ends-next-day-${row.id}`}
                          >
                            <input
                              id={`shift-definition-ends-next-day-${row.id}`}
                              type="checkbox"
                              checked={row.endsNextDay}
                              disabled={row.semanticFieldsLocked}
                              onChange={(event) =>
                                patchDraft(row.id, { endsNextDay: event.target.checked })
                              }
                            />
                            {t('workforce.endsNextDay')}
                          </label>
                        </div>
                      </div>
                    </div>
                    <div className={`${styles.actions} ${styles.shiftActions}`}>
                      <StatusBadge
                        className={styles.shiftStatusBadge}
                        tone={row.isActive ? 'success' : 'neutral'}
                      >
                        {statusLabel}
                      </StatusBadge>
                      <Button
                        variant="secondary"
                        size="sm"
                        layout="inline"
                        disabled={editOvernightInvalid}
                        onClick={() => void onSave(row)}
                      >
                        {t('workforce.save')}
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => {
                          setEditingId(null)
                          setFormError(null)
                          void reload().catch((reason) => setError(t(hrScheduleErrorKey(reason))))
                        }}
                      >
                        {t('workforce.cancel')}
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => void onToggle(row)}
                        aria-label={toggleLabel}
                      >
                        {row.isActive ? t('workforce.deactivate') : t('workforce.activate')}
                      </Button>
                    </div>
                  </div>
                )
              }

              return (
                <div
                  key={row.id}
                  className={`${styles.row} ${styles.shiftDefinitionRow}`}
                  role="row"
                >
                  <div className={styles.shiftIdentity} role="cell">
                    <span className={styles.cellLabel}>{t('workforce.shiftColumnName')}</span>
                    <span className={styles.personName}>{row.name}</span>
                  </div>
                  <div role="cell">
                    <span className={styles.cellLabel}>{t('workforce.shiftCode')}</span>
                    <span className={styles.personMeta}>{row.code}</span>
                  </div>
                  <div className={styles.shiftHoursCell} role="cell">
                    <span className={styles.cellLabel}>{t('workforce.shiftColumnHours')}</span>
                    <span className={styles.shiftClockRange}>{clockRange}</span>
                    {row.endsNextDay ? (
                      <span className={styles.shiftOvernightHint}>{t('workforce.endsNextDayShort')}</span>
                    ) : null}
                  </div>
                  <div role="cell">
                    <span className={styles.cellLabel}>{t('workforce.shiftBreakMinutes')}</span>
                    <span>{formatBreakLabel(row.breakMinutes)}</span>
                  </div>
                  <div role="cell">
                    <span className={styles.cellLabel}>{t('workforce.shiftColumnNet')}</span>
                    <span className={styles.shiftNet}>{formatNetLabel(row.plannedNetMinutes)}</span>
                  </div>
                  <div className={styles.shiftStatusCell} role="cell">
                    <span className={styles.cellLabel}>{t('workforce.status')}</span>
                    <StatusBadge
                      className={styles.shiftStatusBadge}
                      tone={row.isActive ? 'success' : 'neutral'}
                    >
                      {statusLabel}
                    </StatusBadge>
                  </div>
                  {canManage ? (
                    <div className={`${styles.actions} ${styles.shiftActions}`} role="cell">
                      <span className={styles.cellLabel}>{t('workforce.shiftColumnActions')}</span>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => {
                          setEditingId(row.id)
                          setFormError(null)
                        }}
                      >
                        {t('workforce.editShiftDefinition')}
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => void onToggle(row)}
                        aria-label={toggleLabel}
                      >
                        {row.isActive ? t('workforce.deactivate') : t('workforce.activate')}
                      </Button>
                    </div>
                  ) : (
                    <div className={styles.shiftActions} role="cell" />
                  )}
                </div>
              )
            })}
          </div>
        )}
      </section>
    </div>
  )
}
