import { useAuthSession } from '../auth/AuthContext'
import { Button } from '../ui/Button'
import { OperationsCenter } from './OperationsCenter'
import { Sidebar } from './Sidebar'
import { TopBar } from './TopBar'
import styles from './AppShell.module.css'

export function AppShell() {
  const { user, signOut } = useAuthSession()
  const userLabel = user?.email ?? user?.id ?? 'Signed in'

  async function onLogout() {
    await signOut()
  }

  return (
    <div className={styles.shell}>
      <a className="skip-link" href="#main">
        Skip to content
      </a>

      <div className={styles.sidebar}>
        <Sidebar userLabel={userLabel} onSignOut={() => void onLogout()} />
      </div>

      <div className={styles.content}>
        <div className={styles.mobileBar}>
          <span className={styles.mobileBrand}>HuGuWeb</span>
          <span className={styles.mobileUser}>{userLabel}</span>
          <Button variant="ghost" onClick={() => void onLogout()}>
            Sign out
          </Button>
        </div>

        <TopBar
          title="Operations Center"
          subtitle="Here's what needs your attention today."
        />

        <main className={styles.main} id="main" tabIndex={-1}>
          <OperationsCenter />
        </main>
      </div>
    </div>
  )
}
