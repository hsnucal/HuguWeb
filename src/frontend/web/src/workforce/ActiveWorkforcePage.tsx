import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router'
import { useTranslation } from 'react-i18next'
import { formatDateOnly, todayIsoDate } from '../i18n/format'
import { DEFAULT_LANGUAGE, toAppLanguage } from '../i18n/languages'
import { Button } from '../ui/Button'
import { DateField, SelectField } from '../ui/SelectField'
import { StatusBadge } from '../ui/StatusBadge'
import { TextField } from '../ui/TextField'
import styles from './Workforce.module.css'
import {
  type ActiveWorkforceMember,
  type DepartmentRecord,
  type EmployeeDirectoryItem,
  type PositionRecord,
  hireEmployee,
  listActiveWorkforce,
  listDepartments,
  listEmployees,
  listPositions,
  workforceErrorKey,
} from './workforceApi'

async function fetchWorkforceHome() {
  return Promise.all([listActiveWorkforce(), listEmployees(), listDepartments(), listPositions()])
}

export function ActiveWorkforcePage() {
  const { t, i18n } = useTranslation()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const [active, setActive] = useState<ActiveWorkforceMember[]>([])
  const [directory, setDirectory] = useState<EmployeeDirectoryItem[]>([])
  const [departments, setDepartments] = useState<DepartmentRecord[]>([])
  const [positions, setPositions] = useState<PositionRecord[]>([])
  const [hiring, setHiring] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState({
    givenName: '',
    familyName: '',
    personnelNumber: '',
    employmentStartDate: todayIsoDate(),
    departmentId: '',
    positionId: '',
  })

  useEffect(() => {
    let cancelled = false

    async function loadPage() {
      try {
        const [activePeople, people, departmentRows, positionRows] = await fetchWorkforceHome()
        if (cancelled) {
          return
        }

        setActive(activePeople)
        setDirectory(people)
        setDepartments(departmentRows)
        setPositions(positionRows)
        setForm((current) => ({
          ...current,
          departmentId: current.departmentId || departmentRows.find((item) => item.isActive)?.id || '',
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
  }, [t])

  const availablePositions = useMemo(
    () => positions.filter((item) => item.isActive),
    [positions],
  )

  const scheduled = directory.filter((item) => item.employmentStatus === 'Scheduled')
  const former = directory.filter((item) => item.employmentStatus === 'Ended')

  async function onHire() {
    setError(null)
    try {
      await hireEmployee({
        ...form,
        positionId: form.positionId,
      })
      setHiring(false)
      setForm((current) => ({
        ...current,
        givenName: '',
        familyName: '',
        personnelNumber: '',
        employmentStartDate: todayIsoDate(),
      }))
      const [activePeople, people, departmentRows, positionRows] = await fetchWorkforceHome()
      setActive(activePeople)
      setDirectory(people)
      setDepartments(departmentRows)
      setPositions(positionRows)
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    }
  }

  return (
    <div className={styles.page}>
      <div className={styles.toolbar}>
        <p className={styles.muted}>{t('workforce.activeIntro')}</p>
        <Button layout="inline" onClick={() => setHiring((value) => !value)}>
          {t('workforce.hire')}
        </Button>
      </div>

      {hiring ? (
        <form
          className={styles.panel}
          onSubmit={(event) => {
            event.preventDefault()
            void onHire()
          }}
        >
          <div className={styles.formGrid}>
            <TextField
              id="hire-given"
              label={t('workforce.givenName')}
              value={form.givenName}
              onChange={(givenName) => setForm((current) => ({ ...current, givenName }))}
              required
            />
            <TextField
              id="hire-family"
              label={t('workforce.familyName')}
              value={form.familyName}
              onChange={(familyName) => setForm((current) => ({ ...current, familyName }))}
              required
            />
            <TextField
              id="hire-number"
              label={t('workforce.personnelNumber')}
              value={form.personnelNumber}
              onChange={(personnelNumber) => setForm((current) => ({ ...current, personnelNumber }))}
              required
            />
            <DateField
              id="hire-start"
              label={t('workforce.startDate')}
              value={form.employmentStartDate}
              onChange={(employmentStartDate) =>
                setForm((current) => ({ ...current, employmentStartDate }))
              }
              required
            />
            <SelectField
              id="hire-department"
              label={t('workforce.department')}
              value={form.departmentId}
              onChange={(departmentId) => setForm((current) => ({ ...current, departmentId }))}
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
              id="hire-position"
              label={t('workforce.position')}
              value={form.positionId}
              onChange={(positionId) => setForm((current) => ({ ...current, positionId }))}
              required
            >
              <option value="">{t('workforce.selectPosition')}</option>
              {availablePositions.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </SelectField>
          </div>
          <div className={styles.actions}>
            <Button type="submit" layout="inline">
              {t('workforce.hire')}
            </Button>
            <Button variant="ghost" onClick={() => setHiring(false)}>
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

      <section className={styles.list} aria-label={t('workforce.active')}>
        <div className={`${styles.row} ${styles.head}`}>
          <span>{t('workforce.personnelNumber')}</span>
          <span>{t('workforce.givenName')}</span>
          <span>{t('workforce.department')}</span>
          <span>{t('workforce.position')}</span>
          <span />
        </div>
        {active.length === 0 ? (
          <p className={styles.empty}>{t('workforce.emptyActive')}</p>
        ) : (
          active.map((person) => (
            <Link
              key={person.employeeId}
              className={`${styles.row} ${styles.rowLink}`}
              to={`/app/workforce/employees/${person.employeeId}`}
            >
              <span className={styles.personName}>{person.personnelNumber}</span>
              <span>
                {person.givenName} {person.familyName}
              </span>
              <span>{person.departmentName}</span>
              <span>{person.positionName}</span>
              <StatusBadge tone="success">{t('workforce.activeStatus')}</StatusBadge>
            </Link>
          ))
        )}
      </section>

      {scheduled.length > 0 ? (
        <section>
          <h2 className={styles.sectionTitle}>{t('workforce.scheduled')}</h2>
          <div className={styles.list}>
            {scheduled.map((person) => (
              <Link
                key={person.employeeId}
                className={`${styles.row} ${styles.rowLink}`}
                to={`/app/workforce/employees/${person.employeeId}`}
              >
                <span className={styles.personName}>{person.personnelNumber}</span>
                <span>
                  {person.givenName} {person.familyName}
                </span>
                <span>{person.departmentName}</span>
                <span>{formatDateOnly(person.employmentStartDate, language)}</span>
                <StatusBadge tone="info">{t('workforce.scheduledStatus')}</StatusBadge>
              </Link>
            ))}
          </div>
        </section>
      ) : null}

      {former.length > 0 ? (
        <section>
          <h2 className={styles.sectionTitle}>{t('workforce.former')}</h2>
          <div className={styles.list}>
            {former.map((person) => (
              <Link
                key={person.employeeId}
                className={`${styles.row} ${styles.rowLink}`}
                to={`/app/workforce/employees/${person.employeeId}`}
              >
                <span className={styles.personName}>{person.personnelNumber}</span>
                <span>
                  {person.givenName} {person.familyName}
                </span>
                <span>{person.departmentName}</span>
                <span>
                  {person.employmentEndDate
                    ? formatDateOnly(person.employmentEndDate, language)
                    : ''}
                </span>
                <StatusBadge tone="neutral">{t('workforce.endedStatus')}</StatusBadge>
              </Link>
            ))}
          </div>
        </section>
      ) : null}
    </div>
  )
}
