import { NavLink, Outlet } from 'react-router'
import { useTranslation } from 'react-i18next'
import styles from './Workforce.module.css'

export function WorkforceLayout() {
  const { t } = useTranslation()

  return (
    <div className={styles.page}>
      <nav className={styles.subnav} aria-label={t('workforce.title')}>
        <NavLink to="/app/workforce" end>
          {t('workforce.active')}
        </NavLink>
        <NavLink to="/app/workforce/departments">{t('workforce.departments')}</NavLink>
        <NavLink to="/app/workforce/positions">{t('workforce.positions')}</NavLink>
      </nav>
      <Outlet />
    </div>
  )
}
