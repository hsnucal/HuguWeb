import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { EmptyState } from '../ui/EmptyState'
import { ChevronRightIcon } from '../ui/icons'
import { Notice } from '../ui/Notice'
import { Skeleton } from '../ui/Skeleton'
import { StatusBadge } from '../ui/StatusBadge'
import { displayEmployeeName } from './employeeName'
import styles from './RoomOperations.module.css'
import {
  neededActionLabelKey,
  priorityLabelKey,
  priorityTone,
  priorityVariant,
  readinessLabelKey,
  readinessTone,
  workStateTone,
} from './readiness'
import { canReadRoomOperations } from './roomOperationsAccess'
import { listRooms, roomOperationsErrorKey, type RoomOperationsListItem } from './roomOperationsApi'

export function RoomOperationsPage() {
  const { t } = useTranslation()
  const { user } = useAuthSession()
  const [rooms, setRooms] = useState<RoomOperationsListItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function load() {
      try {
        const rows = await listRooms()
        if (!cancelled) {
          setRooms(rows)
        }
      } catch (reason) {
        if (!cancelled) {
          setError(t(roomOperationsErrorKey(reason)))
          setRooms([])
        }
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [t])

  if (!canReadRoomOperations(user)) {
    return <Notice tone="danger">{t('roomOperations.noAccess')}</Notice>
  }

  return (
    <div className={styles.page}>
      {error ? <Notice tone="danger">{error}</Notice> : null}

      {rooms === null ? (
        <Skeleton variant="list" rows={6} label={t('roomOperations.loading')} />
      ) : rooms.length === 0 ? (
        <div className={styles.list}>
          <EmptyState title={t('roomOperations.empty')} description={t('roomOperations.emptyHint')} />
        </div>
      ) : (
        <div className={styles.list}>
          <div className={styles.table} role="table" aria-label={t('roomOperations.title')}>
            <div className={`${styles.row} ${styles.opsRow} ${styles.head}`} role="row">
              <span className={styles.roomCell} role="columnheader">
                {t('roomOperations.room')}
              </span>
              <span className={styles.readinessCell} role="columnheader">
                {t('roomOperations.readinessLabel')}
              </span>
              <span className={styles.personCell} role="columnheader">
                {t('roomOperations.assignedEmployee')}
              </span>
              <span className={styles.priorityCell} role="columnheader">
                {t('roomOperations.priorityLabel')}
              </span>
              <span className={styles.workCell} role="columnheader">
                {t('roomOperations.workState')}
              </span>
              <span className={styles.actionCell} role="columnheader">
                {t('roomOperations.actionNeeded')}
              </span>
            </div>

            {rooms.map((room) => (
              <RoomRow key={room.id} room={room} />
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

function rowKind(room: RoomOperationsListItem): string {
  if (room.readiness === 'Dirty' && room.currentWorkOrigin === 'Rework') {
    return styles.rowRework
  }

  if (room.readiness === 'Dirty' && room.currentWorkState === 'Open') {
    return styles.rowCleaning
  }

  if (room.readiness === 'Dirty') {
    return styles.rowDirty
  }

  if (room.readiness === 'Clean') {
    return styles.rowInspect
  }

  if (room.readiness === 'Inspected') {
    return styles.rowInspected
  }

  if (room.readiness === 'Ready') {
    return styles.rowReady
  }

  return ''
}

function RoomRow({ room }: { room: RoomOperationsListItem }) {
  const { t } = useTranslation()
  const personName = displayEmployeeName(room.assignedEmployeeName)
  const personLabel = personName ?? t('roomOperations.unassigned')
  const readinessLabel = t(readinessLabelKey(room.readiness))
  const priorityLabel = room.priority ? t(priorityLabelKey(room.priority)) : t('roomOperations.noPriority')
  const workLabel = room.currentWorkState
    ? t(`roomOperations.work.${room.currentWorkState}`)
    : t('roomOperations.noWork')
  const actionLabel = t(neededActionLabelKey(room.neededAction))
  const needsAction = room.neededAction !== 'none'

  return (
    <Link
      to={`/app/room-operations/${room.id}`}
      className={`${styles.row} ${styles.opsRow} ${styles.rowLink} ${rowKind(room)} ${needsAction ? styles.rowNeedsAction : ''}`}
      role="row"
      aria-label={t('roomOperations.rowSummary', {
        number: room.number,
        readiness: readinessLabel,
        person: personLabel,
        priority: priorityLabel,
        work: workLabel,
        action: actionLabel,
      })}
    >
      <span className={styles.roomCell} role="cell">
        <span className={styles.cellLabel}>{t('roomOperations.room')}</span>
        <span className={styles.roomNumber}>{room.number}</span>
      </span>
      <span className={styles.readinessCell} role="cell">
        <span className={styles.cellLabel}>{t('roomOperations.readinessLabel')}</span>
        <span className={styles.readinessStack}>
          <StatusBadge tone={readinessTone(room.readiness)} className={styles.chip} title={readinessLabel}>
            {readinessLabel}
          </StatusBadge>
          <span className={`${styles.readinessMeter} ${styles[`meter${room.readiness}`]}`} aria-hidden="true" />
        </span>
      </span>
      <span className={styles.personCell} role="cell">
        <span className={styles.cellLabel}>{t('roomOperations.assignedEmployee')}</span>
        {personName ? (
          <span className={styles.personName} title={personName}>
            {personName}
          </span>
        ) : (
          <span className={`${styles.muted} ${styles.truncate}`}>{personLabel}</span>
        )}
      </span>
      <span className={styles.priorityCell} role="cell">
        <span className={styles.cellLabel}>{t('roomOperations.priorityLabel')}</span>
        {room.priority ? (
          <StatusBadge
            tone={priorityTone(room.priority)}
            variant={priorityVariant(room.priority)}
            className={styles.chip}
            title={priorityLabel}
          >
            {priorityLabel}
          </StatusBadge>
        ) : (
          <span className={styles.muted}>{priorityLabel}</span>
        )}
      </span>
      <span className={styles.workCell} role="cell">
        <span className={styles.cellLabel}>{t('roomOperations.workState')}</span>
        <StatusBadge
          tone={workStateTone(room.currentWorkState)}
          variant="outline"
          className={styles.chip}
          title={workLabel}
        >
          {workLabel}
        </StatusBadge>
      </span>
      <span className={styles.actionCell} role="cell">
        <span className={styles.cellLabel}>{t('roomOperations.actionNeeded')}</span>
        <span className={styles.actionZone}>
          <span className={`${styles.actionText} ${needsAction ? '' : styles.actionQuiet}`}>{actionLabel}</span>
          <ChevronRightIcon className={styles.actionChevron} />
        </span>
      </span>
    </Link>
  )
}
