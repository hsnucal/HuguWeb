import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { formatDateTime } from '../i18n/format'
import { DEFAULT_LANGUAGE, toAppLanguage } from '../i18n/languages'
import { Button } from '../ui/Button'
import { SelectField } from '../ui/SelectField'
import { StatusBadge } from '../ui/StatusBadge'
import { TextField } from '../ui/TextField'
import { displayEmployeeName } from './employeeName'
import styles from './RoomOperations.module.css'
import {
  neededActionFromState,
  neededActionLabelKey,
  priorityLabelKey,
  priorityTone,
  priorityVariant,
  readinessLabelKey,
  readinessTone,
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
  type TaskPriority,
} from './roomOperationsApi'

export function RoomDetailPage() {
  const { roomId } = useParams()
  const { t, i18n } = useTranslation()
  const { user } = useAuthSession()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const canManage = canManageRoomOperations(user)
  const canInspect = canInspectRoomOperations(user)
  const [detail, setDetail] = useState<RoomOperationsDetail | null>(null)
  const [employees, setEmployees] = useState<AssignableEmployeeItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [employeeId, setEmployeeId] = useState('')
  const [priority, setPriority] = useState<TaskPriority>('Normal')
  const [rejectionReason, setRejectionReason] = useState('')

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

  if (!canReadRoomOperations(user)) {
    return <p className={styles.empty}>{t('roomOperations.noAccess')}</p>
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
        {t('roomOperations.back')}
      </Link>

      {error ? (
        <p className={styles.error} role="alert">
          {error}
        </p>
      ) : null}

      {detail === null ? (
        <p className={styles.empty}>{t('roomOperations.loading')}</p>
      ) : (
        <>
          <section className={styles.panel} aria-label={`${t('roomOperations.room')} ${detail.number}`}>
            <div className={styles.identity}>
              <div className={styles.summaryItem}>
                <span className={styles.summaryLabel}>{t('roomOperations.room')}</span>
                <span className={styles.identityNumber}>{detail.number}</span>
              </div>
              <div className={styles.identityState}>
                <StatusBadge tone={readinessTone(detail.readiness)} title={t(readinessLabelKey(detail.readiness))}>
                  {t(readinessLabelKey(detail.readiness))}
                </StatusBadge>
                <div className={styles.currentAction}>
                  <span className={styles.summaryLabel}>{t('roomOperations.actionNeeded')}</span>
                  <span className={`${styles.actionText} ${neededAction === 'none' ? styles.actionQuiet : ''}`}>
                    {actionLabel}
                  </span>
                </div>
              </div>
            </div>

            <div className={styles.meta}>
              <div className={styles.summaryItem}>
                <span className={styles.summaryLabel}>{t('roomOperations.assignedEmployee')}</span>
                {assignedName ? (
                  <span className={styles.truncate} title={assignedName}>
                    {assignedName}
                  </span>
                ) : (
                  <span className={styles.muted}>{t('roomOperations.unassigned')}</span>
                )}
              </div>
              <div className={styles.summaryItem}>
                <span className={styles.summaryLabel}>{t('roomOperations.priorityLabel')}</span>
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
              </div>
              <div className={styles.summaryItem}>
                <span className={styles.summaryLabel}>{t('roomOperations.workState')}</span>
                <StatusBadge tone={workStateTone(work?.state ?? null)} variant="outline" title={workLabel}>
                  {workLabel}
                </StatusBadge>
              </div>
            </div>
          </section>

          {showNeedsCleaning ? (
            <section className={styles.panel}>
              <h2 className={styles.sectionTitle}>{t('roomOperations.needsCleaningTitle')}</h2>
              <p className={styles.muted}>{t('roomOperations.needsCleaningIntro')}</p>
              {employees.length === 0 ? (
                <p className={styles.empty}>{t('roomOperations.noEmployees')}</p>
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
                  <div className={styles.actions}>
                    <Button
                      layout="inline"
                      disabled={saving || !employeeId}
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
            <section className={styles.panel}>
              <h2 className={styles.sectionTitle}>{t('roomOperations.completeTitle')}</h2>
              <p className={styles.muted}>{t('roomOperations.completeIntro')}</p>
              <div className={styles.actions}>
                <Button layout="inline" disabled={saving} onClick={() => void run(() => completeCleaning(work!.id))}>
                  {t('roomOperations.completeSubmit')}
                </Button>
              </div>
            </section>
          ) : null}

          {showInspect ? (
            <section className={styles.panel} aria-labelledby="inspect-heading">
              <h2 className={styles.sectionTitle} id="inspect-heading">
                {t('roomOperations.inspectTitle')}
              </h2>
              <p className={styles.muted}>{t('roomOperations.inspectIntro')}</p>
              <div className={styles.inspectActions}>
                <div className={styles.actions}>
                  <Button
                    layout="inline"
                    disabled={saving}
                    onClick={() => void run(() => inspectRoom(detail.id, 'accepted'))}
                  >
                    {t('roomOperations.accept')}
                  </Button>
                </div>
                <div className={styles.rejectBlock}>
                  <TextField
                    id="rejection-reason"
                    label={t('roomOperations.rejectionReason')}
                    value={rejectionReason}
                    onChange={setRejectionReason}
                    autoComplete="off"
                    aria-describedby="rejection-reason-hint"
                  />
                  <p className={styles.hint} id="rejection-reason-hint">
                    {t('roomOperations.rejectionHint')}
                  </p>
                  <div className={styles.actions}>
                    <Button
                      variant="danger"
                      layout="inline"
                      disabled={saving}
                      onClick={() => void run(() => inspectRoom(detail.id, 'rejected', rejectionReason))}
                    >
                      {t('roomOperations.reject')}
                    </Button>
                  </div>
                </div>
              </div>
            </section>
          ) : null}

          <section className={`${styles.panel} ${styles.panelHistory}`}>
            <h2 className={styles.sectionTitle}>{t('roomOperations.readinessHistory')}</h2>
            {detail.readinessHistory.length === 0 ? (
              <p className={styles.muted}>{t('roomOperations.noHistory')}</p>
            ) : (
              <div className={styles.history}>
                {detail.readinessHistory.map((item) => {
                  const actorName = displayEmployeeName(item.actorEmployeeName)
                  return (
                    <div key={item.id} className={styles.historyItem}>
                      <span>{formatDateTime(item.occurredAt, language)}</span>
                      <span className={styles.historyBody}>
                        <StatusBadge tone={readinessTone(item.readiness)}>
                          {t(readinessLabelKey(item.readiness))}
                        </StatusBadge>
                        <span>
                          {t(`roomOperations.cause.${item.cause}`)}
                          {actorName ? ` · ${actorName}` : ''}
                          {item.comment ? ` · ${item.comment}` : ''}
                        </span>
                      </span>
                    </div>
                  )
                })}
              </div>
            )}
          </section>

          <section className={`${styles.panel} ${styles.panelHistory}`}>
            <h2 className={styles.sectionTitle}>{t('roomOperations.inspectionHistory')}</h2>
            {detail.inspectionHistory.length === 0 ? (
              <p className={styles.muted}>{t('roomOperations.noInspections')}</p>
            ) : (
              <div className={styles.history}>
                {detail.inspectionHistory.map((item) => (
                  <div key={item.id} className={styles.historyItem}>
                    <span>{formatDateTime(item.occurredAt, language)}</span>
                    <span className={styles.historyBody}>
                      <StatusBadge
                        tone={item.result === 'Rejected' ? 'danger' : 'success'}
                        variant="outline"
                      >
                        {t(`roomOperations.inspectionResult.${item.result}`)}
                      </StatusBadge>
                      {item.reason ? <span>{item.reason}</span> : null}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </section>
        </>
      )}
    </div>
  )
}
