import { NavLink, Outlet, useLocation } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { Notice } from '../ui/Notice'
import styles from './Workforce.module.css'
import { canReadWorkforce } from './workforceAccess'

export function WorkforceLayout() {
  const { t } = useTranslation()
  const { user } = useAuthSession()
  const location = useLocation()
  const directoryCurrent =
    location.pathname === '/app/workforce'
    || location.pathname.startsWith('/app/workforce/employees/')

  if (!canReadWorkforce(user)) {
    return <Notice tone="danger">{t('workforce.noAccess')}</Notice>
  }

  return (
    <div className={styles.layout}>
      <nav className={styles.subnav} aria-label={t('workforce.title')}>
        <NavLink to="/app/workforce" end aria-current={directoryCurrent ? 'page' : undefined}>
          {t('workforce.directory')}
        </NavLink>
        <NavLink to="/app/workforce/departments">{t('workforce.departments')}</NavLink>
        <NavLink to="/app/workforce/positions">{t('workforce.positions')}</NavLink>
      </nav>
      <Outlet />
    </div>
  )
}
