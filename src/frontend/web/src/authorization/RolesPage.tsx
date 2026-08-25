import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '../ui/Button'
import { Notice } from '../ui/Notice'
import { Skeleton } from '../ui/Skeleton'
import { TextField } from '../ui/TextField'
import { SelectField } from '../ui/SelectField'
import { StatusBadge } from '../ui/StatusBadge'
import { useAuthSession } from '../auth/AuthContext'
import styles from '../workforce/Workforce.module.css'
import {
  type AuthorizationRole,
  type PermissionCatalogItem,
  authorizationErrorKey,
  createRole,
  listAuthorizationRoles,
  listPermissionCatalog,
  replaceRolePermissions,
  setRoleActive,
} from './authorizationApi'
import { permissionLabel } from './permissionLabel'

const groups = ['hr', 'room-operations', 'technical-service', 'authorization'] as const

export function RolesPage() {
  const { t } = useTranslation()
  const { user } = useAuthSession()
  const [roles, setRoles] = useState<AuthorizationRole[] | null>(null)
  const [catalog, setCatalog] = useState<PermissionCatalogItem[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [checked, setChecked] = useState<string[]>([])
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [scopeType, setScopeType] = useState('Property')
  const [error, setError] = useState<string | null>(null)

  async function reload() {
    const [roleRows, permissions] = await Promise.all([listAuthorizationRoles(), listPermissionCatalog()])
    setRoles(roleRows)
    setCatalog(permissions)
    const current = roleRows.find((role) => role.id === selectedId) ?? roleRows[0]
    if (current) {
      setSelectedId(current.id)
      setChecked(current.permissionCodes)
    }
  }

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const [roleRows, permissions] = await Promise.all([listAuthorizationRoles(), listPermissionCatalog()])
        if (!cancelled) {
          setRoles(roleRows)
          setCatalog(permissions)
          if (roleRows[0]) {
            setSelectedId(roleRows[0].id)
            setChecked(roleRows[0].permissionCodes)
          }
        }
      } catch (reason) {
        if (!cancelled) {
          setError(t(authorizationErrorKey(reason)))
          setRoles([])
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [t])

  const selected = useMemo(
    () => roles?.find((role) => role.id === selectedId) ?? null,
    [roles, selectedId],
  )

  if (roles === null) {
    return <Skeleton variant="list" rows={6} label={t('authorization.roles')} />
  }

  return (
    <div className={styles.page}>
      <p className={styles.muted}>{t('authorization.rolesIntro')}</p>
      <p className={styles.muted}>{t('authorization.rolesOwnedByOrganization')}</p>
      {error ? <Notice tone="danger">{error}</Notice> : null}
      <form
        className={styles.panel}
        onSubmit={(event) => {
          event.preventDefault()
          void (async () => {
            setError(null)
            try {
              await createRole({
                name,
                code,
                scopeType,
                organizationId: user?.organizationId ?? '',
              })
              setName('')
              setCode('')
              await reload()
            } catch (reason) {
              setError(t(authorizationErrorKey(reason)))
            }
          })()
        }}
      >
        <div className={styles.formGrid}>
          <TextField id="role-name" label={t('authorization.roleName')} value={name} onChange={setName} required />
          <TextField id="role-code" label={t('authorization.roleCode')} value={code} onChange={setCode} required />
          <SelectField id="role-scope" label={t('authorization.scope')} value={scopeType} onChange={setScopeType}>
            <option value="Property">{t('authorization.scopeProperty')}</option>
            <option value="Organization">{t('authorization.scopeOrganization')}</option>
          </SelectField>
        </div>
        <div className={styles.formFooter}>
          <Button type="submit" layout="inline">
            {t('authorization.createRole')}
          </Button>
        </div>
      </form>
      <div className={styles.formGrid}>
        {roles.map((role) => (
          <button
            key={role.id}
            type="button"
            className={styles.rowLink}
            onClick={() => {
              setSelectedId(role.id)
              setChecked(role.permissionCodes)
            }}
          >
            {role.name}{' '}
            <StatusBadge tone="neutral">
              {role.scopeType === 'Organization'
                ? t('authorization.scopeOrganization')
                : t('authorization.scopeProperty')}
            </StatusBadge>{' '}
            <StatusBadge tone={role.isActive ? 'success' : 'neutral'}>
              {role.isActive ? t('authorization.active') : t('authorization.inactive')}
            </StatusBadge>
          </button>
        ))}
      </div>
      {selected ? (
        <form
          className={styles.panel}
          onSubmit={(event) => {
            event.preventDefault()
            void (async () => {
              setError(null)
              try {
                await replaceRolePermissions(selected.id, checked)
                await reload()
              } catch (reason) {
                setError(t(authorizationErrorKey(reason)))
              }
            })()
          }}
        >
          <h2>{selected.name}</h2>
          <p className={styles.muted}>
            {t('authorization.organization')} ·{' '}
            {selected.scopeType === 'Organization'
              ? t('authorization.scopeOrganization')
              : t('authorization.scopeProperty')}
          </p>
          {groups.map((group) => (
            <fieldset key={group} className={styles.panel}>
              <legend>
                {group === 'hr'
                  ? t('authorization.groupHr')
                  : group === 'room-operations'
                    ? t('authorization.groupRoomOperations')
                    : group === 'technical-service'
                      ? t('authorization.groupTechnicalService')
                      : t('authorization.groupAuthorization')}
              </legend>
              {catalog
                .filter((item) => item.domain === group)
                .map((item) => (
                  <label key={item.code}>
                    <input
                      type="checkbox"
                      checked={checked.includes(item.code)}
                      onChange={(event) => {
                        setChecked((current) =>
                          event.target.checked
                            ? [...current, item.code]
                            : current.filter((code) => code !== item.code),
                        )
                      }}
                    />{' '}
                    {permissionLabel(t, item.code)}
                  </label>
                ))}
            </fieldset>
          ))}
          <div className={styles.formFooter}>
            <Button type="submit" layout="inline">
              {t('authorization.savePermissions')}
            </Button>
            <Button
              type="button"
              variant="ghost"
              onClick={() => {
                void setRoleActive(selected.id, !selected.isActive)
                  .then(() => reload())
                  .catch((reason) => setError(t(authorizationErrorKey(reason))))
              }}
            >
              {selected.isActive ? t('authorization.deactivate') : t('authorization.activate')}
            </Button>
          </div>
        </form>
      ) : null}
    </div>
  )
}
