import { NavLink, Navigate, Outlet, useLocation } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { Notice } from '../ui/Notice'
import styles from './Workforce.module.css'
import { canReadHrEmployees } from './hrAccess'
import { canReadWorkforce } from './workforceAccess'

export function WorkforceLayout() {
  const { t } = useTranslation()
  const { user } = useAuthSession()
  const location = useLocation()
  const canReadHr = canReadHrEmployees(user)
  const canReadStructure = canReadWorkforce(user)
  const directoryCurrent = location.pathname === '/app/workforce'

  if (!canReadHr && !canReadStructure) {
    return <Notice tone="danger">{t('workforce.noAccess')}</Notice>
  }

  if (directoryCurrent && !canReadHr && canReadStructure) {
    return <Navigate to="/app/workforce/departments" replace />
  }

  return (
    <div className={styles.layout}>
      <nav className={styles.subnav} aria-label={t('workforce.title')}>
        {canReadHr ? (
          <NavLink to="/app/workforce" end aria-current={directoryCurrent ? 'page' : undefined}>
            {t('workforce.directory')}
          </NavLink>
        ) : null}
        {canReadStructure ? (
          <>
            <NavLink to="/app/workforce/departments">{t('workforce.departments')}</NavLink>
            <NavLink to="/app/workforce/positions">{t('workforce.positions')}</NavLink>
            <NavLink to="/app/workforce/official-settings">{t('workforce.officialSettings')}</NavLink>
          </>
        ) : null}
      </nav>
      <Outlet />
    </div>
  )
}
