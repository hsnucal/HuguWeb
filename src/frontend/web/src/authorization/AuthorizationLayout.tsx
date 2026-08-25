import { NavLink, Outlet } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { canManageAuthorizationRoles, canManageAuthorizationUsers } from './authorizationAccess'
import styles from '../workforce/Workforce.module.css'

export function AuthorizationLayout() {
  const { t } = useTranslation()
  const { user } = useAuthSession()
  const showUsers = canManageAuthorizationUsers(user)
  const showRoles = canManageAuthorizationRoles(user)

  return (
    <div className={styles.layout}>
      <nav className={styles.subnav} aria-label={t('navigation.settings')}>
        {showUsers ? (
          <NavLink to="/app/settings/users" end>
            {t('authorization.users')}
          </NavLink>
        ) : null}
        {showRoles ? (
          <NavLink to="/app/settings/roles">{t('authorization.roles')}</NavLink>
        ) : null}
      </nav>
      <Outlet />
    </div>
  )
}
