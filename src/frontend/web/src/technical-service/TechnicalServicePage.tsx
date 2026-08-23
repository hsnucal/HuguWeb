import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { formatDateFromIso } from '../i18n/format'
import type { AppLanguage } from '../i18n/languages'
import { DEFAULT_LANGUAGE, toAppLanguage } from '../i18n/languages'
import { Button } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { ChevronRightIcon } from '../ui/icons'
import { Notice } from '../ui/Notice'
import { Skeleton } from '../ui/Skeleton'
import { StatusBadge } from '../ui/StatusBadge'
import { displayEmployeeName } from '../room-operations/employeeName'
import {
  neededActionLabelKey,
  priorityLabelKey,
  priorityTone,
  statusLabelKey,
  statusTone,
} from './labels'
import { canManageMaintenance, canReadMaintenance } from './maintenanceAccess'
import { listIssues, maintenanceErrorKey, type MaintenanceIssueListItem } from './maintenanceApi'
import styles from './TechnicalService.module.css'

export function TechnicalServicePage() {
  const { t, i18n } = useTranslation()
  const { user } = useAuthSession()
  const navigate = useNavigate()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const canManage = canManageMaintenance(user)
  const [issues, setIssues] = useState<MaintenanceIssueListItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function load() {
      try {
        const rows = await listIssues()
        if (!cancelled) {
          setIssues(rows)
        }
      } catch (reason) {
        if (!cancelled) {
          setError(t(maintenanceErrorKey(reason)))
          setIssues([])
        }
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [t])

  if (!canReadMaintenance(user)) {
    return <Notice tone="danger">{t('maintenance.noAccess')}</Notice>
  }

  return (
    <div className={styles.page}>
      {canManage ? (
        <div className={styles.toolbar}>
          <Button layout="inline" onClick={() => navigate('/app/technical-service/new')}>
            {t('maintenance.create')}
          </Button>
        </div>
      ) : null}

      {error ? <Notice tone="danger">{error}</Notice> : null}

      {issues === null ? (
        <Skeleton variant="list" rows={6} label={t('maintenance.loading')} />
      ) : issues.length === 0 ? (
        <div className={styles.list}>
          <EmptyState title={t('maintenance.empty')} description={t('maintenance.emptyHint')} />
        </div>
      ) : (
        <div className={styles.list}>
          <div className={styles.table} role="table" aria-label={t('maintenance.title')}>
            <div className={`${styles.row} ${styles.opsRow} ${styles.head}`} role="row">
              <span className={styles.roomCell} role="columnheader">
                {t('maintenance.room')}
              </span>
              <span className={styles.issueCell} role="columnheader">
                {t('maintenance.issue')}
              </span>
              <span className={styles.priorityCell} role="columnheader">
                {t('maintenance.priorityLabel')}
              </span>
              <span className={styles.personCell} role="columnheader">
                {t('maintenance.assigned')}
              </span>
              <span className={styles.statusCell} role="columnheader">
                {t('maintenance.statusLabel')}
              </span>
              <span className={styles.actionCell} role="columnheader">
                {t('maintenance.nextAction')}
              </span>
            </div>
            {issues.map((issue) => (
              <IssueRow key={issue.id} issue={issue} language={language} />
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

function rowKind(status: MaintenanceIssueListItem['status']) {
  switch (status) {
    case 'InProgress':
      return styles.rowProgress
    case 'UnableToResolve':
      return styles.rowUnable
    case 'Resolved':
      return styles.rowResolved
    default:
      return styles.rowOpen
  }
}

function IssueRow({
  issue,
  language,
}: {
  issue: MaintenanceIssueListItem
  language: AppLanguage
}) {
  const { t } = useTranslation()
  const personName = displayEmployeeName(issue.assignedEmployeeName)
  const personLabel = personName ?? t('maintenance.unassigned')
  const statusLabel = t(statusLabelKey(issue.status))
  const priorityLabel = t(priorityLabelKey(issue.priority))
  const actionLabel = t(neededActionLabelKey(issue.neededAction))

  return (
    <Link
      to={`/app/technical-service/${issue.id}`}
      className={`${styles.row} ${styles.opsRow} ${styles.rowLink} ${rowKind(issue.status)}`}
      role="row"
      aria-label={t('maintenance.rowSummary', {
        room: issue.roomNumber,
        issue: issue.description,
        priority: priorityLabel,
        person: personLabel,
        status: statusLabel,
        action: actionLabel,
      })}
    >
      <span className={styles.roomCell} role="cell">
        <span className={styles.cellLabel}>{t('maintenance.room')}</span>
        <span className={styles.roomNumber}>{issue.roomNumber}</span>
      </span>
      <span className={styles.issueCell} role="cell">
        <span className={styles.cellLabel}>{t('maintenance.issue')}</span>
        <span className={styles.issueText} title={issue.description}>
          {issue.description}
        </span>
        <span className={styles.issueMeta}>
          {issue.categoryName}
          {' · '}
          {formatDateFromIso(issue.createdAt, language)}
        </span>
      </span>
      <span className={styles.priorityCell} role="cell">
        <span className={styles.cellLabel}>{t('maintenance.priorityLabel')}</span>
        <StatusBadge tone={priorityTone(issue.priority)} variant="priority" className={styles.chip} title={priorityLabel}>
          {priorityLabel}
        </StatusBadge>
      </span>
      <span className={styles.personCell} role="cell">
        <span className={styles.cellLabel}>{t('maintenance.assigned')}</span>
        {personName ? (
          <span className={styles.personName} title={personName}>
            {personName}
          </span>
        ) : (
          <span className={`${styles.muted} ${styles.truncate}`}>{personLabel}</span>
        )}
      </span>
      <span className={styles.statusCell} role="cell">
        <span className={styles.cellLabel}>{t('maintenance.statusLabel')}</span>
        <StatusBadge tone={statusTone(issue.status)} className={styles.chip} title={statusLabel}>
          {statusLabel}
        </StatusBadge>
      </span>
      <span className={styles.actionCell} role="cell">
        <span className={styles.cellLabel}>{t('maintenance.nextAction')}</span>
        <span className={styles.actionZone}>
          <span className={`${styles.actionText} ${issue.neededAction === 'none' ? styles.actionQuiet : ''}`}>
            {actionLabel}
          </span>
          <ChevronRightIcon className={styles.actionChevron} />
        </span>
      </span>
    </Link>
  )
}
