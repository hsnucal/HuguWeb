import { useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { formatDateFromIso, formatTimeFromIso } from '../i18n/format'
import { DEFAULT_LANGUAGE, toAppLanguage } from '../i18n/languages'
import { Button } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { ChevronLeftIcon } from '../ui/icons'
import { Notice } from '../ui/Notice'
import { SelectField } from '../ui/SelectField'
import { Skeleton } from '../ui/Skeleton'
import { StatusBadge } from '../ui/StatusBadge'
import { TextArea } from '../ui/TextField'
import { Timeline, TimelineItem } from '../ui/Timeline'
import { canReadMaintenance } from '../technical-service/maintenanceAccess'
import { displayEmployeeName } from './employeeName'
import styles from './RoomOperations.module.css'
import {
  isTechnicallyUnusable,
  neededActionFromState,
  neededActionLabelKey,
  priorityLabelKey,
  priorityTone,
  priorityVariant,
  readinessLabelKey,
  readinessMarker,
  readinessTone,
  serviceabilityLabelKey,
  serviceabilityTone,
  workStateTone,
} from './readiness'
import { canInspectRoomOperations, canManageRoomOperations, canReadRoomOperations } from './roomOperationsAccess'
import {
  completeCleaning,
  getRoom,
  inspectRoom,
  listAssignableEmployees,
  requestNeedsCleaning,
  roomOperationsErrorKey,
  type AssignableEmployeeItem,
  type RoomOperationsDetail,
  type RoomReadiness,
  type TaskPriority,
} from './roomOperationsApi'

export function RoomDetailPage() {
  const { roomId } = useParams()
  const { t, i18n } = useTranslation()
  const { user } = useAuthSession()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const canManage = canManageRoomOperations(user)
  const canInspect = canInspectRoomOperations(user)
  const canOpenTechnicalIssue = canReadMaintenance(user)
  const [detail, setDetail] = useState<RoomOperationsDetail | null>(null)
  const [employees, setEmployees] = useState<AssignableEmployeeItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [flash, setFlash] = useState(false)
  const [employeeId, setEmployeeId] = useState('')
  const [priority, setPriority] = useState<TaskPriority>('Normal')
  const [rejectionReason, setRejectionReason] = useState('')
  const previousReadiness = useRef<RoomReadiness | null>(null)

  useEffect(() => {
    if (!roomId) {
      return
    }

    let cancelled = false

    async function load() {
      try {
        const [room, people] = await Promise.all([getRoom(roomId!), listAssignableEmployees()])
        if (cancelled) {
          return
        }

        setDetail(room)
        setEmployees(people)
        if (people.length > 0) {
          setEmployeeId((current) => current || people[0].employeeId)
        }
      } catch (reason) {
        if (!cancelled) {
          setError(t(roomOperationsErrorKey(reason)))
        }
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [roomId, t])

  useEffect(() => {
    if (!detail) {
      return
    }

    const next = detail.readiness
    if (previousReadiness.current !== null && previousReadiness.current !== next) {
      setFlash(true)
      const timer = window.setTimeout(() => setFlash(false), 220)
      previousReadiness.current = next
      return () => window.clearTimeout(timer)
    }

    previousReadiness.current = next
  }, [detail])

  if (!canReadRoomOperations(user)) {
    return <Notice tone="danger">{t('roomOperations.noAccess')}</Notice>
  }

  async function run(action: () => Promise<RoomOperationsDetail>) {
    setError(null)
    setSaving(true)
    try {
      setDetail(await action())
      setRejectionReason('')
    } catch (reason) {
      setError(t(roomOperationsErrorKey(reason)))
    } finally {
      setSaving(false)
    }
  }

  const work = detail?.currentWork
  const assignedName = displayEmployeeName(work?.assignedEmployeeName)
  const neededAction = detail
    ? neededActionFromState(detail.readiness, work?.state ?? null, detail.isActive)
    : 'none'
  const actionLabel = t(neededActionLabelKey(neededAction))
  const workLabel = work ? t(`roomOperations.work.${work.state}`) : t('roomOperations.noWork')
  const showNeedsCleaning = canManage && detail?.readiness !== 'Clean' && work?.state !== 'Open'
  const showComplete = canManage && detail?.readiness === 'Dirty' && work?.state === 'Open'
  const showInspect = canInspect && detail?.readiness === 'Clean'

  return (
    <div className={styles.page}>
      <Link to="/app/room-operations" className={styles.backLink}>
        <ChevronLeftIcon />
        {t('roomOperations.back')}
      </Link>

      {error ? <Notice tone="danger">{error}</Notice> : null}

      {detail === null ? (
        <Skeleton variant="block" label={t('roomOperations.loading')} />
      ) : (
        <>
          <section
            className={`${styles.hero} ${styles[`hero${detail.readiness}`]} ${flash ? styles.flash : ''}`}
            aria-label={`${t('roomOperations.room')} ${detail.number}`}
          >
            <div className={styles.heroMain}>
              <p className="kicker">{t('roomOperations.room')}</p>
              <p className={styles.heroNumber}>{detail.number}</p>
              <div className={styles.heroStatus}>
                <StatusBadge tone={readinessTone(detail.readiness)} title={t(readinessLabelKey(detail.readiness))}>
                  {t(readinessLabelKey(detail.readiness))}
                </StatusBadge>
                <StatusBadge
                  tone={serviceabilityTone(detail.technicalServiceability)}
                  variant={isTechnicallyUnusable(detail.technicalServiceability) ? 'fill' : 'outline'}
                  title={t(serviceabilityLabelKey(detail.technicalServiceability))}
                >
                  {t(serviceabilityLabelKey(detail.technicalServiceability))}
                </StatusBadge>
                <span className={`${styles.actionText} ${neededAction === 'none' ? styles.actionQuiet : ''}`}>
                  {actionLabel}
                </span>
              </div>
            </div>

            <dl className={styles.heroMeta}>
              <div className={styles.summaryItem}>
                <dt className={styles.summaryLabel}>{t('roomOperations.readinessLabel')}</dt>
                <dd>
                  <StatusBadge tone={readinessTone(detail.readiness)} title={t(readinessLabelKey(detail.readiness))}>
                    {t(readinessLabelKey(detail.readiness))}
                  </StatusBadge>
                </dd>
              </div>
              <div className={styles.summaryItem}>
                <dt className={styles.summaryLabel}>{t('roomOperations.technicalCondition')}</dt>
                <dd>
                  <StatusBadge
                    tone={serviceabilityTone(detail.technicalServiceability)}
                    variant={isTechnicallyUnusable(detail.technicalServiceability) ? 'fill' : 'outline'}
                    title={t(serviceabilityLabelKey(detail.technicalServiceability))}
                  >
                    {t(serviceabilityLabelKey(detail.technicalServiceability))}
                  </StatusBadge>
                </dd>
              </div>
              <div className={styles.summaryItem}>
                <dt className={styles.summaryLabel}>{t('roomOperations.assignedEmployee')}</dt>
                <dd>
                  {assignedName ? (
                    <span className={styles.truncate} title={assignedName}>
                      {assignedName}
                    </span>
                  ) : (
                    <span className={styles.muted}>{t('roomOperations.unassigned')}</span>
                  )}
                </dd>
              </div>
              <div className={styles.summaryItem}>
                <dt className={styles.summaryLabel}>{t('roomOperations.priorityLabel')}</dt>
                <dd>
                  {work ? (
                    <StatusBadge
                      tone={priorityTone(work.priority)}
                      variant={priorityVariant(work.priority)}
                      title={t(priorityLabelKey(work.priority))}
                    >
                      {t(priorityLabelKey(work.priority))}
                    </StatusBadge>
                  ) : (
                    <span className={styles.muted}>{t('roomOperations.noPriority')}</span>
                  )}
                </dd>
              </div>
              <div className={styles.summaryItem}>
                <dt className={styles.summaryLabel}>{t('roomOperations.workState')}</dt>
                <dd>
                  <StatusBadge tone={workStateTone(work?.state ?? null)} variant="outline" title={workLabel}>
                    {workLabel}
                  </StatusBadge>
                </dd>
              </div>
            </dl>
          </section>

          {detail.hasActiveTechnicalIssue ? (
            <section
              className={`${styles.workSurface} ${styles.technicalSurface}`}
              aria-label={t('roomOperations.technicalCondition')}
            >
              <h2 className={styles.sectionTitle}>{t('roomOperations.technicalCondition')}</h2>
              <dl className={styles.technicalMeta}>
                <div className={styles.summaryItem}>
                  <dt className={styles.summaryLabel}>{t('roomOperations.technicalCondition')}</dt>
                  <dd>
                    <StatusBadge
                      tone={serviceabilityTone(detail.technicalServiceability)}
                      title={t(serviceabilityLabelKey(detail.technicalServiceability))}
                    >
                      {t(serviceabilityLabelKey(detail.technicalServiceability))}
                    </StatusBadge>
                  </dd>
                </div>
                {detail.activeTechnicalIssueDescription ? (
                  <div className={styles.summaryItem}>
                    <dt className={styles.summaryLabel}>{t('roomOperations.activeTechnicalIssue')}</dt>
                    <dd>{detail.activeTechnicalIssueDescription}</dd>
                  </div>
                ) : null}
              </dl>
              {canOpenTechnicalIssue && detail.governingIssueId ? (
                <Link to={`/app/technical-service/${detail.governingIssueId}`} className={styles.technicalLink}>
                  {t('roomOperations.viewTechnicalIssue')}
                </Link>
              ) : null}
            </section>
          ) : null}

          {showNeedsCleaning ? (
            <section className={styles.workSurface}>
              <h2 className={styles.sectionTitle}>{t('roomOperations.needsCleaningTitle')}</h2>
              <p className={styles.muted}>{t('roomOperations.needsCleaningIntro')}</p>
              {employees.length === 0 ? (
                <EmptyState compact title={t('roomOperations.noEmployees')} />
              ) : (
                <div className={styles.formStack}>
                  <SelectField
                    id="assigned-employee"
                    label={t('roomOperations.assignedEmployee')}
                    value={employeeId}
                    onChange={setEmployeeId}
                  >
                    {employees.map((person) => {
                      const name = displayEmployeeName(person.displayName) ?? person.displayName
                      return (
                        <option key={person.employeeId} value={person.employeeId}>
                          {name}
                        </option>
                      )
                    })}
                  </SelectField>
                  <SelectField
                    id="work-priority"
                    label={t('roomOperations.priorityLabel')}
                    value={priority}
                    onChange={(value) => setPriority(value as TaskPriority)}
                  >
                    <option value="Normal">{t('roomOperations.priority.Normal')}</option>
                    <option value="High">{t('roomOperations.priority.High')}</option>
                    <option value="Urgent">{t('roomOperations.priority.Urgent')}</option>
                  </SelectField>
                  <div className={styles.formFooter}>
                    <Button
                      layout="inline"
                      loading={saving}
                      disabled={!employeeId}
                      onClick={() => void run(() => requestNeedsCleaning(detail.id, employeeId, priority))}
                    >
                      {t('roomOperations.needsCleaningSubmit')}
                    </Button>
                  </div>
                </div>
              )}
            </section>
          ) : null}

          {showComplete ? (
            <section className={styles.workSurface}>
              <h2 className={styles.sectionTitle}>{t('roomOperations.completeTitle')}</h2>
              <p className={styles.muted}>{t('roomOperations.completeIntro')}</p>
              <div className={styles.formFooter}>
                <Button layout="inline" loading={saving} onClick={() => void run(() => completeCleaning(work!.id))}>
                  {t('roomOperations.completeSubmit')}
                </Button>
              </div>
            </section>
          ) : null}

          {showInspect ? (
            <section className={styles.workSurface} aria-labelledby="inspect-heading">
              <h2 className={styles.sectionTitle} id="inspect-heading">
                {t('roomOperations.inspectTitle')}
              </h2>
              <p className={styles.muted}>{t('roomOperations.inspectIntro')}</p>
              <div className={styles.inspectActions}>
                <div className={styles.formFooter}>
                  <Button
                    layout="inline"
                    loading={saving}
                    onClick={() => void run(() => inspectRoom(detail.id, 'accepted'))}
                  >
                    {t('roomOperations.accept')}
                  </Button>
                </div>
                <div className={styles.rejectBlock}>
                  <TextArea
                    id="rejection-reason"
                    label={t('roomOperations.rejectionReason')}
                    value={rejectionReason}
                    onChange={setRejectionReason}
                    autoComplete="off"
                    hint={t('roomOperations.rejectionHint')}
                    rows={3}
                  />
                  <div className={styles.actions}>
                    <Button
                      variant="danger"
                      layout="inline"
                      loading={saving}
                      onClick={() => void run(() => inspectRoom(detail.id, 'rejected', rejectionReason))}
                    >
                      {t('roomOperations.reject')}
                    </Button>
                  </div>
                </div>
              </div>
            </section>
          ) : null}

          <section className={styles.historyWell}>
            <h2 className={styles.sectionTitle}>{t('roomOperations.readinessHistory')}</h2>
            {detail.readinessHistory.length === 0 ? (
              <EmptyState compact title={t('roomOperations.noHistory')} />
            ) : (
              <Timeline label={t('roomOperations.readinessHistory')}>
                {detail.readinessHistory.map((item) => {
                  const actorName = displayEmployeeName(item.actorEmployeeName)
                  return (
                    <TimelineItem
                      key={item.id}
                      time={formatTimeFromIso(item.occurredAt, language)}
                      supporting={formatDateFromIso(item.occurredAt, language)}
                      marker={readinessMarker(item.readiness)}
                    >
                      <StatusBadge tone={readinessTone(item.readiness)}>
                        {t(readinessLabelKey(item.readiness))}
                      </StatusBadge>
                      <span>
                        {t(`roomOperations.cause.${item.cause}`)}
                        {actorName ? ` · ${actorName}` : ''}
                        {item.comment ? ` · ${item.comment}` : ''}
                      </span>
                    </TimelineItem>
                  )
                })}
              </Timeline>
            )}
          </section>

          <section className={styles.historyWell}>
            <h2 className={styles.sectionTitle}>{t('roomOperations.inspectionHistory')}</h2>
            {detail.inspectionHistory.length === 0 ? (
              <EmptyState compact title={t('roomOperations.noInspections')} />
            ) : (
              <Timeline label={t('roomOperations.inspectionHistory')}>
                {detail.inspectionHistory.map((item) => (
                  <TimelineItem
                    key={item.id}
                    time={formatTimeFromIso(item.occurredAt, language)}
                    supporting={formatDateFromIso(item.occurredAt, language)}
                    marker={item.result === 'Rejected' ? 'danger' : 'success'}
                  >
                    <StatusBadge tone={item.result === 'Rejected' ? 'danger' : 'success'} variant="outline">
                      {t(`roomOperations.inspectionResult.${item.result}`)}
                    </StatusBadge>
                    {item.reason ? <span>{item.reason}</span> : null}
                  </TimelineItem>
                ))}
              </Timeline>
            )}
          </section>
        </>
      )}
    </div>
  )
}
