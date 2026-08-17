import { BrandMark } from '../ui/BrandMark'
import { Button } from '../ui/Button'
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
  { id: 'rooms', label: 'Rooms', icon: RoomsIcon },
  { id: 'reservations', label: 'Reservations', icon: ReservationsIcon },
  { id: 'tasks', label: 'Tasks', icon: TasksIcon },
] as const

export function Sidebar({
  userLabel,
  onSignOut,
}: {
  userLabel: string
  onSignOut: () => void
}) {
  return (
    <aside className={styles.sidebar} aria-label="Application">
      <div className={styles.brand}>
        <BrandMark />
        <span className={styles.wordmark}>HuGuWeb</span>
      </div>

      <nav className={styles.nav} aria-label="Primary">
        <span className={styles.current} aria-current="page">
          <HomeIcon />
          Home
        </span>

        {futureNav.map((item) => {
          const Icon = item.icon
          return (
            <span
              key={item.id}
              className={styles.future}
              aria-disabled="true"
              aria-label={`${item.label}, not available yet`}
            >
              <Icon />
              {item.label}
            </span>
          )
        })}
      </nav>

      <div className={styles.footer}>
        <span className={styles.future} aria-disabled="true" aria-label="Settings, not available yet">
          <SettingsIcon />
          Settings
        </span>

        <div className={styles.account}>
          <span className={styles.accountName}>{userLabel}</span>
          <span className={styles.accountHint}>Signed in</span>
        </div>

        <Button className={styles.signOut} variant="ghost" onClick={onSignOut}>
          <SignOutIcon />
          Sign out
        </Button>
      </div>
    </aside>
  )
}
