import { useEffect, useId, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { formatDateOnly, formatDateTime } from '../i18n/format'
import { toAppLanguage } from '../i18n/languages'
import { Button } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { Notice } from '../ui/Notice'
import { DateField, SelectField } from '../ui/SelectField'
import { Skeleton } from '../ui/Skeleton'
import { StatusBadge } from '../ui/StatusBadge'
import { TextArea, TextField } from '../ui/TextField'
import { WorkspaceDialog } from '../ui/WorkspaceDialog'
import { toIsoDate } from '../ui/dateEntry'
import styles from './Workforce.module.css'
import { canRequestHrLeave } from './hrAccess'
import {
  createMyLeaveRequest,
  getMyLeaveCatalog,
  getMyLeaveRequest,
  hrLeaveErrorKey,
  listMyLeaveRequests,
  previewMyLeaveRequest,
  withdrawMyLeaveRequest,
  type LeaveBalanceRecord,
  type LeaveRequestDetail,
  type LeaveRequestListItem,
  type LeaveRequestPreview,
  type LeaveTypeRecord,
} from './hrLeaveApi'
import {
  amountAfterLeaveTypeChange,
  amountAfterTypeOrPreview,
  leaveRequestDateFieldUsesCalendar,
  leaveRequestSubmitUsesInlineButtons,
} from './leaveRequestDefaults'
import { formatLeaveAmount, isPositiveHalfDayAmount, parseLeaveAmount } from './leaveAmount'
import { endDateAfterStartChange, endMinDate, isStartOnOrBeforeEnd } from './leaveDateRange'
import { selfServiceActionsForRequest } from './leaveRequestActions'
import {
  countScheduleStates,
  formatLeaveDateRange,
  leaveDecisionLabelKey,
  leaveRequestStatusLabelKey,
  leaveRequestStatusTone,
  leaveScheduleStateLabelKey,
} from './leaveRequestStatus'
import { orderActiveLeaveTypes } from './leaveTypeOrder'

export function MyLeavePage() {
  const { t, i18n } = useTranslation()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? 'tr'
  const { user } = useAuthSession()
  const canRequest = canRequestHrLeave(user)

  const [items, setItems] = useState<LeaveRequestListItem[] | null>(null)
  const [types, setTypes] = useState<LeaveTypeRecord[]>([])
  const [balances, setBalances] = useState<LeaveBalanceRecord[]>([])
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [createOpen, setCreateOpen] = useState(false)
  const [detail, setDetail] = useState<LeaveRequestDetail | null>(null)
  const [withdrawId, setWithdrawId] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [reloadToken, setReloadToken] = useState(0)

  const [leaveTypeId, setLeaveTypeId] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [requestedAmount, setRequestedAmount] = useState('')
  const [amountTouched, setAmountTouched] = useState(false)
  const [reason, setReason] = useState('')
  const [preview, setPreview] = useState<LeaveRequestPreview | null>(null)
  const [previewError, setPreviewError] = useState<string | null>(null)
  const focusRef = useRef<HTMLButtonElement>(null)
  const typeId = useId()

  useEffect(() => {
    if (!canRequest) {
      return
    }
    let cancelled = false
    async function load() {
      setError(null)
      try {
        const [page, catalog] = await Promise.all([listMyLeaveRequests(1, 50), getMyLeaveCatalog()])
        if (!cancelled) {
          setItems(page.items)
          setTypes(orderActiveLeaveTypes(catalog.leaveTypes))
          setBalances(catalog.balances)
        }
      } catch (reason) {
        if (!cancelled) {
          setError(t(hrLeaveErrorKey(reason)))
          setItems([])
          setTypes([])
          setBalances([])
        }
      }
    }
    void load()
    return () => {
      cancelled = true
    }
  }, [canRequest, reloadToken, t])

  useEffect(() => {
    if (!createOpen) {
      return
    }
    const startIso = toIsoDate(startDate)
    const endIso = toIsoDate(endDate)
    if (!startIso || !endIso || !isStartOnOrBeforeEnd(startDate, endDate)) {
      return
    }
    let cancelled = false
    const handle = window.setTimeout(() => {
      void (async () => {
        setPreviewError(null)
        try {
          const data = await previewMyLeaveRequest({
            leaveTypeId: leaveTypeId || null,
            startDate: startIso,
            endDate: endIso,
            requestedAmount: null,
          })
          if (cancelled) {
            return
          }
          setPreview(data)
          setRequestedAmount((current) =>
            amountAfterTypeOrPreview(
              amountTouched,
              types.find((type) => type.id === leaveTypeId)?.defaultRequestAmount,
              data.suggestedAmount,
              current,
            ),
          )
        } catch (reason) {
          if (!cancelled) {
            setPreview(null)
            setPreviewError(t(hrLeaveErrorKey(reason)))
          }
        }
      })()
    }, 250)
    return () => {
      cancelled = true
      window.clearTimeout(handle)
    }
  }, [createOpen, startDate, endDate, leaveTypeId, amountTouched, types, t])

  const previewVisible =
    Boolean(toIsoDate(startDate)) &&
    Boolean(toIsoDate(endDate)) &&
    isStartOnOrBeforeEnd(startDate, endDate)
      ? preview
      : null

  const selectedType = types.find((type) => type.id === leaveTypeId) ?? null

  const onLeaveTypeChange = (value: string) => {
    setLeaveTypeId(value)
    const nextType = types.find((type) => type.id === value) ?? null
    setRequestedAmount((current) =>
      amountAfterLeaveTypeChange(amountTouched, nextType?.defaultRequestAmount, current),
    )
  }

  const resetCreateForm = () => {
    setLeaveTypeId('')
    setStartDate('')
    setEndDate('')
    setRequestedAmount('')
    setAmountTouched(false)
    setReason('')
    setPreview(null)
    setPreviewError(null)
  }

  const openCreate = () => {
    resetCreateForm()
    setCreateOpen(true)
  }

  const openDetail = async (id: string) => {
    setError(null)
    try {
      setDetail(await getMyLeaveRequest(id))
    } catch (reason) {
      setError(t(hrLeaveErrorKey(reason)))
    }
  }

  const submitCreate = async () => {
    const startIso = toIsoDate(startDate)
    const endIso = toIsoDate(endDate)
    const amount = parseLeaveAmount(requestedAmount)
    if (!leaveTypeId || !startIso || !endIso || amount === null || !isPositiveHalfDayAmount(requestedAmount)) {
      return
    }
    setBusy(true)
    setError(null)
    try {
      await createMyLeaveRequest({
        leaveTypeId,
        startDate: startIso,
        endDate: endIso,
        requestedAmount: amount,
        reason: reason.trim() || null,
      })
      setCreateOpen(false)
      resetCreateForm()
      setSuccess(t('personnel.leave.createSuccess'))
      setReloadToken((value) => value + 1)
    } catch (reason) {
      setError(t(hrLeaveErrorKey(reason)))
    } finally {
      setBusy(false)
    }
  }

  const runWithdraw = async () => {
    if (!withdrawId) {
      return
    }
    setBusy(true)
    setError(null)
    try {
      await withdrawMyLeaveRequest(withdrawId)
      setWithdrawId(null)
      setDetail(null)
      setSuccess(t('personnel.leave.withdrawSuccess'))
      setReloadToken((value) => value + 1)
    } catch (reason) {
      setError(t(hrLeaveErrorKey(reason)))
    } finally {
      setBusy(false)
    }
  }

  if (!canRequest) {
    return <Notice tone="danger">{t('workforce.noAccess')}</Notice>
  }

  const scheduleCounts = previewVisible ? countScheduleStates(previewVisible.days) : null
  const projectedNegative = Boolean(previewVisible?.balance?.isNegativeProjected)

  return (
    <div className={styles.page}>
      <p className={styles.muted}>{t('personnel.leave.myLeaveIntro')}</p>

      {error ? <Notice tone="danger">{error}</Notice> : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      {balances.length > 0 ? (
        <section className={styles.panel} aria-label={t('personnel.leave.summaryTitle')}>
          <h2 className={styles.sectionTitle}>{t('personnel.leave.summaryTitle')}</h2>
          <ul className={styles.scheduleDayList}>
            {balances.map((row) => (
              <li key={row.leaveTypeId}>
                <strong>{row.name}</strong>
                <span>
                  {t('personnel.leave.remaining')}: {formatLeaveAmount(row.remaining)} {t('personnel.leave.dayUnit')}
                </span>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      <div className={styles.toolbar}>
        <Button type="button" onClick={openCreate}>
          {t('personnel.leave.newRequest')}
        </Button>
      </div>

      <h2 className={styles.sectionTitle}>{t('personnel.leave.myRequests')}</h2>

      {items === null ? <Skeleton label={t('personnel.leave.loading')} /> : null}
      {items && items.length === 0 ? (
        <EmptyState title={t('personnel.leave.emptyRequests')} description={t('personnel.leave.emptyRequestsHint')} />
      ) : null}

      {items && items.length > 0 ? (
        <div className={styles.list}>
          {items.map((item) => {
            const actions = selfServiceActionsForRequest(item)
            return (
              <div key={item.id} className={`${styles.row} ${styles.structureRow}`}>
                <div>
                  <strong>{item.leaveTypeName}</strong>
                  <div className={styles.muted}>{formatLeaveDateRange(item.startDate, item.endDate)}</div>
                </div>
                <div>
                  {formatLeaveAmount(item.requestedAmount)} {t('personnel.leave.dayUnit')}
                </div>
                <StatusBadge tone={leaveRequestStatusTone(item.status, item.approvalStage)}>
                  {t(leaveRequestStatusLabelKey(item.status, item.approvalStage))}
                </StatusBadge>
                <div className={styles.muted}>{formatDateTime(item.createdAtUtc, language)}</div>
                <div className={styles.actions}>
                  <Button type="button" variant="secondary" onClick={() => void openDetail(item.id)}>
                    {t('personnel.leave.review')}
                  </Button>
                  {actions.canWithdraw ? (
                    <Button type="button" variant="secondary" onClick={() => setWithdrawId(item.id)}>
                      {t('personnel.leave.withdraw')}
                    </Button>
                  ) : null}
                </div>
              </div>
            )
          })}
        </div>
      ) : null}

      {createOpen ? (
        <WorkspaceDialog
          title={t('personnel.leave.newRequest')}
          size="compact"
          onRequestClose={() => {
            setCreateOpen(false)
            resetCreateForm()
          }}
          initialFocusRef={focusRef}
          footer={
            <div className={styles.leaveDetailFooter}>
              <Button
                ref={focusRef}
                type="button"
                variant="secondary"
                layout="inline"
                disabled={busy}
                onClick={() => {
                  setCreateOpen(false)
                  resetCreateForm()
                }}
              >
                {t('personnel.cancel')}
              </Button>
              <div className={styles.leaveDetailFooterActions}>
                <Button
                  type="button"
                  layout={leaveRequestSubmitUsesInlineButtons() ? 'inline' : 'block'}
                  disabled={
                    busy ||
                    !leaveTypeId ||
                    !toIsoDate(startDate) ||
                    !toIsoDate(endDate) ||
                    !isPositiveHalfDayAmount(requestedAmount)
                  }
                  onClick={() => void submitCreate()}
                >
                  {t('personnel.leave.submitRequest')}
                </Button>
              </div>
            </div>
          }
        >
          <div className={styles.formGrid}>
            <SelectField
              id={typeId}
              label={t('personnel.leave.type')}
              value={leaveTypeId}
              onChange={onLeaveTypeChange}
              required
            >
              <option value="">{t('personnel.leave.errors.typeRequired')}</option>
              {types.map((type) => (
                <option key={type.id} value={type.id}>
                  {type.name}
                </option>
              ))}
            </SelectField>
            <DateField
              id="my-leave-start"
              label={t('personnel.leave.startDate')}
              value={startDate}
              calendar={leaveRequestDateFieldUsesCalendar()}
              onChange={(value) => {
                setStartDate(value)
                setEndDate((current) => endDateAfterStartChange(value, current))
              }}
              required
            />
            <DateField
              id="my-leave-end"
              label={t('personnel.leave.endDate')}
              value={endDate}
              calendar={leaveRequestDateFieldUsesCalendar()}
              minDate={endMinDate(startDate)}
              onChange={setEndDate}
              required
            />
            <TextField
              id="my-leave-amount"
              label={t('personnel.leave.requestedAmount')}
              value={requestedAmount}
              onChange={(value) => {
                setAmountTouched(true)
                setRequestedAmount(value)
              }}
              required
              hint={
                selectedType?.defaultRequestAmount != null
                  ? t('personnel.leave.defaultRequestAmountHint', {
                      amount: formatLeaveAmount(selectedType.defaultRequestAmount),
                    })
                  : t('personnel.leave.positiveAmountHint')
              }
              error={
                requestedAmount && !isPositiveHalfDayAmount(requestedAmount)
                  ? t('personnel.leave.errors.invalidAmount')
                  : undefined
              }
            />
            <TextArea id="my-leave-reason" label={t('personnel.leave.reason')} value={reason} onChange={setReason} />
          </div>

          {previewError ? <Notice tone="warning">{previewError}</Notice> : null}

          {previewVisible ? (
            <section className={styles.panel} aria-label={t('personnel.leave.previewTitle')}>
              <h3 className={styles.sectionTitle}>{t('personnel.leave.previewTitle')}</h3>
              <p>
                {t('personnel.leave.requestedAmount')}: {formatLeaveAmount(parseLeaveAmount(requestedAmount) ?? 0)}{' '}
                {t('personnel.leave.dayUnit')}
              </p>
              <p>
                {t('personnel.leave.suggestedAmount')}: {formatLeaveAmount(previewVisible.suggestedAmount)}{' '}
                {t('personnel.leave.dayUnit')}
              </p>
              {scheduleCounts ? (
                <p className={styles.muted}>
                  {t('personnel.leave.scheduledCount', { count: scheduleCounts.scheduled })} ·{' '}
                  {t('personnel.leave.restDayCount', { count: scheduleCounts.restDay })} ·{' '}
                  {t('personnel.leave.unscheduledCount', { count: scheduleCounts.unscheduled })}
                </p>
              ) : null}
              {previewVisible.scheduleIncomplete ? (
                <Notice tone="warning">{t('personnel.leave.scheduleIncompleteWarning')}</Notice>
              ) : null}
              {selectedType?.tracksBalance && previewVisible.balance ? (
                <>
                  <p>
                    {t('personnel.leave.currentBalance')}:{' '}
                    {formatLeaveAmount(previewVisible.balance.currentBalance)} {t('personnel.leave.dayUnit')}
                  </p>
                  <p>
                    {t('personnel.leave.projectedBalance')}:{' '}
                    {formatLeaveAmount(previewVisible.balance.projectedBalance)} {t('personnel.leave.dayUnit')}
                  </p>
                  {projectedNegative ? (
                    <Notice tone="warning">{t('personnel.leave.balanceOverrunWarning')}</Notice>
                  ) : null}
                </>
              ) : null}
              <details>
                <summary>{t('personnel.leave.scheduleSummary')}</summary>
                <ul className={styles.scheduleDayList}>
                  {previewVisible.days.map((day) => (
                    <li key={day.date}>
                      <strong>{formatDateOnly(day.date, language)}</strong>
                      <span>{t(leaveScheduleStateLabelKey(day.state))}</span>
                      <span className={styles.muted}>
                        {day.state === 'Unscheduled'
                          ? t('personnel.leave.scheduleChargeUnknown')
                          : t('personnel.leave.scheduleCharge', {
                              amount: formatLeaveAmount(day.chargeableCandidate),
                            })}
                      </span>
                    </li>
                  ))}
                </ul>
              </details>
            </section>
          ) : null}
        </WorkspaceDialog>
      ) : null}

      {detail ? (
        <WorkspaceDialog
          title={detail.leaveTypeName}
          subtitle={formatLeaveDateRange(detail.startDate, detail.endDate)}
          size="compact"
          onRequestClose={() => setDetail(null)}
          footer={
            <>
              <Button type="button" variant="secondary" onClick={() => setDetail(null)}>
                {t('personnel.close')}
              </Button>
              {selfServiceActionsForRequest(detail).canWithdraw ? (
                <Button type="button" variant="secondary" onClick={() => setWithdrawId(detail.id)}>
                  {t('personnel.leave.withdraw')}
                </Button>
              ) : null}
            </>
          }
        >
          <StatusBadge tone={leaveRequestStatusTone(detail.status, detail.approvalStage)}>
            {t(leaveRequestStatusLabelKey(detail.status, detail.approvalStage))}
          </StatusBadge>
          <p>
            {t('personnel.leave.requestedAmount')}: {formatLeaveAmount(detail.requestedAmount)}{' '}
            {t('personnel.leave.dayUnit')}
          </p>
          {detail.reason ? (
            <p>
              {t('personnel.leave.reason')}: {detail.reason}
            </p>
          ) : null}
          {detail.scheduleIncomplete ? (
            <Notice tone="warning">{t('personnel.leave.scheduleIncompleteWarning')}</Notice>
          ) : null}
          <section className={styles.panel}>
            <h3 className={styles.sectionTitle}>{t('personnel.leave.decisionHistory')}</h3>
            <ol className={styles.decisionTimeline}>
              <li>
                <strong>{t('personnel.leave.decisionCreated')}</strong>
                <span className={styles.muted}>{formatDateTime(detail.createdAtUtc, language)}</span>
              </li>
              {detail.decisions.map((decision) => (
                <li key={decision.id}>
                  <strong>{t(leaveDecisionLabelKey(decision))}</strong>
                  <span className={styles.muted}>{formatDateTime(decision.decisionAtUtc, language)}</span>
                  {decision.note ? <span>{decision.note}</span> : null}
                </li>
              ))}
            </ol>
          </section>
        </WorkspaceDialog>
      ) : null}

      {withdrawId ? (
        <WorkspaceDialog
          title={t('personnel.leave.withdraw')}
          size="confirm"
          stacked={Boolean(detail)}
          onRequestClose={() => setWithdrawId(null)}
          footer={
            <>
              <Button type="button" variant="secondary" disabled={busy} onClick={() => setWithdrawId(null)}>
                {t('personnel.cancel')}
              </Button>
              <Button type="button" disabled={busy} onClick={() => void runWithdraw()}>
                {t('personnel.leave.withdraw')}
              </Button>
            </>
          }
        >
          <p>{t('personnel.leave.withdrawConfirm')}</p>
        </WorkspaceDialog>
      ) : null}
    </div>
  )
}
