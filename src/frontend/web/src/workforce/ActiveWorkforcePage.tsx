import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { formatDateOnly, todayIsoDate } from '../i18n/format'
import { DEFAULT_LANGUAGE, toAppLanguage } from '../i18n/languages'
import { Button } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { Notice } from '../ui/Notice'
import { DateField, SelectField } from '../ui/SelectField'
import { Skeleton } from '../ui/Skeleton'
import { StatusBadge } from '../ui/StatusBadge'
import { TextField } from '../ui/TextField'
import { AvatarMark } from '../ui/AvatarMark'
import styles from './Workforce.module.css'
import { canManageWorkforce } from './workforceAccess'
import {
  type DepartmentRecord,
  type EmployeeDirectoryItem,
  type PositionRecord,
  hireEmployee,
  listDepartments,
  listEmployees,
  listPositions,
  workforceErrorKey,
} from './workforceApi'
import {
  employmentStatusTone,
  inWorkforceView,
  matchesWorkforceSearch,
  type WorkforceView,
} from './workforceStatus'

async function fetchDirectory() {
  return Promise.all([listEmployees(), listDepartments(), listPositions()])
}

export function ActiveWorkforcePage() {
  const { t, i18n } = useTranslation()
  const { user } = useAuthSession()
  const canManage = canManageWorkforce(user)
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const [directory, setDirectory] = useState<EmployeeDirectoryItem[] | null>(null)
  const [departments, setDepartments] = useState<DepartmentRecord[]>([])
  const [positions, setPositions] = useState<PositionRecord[]>([])
  const [view, setView] = useState<WorkforceView>('active')
  const [query, setQuery] = useState('')
  const [departmentFilter, setDepartmentFilter] = useState('')
  const [hiring, setHiring] = useState(false)
  const [saving, setSaving] = useState(false)
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
        const [people, departmentRows, positionRows] = await fetchDirectory()
        if (cancelled) {
          return
        }

        setDirectory(people)
        setDepartments(departmentRows)
        setPositions(positionRows)
      } catch (reason) {
        if (!cancelled) {
          setError(t(workforceErrorKey(reason)))
          setDirectory([])
        }
      }
    }

    void loadPage()
    return () => {
      cancelled = true
    }
  }, [t])

  const activeDepartments = useMemo(
    () => departments.filter((item) => item.isActive),
    [departments],
  )
  const activePositions = useMemo(
    () => positions.filter((item) => item.isActive),
    [positions],
  )
  const people = directory ?? []
  const activeCount = people.filter((item) => item.employmentStatus === 'Active').length
  const scheduledCount = people.filter((item) => item.employmentStatus === 'Scheduled').length
  const formerCount = people.filter((item) => item.employmentStatus === 'Ended').length

  const visible = people.filter((person) => {
    if (!inWorkforceView(person, view)) {
      return false
    }

    if (!matchesWorkforceSearch(person, query)) {
      return false
    }

    if (departmentFilter !== '' && person.departmentName !== departmentFilter) {
      return false
    }

    return true
  })

  const canHire = canManage && activeDepartments.length > 0 && activePositions.length > 0

  async function onHire() {
    setError(null)
    setSaving(true)
    try {
      await hireEmployee(form)
      setHiring(false)
      setForm({
        givenName: '',
        familyName: '',
        personnelNumber: '',
        employmentStartDate: todayIsoDate(),
        departmentId: '',
        positionId: '',
      })
      const [staff, departmentRows, positionRows] = await fetchDirectory()
      setDirectory(staff)
      setDepartments(departmentRows)
      setPositions(positionRows)
      setView(form.employmentStartDate > todayIsoDate() ? 'scheduled' : 'active')
    } catch (reason) {
      setError(t(workforceErrorKey(reason)))
    } finally {
      setSaving(false)
    }
  }

  if (directory === null && error === null) {
    return <Skeleton variant="list" rows={6} label={t('workforce.loading')} />
  }

  return (
    <div className={styles.page}>
      <div className={styles.toolbar}>
        <p className={styles.muted}>
          {view === 'active'
            ? t('workforce.activeIntro')
            : view === 'scheduled'
              ? t('workforce.scheduledIntro')
              : t('workforce.formerIntro')}
        </p>
        {canManage ? (
          <Button layout="inline" onClick={() => setHiring((value) => !value)}>
            {t('workforce.hireNew')}
          </Button>
        ) : null}
      </div>

      {hiring && canManage ? (
        <form
          className={styles.panel}
          onSubmit={(event) => {
            event.preventDefault()
            void onHire()
          }}
        >
          {canHire ? (
            <>
              <fieldset className={styles.formSection}>
                <legend className={styles.formLegend}>{t('workforce.personalSection')}</legend>
                <div className={styles.formGrid}>
                  <TextField
                    id="hire-given"
                    label={t('workforce.givenName')}
                    value={form.givenName}
                    onChange={(givenName) => setForm((current) => ({ ...current, givenName }))}
                    autoComplete="given-name"
                    required
                  />
                  <TextField
                    id="hire-family"
                    label={t('workforce.familyName')}
                    value={form.familyName}
                    onChange={(familyName) => setForm((current) => ({ ...current, familyName }))}
                    autoComplete="family-name"
                    required
                  />
                  <TextField
                    id="hire-number"
                    label={t('workforce.personnelNumber')}
                    value={form.personnelNumber}
                    onChange={(personnelNumber) =>
                      setForm((current) => ({ ...current, personnelNumber }))
                    }
                    autoComplete="off"
                    required
                  />
                </div>
              </fieldset>
              <fieldset className={styles.formSection}>
                <legend className={styles.formLegend}>{t('workforce.startSection')}</legend>
                <div className={styles.formGrid}>
                  <DateField
                    id="hire-start"
                    label={t('workforce.startDate')}
                    value={form.employmentStartDate}
                    onChange={(employmentStartDate) =>
                      setForm((current) => ({ ...current, employmentStartDate }))
                    }
                    required
                  />
                </div>
              </fieldset>
              <fieldset className={styles.formSection}>
                <legend className={styles.formLegend}>{t('workforce.placementSection')}</legend>
                <div className={styles.formGrid}>
                  <SelectField
                    id="hire-department"
                    label={t('workforce.department')}
                    value={form.departmentId}
                    onChange={(departmentId) => setForm((current) => ({ ...current, departmentId }))}
                    required
                  >
                    <option value="">{t('workforce.selectDepartment')}</option>
                    {activeDepartments.map((item) => (
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
                    {activePositions.map((item) => (
                      <option key={item.id} value={item.id}>
                        {item.name}
                      </option>
                    ))}
                  </SelectField>
                </div>
              </fieldset>
              <div className={styles.formFooter}>
                <Button type="submit" layout="inline" loading={saving}>
                  {t('workforce.hireSubmit')}
                </Button>
                <Button variant="ghost" onClick={() => setHiring(false)}>
                  {t('workforce.cancel')}
                </Button>
              </div>
            </>
          ) : (
            <p className={styles.muted}>{t('workforce.hireNeedsStructure')}</p>
          )}
        </form>
      ) : null}

      {error ? <Notice tone="danger">{error}</Notice> : null}

      <div className={styles.segments} role="tablist" aria-label={t('workforce.directory')}>
        <ViewTab
          selected={view === 'active'}
          onSelect={() => setView('active')}
          label={t('workforce.tabCount', { label: t('workforce.active'), count: activeCount })}
        />
        <ViewTab
          selected={view === 'scheduled'}
          onSelect={() => setView('scheduled')}
          label={t('workforce.tabCount', { label: t('workforce.scheduled'), count: scheduledCount })}
        />
        <ViewTab
          selected={view === 'former'}
          onSelect={() => setView('former')}
          label={t('workforce.tabCount', { label: t('workforce.former'), count: formerCount })}
        />
      </div>

      <div className={styles.filters}>
        <TextField
          id="workforce-search"
          label={t('workforce.search')}
          value={query}
          onChange={setQuery}
          placeholder={t('workforce.searchPlaceholder')}
          autoComplete="off"
        />
        <SelectField
          id="workforce-department-filter"
          label={t('workforce.department')}
          value={departmentFilter}
          onChange={setDepartmentFilter}
        >
          <option value="">{t('workforce.allDepartments')}</option>
          {departments.map((item) => (
            <option key={item.id} value={item.name}>
              {item.name}
            </option>
          ))}
        </SelectField>
      </div>

      <section className={styles.list} aria-label={t('workforce.directory')}>
        <div className={`${styles.row} ${styles.head} ${styles.directoryRow}`}>
          <span>{t('workforce.fullName')}</span>
          <span>{t('workforce.department')}</span>
          <span>{t('workforce.position')}</span>
          <span>{t('workforce.startDate')}</span>
          <span>{t('workforce.status')}</span>
        </div>
        {visible.length === 0 ? (
          <EmptyState
            title={
              query.trim() !== '' || departmentFilter !== ''
                ? t('workforce.emptySearch')
                : view === 'active'
                  ? t('workforce.emptyActive')
                  : view === 'scheduled'
                    ? t('workforce.emptyScheduled')
                    : t('workforce.emptyFormer')
            }
            description={
              query.trim() !== '' || departmentFilter !== ''
                ? t('workforce.emptySearchHint')
                : view === 'active'
                  ? t('workforce.emptyActiveHint')
                  : view === 'scheduled'
                    ? t('workforce.emptyScheduledHint')
                    : t('workforce.emptyFormerHint')
            }
            action={
              view === 'active' && canManage && query.trim() === '' && departmentFilter === '' ? (
                <Button layout="inline" onClick={() => setHiring(true)}>
                  {t('workforce.hireNew')}
                </Button>
              ) : undefined
            }
          />
        ) : (
          visible.map((person) => (
            <Link
              key={person.employeeId}
              className={`${styles.row} ${styles.rowLink} ${styles.directoryRow}`}
              to={`/app/workforce/employees/${person.employeeId}`}
            >
              <span>
                <span className={styles.cellLabel}>{t('workforce.fullName')}</span>
                <span className={styles.identityCell}>
                  <AvatarMark name={`${person.givenName} ${person.familyName}`} />
                  <span className={styles.identityCopy}>
                    <span className={styles.personName}>
                      {person.givenName} {person.familyName}
                    </span>
                    <span className={styles.personMeta}>{person.personnelNumber}</span>
                  </span>
                </span>
              </span>
              <span>
                <span className={styles.cellLabel}>{t('workforce.department')}</span>
                {person.departmentName ?? '—'}
              </span>
              <span>
                <span className={styles.cellLabel}>{t('workforce.position')}</span>
                {person.positionName ?? '—'}
              </span>
              <span>
                <span className={styles.cellLabel}>{t('workforce.startDate')}</span>
                {formatDateOnly(person.employmentStartDate, language)}
              </span>
              <span>
                <span className={styles.cellLabel}>{t('workforce.status')}</span>
                <StatusBadge tone={employmentStatusTone(person.employmentStatus)}>
                  {person.employmentStatus === 'Active'
                    ? t('workforce.activeStatus')
                    : person.employmentStatus === 'Scheduled'
                      ? t('workforce.scheduledStatus')
                      : t('workforce.endedStatus')}
                </StatusBadge>
              </span>
            </Link>
          ))
        )}
      </section>
    </div>
  )
}

function ViewTab({
  selected,
  onSelect,
  label,
}: {
  selected: boolean
  onSelect: () => void
  label: string
}) {
  return (
    <button
      type="button"
      role="tab"
      aria-selected={selected}
      className={selected ? styles.segmentCurrent : styles.segment}
      onClick={onSelect}
    >
      {label}
    </button>
  )
}
