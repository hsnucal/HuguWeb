import { useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { formatDateOnly, todayIsoDate } from '../i18n/format'
import { toAppLanguage, type AppLanguage } from '../i18n/languages'
import { Button } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { Notice } from '../ui/Notice'
import { DateField, SelectField } from '../ui/SelectField'
import { Skeleton } from '../ui/Skeleton'
import { StatusBadge } from '../ui/StatusBadge'
import { TextArea, TextField } from '../ui/TextField'
import { WorkspaceDialog } from '../ui/WorkspaceDialog'
import { toIsoDate } from '../ui/dateEntry'
import styles from './PersonnelCard.module.css'
import {
  cancelHrLeaveRecord,
  createHrLeaveEntitlement,
  createHrLeaveRecord,
  getHrEmployeeLeave,
  hrLeaveErrorKey,
  type EmployeeLeaveOverview,
  type LeaveEntitlementSource,
  type LeaveTypeRecord,
} from './hrLeaveApi'
import {
  amountAfterDateChange,
  formatLeaveAmount,
  isNonZeroHalfDayAmount,
  isPositiveHalfDayAmount,
  parseLeaveAmount,
} from './leaveAmount'
import { endDateAfterStartChange, endMinDate, isStartOnOrBeforeEnd } from './leaveDateRange'
import { orderActiveLeaveTypes } from './leaveTypeOrder'

type LeaveDialog = 'none' | 'entitlement' | 'record' | 'cancel'

export function PersonnelLeaveTab({
  employeeId,
  canManage,
  language,
}: {
  employeeId: string
  canManage: boolean
  language: AppLanguage
}) {
  const { t, i18n } = useTranslation()
  const resolvedLanguage = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? language
  const [overview, setOverview] = useState<EmployeeLeaveOverview | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [dialog, setDialog] = useState<LeaveDialog>('none')
  const [cancelRecordId, setCancelRecordId] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError(null)
      try {
        const data = await getHrEmployeeLeave(employeeId)
        if (!cancelled) {
          setOverview(data)
        }
      } catch (reason) {
        if (!cancelled) {
          setError(t(hrLeaveErrorKey(reason)))
          setOverview(null)
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [employeeId, t])

  function applyOverview(next: EmployeeLeaveOverview) {
    setOverview(next)
    setDialog('none')
    setCancelRecordId(null)
    setError(null)
  }

  if (loading && overview === null) {
    return <Skeleton variant="list" rows={4} label={t('personnel.leave.loading')} />
  }

  const typesById = new Map((overview?.leaveTypes ?? []).map((item) => [item.id, item]))
  const hasHistory = (overview?.entitlements.length ?? 0) > 0 || (overview?.records.length ?? 0) > 0
  const balances = overview?.balances ?? []

  return (
    <div className={styles.workStack}>
      {error ? <Notice tone="danger">{error}</Notice> : null}

      <section className={styles.section}>
        <h3 className={styles.legend}>{t('personnel.leave.summaryTitle')}</h3>
        {balances.length === 0 ? (
          <EmptyState
            title={t('personnel.leave.emptyTitle')}
            description={t('personnel.leave.emptyHint')}
          />
        ) : (
          <div className={styles.leaveTableWrap}>
            <table className={styles.leaveTable}>
              <thead>
                <tr>
                  <th>{t('personnel.leave.type')}</th>
                  <th>{t('personnel.leave.netMovement')}</th>
                  <th>{t('personnel.leave.used')}</th>
                  <th>{t('personnel.leave.remaining')}</th>
                </tr>
              </thead>
              <tbody>
                {balances.map((row) => (
                  <tr key={row.leaveTypeId}>
                    <td>{leaveTypeLabel(typesById.get(row.leaveTypeId) ?? row, t)}</td>
                    <td>{formatLeaveAmount(row.netMovement)}</td>
                    <td>{formatLeaveAmount(row.used)}</td>
                    <td className={row.remaining < 0 ? styles.leaveNegative : undefined}>
                      {formatLeaveAmount(row.remaining)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <p className={styles.leaveHint}>{t('personnel.leave.calendarHint')}</p>
      </section>

      <section className={styles.section}>
        <div className={styles.leaveSectionHeader}>
          <h3 className={styles.legend}>{t('personnel.leave.entitlementsTitle')}</h3>
          {canManage ? (
            <Button variant="secondary" size="sm" layout="inline" onClick={() => setDialog('entitlement')}>
              {t('personnel.leave.addEntitlement')}
            </Button>
          ) : null}
        </div>
        {(overview?.entitlements.length ?? 0) === 0 ? (
          <p className={styles.muted}>{hasHistory ? t('personnel.leave.noEntitlements') : t('personnel.leave.emptyHint')}</p>
        ) : (
          <div className={styles.leaveTableWrap}>
            <table className={styles.leaveTable}>
              <thead>
                <tr>
                  <th>{t('personnel.leave.date')}</th>
                  <th>{t('personnel.leave.type')}</th>
                  <th>{t('personnel.leave.source')}</th>
                  <th>{t('personnel.leave.amount')}</th>
                  <th>{t('personnel.leave.note')}</th>
                </tr>
              </thead>
              <tbody>
                {overview!.entitlements.map((row) => (
                  <tr key={row.id}>
                    <td>{formatDateOnly(row.effectiveDate, resolvedLanguage)}</td>
                    <td>{leaveTypeLabel(typesById.get(row.leaveTypeId), t)}</td>
                    <td>{t(`personnel.leave.sources.${row.source}`)}</td>
                    <td>{formatLeaveAmount(row.amount)}</td>
                    <td>{row.note ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className={styles.section}>
        <div className={styles.leaveSectionHeader}>
          <h3 className={styles.legend}>{t('personnel.leave.historyTitle')}</h3>
          {canManage ? (
            <Button variant="secondary" size="sm" layout="inline" onClick={() => setDialog('record')}>
              {t('personnel.leave.addLeave')}
            </Button>
          ) : null}
        </div>
        {(overview?.records.length ?? 0) === 0 ? (
          <p className={styles.muted}>{t('personnel.leave.noRecords')}</p>
        ) : (
          <div className={styles.leaveTableWrap}>
            <table className={styles.leaveTable}>
              <thead>
                <tr>
                  <th>{t('personnel.leave.type')}</th>
                  <th>{t('personnel.leave.startDate')}</th>
                  <th>{t('personnel.leave.endDate')}</th>
                  <th>{t('personnel.leave.amount')}</th>
                  <th>{t('personnel.leave.status')}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {overview!.records.map((row) => (
                  <tr key={row.id}>
                    <td>{leaveTypeLabel(typesById.get(row.leaveTypeId), t)}</td>
                    <td>{formatDateOnly(row.startDate, resolvedLanguage)}</td>
                    <td>{formatDateOnly(row.endDate, resolvedLanguage)}</td>
                    <td>{formatLeaveAmount(row.amount)}</td>
                    <td>
                      <StatusBadge tone={row.status === 'Recorded' ? 'success' : 'neutral'}>
                        {t(`personnel.leave.statuses.${row.status}`)}
                      </StatusBadge>
                    </td>
                    <td>
                      {canManage && row.status === 'Recorded' ? (
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => {
                            setCancelRecordId(row.id)
                            setDialog('cancel')
                          }}
                        >
                          {t('personnel.leave.cancelLeave')}
                        </Button>
                      ) : null}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {dialog === 'entitlement' && overview ? (
        <EntitlementDialog
          employeeId={employeeId}
          types={overview.leaveTypes}
          onClose={() => setDialog('none')}
          onSaved={applyOverview}
        />
      ) : null}

      {dialog === 'record' && overview ? (
        <AddLeaveDialog
          employeeId={employeeId}
          types={overview.leaveTypes}
          onClose={() => setDialog('none')}
          onSaved={applyOverview}
        />
      ) : null}

      {dialog === 'cancel' && cancelRecordId ? (
        <CancelLeaveDialog
          employeeId={employeeId}
          recordId={cancelRecordId}
          onClose={() => {
            setDialog('none')
            setCancelRecordId(null)
          }}
          onSaved={applyOverview}
        />
      ) : null}
    </div>
  )
}

function leaveTypeLabel(
  type: Pick<LeaveTypeRecord, 'name' | 'systemKind'> | undefined,
  t: (key: string) => string,
) {
  if (!type) {
    return '—'
  }

  if (type.systemKind) {
    return t(`personnel.leave.kinds.${type.systemKind}`)
  }

  return type.name
}

function EntitlementDialog({
  employeeId,
  types,
  onClose,
  onSaved,
}: {
  employeeId: string
  types: LeaveTypeRecord[]
  onClose: () => void
  onSaved: (overview: EmployeeLeaveOverview) => void
}) {
  const { t } = useTranslation()
  const focusRef = useRef<HTMLButtonElement>(null)
  const selectable = useMemo(
    () => types.filter((item) => item.isActive && item.tracksBalance),
    [types],
  )
  const [leaveTypeId, setLeaveTypeId] = useState(selectable[0]?.id ?? '')
  const [effectiveDate, setEffectiveDate] = useState(todayIsoDate())
  const [source, setSource] = useState<LeaveEntitlementSource>('Entitlement')
  const [amount, setAmount] = useState('14')
  const [note, setNote] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  async function submit() {
    setError(null)
    const isoDate = toIsoDate(effectiveDate)
    const parsed = parseLeaveAmount(amount)
    if (!leaveTypeId) {
      setError(t('personnel.leave.errors.typeRequired'))
      return
    }

    if (!isoDate) {
      setError(t('personnel.leave.errors.invalidDate'))
      return
    }

    if (source === 'ManualAdjustment') {
      if (!isNonZeroHalfDayAmount(amount) || parsed === null) {
        setError(t('personnel.leave.errors.adjustmentAmount'))
        return
      }

      if (note.trim() === '') {
        setError(t('personnel.leave.errors.noteRequired'))
        return
      }
    } else if (!isPositiveHalfDayAmount(amount) || parsed === null) {
      setError(t('personnel.leave.errors.positiveAmount'))
      return
    }

    setSaving(true)
    try {
      const next = await createHrLeaveEntitlement(employeeId, {
        leaveTypeId,
        effectiveDate: isoDate,
        amount: parsed,
        source,
        note: note.trim() === '' ? null : note.trim(),
      })
      onSaved(next)
    } catch (reason) {
      setError(t(hrLeaveErrorKey(reason)))
    } finally {
      setSaving(false)
    }
  }

  return (
    <WorkspaceDialog
      title={t('personnel.leave.addEntitlement')}
      size="compact"
      stacked
      onRequestClose={onClose}
      initialFocusRef={focusRef}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>
            {t('personnel.cancel')}
          </Button>
          <Button ref={focusRef} variant="primary" layout="inline" disabled={saving} onClick={() => void submit()}>
            {saving ? t('personnel.saving') : t('personnel.save')}
          </Button>
        </>
      }
    >
      <div className={styles.leaveForm}>
        {error ? <Notice tone="danger">{error}</Notice> : null}
        {selectable.length === 0 ? (
          <Notice tone="info">{t('personnel.leave.noBalanceTypes')}</Notice>
        ) : (
          <SelectField
            id="leave-entitlement-type"
            label={t('personnel.leave.type')}
            value={leaveTypeId}
            onChange={setLeaveTypeId}
            required
          >
            {selectable.map((item) => (
              <option key={item.id} value={item.id}>
                {leaveTypeLabel(item, t)}
              </option>
            ))}
          </SelectField>
        )}
        <DateField
          id="leave-entitlement-date"
          label={t('personnel.leave.date')}
          value={effectiveDate}
          onChange={setEffectiveDate}
          required
        />
        <SelectField
          id="leave-entitlement-source"
          label={t('personnel.leave.source')}
          value={source}
          onChange={(value) => setSource(value as LeaveEntitlementSource)}
          required
        >
          <option value="Entitlement">{t('personnel.leave.sources.Entitlement')}</option>
          <option value="CarryOver">{t('personnel.leave.sources.CarryOver')}</option>
          <option value="ManualAdjustment">{t('personnel.leave.sources.ManualAdjustment')}</option>
        </SelectField>
        <TextField
          id="leave-entitlement-amount"
          label={t('personnel.leave.amount')}
          value={amount}
          onChange={setAmount}
          hint={
            source === 'ManualAdjustment'
              ? t('personnel.leave.adjustmentAmountHint')
              : t('personnel.leave.positiveAmountHint')
          }
          required
        />
        <TextArea
          id="leave-entitlement-note"
          label={t('personnel.leave.note')}
          value={note}
          onChange={setNote}
          required={source === 'ManualAdjustment'}
        />
      </div>
    </WorkspaceDialog>
  )
}

function AddLeaveDialog({
  employeeId,
  types,
  onClose,
  onSaved,
}: {
  employeeId: string
  types: LeaveTypeRecord[]
  onClose: () => void
  onSaved: (overview: EmployeeLeaveOverview) => void
}) {
  const { t } = useTranslation()
  const focusRef = useRef<HTMLButtonElement>(null)
  const selectable = useMemo(() => orderActiveLeaveTypes(types), [types])
  const [leaveTypeId, setLeaveTypeId] = useState(selectable[0]?.id ?? '')
  const [startDate, setStartDate] = useState(todayIsoDate())
  const [endDate, setEndDate] = useState(todayIsoDate())
  const [amount, setAmount] = useState('1')
  const [amountTouched, setAmountTouched] = useState(false)
  const [note, setNote] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  function refreshSuggestion(nextStart: string, nextEnd: string) {
    setAmount((current) => amountAfterDateChange(amountTouched, nextStart, nextEnd, current))
  }

  async function submit() {
    setError(null)
    const start = toIsoDate(startDate)
    const end = toIsoDate(endDate)
    const parsed = parseLeaveAmount(amount)
    if (!leaveTypeId) {
      setError(t('personnel.leave.errors.typeRequired'))
      return
    }

    if (!start || !end) {
      setError(t('personnel.leave.errors.invalidDate'))
      return
    }

    if (!isStartOnOrBeforeEnd(startDate, endDate)) {
      setError(t('personnel.leave.errors.invalidDateRange'))
      return
    }

    if (!isPositiveHalfDayAmount(amount) || parsed === null) {
      setError(t('personnel.leave.errors.invalidAmount'))
      return
    }

    setSaving(true)
    try {
      const next = await createHrLeaveRecord(employeeId, {
        leaveTypeId,
        startDate: start,
        endDate: end,
        amount: parsed,
        note: note.trim() === '' ? null : note.trim(),
      })
      onSaved(next)
    } catch (reason) {
      setError(t(hrLeaveErrorKey(reason)))
    } finally {
      setSaving(false)
    }
  }

  return (
    <WorkspaceDialog
      title={t('personnel.leave.addLeave')}
      size="compact"
      stacked
      onRequestClose={onClose}
      initialFocusRef={focusRef}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>
            {t('personnel.cancel')}
          </Button>
          <Button ref={focusRef} variant="primary" layout="inline" disabled={saving} onClick={() => void submit()}>
            {saving ? t('personnel.saving') : t('personnel.save')}
          </Button>
        </>
      }
    >
      <div className={styles.leaveForm}>
        {error ? <Notice tone="danger">{error}</Notice> : null}
        <SelectField
          id="leave-record-type"
          label={t('personnel.leave.type')}
          value={leaveTypeId}
          onChange={setLeaveTypeId}
          required
        >
          {selectable.map((item) => (
            <option key={item.id} value={item.id}>
              {leaveTypeLabel(item, t)}
            </option>
          ))}
        </SelectField>
        <div className={styles.grid2}>
          <DateField
            id="leave-record-start"
            label={t('personnel.leave.startDate')}
            value={startDate}
            onChange={(value) => {
              const nextEnd = endDateAfterStartChange(value, endDate)
              setStartDate(value)
              setEndDate(nextEnd)
              refreshSuggestion(value, nextEnd)
            }}
            calendar
            required
          />
          <DateField
            id="leave-record-end"
            label={t('personnel.leave.endDate')}
            value={endDate}
            onChange={(value) => {
              setEndDate(value)
              refreshSuggestion(startDate, value)
            }}
            calendar
            minDate={endMinDate(startDate)}
            required
          />
        </div>
        <TextField
          id="leave-record-amount"
          label={t('personnel.leave.amount')}
          value={amount}
          onChange={(value) => {
            setAmountTouched(true)
            setAmount(value)
          }}
          hint={t('personnel.leave.amountHint')}
          required
        />
        <TextArea id="leave-record-note" label={t('personnel.leave.note')} value={note} onChange={setNote} />
      </div>
    </WorkspaceDialog>
  )
}

function CancelLeaveDialog({
  employeeId,
  recordId,
  onClose,
  onSaved,
}: {
  employeeId: string
  recordId: string
  onClose: () => void
  onSaved: (overview: EmployeeLeaveOverview) => void
}) {
  const { t } = useTranslation()
  const focusRef = useRef<HTMLButtonElement>(null)
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  async function submit() {
    setError(null)
    if (reason.trim() === '') {
      setError(t('personnel.leave.errors.reasonRequired'))
      return
    }

    setSaving(true)
    try {
      const next = await cancelHrLeaveRecord(employeeId, recordId, reason.trim())
      onSaved(next)
    } catch (caught) {
      setError(t(hrLeaveErrorKey(caught)))
    } finally {
      setSaving(false)
    }
  }

  return (
    <WorkspaceDialog
      title={t('personnel.leave.cancelLeave')}
      size="confirm"
      stacked
      onRequestClose={onClose}
      initialFocusRef={focusRef}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>
            {t('personnel.cancel')}
          </Button>
          <Button ref={focusRef} variant="danger" layout="inline" disabled={saving} onClick={() => void submit()}>
            {saving ? t('personnel.saving') : t('personnel.leave.confirmCancel')}
          </Button>
        </>
      }
    >
      <div className={styles.leaveForm}>
        {error ? <Notice tone="danger">{error}</Notice> : null}
        <TextArea
          id="leave-cancel-reason"
          label={t('personnel.leave.cancellationReason')}
          value={reason}
          onChange={setReason}
          required
        />
      </div>
    </WorkspaceDialog>
  )
}
