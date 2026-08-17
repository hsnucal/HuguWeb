import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { BrandMark } from '../ui/BrandMark'
import { Button } from '../ui/Button'
import { LanguageSelect } from '../ui/LanguageSelect'
import {
  HomeIcon,
  ReservationsIcon,
  RoomsIcon,
  SettingsIcon,
  SignOutIcon,
  TasksIcon,
} from '../ui/icons'
import styles from './Sidebar.module.css'

const futureNav = [
  { id: 'rooms', labelKey: 'navigation.rooms', icon: RoomsIcon },
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
  const { preferenceError, updatePreferredLanguage } = useAuthSession()

  return (
    <aside className={styles.sidebar} aria-label={t('navigation.application')}>
      <div className={styles.brand}>
        <BrandMark />
        <span className={styles.wordmark}>HuGuWeb</span>
      </div>

      <nav className={styles.nav} aria-label={t('navigation.primary')}>
        <span className={styles.current} aria-current="page">
          <HomeIcon />
          {t('navigation.home')}
        </span>

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
              <Icon />
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
          <SettingsIcon />
          {t('navigation.settings')}
        </span>

        <div className={styles.account}>
          <span className={styles.accountName}>{userLabel}</span>
          <span className={styles.accountHint}>{t('auth.signedIn')}</span>
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
