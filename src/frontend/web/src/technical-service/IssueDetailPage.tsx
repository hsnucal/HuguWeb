import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { formatDateFromIso, formatTimeFromIso } from '../i18n/format'
import { DEFAULT_LANGUAGE, toAppLanguage } from '../i18n/languages'
import { displayEmployeeName } from '../room-operations/employeeName'
import { Button } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { ChevronLeftIcon } from '../ui/icons'
import { Notice } from '../ui/Notice'
import { SelectField } from '../ui/SelectField'
import { Skeleton } from '../ui/Skeleton'
import { StatusBadge } from '../ui/StatusBadge'
import { Surface } from '../ui/Surface'
import { TextArea } from '../ui/TextField'
import { Timeline, TimelineItem } from '../ui/Timeline'
import {
  historyLabelKey,
  historyMarker,
  impactLabelKey,
  neededActionLabelKey,
  outageLabelKey,
  priorityLabelKey,
  priorityTone,
  serviceabilityLabelKey,
  serviceabilityTone,
  statusLabelKey,
  statusTone,
} from './labels'
import { canManageMaintenance, canReadMaintenance, canResolveMaintenance } from './maintenanceAccess'
import {
  assignIssue,
  changeBlocking,
  changePriority,
  getIssue,
  listAssignableEmployees,
  maintenanceErrorKey,
  markUnableToResolve,
  resolveWork,
  resumeWork,
  startWork,
  type AssignableEmployeeItem,
  type MaintenanceIssueDetail,
  type MaintenancePriority,
  type OutageClassification,
  type PreparationImpact,
} from './maintenanceApi'
import styles from './TechnicalService.module.css'

export function IssueDetailPage() {
  const { issueId } = useParams()
  const { t, i18n } = useTranslation()
  const { user } = useAuthSession()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const canManage = canManageMaintenance(user)
  const canResolve = canResolveMaintenance(user)
  const [detail, setDetail] = useState<MaintenanceIssueDetail | null>(null)
  const [employees, setEmployees] = useState<AssignableEmployeeItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [employeeId, setEmployeeId] = useState('')
  const [priority, setPriority] = useState<MaintenancePriority>('Normal')
  const [blocksRoomUse, setBlocksRoomUse] = useState(false)
  const [outage, setOutage] = useState<OutageClassification>('OutOfOrder')
  const [unableNote, setUnableNote] = useState('')
  const [resolutionNote, setResolutionNote] = useState('')
  const [impact, setImpact] = useState<PreparationImpact>('None')

  useEffect(() => {
    if (!issueId) {
      return
    }

    let cancelled = false

    async function load() {
      try {
        const issue = await getIssue(issueId!)
        const people = canManage ? await listAssignableEmployees() : []
        if (cancelled) {
          return
        }

        setDetail(issue)
        setEmployees(people)
        setEmployeeId(issue.assignedEmployeeId ?? people[0]?.employeeId ?? '')
        setPriority(issue.priority)
        setBlocksRoomUse(issue.blocksRoomUse)
        setOutage(issue.outageClassification ?? 'OutOfOrder')
      } catch (reason) {
        if (!cancelled) {
          setError(t(maintenanceErrorKey(reason)))
        }
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [canManage, issueId, t])

  if (!canReadMaintenance(user)) {
    return <Notice tone="danger">{t('maintenance.noAccess')}</Notice>
  }

  async function run(action: () => Promise<MaintenanceIssueDetail>) {
    setError(null)
    setSaving(true)
    try {
      const next = await action()
      setDetail(next)
      setEmployeeId(next.assignedEmployeeId ?? employeeId)
      setPriority(next.priority)
      setBlocksRoomUse(next.blocksRoomUse)
      setOutage(next.outageClassification ?? 'OutOfOrder')
      setUnableNote('')
      setResolutionNote('')
    } catch (reason) {
      setError(t(maintenanceErrorKey(reason)))
    } finally {
      setSaving(false)
    }
  }

  const assignedName = displayEmployeeName(detail?.assignedEmployeeName)
  const mutable = detail !== null && detail.status !== 'Resolved'

  return (
    <div className={styles.page}>
      <Link to="/app/technical-service" className={styles.backLink}>
        <ChevronLeftIcon />
        {t('maintenance.back')}
      </Link>

      {error ? <Notice tone="danger">{error}</Notice> : null}

      {detail === null ? (
        <Skeleton variant="block" label={t('maintenance.loading')} />
      ) : (
        <>
          <section className={styles.hero} aria-label={`${t('maintenance.room')} ${detail.roomNumber}`}>
            <div>
              <p className="kicker">{t('maintenance.room')}</p>
              <p className={styles.heroNumber}>{detail.roomNumber}</p>
              <p className={styles.heroIssue}>{detail.description}</p>
              <div className={styles.heroStatus}>
                <StatusBadge tone={statusTone(detail.status)} title={t(statusLabelKey(detail.status))}>
                  {t(statusLabelKey(detail.status))}
                </StatusBadge>
                <StatusBadge
                  tone={priorityTone(detail.priority)}
                  variant="priority"
                  title={t(priorityLabelKey(detail.priority))}
                >
                  {t(priorityLabelKey(detail.priority))}
                </StatusBadge>
                <span className={`${styles.actionText} ${detail.neededAction === 'none' ? styles.actionQuiet : ''}`}>
                  {t(neededActionLabelKey(detail.neededAction))}
                </span>
              </div>
            </div>
            <dl className={styles.heroMeta}>
              <div className={styles.summaryItem}>
                <dt className={styles.summaryLabel}>{t('maintenance.assigned')}</dt>
                <dd>
                  {assignedName ? (
                    <span className={styles.truncate} title={assignedName}>
                      {assignedName}
                    </span>
                  ) : (
                    <span className={styles.muted}>{t('maintenance.unassigned')}</span>
                  )}
                </dd>
              </div>
              <div className={styles.summaryItem}>
                <dt className={styles.summaryLabel}>{t('maintenance.category')}</dt>
                <dd>{detail.categoryName}</dd>
              </div>
              <div className={styles.summaryItem}>
                <dt className={styles.summaryLabel}>{t('maintenance.serviceabilityLabel')}</dt>
                <dd>
                  <StatusBadge
                    tone={serviceabilityTone(detail.roomServiceability)}
                    variant="outline"
                    title={t(serviceabilityLabelKey(detail.roomServiceability))}
                  >
                    {t(serviceabilityLabelKey(detail.roomServiceability))}
                  </StatusBadge>
                </dd>
              </div>
              <div className={styles.summaryItem}>
                <dt className={styles.summaryLabel}>{t('maintenance.createdAt')}</dt>
                <dd>{formatDateFromIso(detail.createdAt, language)}</dd>
              </div>
            </dl>
          </section>

          {canManage && mutable ? (
            <Surface tone="section">
              <h2 className={styles.sectionTitle}>{t('maintenance.managerActions')}</h2>
              <div className={styles.formGrid}>
                <SelectField
                  id="detail-employee"
                  label={t('maintenance.assigned')}
                  value={employeeId}
                  onChange={setEmployeeId}
                >
                  {employees.map((person) => (
                    <option key={person.employeeId} value={person.employeeId}>
                      {person.displayName}
                    </option>
                  ))}
                </SelectField>
                <div className={styles.formFooter}>
                  <Button
                    layout="inline"
                    loading={saving}
                    disabled={!employeeId}
                    onClick={() =>
                      void run(() => assignIssue(detail.id, employeeId, detail.version))
                    }
                  >
                    {detail.assignedEmployeeId ? t('maintenance.reassign') : t('maintenance.assign')}
                  </Button>
                </div>
                <SelectField
                  id="detail-priority"
                  label={t('maintenance.priorityLabel')}
                  value={priority}
                  onChange={(value) => setPriority(value as MaintenancePriority)}
                >
                  <option value="Normal">{t('maintenance.priority.Normal')}</option>
                  <option value="High">{t('maintenance.priority.High')}</option>
                  <option value="Urgent">{t('maintenance.priority.Urgent')}</option>
                </SelectField>
                <div className={styles.formFooter}>
                  <Button
                    variant="secondary"
                    layout="inline"
                    loading={saving}
                    onClick={() => void run(() => changePriority(detail.id, priority, detail.version))}
                  >
                    {t('maintenance.changePriority')}
                  </Button>
                </div>
                <SelectField
                  id="detail-blocking"
                  label={t('maintenance.blocksRoomUse')}
                  value={blocksRoomUse ? 'yes' : 'no'}
                  onChange={(value) => setBlocksRoomUse(value === 'yes')}
                >
                  <option value="no">{t('maintenance.blocksNo')}</option>
                  <option value="yes">{t('maintenance.blocksYes')}</option>
                </SelectField>
                {blocksRoomUse ? (
                  <SelectField
                    id="detail-outage"
                    label={t('maintenance.outageLabel')}
                    value={outage}
                    onChange={(value) => setOutage(value as OutageClassification)}
                  >
                    <option value="OutOfOrder">{t('maintenance.outage.OutOfOrder')}</option>
                    <option value="OutOfService">{t('maintenance.outage.OutOfService')}</option>
                  </SelectField>
                ) : null}
                <div className={styles.formFooter}>
                  <Button
                    variant="secondary"
                    layout="inline"
                    loading={saving}
                    onClick={() =>
                      void run(() =>
                        changeBlocking(detail.id, blocksRoomUse, detail.version, blocksRoomUse ? outage : undefined),
                      )
                    }
                  >
                    {t('maintenance.changeBlocking')}
                  </Button>
                </div>
              </div>
            </Surface>
          ) : null}

          {canResolve && detail.status === 'Open' ? (
            <Surface tone="section">
              <h2 className={styles.sectionTitle}>{t('maintenance.startTitle')}</h2>
              <p className={styles.issueMeta}>{t('maintenance.startIntro')}</p>
              <div className={styles.formFooter}>
                <Button
                  layout="inline"
                  loading={saving}
                  disabled={!detail.assignedEmployeeId}
                  onClick={() => void run(() => startWork(detail.id, detail.version))}
                >
                  {t('maintenance.start')}
                </Button>
              </div>
            </Surface>
          ) : null}

          {canResolve && detail.status === 'InProgress' ? (
            <Surface tone="section">
              <h2 className={styles.sectionTitle}>{t('maintenance.resolveTitle')}</h2>
              <div className={styles.form}>
                <TextArea
                  id="unable-note"
                  label={t('maintenance.unableNote')}
                  value={unableNote}
                  onChange={setUnableNote}
                />
                <Button
                  variant="secondary"
                  layout="inline"
                  loading={saving}
                  disabled={!unableNote.trim()}
                  onClick={() => void run(() => markUnableToResolve(detail.id, unableNote, detail.version))}
                >
                  {t('maintenance.unable')}
                </Button>
                <TextArea
                  id="resolution-note"
                  label={t('maintenance.resolutionNote')}
                  value={resolutionNote}
                  onChange={setResolutionNote}
                />
                <SelectField
                  id="preparation-impact"
                  label={t('maintenance.preparationImpact')}
                  value={impact}
                  onChange={(value) => setImpact(value as PreparationImpact)}
                >
                  <option value="None">{t('maintenance.impact.None')}</option>
                  <option value="RequiresPreparation">{t('maintenance.impact.RequiresPreparation')}</option>
                </SelectField>
                <Button
                  layout="inline"
                  loading={saving}
                  disabled={!resolutionNote.trim()}
                  onClick={() => void run(() => resolveWork(detail.id, resolutionNote, impact, detail.version))}
                >
                  {t('maintenance.resolve')}
                </Button>
              </div>
            </Surface>
          ) : null}

          {canResolve && detail.status === 'UnableToResolve' ? (
            <Surface tone="section">
              <h2 className={styles.sectionTitle}>{t('maintenance.resumeTitle')}</h2>
              {detail.unableToResolveNote ? (
                <p className={styles.issueMeta}>{detail.unableToResolveNote}</p>
              ) : null}
              <div className={styles.formFooter}>
                <Button layout="inline" loading={saving} onClick={() => void run(() => resumeWork(detail.id, detail.version))}>
                  {t('maintenance.resume')}
                </Button>
              </div>
            </Surface>
          ) : null}

          <Surface tone="inset">
            <h2 className={styles.sectionTitle}>{t('maintenance.history')}</h2>
            {detail.history.length === 0 ? (
              <EmptyState compact title={t('maintenance.noHistory')} />
            ) : (
              <Timeline label={t('maintenance.history')}>
                {detail.history.map((item) => (
                  <TimelineItem
                    key={item.id}
                    time={formatTimeFromIso(item.occurredAt, language)}
                    supporting={formatDateFromIso(item.occurredAt, language)}
                    marker={historyMarker(item.eventType)}
                  >
                    <strong>{t(historyLabelKey(item.eventType))}</strong>
                    {item.toEmployeeName ? ` · ${item.toEmployeeName}` : null}
                    {item.toPriority ? ` · ${t(priorityLabelKey(item.toPriority))}` : null}
                    {item.outageClassification ? ` · ${t(outageLabelKey(item.outageClassification))}` : null}
                    {item.preparationImpact ? ` · ${t(impactLabelKey(item.preparationImpact))}` : null}
                    {item.note ? <div>{item.note}</div> : null}
                  </TimelineItem>
                ))}
              </Timeline>
            )}
          </Surface>
        </>
      )}
    </div>
  )
}
