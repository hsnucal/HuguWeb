import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { Button } from '../ui/Button'
import { LanguageSelect } from '../ui/LanguageSelect'
import { OperationsCenter } from './OperationsCenter'
import { Sidebar } from './Sidebar'
import { TopBar } from './TopBar'
import styles from './AppShell.module.css'

export function AppShell() {
  const { t } = useTranslation()
  const { user, preferenceError, signOut, updatePreferredLanguage } = useAuthSession()
  const userLabel = user?.email ?? user?.id ?? t('auth.signedIn')

  async function onLogout() {
    await signOut()
  }

  return (
    <div className={styles.shell}>
      <a className="skip-link" href="#main">
        {t('common.skipToContent')}
      </a>

      <div className={styles.sidebar}>
        <Sidebar userLabel={userLabel} onSignOut={() => void onLogout()} />
      </div>

      <div className={styles.content}>
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

        <TopBar title={t('operations.title')} subtitle={t('operations.intro')} />

        <main className={styles.main} id="main" tabIndex={-1}>
          <OperationsCenter />
        </main>
      </div>
    </div>
  )
}
