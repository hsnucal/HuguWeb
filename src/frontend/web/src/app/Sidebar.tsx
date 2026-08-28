import type { Ref } from 'react'
import { NavLink } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { canOpenSettings } from '../authorization/authorizationAccess'
import { DEFAULT_LANGUAGE, languageNativeName, toAppLanguage } from '../i18n/languages'
import { canReadRoomOperations } from '../room-operations/roomOperationsAccess'
import { canReadMaintenance } from '../technical-service/maintenanceAccess'
import { AvatarMark } from '../ui/AvatarMark'
import { BrandMark } from '../ui/BrandMark'
import { Button } from '../ui/Button'
import { LanguageSelect } from '../ui/LanguageSelect'
import { Tooltip } from '../ui/Tooltip'
import { canReadHrEmployees } from '../workforce/hrAccess'
import { canReadWorkforce } from '../workforce/workforceAccess'
import {
  CloseIcon,
  HomeIcon,
  PeopleIcon,
  ReservationsIcon,
  RoomsIcon,
  SettingsIcon,
  SignOutIcon,
  TasksIcon,
  WrenchIcon,
} from '../ui/icons'
import { buildPrimaryNav, buildSettingsNav, type SidebarIconId, type SidebarNavItem } from './sidebarNav'
import styles from './Sidebar.module.css'

const icons: Record<SidebarIconId, typeof HomeIcon> = {
  home: HomeIcon,
  rooms: RoomsIcon,
  wrench: WrenchIcon,
  people: PeopleIcon,
  reservations: ReservationsIcon,
  tasks: TasksIcon,
  settings: SettingsIcon,
}

export function Sidebar({
  userLabel,
  railCollapsed,
  isDrawer,
  drawerOpen,
  onSignOut,
  onToggleCollapsed,
  onCloseDrawer,
  onNavigate,
  ref,
}: {
  userLabel: string
  railCollapsed: boolean
  isDrawer: boolean
  drawerOpen: boolean
  onSignOut: () => void
  onToggleCollapsed: () => void
  onCloseDrawer: () => void
  onNavigate: () => void
  ref?: Ref<HTMLElement>
}) {
  const { t, i18n } = useTranslation()
  const { user, preferenceError, updatePreferredLanguage } = useAuthSession()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const languageName = languageNativeName(language)
  const languageLabel = t('common.languageCurrent', { name: languageName })
  const primaryNav = buildPrimaryNav({
    canReadRoomOperations: canReadRoomOperations(user),
    canReadMaintenance: canReadMaintenance(user),
    canReadHrEmployees: canReadHrEmployees(user),
    canReadWorkforce: canReadWorkforce(user),
  })
  const settingsNav = buildSettingsNav(canOpenSettings(user))
  const hiddenFromTree = isDrawer && !drawerOpen

  return (
    <aside
      ref={ref}
      id="app-sidebar"
      className={`${styles.sidebar} ${railCollapsed ? styles.collapsed : ''}`.trim()}
      aria-label={t('navigation.application')}
      aria-hidden={hiddenFromTree || undefined}
      aria-modal={isDrawer && drawerOpen ? true : undefined}
      role={isDrawer && drawerOpen ? 'dialog' : undefined}
      inert={hiddenFromTree || undefined}
      tabIndex={isDrawer ? -1 : undefined}
    >
      <div className={styles.brand}>
        {isDrawer ? (
          <>
            <div className={styles.brandIdentity}>
              <BrandMark tone="inverse" />
              <span className={styles.wordmark}>HuGu</span>
            </div>
            <button
              type="button"
              className={styles.drawerClose}
              aria-label={t('navigation.closeMenu')}
              onClick={onCloseDrawer}
            >
              <CloseIcon />
            </button>
          </>
        ) : (
          <div className={styles.brandToggleWrap}>
            <button
              type="button"
              className={styles.brandToggle}
              aria-label="HuGu"
              aria-expanded={!railCollapsed}
              onClick={onToggleCollapsed}
            >
              <BrandMark tone="inverse" />
              <span className={styles.wordmark}>HuGu</span>
            </button>
          </div>
        )}
      </div>

      <nav className={styles.nav} aria-label={t('navigation.primary')}>
        {primaryNav.map((item) => (
          <SidebarNavEntry
            key={item.id}
            item={item}
            label={t(item.labelKey)}
            railCollapsed={railCollapsed}
            onNavigate={onNavigate}
          />
        ))}
      </nav>

      <div className={styles.footer}>
        <SidebarNavEntry
          item={settingsNav}
          label={t(settingsNav.labelKey)}
          railCollapsed={railCollapsed}
          onNavigate={onNavigate}
        />

        <Tooltip label={userLabel} enabled={railCollapsed}>
          <div className={styles.account}>
            <AvatarMark name={userLabel} size="sm" tone="onBrand" />
            <div className={styles.accountCopy}>
              <span className={styles.accountName}>{userLabel}</span>
              <span className={styles.accountHint}>{t('auth.signedIn')}</span>
            </div>
          </div>
        </Tooltip>

        <Tooltip label={languageLabel} enabled={railCollapsed}>
          <LanguageSelect
            id={isDrawer ? 'app-language-drawer' : 'app-language'}
            className={styles.language}
            tone="onBrand"
            compact={railCollapsed}
            onChange={(nextLanguage) => void updatePreferredLanguage(nextLanguage)}
          />
        </Tooltip>

        {preferenceError ? (
          <p className={styles.preferenceError} role="alert">
            {preferenceError === 'reverted'
              ? t('common.preferenceSaveFailed')
              : t('common.preferenceSaveFailedKeep')}
          </p>
        ) : null}

        <Tooltip label={t('auth.signOut')} enabled={railCollapsed}>
          <Button
            className={styles.signOut}
            variant="ghost"
            aria-label={railCollapsed ? t('auth.signOut') : undefined}
            onClick={onSignOut}
          >
            <SignOutIcon />
            <span className={styles.signOutLabel}>{t('auth.signOut')}</span>
          </Button>
        </Tooltip>
      </div>
    </aside>
  )
}

function SidebarNavEntry({
  item,
  label,
  railCollapsed,
  onNavigate,
}: {
  item: SidebarNavItem
  label: string
  railCollapsed: boolean
  onNavigate: () => void
}) {
  const { t } = useTranslation()
  const Icon = icons[item.icon]
  const unavailable = t('navigation.unavailable', { label })
  const body = (
    <>
      <span className={styles.icon}>
        <Icon />
      </span>
      <span className={styles.label} aria-hidden={railCollapsed || undefined}>
        {label}
      </span>
    </>
  )

  if (item.destination.kind === 'future') {
    return (
      <Tooltip label={label} enabled={railCollapsed}>
        <span className={styles.future} aria-disabled="true" aria-label={unavailable}>
          {body}
        </span>
      </Tooltip>
    )
  }

  const { to, end } = item.destination

  return (
    <Tooltip label={label} enabled={railCollapsed}>
      <NavLink
        to={to}
        end={end}
        aria-label={railCollapsed ? label : undefined}
        className={({ isActive }) => (isActive ? styles.current : styles.item)}
        onClick={onNavigate}
      >
        {body}
      </NavLink>
    </Tooltip>
  )
}
