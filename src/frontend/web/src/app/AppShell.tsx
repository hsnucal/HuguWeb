import { useEffect, useRef, useState } from 'react'
import { Outlet, useLocation } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { BrandMark } from '../ui/BrandMark'
import { Button } from '../ui/Button'
import { LanguageSelect } from '../ui/LanguageSelect'
import { Notice } from '../ui/Notice'
import { CloseIcon, MenuIcon } from '../ui/icons'
import { Sidebar } from './Sidebar'
import { TopBar } from './TopBar'
import { PropertySelect } from './PropertySelect'
import { resolveSidebarChrome } from './sidebarChrome'
import { useNarrowViewport, useSidebarCollapsedPreference } from './useSidebarLayout'
import styles from './AppShell.module.css'

export function AppShell() {
  const { t } = useTranslation()
  const location = useLocation()
  const { user, preferenceError, signOut, updatePreferredLanguage } = useAuthSession()
  const userLabel = user?.email ?? user?.id ?? t('auth.signedIn')
  const heading = headingFor(location.pathname, t)
  const isNarrow = useNarrowViewport()
  const { collapsed, toggleSidebarCollapsed } = useSidebarCollapsedPreference()
  const chrome = resolveSidebarChrome({
    collapsedPreference: collapsed,
    isNarrowViewport: isNarrow,
  })
  const [drawerOpen, setDrawerOpen] = useState(false)
  const sidebarRef = useRef<HTMLElement>(null)
  const menuButtonRef = useRef<HTMLButtonElement>(null)
  const drawerVisible = chrome.isDrawer && drawerOpen

  async function onLogout() {
    await signOut()
  }

  function closeDrawer() {
    setDrawerOpen(false)
  }

  useEffect(() => {
    if (!drawerVisible) {
      return
    }

    const node = sidebarRef.current
    const previous = document.activeElement
    const menuButton = menuButtonRef.current
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    node?.focus()

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        event.preventDefault()
        setDrawerOpen(false)
        return
      }

      if (event.key !== 'Tab' || !node) {
        return
      }

      const items = getFocusable(node)
      if (items.length === 0) {
        return
      }

      const first = items[0]
      const last = items[items.length - 1]
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first.focus()
      }
    }

    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.body.style.overflow = previousOverflow
      document.removeEventListener('keydown', onKeyDown)
      const restore =
        previous instanceof HTMLElement && !node?.contains(previous) ? previous : menuButton
      restore?.focus()
    }
  }, [drawerVisible])

  return (
    <div
      className={styles.shell}
      data-sidebar-collapsed={chrome.railCollapsed ? 'true' : 'false'}
      data-drawer-open={drawerVisible ? 'true' : 'false'}
    >
      <a className="skip-link" href="#main">
        {t('common.skipToContent')}
      </a>

      <div className={styles.backdrop} aria-hidden="true" onClick={closeDrawer} />

      <Sidebar
        ref={sidebarRef}
        userLabel={userLabel}
        railCollapsed={chrome.railCollapsed}
        isDrawer={chrome.isDrawer}
        drawerOpen={drawerVisible}
        onSignOut={() => void onLogout()}
        onToggleCollapsed={toggleSidebarCollapsed}
        onCloseDrawer={closeDrawer}
        onNavigate={closeDrawer}
      />

      <div className={styles.workspace}>
        <div className={styles.mobileBar}>
          <button
            ref={menuButtonRef}
            type="button"
            className={styles.menuTrigger}
            aria-expanded={drawerVisible}
            aria-controls="app-sidebar"
            aria-label={drawerVisible ? t('navigation.closeMenu') : t('navigation.openMenu')}
            onClick={() => setDrawerOpen((open) => !open)}
          >
            {drawerVisible ? <CloseIcon /> : <MenuIcon />}
          </button>
          <span className={styles.mobileBrand}>
            <BrandMark size="sm" tone="inverse" />
            HuGu
          </span>
          <span className={styles.mobileUser}>{userLabel}</span>
          <PropertySelect id="mobile-property" className={styles.mobileLanguage} tone="onBrand" />
          <LanguageSelect
            id="mobile-language"
            className={styles.mobileLanguage}
            tone="onBrand"
            onChange={(language) => void updatePreferredLanguage(language)}
          />
          <Button className={styles.mobileSignOut} variant="ghost" onClick={() => void onLogout()}>
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

        <TopBar
          kicker={heading.kicker}
          title={heading.title}
          subtitle={heading.subtitle}
          actions={<PropertySelect id="shell-property" />}
        />

        {user?.propertySelectionRequired ? (
          <div className={styles.main}>
            <Notice tone="warning">{t('common.propertySelectionRequired')}</Notice>
          </div>
        ) : null}

        <main className={styles.main} id="main" tabIndex={-1}>
          <Outlet key={user?.propertyId ?? 'organization'} />
        </main>
      </div>
    </div>
  )
}

function getFocusable(root: HTMLElement): HTMLElement[] {
  return [
    ...root.querySelectorAll<HTMLElement>(
      'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
    ),
  ].filter((item) => !item.hasAttribute('disabled') && item.getAttribute('aria-hidden') !== 'true')
}

function headingFor(pathname: string, t: (key: string) => string) {
  if (pathname.startsWith('/app/workforce/departments')) {
    return {
      kicker: t('workforce.title'),
      title: t('workforce.departments'),
      subtitle: t('workforce.departmentsIntro'),
    }
  }

  if (pathname.startsWith('/app/workforce/leave-types')) {
    return {
      kicker: t('workforce.title'),
      title: t('workforce.leaveTypes'),
      subtitle: t('workforce.leaveTypesIntro'),
    }
  }

  if (pathname.startsWith('/app/workforce/leave-management')) {
    return {
      kicker: t('workforce.title'),
      title: t('workforce.leaveManagement'),
      subtitle: t('workforce.leaveManagementIntro'),
    }
  }

  if (pathname.startsWith('/app/my/leave')) {
    return {
      kicker: t('navigation.myLeave'),
      title: t('personnel.leave.myLeaveTitle'),
      subtitle: t('personnel.leave.myLeaveIntro'),
    }
  }

  if (pathname.startsWith('/app/workforce/shift-definitions')) {
    return {
      kicker: t('workforce.title'),
      title: t('workforce.shiftDefinitions'),
      subtitle: t('workforce.shiftDefinitionsIntro'),
    }
  }

  if (pathname.startsWith('/app/workforce/shift-plan')) {
    return {
      kicker: t('workforce.title'),
      title: t('workforce.shiftPlan'),
      subtitle: t('workforce.shiftPlanIntro'),
    }
  }

  if (pathname.startsWith('/app/workforce/official-settings')) {
    return {
      kicker: t('workforce.title'),
      title: t('workforce.officialSettings'),
      subtitle: t('workforce.officialSettingsIntro'),
    }
  }

  if (pathname.startsWith('/app/workforce/positions')) {
    return {
      kicker: t('workforce.title'),
      title: t('workforce.positions'),
      subtitle: t('workforce.positionsIntro'),
    }
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

  if (pathname === '/app/technical-service/new') {
    return {
      kicker: t('navigation.technicalService'),
      title: t('maintenance.create'),
      subtitle: t('maintenance.createIntro'),
    }
  }

  if (pathname.startsWith('/app/technical-service/')) {
    return {
      kicker: t('navigation.technicalService'),
      title: t('maintenance.detailTitle'),
      subtitle: t('maintenance.detailIntro'),
    }
  }

  if (pathname.startsWith('/app/technical-service')) {
    return {
      kicker: t('navigation.technicalService'),
      title: t('maintenance.title'),
      subtitle: t('maintenance.intro'),
    }
  }

  if (pathname.startsWith('/app/settings/roles')) {
    return {
      kicker: t('navigation.settings'),
      title: t('authorization.roles'),
      subtitle: t('authorization.rolesIntro'),
    }
  }

  if (pathname.startsWith('/app/settings')) {
    return {
      kicker: t('navigation.settings'),
      title: t('authorization.users'),
      subtitle: t('authorization.usersIntro'),
    }
  }

  return {
    kicker: t('navigation.home'),
    title: t('operations.title'),
    subtitle: t('operations.intro'),
  }
}
