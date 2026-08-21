import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { addDaysIso, formatDateOnly, laterIsoDate, todayIsoDate } from '../i18n/format'
import { DEFAULT_LANGUAGE, toAppLanguage } from '../i18n/languages'
import { Button } from '../ui/Button'
import { DateField, SelectField } from '../ui/SelectField'
import { StatusBadge } from '../ui/StatusBadge'
import { Surface } from '../ui/Surface'
import styles from './Workforce.module.css'
import { canManageWorkforce } from './workforceAccess'
import {
  type AssignmentHistoryRecord,
  type DepartmentRecord,
  type EmployeeHistory,
  type PositionRecord,
  endEmployment,
  getEmployee,
  listDepartments,
  listPositions,
  transferEmployee,
  workforceErrorKey,
} from './workforceApi'
import { employmentStatusTone } from './workforceStatus'

function statusLabel(status: string | undefined, translate: (key: string) => string) {
  if (status === 'Active') {
    return translate('workforce.activeStatus')
  }

  if (status === 'Scheduled') {
    return translate('workforce.scheduledStatus')
  }

  return translate('workforce.endedStatus')
}

function lastAssignment(employee: EmployeeHistory): AssignmentHistoryRecord | null {
  if (employee.currentPrimaryAssignment) {
    return employee.currentPrimaryAssignment
  }

  const primaries = employee.employments[0]?.primaryAssignments ?? []
  return primaries[primaries.length - 1] ?? null
}

function defaultTransferDate(assignment: AssignmentHistoryRecord | null): string {
  if (!assignment) {
    return todayIsoDate()
  }

  return laterIsoDate(todayIsoDate(), addDaysIso(assignment.startDate, 1))
}

function assignmentTimeline(employee: EmployeeHistory): AssignmentHistoryRecord[] {
  return employee.employments
    .flatMap((employment) => employment.primaryAssignments)
    .slice()
    .sort((left, right) => left.startDate.localeCompare(right.startDate) || left.id.localeCompare(right.id))
}

export function EmployeeDetailPage() {
  const { employeeId } = useParams()
  const { t, i18n } = useTranslation()
  const { user } = useAuthSession()
  const canManage = canManageWorkforce(user)
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const [employee, setEmployee] = useState<EmployeeHistory | null>(null)
  const [departments, setDepartments] = useState<DepartmentRecord[]>([])
  const [positions, setPositions] = useState<PositionRecord[]>([])
  const [mode, setMode] = useState<'none' | 'transfer' | 'end'>('none')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [transfer, setTransfer] = useState({
    departmentId: '',
    positionId: '',
    effectiveDate: todayIsoDate(),
  })
  const [endDate, setEndDate] = useState(todayIsoDate())

  useEffect(() => {
    let cancelled = false

    async function loadPage() {
      if (!employeeId) {
        return
      }

      try {
        const [detail, departmentRows, positionRows] = await Promise.all([
          getEmployee(employeeId),
          listDepartments(),
          listPositions(),
        ])
        if (cancelled) {
          return
        }

        setEmployee(detail)
        setDepartments(departmentRows)
        setPositions(positionRows)
        setTransfer({
          departmentId: detail.currentPrimaryAssignment?.departmentId ?? '',
          positionId: detail.currentPrimaryAssignment?.positionId ?? '',
          effectiveDate: defaultTransferDate(detail.currentPrimaryAssignment),
        })
        setLoading(false)
      } catch (reason) {
        if (!cancelled) {
          setError(t(workforceErrorKey(reason)))
          setLoading(false)
        }
      }
    }

    void loadPage()
    return () => {
      cancelled = true
    }
  }, [employeeId, t])

  const availableDepartments = useMemo(
    () => departments.filter((item) => item.isActive),
    [departments],
  )
  const availablePositions = useMemo(
    () => positions.filter((item) => item.isActive),
    [positions],
  )

  async function reload(id: string) {
    const [detail, departmentRows, positionRows] = await Promise.all([
      getEmployee(id),
      listDepartments(),
      listPositions(),
    ])
    setEmployee(detail)
    setDepartments(departmentRows)
    setPositions(positionRows)
    setTransfer({
      departmentId: detail.currentPrimaryAssignment?.departmentId ?? '',
      positionId: detail.currentPrimaryAssignment?.positionId ?? '',
      effectiveDate: defaultTransferDate(detail.currentPrimaryAssignment),
    })
  }

  async function onTransfer() {
    if (!employeeId) {
      return
    }

    setError(null)
    setSaving(true)
    try {
      await transferEmployee(employeeId, transfer)
      setMode('none')
      await reload(employeeId)
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    } finally {
      setSaving(false)
    }
  }

  async function onEnd() {
    if (!employeeId) {
      return
    }

    setError(null)
    setSaving(true)
    try {
      await endEmployment(employeeId, endDate)
      setMode('none')
      await reload(employeeId)
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <p className={styles.muted} role="status">
        {t('workforce.loading')}
      </p>
    )
  }

  if (!employee) {
    return error ? (
      <p className={styles.error} role="alert">
        {error}
      </p>
    ) : null
  }

  const status = employee.currentEmployment?.status ?? employee.employments[0]?.status
  const ended = status === 'Ended'
  const canMutate = canManage && !ended
  const assignment = lastAssignment(employee)
  const timeline = assignmentTimeline(employee)
  const currentEmployment = employee.currentEmployment ?? employee.employments[0]
  const earliestTransferDate = employee.currentPrimaryAssignment
    ? addDaysIso(employee.currentPrimaryAssignment.startDate, 1)
    : todayIsoDate()

  return (
    <div className={styles.page}>
      <Link className={styles.backLink} to="/app/workforce">
        {t('workforce.backToDirectory')}
      </Link>

      <div className={styles.toolbar}>
        <div>
          <p className={styles.personName}>
            {employee.givenName} {employee.familyName}
          </p>
          <p className={styles.muted}>
            {t('workforce.personnelNumber')}: {employee.personnelNumber}
          </p>
        </div>
        <StatusBadge tone={employmentStatusTone(status)}>{statusLabel(status, t)}</StatusBadge>
      </div>

      <h2 className={styles.sectionTitle}>
        {ended ? t('workforce.lastWork') : t('workforce.currentWork')}
      </h2>
      <Surface className={styles.summary}>
        <div className={styles.summaryItem}>
          <span className={styles.summaryLabel}>{t('workforce.status')}</span>
          <span>{statusLabel(status, t)}</span>
        </div>
        <div className={styles.summaryItem}>
          <span className={styles.summaryLabel}>{t('workforce.startDate')}</span>
          <span>
            {currentEmployment ? formatDateOnly(currentEmployment.startDate, language) : '—'}
          </span>
        </div>
        {ended && currentEmployment?.endDate ? (
          <div className={styles.summaryItem}>
            <span className={styles.summaryLabel}>{t('workforce.endDate')}</span>
            <span>{formatDateOnly(currentEmployment.endDate, language)}</span>
          </div>
        ) : null}
        <div className={styles.summaryItem}>
          <span className={styles.summaryLabel}>{t('workforce.department')}</span>
          <span>{assignment?.departmentName ?? '—'}</span>
        </div>
        <div className={styles.summaryItem}>
          <span className={styles.summaryLabel}>{t('workforce.position')}</span>
          <span>{assignment?.positionName ?? '—'}</span>
        </div>
      </Surface>

      {canMutate ? (
        <div className={styles.actions}>
          <Button layout="inline" onClick={() => setMode('transfer')}>
            {t('workforce.transfer')}
          </Button>
          <Button variant="danger" onClick={() => setMode('end')}>
            {t('workforce.endEmployment')}
          </Button>
        </div>
      ) : null}

      {mode === 'transfer' ? (
        <form
          className={styles.panel}
          onSubmit={(event) => {
            event.preventDefault()
            void onTransfer()
          }}
        >
          <p className={styles.muted}>{t('workforce.transferIntro')}</p>
          <div className={styles.compare}>
            <section className={styles.compareCard} aria-label={t('workforce.currentWork')}>
              <h2 className={styles.sectionTitle}>{t('workforce.currentWork')}</h2>
              <p>
                <span className={styles.summaryLabel}>{t('workforce.currentDepartment')}</span>
                <br />
                {employee.currentPrimaryAssignment?.departmentName ?? '—'}
              </p>
              <p>
                <span className={styles.summaryLabel}>{t('workforce.currentPosition')}</span>
                <br />
                {employee.currentPrimaryAssignment?.positionName ?? '—'}
              </p>
            </section>
            <section className={styles.compareCard} aria-label={t('workforce.placementSection')}>
              <h2 className={styles.sectionTitle}>{t('workforce.placementSection')}</h2>
              <div className={styles.formStack}>
                <SelectField
                  id="transfer-department"
                  label={t('workforce.newDepartment')}
                  value={transfer.departmentId}
                  onChange={(departmentId) =>
                    setTransfer((current) => ({ ...current, departmentId }))
                  }
                  required
                >
                  <option value="">{t('workforce.selectDepartment')}</option>
                  {availableDepartments.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.name}
                    </option>
                  ))}
                </SelectField>
                <SelectField
                  id="transfer-position"
                  label={t('workforce.newPosition')}
                  value={transfer.positionId}
                  onChange={(positionId) => setTransfer((current) => ({ ...current, positionId }))}
                  required
                >
                  <option value="">{t('workforce.selectPosition')}</option>
                  {availablePositions.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.name}
                    </option>
                  ))}
                </SelectField>
                <DateField
                  id="transfer-date"
                  label={t('workforce.effectiveDate')}
                  value={transfer.effectiveDate}
                  min={earliestTransferDate}
                  onChange={(effectiveDate) =>
                    setTransfer((current) => ({ ...current, effectiveDate }))
                  }
                  required
                />
              </div>
            </section>
          </div>
          <div className={styles.actions}>
            <Button type="submit" layout="inline" disabled={saving}>
              {t('workforce.transferSubmit')}
            </Button>
            <Button variant="ghost" onClick={() => setMode('none')}>
              {t('workforce.cancel')}
            </Button>
          </div>
        </form>
      ) : null}

      {mode === 'end' ? (
        <form
          className={styles.panel}
          onSubmit={(event) => {
            event.preventDefault()
            void onEnd()
          }}
        >
          <p className={styles.endNotice}>{t('workforce.confirmEnd')}</p>
          <DateField
            id="end-date"
            label={t('workforce.endDate')}
            value={endDate}
            onChange={setEndDate}
            required
          />
          <div className={styles.actions}>
            <Button type="submit" variant="danger" layout="inline" disabled={saving}>
              {t('workforce.endEmploymentSubmit')}
            </Button>
            <Button variant="ghost" onClick={() => setMode('none')}>
              {t('workforce.cancel')}
            </Button>
          </div>
        </form>
      ) : null}

      {error ? (
        <p className={styles.error} role="alert">
          {error}
        </p>
      ) : null}

      <section>
        <h2 className={styles.sectionTitle}>{t('workforce.workHistory')}</h2>
        <div className={styles.history}>
          {timeline.length === 0 ? (
            <p className={styles.emptyPlain}>{t('workforce.noHistory')}</p>
          ) : (
            timeline.map((assignmentRow) => (
              <div key={assignmentRow.id} className={styles.historyItem}>
                <span className={styles.muted}>
                  {formatDateOnly(assignmentRow.startDate, language)}
                  {' – '}
                  {assignmentRow.endDate
                    ? formatDateOnly(assignmentRow.endDate, language)
                    : t('workforce.present')}
                </span>
                <span>
                  {assignmentRow.departmentName} · {assignmentRow.positionName}
                </span>
              </div>
            ))
          )}
        </div>
      </section>
    </div>
  )
}
