import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { formatDateOnly, formatDateTime } from '../i18n/format'
import { DEFAULT_LANGUAGE, toAppLanguage } from '../i18n/languages'
import { Button } from '../ui/Button'
import { Notice } from '../ui/Notice'
import { SelectField } from '../ui/SelectField'
import { TextArea } from '../ui/TextField'
import { WorkspaceDialog } from '../ui/WorkspaceDialog'
import {
  attendanceKindLabelKey,
  attendanceProvenanceStatus,
  attendanceScheduleStateLabelKey,
  attendanceSourceLabelKey,
  formatAttendanceClockRange,
  reverseChronological,
  scheduleClockRange,
} from './attendanceCellDisplay'
import {
  ATTENDANCE_REASON_MAX_LENGTH,
  canClearAttendanceCorrection,
  canShowAttendanceCorrectionForm,
  resolveAttendanceCorrectionEmploymentId,
  shouldShowPastMonthWarning,
  validateAttendanceReason,
  type YearMonth,
} from './attendanceMonth'
import {
  ATTENDANCE_CORRECTION_KINDS,
  clearHrAttendanceCorrection,
  getHrAttendanceHistory,
  hrAttendanceErrorMessage,
  isAttendanceCorrectionKind,
  setHrAttendanceCorrection,
  type AttendanceCorrectionHistoryItem,
  type AttendanceDayResult,
  type AttendanceMonthEmployee,
} from './hrAttendanceApi'
import { attendanceLeaveDetailLabel } from './leaveDisplay'
import styles from './AttendancePage.module.css'

export function AttendanceDayPanel({
  employee,
  day,
  canManage,
  selectedMonth,
  currentMonth,
  onClose,
  onMutated,
}: {
  employee: AttendanceMonthEmployee
  day: AttendanceDayResult
  canManage: boolean
  selectedMonth: YearMonth
  currentMonth: YearMonth
  onClose: () => void
  onMutated: () => Promise<void>
}) {
  const { t, i18n } = useTranslation()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const employeeName = `${employee.givenName} ${employee.familyName}`.trim()
  const formVisible = canShowAttendanceCorrectionForm(canManage, day.coverage)
  const clearVisible = canClearAttendanceCorrection(canManage, day)
  const pastWarning = shouldShowPastMonthWarning({
    canManage,
    formVisible,
    selected: selectedMonth,
    current: currentMonth,
  })

  const [kind, setKind] = useState(() =>
    day.isManual && isAttendanceCorrectionKind(day.acceptedKind) ? day.acceptedKind : '',
  )
  const [reason, setReason] = useState(() => (day.isManual ? day.correctionReason ?? '' : ''))
  const [formError, setFormError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [confirmClear, setConfirmClear] = useState(false)
  const [history, setHistory] = useState<AttendanceCorrectionHistoryItem[] | null>(null)
  const [historyError, setHistoryError] = useState<string | null>(null)
  const employmentId = resolveAttendanceCorrectionEmploymentId(day, employee)
  const localDate = day.localDate

  useEffect(() => {
    if (!employmentId) {
      return
    }

    let cancelled = false
    void getHrAttendanceHistory(employmentId, localDate)
      .then((payload) => {
        if (!cancelled) {
          setHistory(reverseChronological(payload.changes))
        }
      })
      .catch((error: unknown) => {
        if (!cancelled) {
          setHistory([])
          setHistoryError(hrAttendanceErrorMessage(error, t))
        }
      })

    return () => {
      cancelled = true
    }
  }, [employmentId, localDate, t])

  const acceptedKey = attendanceKindLabelKey(day.acceptedKind)
  const sourceKey = attendanceSourceLabelKey(day.source)
  const status = attendanceProvenanceStatus(day)
  const planStateKey = attendanceScheduleStateLabelKey(day.schedule?.state)
  const plannedRange = scheduleClockRange(day.schedule, formatAttendanceClockRange)

  async function onSave() {
    const employmentId = resolveAttendanceCorrectionEmploymentId(day, employee)
    if (!formVisible) {
      return
    }
    if (!employmentId) {
      setFormError(t('attendance.errors.missingEmployment'))
      return
    }

    const reasonProblem = validateAttendanceReason(reason)
    if (reasonProblem === 'required') {
      setFormError(t('attendance.errors.reasonRequired'))
      return
    }
    if (reasonProblem === 'tooLong') {
      setFormError(t('attendance.errors.reasonTooLong'))
      return
    }
    if (!isAttendanceCorrectionKind(kind)) {
      setFormError(t('attendance.errors.kindInvalid'))
      return
    }

    setBusy(true)
    setFormError(null)
    setSuccess(null)
    try {
      await setHrAttendanceCorrection(employmentId, day.localDate, {
        kind,
        reason: reason.trim(),
      })
      await onMutated()
      const payload = await getHrAttendanceHistory(employmentId, day.localDate)
      setHistory(reverseChronological(payload.changes))
      setSuccess(t('attendance.successSaved'))
    } catch (error) {
      setFormError(hrAttendanceErrorMessage(error, t))
    } finally {
      setBusy(false)
    }
  }

  async function onClear() {
    const employmentId = resolveAttendanceCorrectionEmploymentId(day, employee)
    if (!clearVisible) {
      return
    }
    if (!employmentId) {
      setFormError(t('attendance.errors.missingEmployment'))
      return
    }

    setBusy(true)
    setFormError(null)
    setSuccess(null)
    try {
      await clearHrAttendanceCorrection(employmentId, day.localDate)
      setConfirmClear(false)
      await onMutated()
      const payload = await getHrAttendanceHistory(employmentId, day.localDate)
      setHistory(reverseChronological(payload.changes))
      setKind('')
      setReason('')
      setSuccess(t('attendance.successCleared'))
    } catch (error) {
      setConfirmClear(false)
      setFormError(hrAttendanceErrorMessage(error, t))
    } finally {
      setBusy(false)
    }
  }

  return (
    <aside className={styles.drawer} data-attendance-drawer="overlay" aria-label={t('attendance.manualCorrection')}>
      <div className={styles.drawerHeader}>
        <div>
          <h2 className={styles.drawerTitle}>{employeeName}</h2>
          <p className={styles.drawerMeta}>{formatDateOnly(day.localDate, language)}</p>
        </div>
        <Button variant="ghost" size="sm" layout="inline" onClick={onClose}>
          {t('attendance.closePanel')}
        </Button>
      </div>

      <div className={styles.drawerBody}>
      {success ? <Notice tone="success">{success}</Notice> : null}
      {formError ? <Notice tone="danger">{formError}</Notice> : null}

      <section className={styles.section}>
        <h3 className={styles.sectionTitle}>{t('attendance.acceptedResult')}</h3>
        <dl className={styles.dl}>
          <dt>{t('attendance.employee')}</dt>
          <dd>{employeeName}</dd>
          <dt>{t('attendance.date')}</dt>
          <dd>{formatDateOnly(day.localDate, language)}</dd>
          {day.departmentName ? (
            <>
              <dt>{t('attendance.departmentField')}</dt>
              <dd>{day.departmentName}</dd>
            </>
          ) : null}
          <dt>{t('attendance.acceptedResult')}</dt>
          <dd>{acceptedKey ? t(acceptedKey) : t('attendance.cellUnresolved')}</dd>
          {sourceKey ? (
            <>
              <dt>{t('attendance.sourceLabel')}</dt>
              <dd>{t(sourceKey)}</dd>
            </>
          ) : null}
          {status === 'fromPlan' ? (
            <>
              <dt>{t('attendance.statusLabel')}</dt>
              <dd>{t('attendance.statusFromPlan')}</dd>
            </>
          ) : null}
          {status === 'manual' ? (
            <>
              <dt>{t('attendance.statusLabel')}</dt>
              <dd>{t('attendance.statusManual')}</dd>
            </>
          ) : null}
          {status === 'fromLeave' ? (
            <>
              <dt>{t('attendance.statusLabel')}</dt>
              <dd>{t('attendance.statusFromLeave')}</dd>
            </>
          ) : null}
        </dl>
      </section>

      {day.schedule ? (
        <section className={styles.section}>
          <h3 className={styles.sectionTitle}>{t('attendance.plan')}</h3>
          <dl className={styles.dl}>
            {planStateKey ? (
              <>
                <dt>{t('attendance.planState')}</dt>
                <dd>{t(planStateKey)}</dd>
              </>
            ) : null}
            {day.schedule.shiftName ? (
              <>
                <dt>{t('attendance.shiftName')}</dt>
                <dd>{day.schedule.shiftName}</dd>
              </>
            ) : null}
            {plannedRange ? (
              <>
                <dt>{t('attendance.plannedTime')}</dt>
                <dd>{plannedRange}</dd>
              </>
            ) : null}
            {day.schedule.shiftCode ? (
              <>
                <dt>{t('attendance.shiftCode')}</dt>
                <dd>{day.schedule.shiftCode}</dd>
              </>
            ) : null}
          </dl>
        </section>
      ) : null}

      {day.leave ? (
        <section className={styles.section}>
          <h3 className={styles.sectionTitle}>{t('attendance.leave')}</h3>
          {day.isManual && day.source !== 'Leave' ? (
            <Notice tone="info">{t('attendance.overriddenLeave')}</Notice>
          ) : null}
          <dl className={styles.dl}>
            <dt>{t('attendance.leaveType')}</dt>
            <dd>{day.leave ? attendanceLeaveDetailLabel(day.leave, t) : '—'}</dd>
            <dt>{t('attendance.leaveDates')}</dt>
            <dd>
              {formatDateOnly(day.leave.startDate, language)} – {formatDateOnly(day.leave.endDate, language)}
            </dd>
            <dt>{t('attendance.sourceLabel')}</dt>
            <dd>{t('attendance.leaveSource')}</dd>
          </dl>
        </section>
      ) : null}

      {day.isManual ? (
        <section className={styles.section}>
          <h3 className={styles.sectionTitle}>{t('attendance.currentCorrection')}</h3>
          <dl className={styles.dl}>
            <dt>{t('attendance.correctionKind')}</dt>
            <dd>{acceptedKey ? t(acceptedKey) : day.acceptedKind}</dd>
            <dt>{t('attendance.reason')}</dt>
            <dd>{day.correctionReason || '—'}</dd>
          </dl>
        </section>
      ) : null}

      {formVisible ? (
        <section className={styles.section}>
          <h3 className={styles.sectionTitle}>{t('attendance.manualCorrection')}</h3>
          {pastWarning ? <Notice tone="warning">{t('attendance.pastMonthWarning')}</Notice> : null}
          <div className={styles.form}>
            <SelectField
              id="attendance-correction-kind"
              label={t('attendance.correctionKind')}
              value={kind}
              required
              onChange={setKind}
            >
              <option value="">{t('attendance.correctionKind')}</option>
              {ATTENDANCE_CORRECTION_KINDS.map((item) => (
                <option key={item} value={item}>
                  {t(attendanceKindLabelKey(item)!)}
                </option>
              ))}
            </SelectField>
            <TextArea
              id="attendance-correction-reason"
              label={t('attendance.reason')}
              value={reason}
              required
              maxLength={ATTENDANCE_REASON_MAX_LENGTH}
              onChange={setReason}
            />
            <div className={styles.formActions}>
              <Button variant="primary" layout="inline" loading={busy} onClick={() => void onSave()}>
                {busy ? t('attendance.saving') : t('attendance.saveCorrection')}
              </Button>
              {clearVisible ? (
                <Button
                  variant="danger"
                  layout="inline"
                  disabled={busy}
                  onClick={() => setConfirmClear(true)}
                >
                  {t('attendance.clearCorrection')}
                </Button>
              ) : null}
            </div>
          </div>
        </section>
      ) : canManage ? null : (
        <p className={styles.muted}>{t('attendance.readOnlyHint')}</p>
      )}

      <section className={styles.section}>
        <h3 className={styles.sectionTitle}>{t('attendance.history')}</h3>
        {historyError ? <Notice tone="danger">{historyError}</Notice> : null}
        {!history || history.length === 0 ? (
          <p className={styles.muted}>{t('attendance.historyEmpty')}</p>
        ) : (
          <ol className={styles.historyList}>
            {history.map((item) => {
              const previousKey = attendanceKindLabelKey(item.previousKind)
              const nextKey = attendanceKindLabelKey(item.newKind)
              const action = item.changeType === 'Clear' ? t('attendance.historyClear') : t('attendance.historySet')
              return (
                <li key={item.id} className={styles.historyItem}>
                  <span className={styles.historyAction}>{action}</span>
                  <span className={styles.historyMeta}>
                    {t('attendance.historyPrevious')}: {previousKey ? t(previousKey) : '—'}
                    {' → '}
                    {t('attendance.historyNew')}: {nextKey ? t(nextKey) : '—'}
                  </span>
                  {item.newReason || item.previousReason ? (
                    <span className={styles.historyMeta}>
                      {t('attendance.historyReason')}: {item.newReason || item.previousReason}
                    </span>
                  ) : null}
                  {item.changedByUserId ? (
                    <span className={styles.historyMeta}>
                      {t('attendance.historyActor')}: {item.changedByUserId}
                    </span>
                  ) : null}
                  <span className={styles.historyMeta}>
                    {t('attendance.historyAt')}: {formatDateTime(item.changedAtUtc, language)}
                  </span>
                </li>
              )
            })}
          </ol>
        )}
      </section>

      {confirmClear ? (
        <WorkspaceDialog
          title={t('attendance.clearConfirmTitle')}
          subtitle={t('attendance.clearConfirmBody')}
          size="confirm"
          onRequestClose={() => setConfirmClear(false)}
          footer={
            <>
              <Button variant="ghost" onClick={() => setConfirmClear(false)}>
                {t('attendance.cancel')}
              </Button>
              <Button variant="danger" layout="inline" loading={busy} onClick={() => void onClear()}>
                {t('attendance.clearConfirm')}
              </Button>
            </>
          }
        >
          <p className={styles.muted}>{t('attendance.clearConfirmBody')}</p>
        </WorkspaceDialog>
      ) : null}
      </div>
    </aside>
  )
}
