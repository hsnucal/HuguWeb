import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router'
import { useTranslation } from 'react-i18next'
import { formatDateOnly, todayIsoDate } from '../i18n/format'
import { DEFAULT_LANGUAGE, toAppLanguage } from '../i18n/languages'
import { Button } from '../ui/Button'
import { DateField, SelectField } from '../ui/SelectField'
import { StatusBadge } from '../ui/StatusBadge'
import styles from './Workforce.module.css'
import {
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

export function EmployeeDetailPage() {
  const { employeeId } = useParams()
  const { t, i18n } = useTranslation()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const [employee, setEmployee] = useState<EmployeeHistory | null>(null)
  const [departments, setDepartments] = useState<DepartmentRecord[]>([])
  const [positions, setPositions] = useState<PositionRecord[]>([])
  const [mode, setMode] = useState<'none' | 'transfer' | 'end'>('none')
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
        setTransfer((current) => ({
          ...current,
          departmentId:
            current.departmentId
            || detail.currentPrimaryAssignment?.departmentId
            || departmentRows[0]?.id
            || '',
        }))
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
  }, [employeeId, t])

  const availablePositions = useMemo(
    () => positions.filter((item) => item.isActive),
    [positions],
  )

  const canMutate = employee?.currentEmployment?.status !== 'Ended'

  async function onTransfer() {
    if (!employeeId) {
      return
    }

    setError(null)
    try {
      await transferEmployee(employeeId, transfer)
      setMode('none')
      const [detail, departmentRows, positionRows] = await Promise.all([
        getEmployee(employeeId),
        listDepartments(),
        listPositions(),
      ])
      setEmployee(detail)
      setDepartments(departmentRows)
      setPositions(positionRows)
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    }
  }

  async function onEnd() {
    if (!employeeId) {
      return
    }

    setError(null)
    try {
      await endEmployment(employeeId, endDate)
      setMode('none')
      const [detail, departmentRows, positionRows] = await Promise.all([
        getEmployee(employeeId),
        listDepartments(),
        listPositions(),
      ])
      setEmployee(detail)
      setDepartments(departmentRows)
      setPositions(positionRows)
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    }
  }

  if (!employee) {
    return error ? (
      <p className={styles.error} role="alert">
        {error}
      </p>
    ) : null
  }

  const status = employee.currentEmployment?.status ?? employee.employments[0]?.status
  const statusTone = status === 'Active' ? 'success' : status === 'Scheduled' ? 'info' : 'neutral'

  return (
    <div className={styles.page}>
      <div className={styles.toolbar}>
        <div>
          <p className={styles.personName}>
            {employee.givenName} {employee.familyName}
          </p>
          <p className={styles.muted}>
            {t('workforce.personnelNumber')}: {employee.personnelNumber}
          </p>
        </div>
        <StatusBadge tone={statusTone}>
          {status === 'Active'
            ? t('workforce.activeStatus')
            : status === 'Scheduled'
              ? t('workforce.scheduledStatus')
              : t('workforce.endedStatus')}
        </StatusBadge>
      </div>

      {employee.currentPrimaryAssignment ? (
        <p>
          {t('workforce.workingIn', {
            department: employee.currentPrimaryAssignment.departmentName,
            position: employee.currentPrimaryAssignment.positionName,
          })}
        </p>
      ) : null}

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
          <div className={styles.formGrid}>
            <SelectField
              id="transfer-department"
              label={t('workforce.department')}
              value={transfer.departmentId}
              onChange={(departmentId) => setTransfer((current) => ({ ...current, departmentId }))}
              required
            >
              <option value="">{t('workforce.selectDepartment')}</option>
              {departments
                .filter((item) => item.isActive)
                .map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
            </SelectField>
            <SelectField
              id="transfer-position"
              label={t('workforce.position')}
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
              onChange={(effectiveDate) => setTransfer((current) => ({ ...current, effectiveDate }))}
              required
            />
          </div>
          <div className={styles.actions}>
            <Button type="submit" layout="inline">
              {t('workforce.transfer')}
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
          <p className={styles.muted}>{t('workforce.confirmEnd')}</p>
          <DateField
            id="end-date"
            label={t('workforce.endDate')}
            value={endDate}
            onChange={setEndDate}
            required
          />
          <div className={styles.actions}>
            <Button type="submit" variant="danger" layout="inline">
              {t('workforce.endEmployment')}
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
        <h2 className={styles.sectionTitle}>{t('workforce.employment')}</h2>
        <div className={styles.list}>
          {employee.employments.map((employment) => (
            <div key={employment.id} className={styles.row}>
              <span>{formatDateOnly(employment.startDate, language)}</span>
              <span>
                {employment.endDate ? formatDateOnly(employment.endDate, language) : '—'}
              </span>
              <StatusBadge
                tone={
                  employment.status === 'Active'
                    ? 'success'
                    : employment.status === 'Scheduled'
                      ? 'info'
                      : 'neutral'
                }
              >
                {employment.status === 'Active'
                  ? t('workforce.activeStatus')
                  : employment.status === 'Scheduled'
                    ? t('workforce.scheduledStatus')
                    : t('workforce.endedStatus')}
              </StatusBadge>
            </div>
          ))}
        </div>
      </section>

      <section>
        <h2 className={styles.sectionTitle}>{t('workforce.assignmentHistory')}</h2>
        <div className={styles.history}>
          {employee.employments.flatMap((employment) => employment.primaryAssignments).length === 0 ? (
            <p className={styles.empty}>{t('workforce.noHistory')}</p>
          ) : (
            employee.employments.flatMap((employment) =>
              employment.primaryAssignments.map((assignment) => (
                <div key={assignment.id} className={styles.historyItem}>
                  <span className={styles.muted}>
                    {formatDateOnly(assignment.startDate, language)}
                    {assignment.endDate ? ` – ${formatDateOnly(assignment.endDate, language)}` : ''}
                  </span>
                  <span>
                    {assignment.departmentName} · {assignment.positionName}
                  </span>
                </div>
              )),
            )
          )}
        </div>
      </section>

      <Link to="/app/workforce">{t('workforce.active')}</Link>
    </div>
  )
}
