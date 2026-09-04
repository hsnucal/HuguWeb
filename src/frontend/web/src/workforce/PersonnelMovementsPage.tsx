import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router'
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
import { Toast } from '../ui/Toast'
import { WorkspaceDialog } from '../ui/WorkspaceDialog'
import { CloseIcon } from '../ui/icons'
import { canManageHrMovements, canReadHrMovements } from './hrAccess'
import {
  MOVEMENT_REASON_MAX,
  MOVEMENT_TYPES,
  cancelHrMovement,
  getHrMovement,
  hrMovementErrorMessage,
  listHrMovements,
  type PersonnelMovementDetail,
  type PersonnelMovementListItem,
} from './hrMovementsApi'
import {
  isScheduledCancellable,
  movementActorLabel,
  movementDiffSummary,
  movementLifecycleLabelKey,
  movementLifecycleTone,
  movementTypeLabelKey,
} from './movementDisplay'
import { PersonnelMovementWizard } from './PersonnelMovementWizard'
import styles from './PersonnelMovementsPage.module.css'
import { listDepartments, type DepartmentRecord } from './workforceApi'

const SEARCH_DEBOUNCE_MS = 300

export function PersonnelMovementsPage() {
  const { t, i18n } = useTranslation()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? 'tr'
  const actorLabels = { system: t('movements.actorSystem'), unknown: t('movements.actorUnknown') }
  const { user } = useAuthSession()
  const canRead = canReadHrMovements(user)
  const canManage = canManageHrMovements(user)
  const [searchParams, setSearchParams] = useSearchParams()
  const employeeIdParam = searchParams.get('employeeId') ?? ''

  const properties = user?.accessibleProperties ?? []
  const [propertyId, setPropertyId] = useState(user?.propertyId ?? '')
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')
  const [type, setType] = useState('')
  const [departmentId, setDepartmentId] = useState('')
  const [search, setSearch] = useState('')
  const [searchApplied, setSearchApplied] = useState('')
  const [departments, setDepartments] = useState<DepartmentRecord[]>([])
  const [items, setItems] = useState<PersonnelMovementListItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [toast, setToast] = useState<string | null>(null)
  const dismissToast = useCallback(() => setToast(null), [setToast])
  const [reload, setReload] = useState(0)
  const [detail, setDetail] = useState<PersonnelMovementDetail | null>(null)
  const [wizardOpen, setWizardOpen] = useState(false)
  const [cancelOpen, setCancelOpen] = useState(false)
  const [cancelReason, setCancelReason] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    const handle = window.setTimeout(() => setSearchApplied(search.trim()), SEARCH_DEBOUNCE_MS)
    return () => window.clearTimeout(handle)
  }, [search])

  useEffect(() => {
    let cancelled = false
    void listDepartments()
      .then((rows) => {
        if (!cancelled) {
          setDepartments(rows.filter((item) => item.isActive))
        }
      })
      .catch(() => {
        if (!cancelled) {
          setDepartments([])
        }
      })
    return () => {
      cancelled = true
    }
  }, [user?.propertyId])

  useEffect(() => {
    if (!canRead) {
      return
    }
    let cancelled = false
    async function load() {
      setError(null)
      try {
        const rows = await listHrMovements({
          dateFrom: dateFrom || undefined,
          dateTo: dateTo || undefined,
          type: type || undefined,
          departmentId: departmentId || undefined,
          employeeId: employeeIdParam || undefined,
          propertyId: propertyId || undefined,
          search: searchApplied || undefined,
        })
        if (!cancelled) {
          setItems(rows)
        }
      } catch (reason) {
        if (!cancelled) {
          setError(hrMovementErrorMessage(reason, t))
          setItems([])
        }
      }
    }
    void load()
    return () => {
      cancelled = true
    }
  }, [
    canRead,
    dateFrom,
    dateTo,
    type,
    departmentId,
    employeeIdParam,
    propertyId,
    searchApplied,
    reload,
    t,
  ])

  const openDetail = async (id: string) => {
    setError(null)
    try {
      setDetail(await getHrMovement(id))
    } catch (reason) {
      setError(hrMovementErrorMessage(reason, t))
    }
  }

  const runCancel = async () => {
    if (!detail || cancelReason.trim() === '') {
      return
    }
    setBusy(true)
    setError(null)
    try {
      const next = await cancelHrMovement(detail.id, { reason: cancelReason.trim() })
      setCancelOpen(false)
      setCancelReason('')
      setToast(t('movements.cancel.success'))
      setReload((value) => value + 1)
      setDetail(next)
    } catch (reason) {
      setError(hrMovementErrorMessage(reason, t))
    } finally {
      setBusy(false)
    }
  }

  useEffect(() => {
    if (!detail) {
      return
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setDetail(null)
      }
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [detail])

  const filteredEmpty = Boolean(
    items
      && items.length === 0
      && (dateFrom || dateTo || type || departmentId || propertyId || searchApplied || employeeIdParam),
  )

  if (!canRead) {
    return <Notice tone="danger">{t('movements.noAccess')}</Notice>
  }

  return (
    <div className={styles.page}>
      {error ? (
        <Notice tone="danger">
          {error}{' '}
          <Button
            variant="ghost"
            layout="inline"
            size="sm"
            onClick={() => setReload((value) => value + 1)}
          >
            {t('movements.retry')}
          </Button>
        </Notice>
      ) : null}

      <div className={styles.toolbar}>
        <div className={styles.filters}>
          {properties.length > 1 ? (
            <SelectField
              id="movement-property"
              label={t('personnel.property')}
              value={propertyId}
              onChange={setPropertyId}
            >
              <option value="">{t('movements.filters.allProperties')}</option>
              {properties.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </SelectField>
          ) : null}
          <DateField id="movement-from" label={t('movements.filters.dateFrom')} value={dateFrom} onChange={setDateFrom} />
          <DateField id="movement-to" label={t('movements.filters.dateTo')} value={dateTo} onChange={setDateTo} />
          <SelectField id="movement-type-filter" label={t('movements.columns.type')} value={type} onChange={setType}>
            <option value="">{t('movements.filters.allTypes')}</option>
            {MOVEMENT_TYPES.map((item) => (
              <option key={item} value={item}>
                {t(movementTypeLabelKey(item))}
              </option>
            ))}
          </SelectField>
          <SelectField
            id="movement-department"
            label={t('workforce.department')}
            value={departmentId}
            onChange={setDepartmentId}
          >
            <option value="">{t('workforce.allDepartments')}</option>
            {departments.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </SelectField>
          <TextField
            id="movement-search"
            label={t('movements.filters.employee')}
            value={search}
            onChange={setSearch}
            placeholder={t('movements.filters.employeeHint')}
          />
        </div>
        {canManage ? (
          <Button layout="inline" onClick={() => setWizardOpen(true)}>
            {t('movements.new')}
          </Button>
        ) : null}
      </div>

      {employeeIdParam ? (
        <Notice tone="info">
          {t('movements.filters.employeePinned')}{' '}
          <Button
            variant="ghost"
            layout="inline"
            size="sm"
            onClick={() => {
              const next = new URLSearchParams(searchParams)
              next.delete('employeeId')
              setSearchParams(next)
            }}
          >
            {t('movements.filters.clearEmployee')}
          </Button>
        </Notice>
      ) : null}

      {items === null ? <Skeleton label={t('movements.loading')} /> : null}

      {items && items.length === 0 ? (
        <EmptyState
          title={filteredEmpty ? t('movements.emptyFiltered') : t('movements.empty')}
          action={
            canManage && !filteredEmpty ? (
              <Button layout="inline" onClick={() => setWizardOpen(true)}>
                {t('movements.new')}
              </Button>
            ) : undefined
          }
        />
      ) : null}

      <div className={styles.workspace} data-movements-grid-layout="full">
        <div className={styles.listColumn}>
          {items && items.length > 0 ? (
            <>
              <div className={styles.list}>
                <div className={`${styles.row} ${styles.head}`}>
                  <span>{t('movements.columns.personnel')}</span>
                  <span>{t('movements.columns.type')}</span>
                  <span>{t('movements.columns.previous')}</span>
                  <span>{t('movements.columns.next')}</span>
                  <span>{t('movements.columns.effectiveDate')}</span>
                  <span>{t('movements.columns.status')}</span>
                  <span>{t('movements.columns.reason')}</span>
                  <span>{t('movements.columns.actor')}</span>
                </div>
                {items.map((item) => {
                  const diff = movementDiffSummary(item)
                  return (
                    <button
                      key={item.id}
                      type="button"
                      className={`${styles.row} ${styles.clickRow}`}
                      onClick={() => void openDetail(item.id)}
                    >
                      <span className={styles.cellStack}>
                        <span className={styles.personName}>
                          {item.givenName} {item.familyName}
                        </span>
                        <span className={styles.muted}>{item.personnelNumber}</span>
                      </span>
                      <span>{t(movementTypeLabelKey(item.type))}</span>
                      <span>{diff.previous}</span>
                      <span>{diff.next}</span>
                      <span>{formatDateOnly(item.effectiveDate, language)}</span>
                      <StatusBadge tone={movementLifecycleTone(item.lifecycle)} variant="outline">
                        {t(movementLifecycleLabelKey(item.lifecycle))}
                      </StatusBadge>
                      <span>{item.reason}</span>
                      <span className={styles.muted}>
                        {movementActorLabel(item.actor, item.createdByUserId, actorLabels)}
                      </span>
                    </button>
                  )
                })}
              </div>
              <div className={styles.cards}>
                {items.map((item) => {
                  const diff = movementDiffSummary(item)
                  return (
                    <button key={item.id} type="button" className={styles.card} onClick={() => void openDetail(item.id)}>
                      <strong>
                        {item.givenName} {item.familyName}
                      </strong>
                      <span className={styles.muted}>{item.personnelNumber}</span>
                      <span>{t(movementTypeLabelKey(item.type))}</span>
                      <span>
                        {diff.previous} → {diff.next}
                      </span>
                      <StatusBadge tone={movementLifecycleTone(item.lifecycle)} variant="outline">
                        {t(movementLifecycleLabelKey(item.lifecycle))}
                      </StatusBadge>
                    </button>
                  )
                })}
              </div>
            </>
          ) : null}
        </div>

        {detail ? (
          <>
            <button
              type="button"
              className={styles.drawerScrim}
              aria-label={t('personnel.close')}
              onClick={() => setDetail(null)}
            />
            <aside className={styles.drawer} data-movements-drawer="overlay" aria-label={t('movements.detail.title')}>
              <div className={styles.drawerHeader}>
                <div>
                  <h2 className={styles.drawerTitle}>
                    {detail.givenName} {detail.familyName}
                  </h2>
                  <p className={styles.drawerMeta}>{detail.personnelNumber}</p>
                </div>
                <Button variant="ghost" layout="inline" size="sm" aria-label={t('personnel.close')} onClick={() => setDetail(null)}>
                  <CloseIcon />
                </Button>
              </div>
              <div className={styles.drawerBody}>
                <DetailSection
                  label={t('movements.columns.personnel')}
                  value={`${detail.givenName} ${detail.familyName} · ${detail.personnelNumber}`}
                />
                <DetailSection label={t('movements.columns.type')} value={t(movementTypeLabelKey(detail.type))} />
                <DetailSection label={t('movements.detail.previous')} value={movementDiffSummary(detail).previous} />
                <DetailSection label={t('movements.detail.next')} value={movementDiffSummary(detail).next} />
                <DetailSection
                  label={t('movements.columns.effectiveDate')}
                  value={formatDateOnly(detail.effectiveDate, language)}
                />
                <div className={styles.section}>
                  <span className={styles.sectionLabel}>{t('movements.columns.status')}</span>
                  <StatusBadge tone={movementLifecycleTone(detail.lifecycle)} variant="outline">
                    {t(movementLifecycleLabelKey(detail.lifecycle))}
                  </StatusBadge>
                  {detail.lifecycle === 'Scheduled' ? (
                    <p className={styles.intro}>
                      {t('movements.detail.scheduledHint', {
                        date: formatDateOnly(detail.effectiveDate, language),
                      })}
                    </p>
                  ) : null}
                </div>
                <DetailSection label={t('movements.columns.reason')} value={detail.reason} />
                {detail.note ? <DetailSection label={t('personnel.notes')} value={detail.note} /> : null}
                <DetailSection
                  label={t('movements.columns.actor')}
                  value={movementActorLabel(detail.actor, detail.createdByUserId, actorLabels)}
                />
                <DetailSection
                  label={t('movements.columns.createdAt')}
                  value={formatDateTime(detail.createdAtUtc, language)}
                />
                {detail.lifecycle === 'Cancelled' ? (
                  <>
                    <DetailSection
                      label={t('movements.detail.cancelledAt')}
                      value={detail.cancelledAtUtc ? formatDateTime(detail.cancelledAtUtc, language) : '—'}
                    />
                    <DetailSection
                      label={t('movements.detail.cancelledBy')}
                      value={movementActorLabel(detail.cancelledBy, detail.cancelledByUserId, actorLabels)}
                    />
                    <DetailSection
                      label={t('movements.detail.cancellationReason')}
                      value={detail.cancellationReason ?? '—'}
                    />
                  </>
                ) : null}
                {isScheduledCancellable(detail.lifecycle, canManage) ? (
                  <div className={styles.drawerActions}>
                    <Button
                      variant="danger"
                      layout="inline"
                      size="sm"
                      data-drawer-cancel
                      onClick={() => setCancelOpen(true)}
                    >
                      {t('movements.cancel.action')}
                    </Button>
                  </div>
                ) : null}
              </div>
            </aside>
          </>
        ) : null}
      </div>

      {wizardOpen ? (
        <PersonnelMovementWizard
          accessibleProperties={properties}
          onClose={() => setWizardOpen(false)}
          onCreated={(created) => {
            setWizardOpen(false)
            setDetail(created)
            setToast(t('movements.wizard.success'))
            setReload((value) => value + 1)
          }}
        />
      ) : null}

      {cancelOpen && detail ? (
        <WorkspaceDialog
          title={t('movements.cancel.title')}
          subtitle={t('movements.cancel.body')}
          size="confirm"
          onRequestClose={() => setCancelOpen(false)}
          footer={
            <>
              <Button variant="ghost" layout="inline" onClick={() => setCancelOpen(false)}>
                {t('personnel.cancel')}
              </Button>
              <Button
                variant="danger"
                layout="inline"
                loading={busy}
                disabled={cancelReason.trim() === ''}
                onClick={() => void runCancel()}
              >
                {t('movements.cancel.confirm')}
              </Button>
            </>
          }
        >
          <TextArea
            id="movement-cancel-reason"
            label={t('movements.cancel.reason')}
            value={cancelReason}
            onChange={(value) => setCancelReason(value.slice(0, MOVEMENT_REASON_MAX))}
            required
            maxLength={MOVEMENT_REASON_MAX}
          />
        </WorkspaceDialog>
      ) : null}
      <Toast message={toast} onDismiss={dismissToast} />
    </div>
  )
}

function DetailSection({ label, value }: { label: string; value: string }) {
  return (
    <div className={styles.section}>
      <span className={styles.sectionLabel}>{label}</span>
      <span className={styles.diff}>{value}</span>
    </div>
  )
}