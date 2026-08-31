import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { formatDateOnly } from '../i18n/format'
import { DEFAULT_LANGUAGE, toAppLanguage, type AppLanguage } from '../i18n/languages'
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
import { canManageHrEmployees, canManageHrLeave, canReadHrLeave, canReadHrSensitive } from './hrAccess'
import {
  hrEmployeePhotoUrl,
  hrErrorKey,
  hrListErrorKey,
  listHrEmployees,
  type HrEmployeeListItem,
} from './hrApi'
import {
  asCollection,
  asHrEmployeeList,
  personnelEmptyKind,
} from './personnelDirectory'
import { listDepartments, listPositions, type DepartmentRecord, type PositionRecord } from './workforceApi'
import { employmentStatusTone, type WorkforceView } from './workforceStatus'
import {
  availablePersonnelColumns,
  loadPersonnelColumns,
  requiredPersonnelColumns,
  savePersonnelColumns,
  type PersonnelColumnId,
} from './personnelColumns'
import { PersonnelCard } from './PersonnelCard'
import { PersonnelImportDialog } from './PersonnelImportDialog'
import { exportHrEmployees, downloadBlob } from './hrPersonnelMasterApi'
import { formatMobileForDisplay } from './personnelInput'

async function fetchDirectory() {
  return Promise.all([listHrEmployees(), listDepartments(), listPositions()])
}

function matchesSearch(person: HrEmployeeListItem, query: string): boolean {
  const needle = query.trim().toLocaleLowerCase()
  if (needle === '') {
    return true
  }

  const haystack = `${person.givenName} ${person.familyName} ${person.personnelNumber}`.toLocaleLowerCase()
  return haystack.includes(needle)
}

export function ActiveWorkforcePage() {
  const { t, i18n } = useTranslation()
  const { user } = useAuthSession()
  const canManage = canManageHrEmployees(user)
  const canWorkforceManage = canManageWorkforce(user)
  const canReadSensitive = canReadHrSensitive(user)
  const canReadLeave = canReadHrLeave(user)
  const canManageLeave = canManageHrLeave(user)
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const [directory, setDirectory] = useState<HrEmployeeListItem[] | null>(null)
  const [departments, setDepartments] = useState<DepartmentRecord[]>([])
  const [positions, setPositions] = useState<PositionRecord[]>([])
  const [view, setView] = useState<WorkforceView>('active')
  const [query, setQuery] = useState('')
  const [departmentFilter, setDepartmentFilter] = useState('')
  const [positionFilter, setPositionFilter] = useState('')
  const [startFrom, setStartFrom] = useState('')
  const [startTo, setStartTo] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loadFailed, setLoadFailed] = useState(false)
  const [card, setCard] = useState<{ type: 'create' } | { type: 'edit'; employeeId: string } | null>(null)
  const [feedback, setFeedback] = useState<string | null>(null)
  const [columns, setColumns] = useState<PersonnelColumnId[]>(() => loadPersonnelColumns(canReadSensitive))
  const [pickerOpen, setPickerOpen] = useState(false)
  const [importOpen, setImportOpen] = useState(false)
  const pickerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!pickerOpen) {
      return
    }

    function onPointerDown(event: MouseEvent) {
      if (pickerRef.current && !pickerRef.current.contains(event.target as Node)) {
        setPickerOpen(false)
      }
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setPickerOpen(false)
      }
    }

    document.addEventListener('mousedown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('mousedown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [pickerOpen])

  useEffect(() => {
    let cancelled = false

    async function loadPage() {
      try {
        const [people, departmentRows, positionRows] = await fetchDirectory()
        if (cancelled) {
          return
        }

        setDirectory(asHrEmployeeList<HrEmployeeListItem>(people))
        setDepartments(asCollection<DepartmentRecord>(departmentRows))
        setPositions(asCollection<PositionRecord>(positionRows))
        setError(null)
        setLoadFailed(false)
      } catch (reason) {
        if (!cancelled) {
          setError(t(hrListErrorKey(reason)))
          setLoadFailed(true)
          setDirectory([])
        }
      }
    }

    void loadPage()
    return () => {
      cancelled = true
    }
  }, [t])

  const people = asHrEmployeeList<HrEmployeeListItem>(directory)
  const activeCount = people.filter((item) => item.employmentStatus === 'Active').length
  const scheduledCount = people.filter((item) => item.employmentStatus === 'Scheduled').length
  const formerCount = people.filter((item) => item.employmentStatus === 'Ended').length
  const visibleColumns = columns.filter((id) => availablePersonnelColumns(canReadSensitive).includes(id))
  const canHire = canManage && departments.some((item) => item.isActive) && positions.some((item) => item.isActive)

  const visible = people.filter((person) => {
    if (view === 'active' && person.employmentStatus !== 'Active') {
      return false
    }
    if (view === 'scheduled' && person.employmentStatus !== 'Scheduled') {
      return false
    }
    if (view === 'former' && person.employmentStatus !== 'Ended') {
      return false
    }
    if (!matchesSearch(person, query)) {
      return false
    }
    if (departmentFilter !== '' && person.departmentId !== departmentFilter) {
      return false
    }
    if (positionFilter !== '' && person.positionId !== positionFilter) {
      return false
    }
    if (startFrom !== '' && person.employmentStartDate < startFrom) {
      return false
    }
    if (startTo !== '' && person.employmentStartDate > startTo) {
      return false
    }
    return true
  })
  const emptyKind = personnelEmptyKind({
    loadFailed,
    totalCount: people.length,
    visibleCount: visible.length,
  })

  async function reload() {
    const [staff, departmentRows, positionRows] = await fetchDirectory()
    setDirectory(asHrEmployeeList<HrEmployeeListItem>(staff))
    setDepartments(asCollection<DepartmentRecord>(departmentRows))
    setPositions(asCollection<PositionRecord>(positionRows))
    setLoadFailed(false)
    setError(null)
  }

  function toggleColumn(id: PersonnelColumnId) {
    if (requiredPersonnelColumns().includes(id)) {
      return
    }

    const next = columns.includes(id) ? columns.filter((item) => item !== id) : [...columns, id]
    const allowed = next.filter((item) => availablePersonnelColumns(canReadSensitive).includes(item))
    setColumns(allowed)
    savePersonnelColumns(allowed)
  }

  async function onExport() {
    setError(null)
    try {
      const blob = await exportHrEmployees({
        search: query,
        departmentId: departmentFilter || undefined,
        positionId: positionFilter || undefined,
        status: view === 'active' ? 'Active' : view === 'scheduled' ? 'Scheduled' : 'Ended',
        startFrom: startFrom || undefined,
        startTo: startTo || undefined,
        columns: visibleColumns,
      })
      downloadBlob(blob, `personnel-${new Date().toISOString().slice(0, 10)}.xlsx`)
    } catch (reason) {
      setError(t(hrErrorKey(reason)))
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
          <div className={styles.formActions}>
            <Button layout="inline" onClick={() => {
              setFeedback(null)
              setCard({ type: 'create' })
            }}>
              {t('personnel.addPersonnel')}
            </Button>
            <Button variant="secondary" layout="inline" onClick={() => void onExport()}>
              {t('personnel.exportExcel')}
            </Button>
            <Button variant="secondary" layout="inline" onClick={() => setImportOpen(true)}>
              {t('personnel.importExcel')}
            </Button>
          </div>
        ) : (
          <Button variant="secondary" layout="inline" onClick={() => void onExport()}>
            {t('personnel.exportExcel')}
          </Button>
        )}
      </div>

      {feedback ? <Notice tone="success">{feedback}</Notice> : null}
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

      <div className={styles.hrFilters}>
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
            <option key={item.id} value={item.id}>
              {item.name}
            </option>
          ))}
        </SelectField>
        <SelectField
          id="workforce-position-filter"
          label={t('workforce.position')}
          value={positionFilter}
          onChange={setPositionFilter}
        >
          <option value="">{t('personnel.allPositions')}</option>
          {positions.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name}
            </option>
          ))}
        </SelectField>
        <DateField
          id="workforce-start-from"
          label={t('personnel.startFrom')}
          value={startFrom}
          onChange={setStartFrom}
          calendar
        />
        <DateField
          id="workforce-start-to"
          label={t('personnel.startTo')}
          value={startTo}
          onChange={setStartTo}
          calendar
        />
        <div className={styles.columnPicker} ref={pickerRef}>
          <Button
            variant="secondary"
            onClick={() => setPickerOpen((value) => !value)}
            aria-expanded={pickerOpen}
            aria-haspopup="dialog"
            aria-controls="personnel-column-picker"
          >
            {t('personnel.columns')}
          </Button>
          {pickerOpen ? (
            <fieldset
              id="personnel-column-picker"
              className={styles.columnMenu}
              aria-label={t('personnel.columnPicker')}
            >
              <legend className={styles.columnLegend}>{t('personnel.columnPicker')}</legend>
              <p className={styles.columnGroup}>{t('personnel.columnFixed')}</p>
              {requiredPersonnelColumns()
                .filter((id) => availablePersonnelColumns(canReadSensitive).includes(id))
                .map((id) => (
                  <label key={id} className={`${styles.columnOption} ${styles.columnOptionFixed}`}>
                    <input type="checkbox" checked disabled />
                    {columnLabel(id, t)}
                  </label>
                ))}
              <p className={styles.columnGroup}>{t('personnel.columnOptional')}</p>
              {availablePersonnelColumns(canReadSensitive)
                .filter((id) => !requiredPersonnelColumns().includes(id))
                .map((id) => (
                  <label key={id} className={styles.columnOption}>
                    <input
                      type="checkbox"
                      checked={visibleColumns.includes(id)}
                      onChange={() => toggleColumn(id)}
                    />
                    {columnLabel(id, t)}
                  </label>
                ))}
            </fieldset>
          ) : null}
        </div>
      </div>

      <section className={styles.list} aria-label={t('workforce.directory')}>
        <div
          className={`${styles.row} ${styles.head} ${styles.hrRow}`}
          style={{ gridTemplateColumns: gridFor(visibleColumns) }}
        >
          {visibleColumns.map((id) => (
            <span key={id}>{columnLabel(id, t)}</span>
          ))}
        </div>
        {emptyKind === 'dataset' ? (
          <EmptyState
            title={t('personnel.emptyTitle')}
            description={t('personnel.emptyHint')}
            action={
              canManage ? (
                <Button layout="inline" onClick={() => {
                  setFeedback(null)
                  setCard({ type: 'create' })
                }}>
                  {t('personnel.addPersonnel')}
                </Button>
              ) : undefined
            }
          />
        ) : emptyKind === 'filter' ? (
          <EmptyState
            title={
              query !== '' || departmentFilter !== '' || positionFilter !== '' || startFrom !== '' || startTo !== ''
                ? t('workforce.emptySearch')
                : view === 'active'
                  ? t('workforce.emptyActive')
                  : view === 'scheduled'
                    ? t('workforce.emptyScheduled')
                    : t('workforce.emptyFormer')
            }
            description={
              query !== '' || departmentFilter !== '' || positionFilter !== '' || startFrom !== '' || startTo !== ''
                ? t('workforce.emptySearchHint')
                : view === 'active'
                  ? t('workforce.emptyActiveHint')
                  : view === 'scheduled'
                    ? t('workforce.emptyScheduledHint')
                    : t('workforce.emptyFormerHint')
            }
            action={
              view === 'active' && canManage ? (
                <Button layout="inline" onClick={() => {
                  setFeedback(null)
                  setCard({ type: 'create' })
                }}>
                  {t('personnel.addPersonnel')}
                </Button>
              ) : undefined
            }
          />
        ) : (
          visible.map((person) => (
            <button
              key={person.employeeId}
              type="button"
              className={`${styles.row} ${styles.rowLink} ${styles.hrRow}`}
              style={{ gridTemplateColumns: gridFor(visibleColumns) }}
              onClick={() => {
                setFeedback(null)
                setCard({ type: 'edit', employeeId: person.employeeId })
              }}
            >
              {visibleColumns.map((id) => (
                <span key={id}>
                  <span className={styles.cellLabel}>{columnLabel(id, t)}</span>
                  {cellValue(id, person, language, t)}
                </span>
              ))}
            </button>
          ))
        )}
      </section>

      {card ? (
        <PersonnelCard
          mode={card}
          departments={departments}
          positions={positions}
          canManage={canManage}
          canManageWorkforce={canWorkforceManage}
          canReadSensitive={canReadSensitive}
          canReadLeave={canReadLeave}
          canManageLeave={canManageLeave}
          onClose={() => setCard(null)}
          onSaved={async (employeeId) => {
            await reload()
            if (employeeId) {
              setCard({ type: 'edit', employeeId })
              return
            }
            if (card?.type === 'create') {
              setFeedback(t('personnel.createSuccess'))
            }
          }}
        />
      ) : null}

      {canManage && !canHire ? <Notice tone="info">{t('personnel.createNeedsStructure')}</Notice> : null}

      {importOpen ? (
        <PersonnelImportDialog
          onClose={() => setImportOpen(false)}
          onCompleted={() => void reload()}
        />
      ) : null}
    </div>
  )
}

function columnLabel(id: PersonnelColumnId, t: (key: string) => string): string {
  const labels: Record<PersonnelColumnId, string> = {
    photo: t('personnel.photo'),
    name: t('workforce.fullName'),
    personnelNumber: t('workforce.personnelNumber'),
    department: t('workforce.department'),
    position: t('workforce.position'),
    startDate: t('workforce.startDate'),
    status: t('workforce.status'),
    educationLevel: t('personnel.educationLevel'),
    mobilePhone: t('personnel.mobilePhone'),
    email: t('personnel.email'),
    bloodType: t('personnel.bloodType'),
    nationalIdentity: t('personnel.identityNumber'),
  }
  return labels[id]
}

function gridFor(columns: PersonnelColumnId[]): string {
  return columns
    .map((id) => {
      if (id === 'name') {
        return 'minmax(12rem, 1.5fr)'
      }
      if (id === 'photo') {
        return 'auto'
      }
      return 'minmax(6rem, 1fr)'
    })
    .join(' ')
}

function cellValue(
  id: PersonnelColumnId,
  person: HrEmployeeListItem,
  language: AppLanguage,
  t: (key: string) => string,
) {
  if (id === 'photo' || id === 'name') {
    if (id === 'photo') {
      return (
        <AvatarMark
          name={`${person.givenName} ${person.familyName}`}
          src={person.hasPhoto ? hrEmployeePhotoUrl(person.employeeId) : null}
        />
      )
    }

    return (
      <span className={styles.identityCell}>
        <span className={styles.identityCopy}>
          <span className={styles.personName}>
            {person.givenName} {person.familyName}
          </span>
        </span>
      </span>
    )
  }

  if (id === 'personnelNumber') {
    return person.personnelNumber
  }
  if (id === 'department') {
    return person.departmentName ?? '—'
  }
  if (id === 'position') {
    return person.positionName ?? '—'
  }
  if (id === 'startDate') {
    return formatDateOnly(person.employmentStartDate, language)
  }
  if (id === 'status') {
    return (
      <StatusBadge tone={employmentStatusTone(person.employmentStatus)}>
        {person.employmentStatus === 'Active'
          ? t('workforce.activeStatus')
          : person.employmentStatus === 'Scheduled'
            ? t('workforce.scheduledStatus')
            : t('workforce.endedStatus')}
      </StatusBadge>
    )
  }
  if (id === 'educationLevel') {
    return person.educationLevel ?? '—'
  }
  if (id === 'mobilePhone') {
    return formatMobileForDisplay(person.mobilePhone) ?? '—'
  }
  if (id === 'email') {
    return person.email ?? '—'
  }
  if (id === 'bloodType') {
    return person.bloodType ?? '—'
  }
  return person.nationalIdentityNumber ?? '—'
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
