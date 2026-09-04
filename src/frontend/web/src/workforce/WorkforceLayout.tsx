import { NavLink, Navigate, Outlet, useLocation } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { Notice } from '../ui/Notice'
import styles from './Workforce.module.css'
import {
  canReadHrEmployees,
  canReadHrLeave,
  canReadHrMovements,
  canReadHrSchedule,
  canReadHrShiftDefinitions,
} from './hrAccess'
import { canReadWorkforce } from './workforceAccess'
import { buildWorkforceSubnav } from './workforceSubnav'

export function WorkforceLayout() {
  const { t } = useTranslation()
  const { user } = useAuthSession()
  const location = useLocation()
  const canReadHr = canReadHrEmployees(user)
  const canReadLeave = canReadHrLeave(user)
  const canReadShiftDefinitions = canReadHrShiftDefinitions(user)
  const canReadSchedule = canReadHrSchedule(user)
  const canReadMovements = canReadHrMovements(user)
  const canReadStructure = canReadWorkforce(user)
  const directoryCurrent = location.pathname === '/app/workforce'
  const subnav = buildWorkforceSubnav({
    canReadHrEmployees: canReadHr,
    canReadWorkforce: canReadStructure,
    canReadHrLeave: canReadLeave,
    canReadHrShiftDefinitions: canReadShiftDefinitions,
    canReadHrSchedule: canReadSchedule,
  })

  if (
    !canReadHr &&
    !canReadStructure &&
    !canReadLeave &&
    !canReadShiftDefinitions &&
    !canReadSchedule &&
    !canReadMovements
  ) {
    return <Notice tone="danger">{t('workforce.noAccess')}</Notice>
  }

  if (directoryCurrent && !canReadHr) {
    if (canReadLeave) {
      return <Navigate to="/app/workforce/leave-management" replace />
    }

    if (canReadShiftDefinitions) {
      return <Navigate to="/app/workforce/shift-definitions" replace />
    }

    if (canReadSchedule) {
      return <Navigate to="/app/workforce/shift-plan" replace />
    }

    if (canReadMovements) {
      return <Navigate to="/app/workforce/movements" replace />
    }

    if (canReadStructure) {
      return <Navigate to="/app/workforce/departments" replace />
    }
  }

  return (
    <div className={styles.layout}>
      <nav className={styles.subnav} aria-label={t('workforce.title')}>
        {subnav.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            aria-current={item.end && directoryCurrent ? 'page' : undefined}
          >
            {t(item.labelKey)}
          </NavLink>
        ))}
      </nav>
      <Outlet />
    </div>
  )
}
