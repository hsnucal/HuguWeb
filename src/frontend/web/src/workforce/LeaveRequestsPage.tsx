import { useEffect, useId, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { formatDateOnly, formatDateTime } from '../i18n/format'
import { toAppLanguage } from '../i18n/languages'
import { Button } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { Notice } from '../ui/Notice'
import { SelectField } from '../ui/SelectField'
import { Skeleton } from '../ui/Skeleton'
import { StatusBadge } from '../ui/StatusBadge'
import { TextArea, TextField } from '../ui/TextField'
import { WorkspaceDialog } from '../ui/WorkspaceDialog'
import styles from './Workforce.module.css'
import { canApproveHrLeave, canManageHrLeave, canReadHrLeave } from './hrAccess'
import {
  cancelApprovedLeaveRequest,
  departmentApproveLeaveRequest,
  getHrLeaveRequest,
  hrApproveLeaveRequest,
  hrLeaveErrorKey,
  listHrLeaveRequests,
  listHrLeaveTypes,
  rejectLeaveRequest,
  type LeaveRequestDetail,
  type LeaveRequestListItem,
  type LeaveTypeRecord,
} from './hrLeaveApi'
import { formatLeaveAmount, isPositiveHalfDayAmount, parseLeaveAmount } from './leaveAmount'
import {
  defaultHrFinalAmount,
  departmentPrimaryActionLabelKey,
  hrPrimaryActionLabelKey,
  managementActionsForRequest,
  projectedBalanceAfterFinal,
  stageFilterValue,
  tabStatus,
} from './leaveRequestActions'
import {
  countScheduleStates,
  formatLeaveDateRange,
  leaveDecisionLabelKey,
  leaveRequestStatusLabelKey,
  leaveRequestStatusTone,
  leaveScheduleStateLabelKey,
} from './leaveRequestStatus'
import { listDepartments, type DepartmentRecord } from './workforceApi'

type StatusTab = 'pending' | 'approved' | 'rejected' | 'cancelled'
type StageChip = 'all' | 'department' | 'hr'
type InlineMode = 'none' | 'reject' | 'cancel'

const PAGE_SIZE = 20

export function LeaveRequestsPage() {
  const { t, i18n } = useTranslation()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? 'tr'
  const { user } = useAuthSession()
  const canRead = canReadHrLeave(user)
  const canApprove = canApproveHrLeave(user)
  const canManage = canManageHrLeave(user)

  const [tab, setTab] = useState<StatusTab>('pending')
  const [stageChip, setStageChip] = useState<StageChip>('all')
  const [departmentId, setDepartmentId] = useState('')
  const [leaveTypeId, setLeaveTypeId] = useState('')
  const [search, setSearch] = useState('')
  const [searchApplied, setSearchApplied] = useState('')
  const [page, setPage] = useState(1)
  const [items, setItems] = useState<LeaveRequestListItem[] | null>(null)
  const [totalCount, setTotalCount] = useState(0)
  const [departments, setDepartments] = useState<DepartmentRecord[]>([])
  const [leaveTypes, setLeaveTypes] = useState<LeaveTypeRecord[]>([])
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [detail, setDetail] = useState<LeaveRequestDetail | null>(null)
  const [detailLoading, setDetailLoading] = useState(false)
  const [inlineMode, setInlineMode] = useState<InlineMode>('none')
  const [note, setNote] = useState('')
  const [finalAmount, setFinalAmount] = useState('')
  const [scheduleExpanded, setScheduleExpanded] = useState(false)
  const [busy, setBusy] = useState(false)
  const [reloadToken, setReloadToken] = useState(0)
  const focusRef = useRef<HTMLButtonElement>(null)
  const rejectFocusRef = useRef<HTMLTextAreaElement>(null)
  const searchId = useId()

  useEffect(() => {
    let cancelled = false
    async function loadLookups() {
      try {
        const [deps, types] = await Promise.all([listDepartments(), listHrLeaveTypes(true)])
        if (cancelled) {
          return
        }
        setDepartments(deps)
        setLeaveTypes(types)
        if (deps.length === 1) {
          setDepartmentId(deps[0]!.id)
        }
      } catch {
        /* list still works; filters may be empty */
      }
    }
    void loadLookups()
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!canRead) {
      return
    }
    let cancelled = false
    async function load() {
      setError(null)
      try {
        const data = await listHrLeaveRequests({
          status: tabStatus(tab),
          approvalStage: tab === 'pending' ? stageFilterValue(stageChip) : undefined,
          departmentId: departmentId || undefined,
          leaveTypeId: leaveTypeId || undefined,
          search: searchApplied || undefined,
          page,
          pageSize: PAGE_SIZE,
        })
        if (!cancelled) {
          setItems(data.items)
          setTotalCount(data.totalCount)
        }
      } catch (reason) {
        if (!cancelled) {
          setError(t(hrLeaveErrorKey(reason)))
          setItems([])
          setTotalCount(0)
        }
      }
    }
    void load()
    return () => {
      cancelled = true
    }
  }, [canRead, tab, stageChip, departmentId, leaveTypeId, searchApplied, page, reloadToken, t])

  useEffect(() => {
    if (inlineMode === 'reject') {
      rejectFocusRef.current?.focus()
    }
  }, [inlineMode])

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))
  const singleDepartment = departments.length === 1

  const applyDetail = (request: LeaveRequestDetail) => {
    setDetail(request)
    setInlineMode('none')
    setNote('')
    setScheduleExpanded(false)
    setFinalAmount(
      request.status === 'Pending' && request.approvalStage === 'Hr'
        ? defaultHrFinalAmount(request.requestedAmount)
        : '',
    )
  }

  const openDetail = async (id: string) => {
    setDetailLoading(true)
    setError(null)
    try {
      applyDetail(await getHrLeaveRequest(id))
    } catch (reason) {
      setError(t(hrLeaveErrorKey(reason)))
      setDetail(null)
    } finally {
      setDetailLoading(false)
    }
  }

  const closeDetail = () => {
    setDetail(null)
    setInlineMode('none')
    setNote('')
    setFinalAmount('')
    setScheduleExpanded(false)
  }

  const refreshAfterAction = async (messageKey: string, nextDetail?: LeaveRequestDetail) => {
    setSuccess(t(messageKey))
    setInlineMode('none')
    setNote('')
    setReloadToken((value) => value + 1)
    if (nextDetail) {
      applyDetail(nextDetail)
    } else if (detail) {
      try {
        applyDetail(await getHrLeaveRequest(detail.id))
      } catch {
        setDetail(null)
      }
    }
  }

  const runDepartmentApprove = async () => {
    if (!detail) {
      return
    }
    setBusy(true)
    setError(null)
    try {
      const result = await departmentApproveLeaveRequest(detail.id, note || null)
      await refreshAfterAction('personnel.leave.successDepartmentApproved', result.request)
    } catch (reason) {
      setError(t(hrLeaveErrorKey(reason)))
    } finally {
      setBusy(false)
    }
  }

  const runHrApprove = async () => {
    if (!detail || !isPositiveHalfDayAmount(finalAmount)) {
      return
    }
    const amount = parseLeaveAmount(finalAmount)
    if (amount === null) {
      return
    }
    setBusy(true)
    setError(null)
    try {
      const result = await hrApproveLeaveRequest(detail.id, amount, note || null)
      await refreshAfterAction('personnel.leave.successHrApproved', result.request)
    } catch (reason) {
      setError(t(hrLeaveErrorKey(reason)))
    } finally {
      setBusy(false)
    }
  }

  const runReject = async () => {
    if (!detail) {
      return
    }
    setBusy(true)
    setError(null)
    try {
      const result = await rejectLeaveRequest(detail.id, note || null)
      await refreshAfterAction('personnel.leave.successRejected', result.request)
    } catch (reason) {
      setError(t(hrLeaveErrorKey(reason)))
    } finally {
      setBusy(false)
    }
  }

  const runCancelApproved = async () => {
    if (!detail || !note.trim()) {
      return
    }
    setBusy(true)
    setError(null)
    try {
      const result = await cancelApprovedLeaveRequest(detail.id, note.trim())
      await refreshAfterAction('personnel.leave.successCancelled', result.request)
    } catch (reason) {
      setError(t(hrLeaveErrorKey(reason)))
    } finally {
      setBusy(false)
    }
  }

  const scheduleCounts = useMemo(
    () => (detail ? countScheduleStates(detail.scheduleDays) : null),
    [detail],
  )

  const parsedFinal = parseLeaveAmount(finalAmount)
  const hrProjected = detail?.tracksBalance
    ? projectedBalanceAfterFinal(detail.balance?.currentBalance, parsedFinal)
    : null

  if (!canRead) {
    return <Notice tone="danger">{t('workforce.noAccess')}</Notice>
  }

  const detailActions = detail
    ? managementActionsForRequest(detail, { canApprove, canManage })
    : null

  const detailMeta = detail
    ? [
        detail.personnelNumber,
        detail.departmentName,
        detail.positionName || null,
      ]
        .filter(Boolean)
        .join(' · ')
    : ''

  return (
    <div className={styles.page}>
      <p className={styles.muted}>{t('workforce.leaveManagementIntro')}</p>

      {error ? <Notice tone="danger">{error}</Notice> : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      <div className={styles.segments} role="tablist" aria-label={t('workforce.leaveManagement')}>
        {(
          [
            ['pending', 'personnel.leave.tabPending'],
            ['approved', 'personnel.leave.tabApproved'],
            ['rejected', 'personnel.leave.tabRejected'],
            ['cancelled', 'personnel.leave.tabCancelled'],
          ] as const
        ).map(([id, key]) => (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={tab === id}
            className={tab === id ? styles.segmentCurrent : styles.segment}
            onClick={() => {
              setTab(id)
              setPage(1)
              setStageChip('all')
            }}
          >
            {t(key)}
          </button>
        ))}
      </div>

      {tab === 'pending' ? (
        <div className={styles.stageChips} role="group" aria-label={t('personnel.leave.stageAll')}>
          {(
            [
              ['all', 'personnel.leave.stageAll'],
              ['department', 'personnel.leave.stageDepartment'],
              ['hr', 'personnel.leave.stageHr'],
            ] as const
          ).map(([id, key]) => (
            <button
              key={id}
              type="button"
              className={stageChip === id ? styles.stageChipCurrent : styles.stageChip}
              onClick={() => {
                setStageChip(id)
                setPage(1)
              }}
            >
              {t(key)}
            </button>
          ))}
        </div>
      ) : null}

      <div className={styles.leaveMgmtFilters}>
        <TextField
          id={searchId}
          label={t('personnel.leave.searchPlaceholder')}
          value={search}
          onChange={setSearch}
          onKeyDown={(event) => {
            if (event.key === 'Enter') {
              setSearchApplied(search.trim())
              setPage(1)
            }
          }}
        />
        <SelectField
          id="leave-mgmt-department"
          label={t('personnel.leave.department')}
          value={departmentId}
          onChange={(value) => {
            setDepartmentId(value)
            setPage(1)
          }}
          disabled={singleDepartment}
        >
          {!singleDepartment ? <option value="">{t('workforce.allDepartments')}</option> : null}
          {departments.map((department) => (
            <option key={department.id} value={department.id}>
              {department.name}
            </option>
          ))}
        </SelectField>
        <SelectField
          id="leave-mgmt-type"
          label={t('personnel.leave.type')}
          value={leaveTypeId}
          onChange={(value) => {
            setLeaveTypeId(value)
            setPage(1)
          }}
        >
          <option value="">{t('personnel.leave.allLeaveTypes')}</option>
          {leaveTypes.map((type) => (
            <option key={type.id} value={type.id}>
              {type.name}
            </option>
          ))}
        </SelectField>
        <div className={styles.hrFilterActions}>
          <Button
            type="button"
            variant="secondary"
            layout="inline"
            onClick={() => {
              setSearchApplied(search.trim())
              setPage(1)
            }}
          >
            {t('personnel.filters')}
          </Button>
        </div>
      </div>

      {items === null ? <Skeleton label={t('personnel.leave.loading')} /> : null}

      {items && items.length === 0 ? (
        <EmptyState
          title={t('personnel.leave.emptyRequests')}
          description={t('personnel.leave.emptyRequestsHint')}
        />
      ) : null}

      {items && items.length > 0 ? (
        <>
          <div className={`${styles.list} ${styles.leaveRequestList}`}>
            <div className={`${styles.row} ${styles.head} ${styles.leaveRequestRow}`}>
              <span>{t('personnel.leave.personnel')}</span>
              <span>{t('personnel.leave.department')}</span>
              <span>{t('personnel.leave.type')}</span>
              <span>{t('personnel.leave.date')}</span>
              <span>{t('personnel.leave.requestedAmountShort')}</span>
              <span>{t('personnel.leave.status')}</span>
              <span>{t('personnel.leave.createdAt')}</span>
              <span>{t('personnel.leave.actions')}</span>
            </div>
            {items.map((item) => {
              const statusLabel = t(leaveRequestStatusLabelKey(item.status, item.approvalStage))
              return (
                <div key={item.id} className={`${styles.row} ${styles.leaveRequestRow}`}>
                  <button type="button" className={styles.rowLink} onClick={() => void openDetail(item.id)}>
                    <strong>{item.displayName}</strong>
                    <span className={styles.muted}>{item.personnelNumber}</span>
                  </button>
                  <span>{item.departmentName}</span>
                  <span>{item.leaveTypeName}</span>
                  <span>{formatLeaveDateRange(item.startDate, item.endDate)}</span>
                  <span>
                    {formatLeaveAmount(item.requestedAmount)} {t('personnel.leave.dayUnit')}
                  </span>
                  <StatusBadge tone={leaveRequestStatusTone(item.status, item.approvalStage)}>
                    {statusLabel}
                  </StatusBadge>
                  <span>{formatDateTime(item.createdAtUtc, language)}</span>
                  <div className={styles.actions}>
                    <Button
                      type="button"
                      variant="secondary"
                      layout="inline"
                      size="sm"
                      onClick={() => void openDetail(item.id)}
                    >
                      {t('personnel.leave.review')}
                    </Button>
                  </div>
                </div>
              )
            })}
          </div>
          <div className={styles.actions}>
            <Button type="button" variant="secondary" layout="inline" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
              {t('personnel.leave.pagePrev')}
            </Button>
            <span className={styles.muted}>
              {t('personnel.leave.pageStatus', { page, total: totalCount })}
            </span>
            <Button
              type="button"
              variant="secondary"
              layout="inline"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              {t('personnel.leave.pageNext')}
            </Button>
          </div>
        </>
      ) : null}

      {detailLoading ? <Skeleton label={t('personnel.leave.loading')} /> : null}

      {detail && detailActions ? (
        <WorkspaceDialog
          title={detail.displayName}
          subtitle={detailMeta}
          size="compact"
          onRequestClose={closeDetail}
          initialFocusRef={focusRef}
          footer={
            inlineMode === 'reject' ? (
              <div className={styles.leaveDetailFooter}>
                <Button
                  type="button"
                  variant="secondary"
                  layout="inline"
                  disabled={busy}
                  onClick={() => {
                    setInlineMode('none')
                    setNote('')
                  }}
                >
                  {t('personnel.cancel')}
                </Button>
                <div className={styles.leaveDetailFooterActions}>
                  <Button type="button" variant="danger" layout="inline" disabled={busy} onClick={() => void runReject()}>
                    {t('personnel.leave.reject')}
                  </Button>
                </div>
              </div>
            ) : (
              <div className={styles.leaveDetailFooter}>
                <Button ref={focusRef} type="button" variant="secondary" layout="inline" onClick={closeDetail}>
                  {t('personnel.close')}
                </Button>
                <div className={styles.leaveDetailFooterActions}>
                  {detailActions.canReject ? (
                    <Button
                      type="button"
                      variant="secondary"
                      layout="inline"
                      disabled={busy}
                      onClick={() => {
                        setNote('')
                        setInlineMode('reject')
                      }}
                    >
                      {t('personnel.leave.reject')}
                    </Button>
                  ) : null}
                  {detailActions.canDepartmentApprove ? (
                    <Button
                      type="button"
                      layout="inline"
                      disabled={busy}
                      onClick={() => void runDepartmentApprove()}
                    >
                      {t(departmentPrimaryActionLabelKey())}
                    </Button>
                  ) : null}
                  {detailActions.canHrApprove ? (
                    <Button
                      type="button"
                      layout="inline"
                      disabled={busy || !isPositiveHalfDayAmount(finalAmount)}
                      onClick={() => void runHrApprove()}
                    >
                      {t(hrPrimaryActionLabelKey())}
                    </Button>
                  ) : null}
                  {detailActions.canCancelApproved ? (
                    <Button
                      type="button"
                      variant="secondary"
                      layout="inline"
                      disabled={busy}
                      onClick={() => {
                        setNote('')
                        setInlineMode('cancel')
                      }}
                    >
                      {t('personnel.leave.cancelApproved')}
                    </Button>
                  ) : null}
                </div>
              </div>
            )
          }
        >
          <div className={styles.leaveDetailStatusRow}>
            <StatusBadge tone={leaveRequestStatusTone(detail.status, detail.approvalStage)}>
              {t(leaveRequestStatusLabelKey(detail.status, detail.approvalStage))}
            </StatusBadge>
          </div>

          <section className={styles.leavePrimarySummary} aria-label={t('personnel.leave.primarySummary')}>
            <h3 className={styles.leavePrimaryTitle}>{detail.leaveTypeName}</h3>
            <p className={styles.leavePrimaryRange}>
              {formatDateOnly(detail.startDate, language)}
              {detail.startDate === detail.endDate
                ? null
                : ` – ${formatDateOnly(detail.endDate, language)}`}
            </p>
            <p className={styles.leavePrimaryAmount}>
              <span className={styles.muted}>{t('personnel.leave.requestedAmountLabel')}</span>
              <strong>
                {formatLeaveAmount(detail.requestedAmount)} {t('personnel.leave.dayUnit')}
              </strong>
            </p>
          </section>

          <section className={styles.leaveCompactSection} aria-label={t('personnel.leave.reason')}>
            <h4 className={styles.leaveSectionLabel}>{t('personnel.leave.reason')}</h4>
            <p className={detail.reason ? undefined : styles.muted}>
              {detail.reason?.trim() ? detail.reason : t('personnel.leave.reasonEmpty')}
            </p>
          </section>

          {detail.tracksBalance && detail.balance ? (
            <section className={styles.leaveCompactSection} aria-label={t('personnel.leave.balanceSummary')}>
              <div className={styles.leaveMetricGrid}>
                <div>
                  <span className={styles.leaveSectionLabel}>{t('personnel.leave.currentBalance')}</span>
                  <strong>
                    {formatLeaveAmount(detail.balance.currentBalance)} {t('personnel.leave.dayUnit')}
                  </strong>
                </div>
                <div>
                  <span className={styles.leaveSectionLabel}>{t('personnel.leave.projectedBalance')}</span>
                  <strong>
                    {formatLeaveAmount(detail.balance.projectedBalance)} {t('personnel.leave.dayUnit')}
                  </strong>
                </div>
              </div>
              {detail.balance.isNegativeProjected ? (
                <Notice tone="warning" className={styles.leaveCompactNotice}>
                  {t('personnel.leave.balanceOverrunWarningShort')}
                </Notice>
              ) : null}
            </section>
          ) : null}

          <section className={styles.leaveCompactSection} aria-label={t('personnel.leave.scheduleSummary')}>
            <h4 className={styles.leaveSectionLabel}>{t('personnel.leave.scheduleSummary')}</h4>
            {scheduleCounts ? (
              <div className={styles.leaveMetricGrid}>
                <div>
                  <span className={styles.leaveSectionLabel}>{t('personnel.leave.metricScheduled')}</span>
                  <strong>{scheduleCounts.scheduled}</strong>
                </div>
                <div>
                  <span className={styles.leaveSectionLabel}>{t('personnel.leave.metricRestDay')}</span>
                  <strong>{scheduleCounts.restDay}</strong>
                </div>
                <div>
                  <span className={styles.leaveSectionLabel}>{t('personnel.leave.metricUnscheduled')}</span>
                  <strong>{scheduleCounts.unscheduled}</strong>
                </div>
                <div>
                  <span className={styles.leaveSectionLabel}>{t('personnel.leave.suggestedAmount')}</span>
                  <strong>
                    {formatLeaveAmount(detail.suggestedAmount)} {t('personnel.leave.dayUnit')}
                  </strong>
                </div>
              </div>
            ) : null}
            {detail.scheduleIncomplete ? (
              <Notice tone="warning" className={styles.leaveCompactNotice}>
                {t('personnel.leave.scheduleIncompleteWarningShort')}
              </Notice>
            ) : null}
            <button
              type="button"
              className={styles.leaveExpandToggle}
              aria-expanded={scheduleExpanded}
              onClick={() => setScheduleExpanded((value) => !value)}
            >
              {scheduleExpanded
                ? t('personnel.leave.hideScheduleDetails')
                : t('personnel.leave.showScheduleDetails')}
            </button>
            {scheduleExpanded ? (
              <ul className={styles.scheduleDayListCompact}>
                {detail.scheduleDays.map((day) => (
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
            ) : null}
          </section>

          <section className={styles.leaveCompactSection} aria-label={t('personnel.leave.decisionHistory')}>
            <h4 className={styles.leaveSectionLabel}>{t('personnel.leave.decisionHistory')}</h4>
            <ol className={styles.decisionTimelineCompact}>
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

          {detailActions.canDepartmentApprove && inlineMode === 'none' ? (
            <section className={styles.leaveActionHint} aria-label={t(departmentPrimaryActionLabelKey())}>
              <Notice tone="info" className={styles.leaveCompactNotice}>
                {t('personnel.leave.departmentApproveHint')}
              </Notice>
              <TextArea
                id="dept-note-inline"
                label={t('personnel.leave.noteOptional')}
                value={note}
                onChange={setNote}
              />
            </section>
          ) : null}

          {detailActions.canHrApprove && inlineMode === 'none' ? (
            <section className={styles.leaveFinalApproval} aria-label={t('personnel.leave.finalApprovalSection')}>
              <h4 className={styles.leaveSectionLabel}>{t('personnel.leave.finalApprovalSection')}</h4>
              <Notice tone="info" className={styles.leaveCompactNotice}>
                {t('personnel.leave.hrApproveHint')}
              </Notice>
              <div className={styles.leaveMetricGrid}>
                <div>
                  <span className={styles.leaveSectionLabel}>{t('personnel.leave.requestedAmountLabel')}</span>
                  <strong>
                    {formatLeaveAmount(detail.requestedAmount)} {t('personnel.leave.dayUnit')}
                  </strong>
                </div>
                <div>
                  <span className={styles.leaveSectionLabel}>{t('personnel.leave.systemSuggestion')}</span>
                  <strong>
                    {formatLeaveAmount(detail.suggestedAmount)} {t('personnel.leave.dayUnit')}
                  </strong>
                </div>
                {detail.tracksBalance && detail.balance ? (
                  <>
                    <div>
                      <span className={styles.leaveSectionLabel}>{t('personnel.leave.currentBalance')}</span>
                      <strong>
                        {formatLeaveAmount(detail.balance.currentBalance)} {t('personnel.leave.dayUnit')}
                      </strong>
                    </div>
                    <div>
                      <span className={styles.leaveSectionLabel}>{t('personnel.leave.afterApprovalBalance')}</span>
                      <strong>
                        {hrProjected === null
                          ? '—'
                          : `${formatLeaveAmount(hrProjected)} ${t('personnel.leave.dayUnit')}`}
                      </strong>
                    </div>
                  </>
                ) : null}
              </div>
              {detail.scheduleIncomplete ? (
                <Notice tone="warning" className={styles.leaveCompactNotice}>
                  {t('personnel.leave.scheduleIncompleteWarningShort')}
                </Notice>
              ) : null}
              {hrProjected !== null && hrProjected < 0 ? (
                <Notice tone="warning" className={styles.leaveCompactNotice}>
                  {t('personnel.leave.balanceOverrunWarningShort')}
                </Notice>
              ) : null}
              <TextField
                id="hr-final-amount"
                label={t('personnel.leave.finalAmount')}
                value={finalAmount}
                onChange={setFinalAmount}
                required
                hint={t('personnel.leave.positiveAmountHint')}
                error={
                  finalAmount && !isPositiveHalfDayAmount(finalAmount)
                    ? t('personnel.leave.errors.invalidAmount')
                    : undefined
                }
              />
              <TextArea id="hr-note-inline" label={t('personnel.leave.noteOptional')} value={note} onChange={setNote} />
            </section>
          ) : null}

          {detail.status === 'Approved' ? (
            <section className={styles.leaveCompactSection} aria-label={t('personnel.leave.finalAmount')}>
              <div className={styles.leaveMetricGrid}>
                <div>
                  <span className={styles.leaveSectionLabel}>{t('personnel.leave.finalAmount')}</span>
                  <strong>
                    {formatLeaveAmount(detail.finalAmount ?? detail.requestedAmount)}{' '}
                    {t('personnel.leave.dayUnit')}
                  </strong>
                </div>
                {detail.linkedRecord ? (
                  <div>
                    <span className={styles.leaveSectionLabel}>{t('personnel.leave.linkedRecord')}</span>
                    <strong>
                      {formatLeaveAmount(detail.linkedRecord.amount)} {t('personnel.leave.dayUnit')} ·{' '}
                      {detail.linkedRecord.status}
                    </strong>
                  </div>
                ) : null}
              </div>
            </section>
          ) : null}

          {inlineMode === 'reject' ? (
            <section className={styles.leaveRejectPanel} aria-label={t('personnel.leave.reject')}>
              <TextArea
                ref={rejectFocusRef}
                id="reject-note-inline"
                label={t('personnel.leave.rejectNote')}
                value={note}
                onChange={setNote}
              />
            </section>
          ) : null}
        </WorkspaceDialog>
      ) : null}

      {inlineMode === 'cancel' && detail ? (
        <WorkspaceDialog
          title={t('personnel.leave.cancelApproved')}
          size="confirm"
          stacked
          onRequestClose={() => setInlineMode('none')}
          footer={
            <>
              <Button type="button" variant="secondary" layout="inline" disabled={busy} onClick={() => setInlineMode('none')}>
                {t('personnel.cancel')}
              </Button>
              <Button
                type="button"
                variant="danger"
                layout="inline"
                disabled={busy || !note.trim()}
                onClick={() => void runCancelApproved()}
              >
                {t('personnel.leave.cancelApproved')}
              </Button>
            </>
          }
        >
          <Notice tone="warning">{t('personnel.leave.cancelApprovedHint')}</Notice>
          <TextArea
            id="cancel-reason"
            label={t('personnel.leave.cancellationReason')}
            value={note}
            onChange={setNote}
            required
            error={!note.trim() ? t('personnel.leave.errors.reasonRequired') : undefined}
          />
        </WorkspaceDialog>
      ) : null}
    </div>
  )
}
