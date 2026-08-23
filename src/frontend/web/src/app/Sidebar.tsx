import { NavLink } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { AvatarMark } from '../ui/AvatarMark'
import { BrandMark } from '../ui/BrandMark'
import { Button } from '../ui/Button'
import { LanguageSelect } from '../ui/LanguageSelect'
import { canReadWorkforce } from '../workforce/workforceAccess'
import { canReadRoomOperations } from '../room-operations/roomOperationsAccess'
import { canReadMaintenance } from '../technical-service/maintenanceAccess'
import {
  HomeIcon,
  PeopleIcon,
  ReservationsIcon,
  RoomsIcon,
  SettingsIcon,
  SignOutIcon,
  TasksIcon,
  WrenchIcon,
} from '../ui/icons'
import styles from './Sidebar.module.css'

const futureNav = [
  { id: 'reservations', labelKey: 'navigation.reservations', icon: ReservationsIcon },
  { id: 'tasks', labelKey: 'navigation.tasks', icon: TasksIcon },
] as const

export function Sidebar({
  userLabel,
  onSignOut,
}: {
  userLabel: string
  onSignOut: () => void
}) {
  const { t } = useTranslation()
  const { user, preferenceError, updatePreferredLanguage } = useAuthSession()
  const showWorkforce = canReadWorkforce(user)
  const showRoomOperations = canReadRoomOperations(user)
  const showTechnicalService = canReadMaintenance(user)

  return (
    <aside className={styles.sidebar} aria-label={t('navigation.application')}>
      <div className={styles.brand}>
        <BrandMark />
        <span className={styles.wordmark}>HuGuWeb</span>
      </div>

      <nav className={styles.nav} aria-label={t('navigation.primary')}>
        <NavLink
          to="/app"
          end
          className={({ isActive }) => (isActive ? styles.current : styles.item)}
        >
          <span className={styles.icon}>
            <HomeIcon />
          </span>
          {t('navigation.home')}
        </NavLink>

        {showRoomOperations ? (
          <NavLink
            to="/app/room-operations"
            className={({ isActive }) => (isActive ? styles.current : styles.item)}
          >
            <span className={styles.icon}>
              <RoomsIcon />
            </span>
            {t('navigation.roomOperations')}
          </NavLink>
        ) : null}

        {showTechnicalService ? (
          <NavLink
            to="/app/technical-service"
            className={({ isActive }) => (isActive ? styles.current : styles.item)}
          >
            <span className={styles.icon}>
              <WrenchIcon />
            </span>
            {t('navigation.technicalService')}
          </NavLink>
        ) : null}

        {showWorkforce ? (
          <NavLink
            to="/app/workforce"
            className={({ isActive }) => (isActive ? styles.current : styles.item)}
          >
            <span className={styles.icon}>
              <PeopleIcon />
            </span>
            {t('navigation.workforce')}
          </NavLink>
        ) : null}

        {futureNav.map((item) => {
          const Icon = item.icon
          const label = t(item.labelKey)
          return (
            <span
              key={item.id}
              className={styles.future}
              aria-disabled="true"
              aria-label={t('navigation.unavailable', { label })}
            >
              <span className={styles.icon}>
                <Icon />
              </span>
              {label}
            </span>
          )
        })}
      </nav>

      <div className={styles.footer}>
        <span
          className={styles.future}
          aria-disabled="true"
          aria-label={t('navigation.unavailable', { label: t('navigation.settings') })}
        >
          <span className={styles.icon}>
            <SettingsIcon />
          </span>
          {t('navigation.settings')}
        </span>

        <div className={styles.account}>
          <AvatarMark name={userLabel} size="sm" />
          <div className={styles.accountCopy}>
            <span className={styles.accountName}>{userLabel}</span>
            <span className={styles.accountHint}>{t('auth.signedIn')}</span>
          </div>
        </div>

        <LanguageSelect
          id="app-language"
          className={styles.language}
          onChange={(language) => void updatePreferredLanguage(language)}
        />

        {preferenceError ? (
          <p className={styles.preferenceError} role="alert">
            {preferenceError === 'reverted'
              ? t('common.preferenceSaveFailed')
              : t('common.preferenceSaveFailedKeep')}
          </p>
        ) : null}

        <Button className={styles.signOut} variant="ghost" onClick={onSignOut}>
          <SignOutIcon />
          {t('auth.signOut')}
        </Button>
      </div>
    </aside>
  )
}
