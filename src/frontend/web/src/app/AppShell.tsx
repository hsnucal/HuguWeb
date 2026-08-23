import { Outlet, useLocation } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { Button } from '../ui/Button'
import { LanguageSelect } from '../ui/LanguageSelect'
import { Sidebar } from './Sidebar'
import { TopBar } from './TopBar'
import styles from './AppShell.module.css'

export function AppShell() {
  const { t } = useTranslation()
  const location = useLocation()
  const { user, preferenceError, signOut, updatePreferredLanguage } = useAuthSession()
  const userLabel = user?.email ?? user?.id ?? t('auth.signedIn')
  const heading = headingFor(location.pathname, t)

  async function onLogout() {
    await signOut()
  }

  return (
    <div className={styles.shell}>
      <a className="skip-link" href="#main">
        {t('common.skipToContent')}
      </a>

      <Sidebar userLabel={userLabel} onSignOut={() => void onLogout()} />

      <div className={styles.workspace}>
        <div className={styles.mobileBar}>
          <span className={styles.mobileBrand}>HuGuWeb</span>
          <span className={styles.mobileUser}>{userLabel}</span>
          <LanguageSelect
            id="mobile-language"
            className={styles.mobileLanguage}
            onChange={(language) => void updatePreferredLanguage(language)}
          />
          <Button variant="ghost" onClick={() => void onLogout()}>
            {t('auth.signOut')}
          </Button>
        </div>

        {preferenceError ? (
          <p className={styles.mobilePreferenceError} role="alert">
            {preferenceError === 'reverted'
              ? t('common.preferenceSaveFailed')
              : t('common.preferenceSaveFailedKeep')}
          </p>
        ) : null}

        <TopBar kicker={heading.kicker} title={heading.title} subtitle={heading.subtitle} />

        <main className={styles.main} id="main" tabIndex={-1}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}

function headingFor(pathname: string, t: (key: string) => string) {
  if (pathname.startsWith('/app/workforce/departments')) {
    return {
      kicker: t('workforce.title'),
      title: t('workforce.departments'),
      subtitle: t('workforce.departmentsIntro'),
    }
  }

  if (pathname.startsWith('/app/workforce/positions')) {
    return {
      kicker: t('workforce.title'),
      title: t('workforce.positions'),
      subtitle: t('workforce.positionsIntro'),
    }
  }

  if (pathname.startsWith('/app/workforce/employees/')) {
    return { kicker: t('workforce.title'), title: t('workforce.title'), subtitle: t('workforce.intro') }
  }

  if (pathname.startsWith('/app/workforce')) {
    return { kicker: t('navigation.workforce'), title: t('workforce.title'), subtitle: t('workforce.intro') }
  }

  if (pathname.startsWith('/app/room-operations')) {
    return {
      kicker: t('navigation.roomOperations'),
      title: t('roomOperations.title'),
      subtitle: t('roomOperations.intro'),
    }
  }

  return {
    kicker: t('navigation.home'),
    title: t('operations.title'),
    subtitle: t('operations.intro'),
  }
}
