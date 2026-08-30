import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { useAuthSession } from '../auth/AuthContext'
import { Button } from '../ui/Button'
import { Notice } from '../ui/Notice'
import { Skeleton } from '../ui/Skeleton'
import { TextField } from '../ui/TextField'
import { SelectField } from '../ui/SelectField'
import { StatusBadge } from '../ui/StatusBadge'
import styles from '../workforce/Workforce.module.css'
import {
  type AuthorizationRole,
  type AuthorizationUser,
  assignRole,
  authorizationErrorKey,
  createAuthorizationUser,
  createMembership,
  listAuthorizationRoles,
  listAuthorizationUsers,
  removeRole,
  replaceMembershipDepartmentScopes,
  setMembershipActive,
} from './authorizationApi'
import { permissionLabel } from './permissionLabel'
import { listDepartments, type DepartmentRecord } from '../workforce/workforceApi'

export function UsersPage() {
  const { t } = useTranslation()
  const { user } = useAuthSession()
  const [searchParams] = useSearchParams()
  const linkedEmployeeId = searchParams.get('employeeId') ?? ''
  const [rows, setRows] = useState<AuthorizationUser[] | null>(null)
  const [roles, setRoles] = useState<AuthorizationRole[]>([])
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [openId, setOpenId] = useState<string | null>(null)

  async function reload() {
    const [users, roleRows] = await Promise.all([listAuthorizationUsers(), listAuthorizationRoles()])
    setRows(users)
    setRoles(roleRows)
  }

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const [users, roleRows] = await Promise.all([listAuthorizationUsers(), listAuthorizationRoles()])
        if (!cancelled) {
          setRows(users)
          setRoles(roleRows)
        }
      } catch (reason) {
        if (!cancelled) {
          setError(t(authorizationErrorKey(reason)))
          setRows([])
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [t])

  async function onCreate() {
    setError(null)
    try {
      const created = await createAuthorizationUser(
        email,
        password,
        linkedEmployeeId === '' ? undefined : linkedEmployeeId,
      )
      if (user?.organizationId) {
        await createMembership(created.id, user.organizationId, user.propertyId ?? null)
      }
      setEmail('')
      setPassword('')
      await reload()
    } catch (reason) {
      setError(t(authorizationErrorKey(reason)))
    }
  }

  if (rows === null && error === null) {
    return <Skeleton variant="list" rows={5} label={t('authorization.users')} />
  }

  const list = rows ?? []

  return (
    <div className={styles.page}>
      <p className={styles.muted}>{t('authorization.usersIntro')}</p>
      {linkedEmployeeId ? (
        <Notice tone="info">{t('authorization.linkedEmployeeHint', { employeeId: linkedEmployeeId })}</Notice>
      ) : null}
      {error ? <Notice tone="danger">{error}</Notice> : null}
      <form
        className={styles.panel}
        onSubmit={(event) => {
          event.preventDefault()
          void onCreate()
        }}
      >
        <div className={styles.formGrid}>
          <TextField id="auth-user-email" label={t('authorization.email')} value={email} onChange={setEmail} required />
          <TextField
            id="auth-user-password"
            label={t('authorization.password')}
            value={password}
            onChange={setPassword}
            type="password"
            required
          />
        </div>
        <div className={styles.formFooter}>
          <Button type="submit" layout="inline">
            {t('authorization.createUser')}
          </Button>
        </div>
      </form>
      <div>
        {list.map((row) => {
          const membership = row.memberships[0]
          const roleNames = (membership?.roleIds ?? [])
            .map((id) => roles.find((role) => role.id === id)?.name)
            .filter(Boolean)
            .join(', ')
          return (
            <div key={row.id} className={styles.row} role="row">
              <div>
                <strong>{row.email ?? row.id}</strong>
                <div className={styles.muted}>
                  {t('authorization.organization')}: {membership?.organizationName ?? membership?.organizationId ?? t('authorization.none')}
                  {' · '}
                  {membership?.scopeType === 'Organization'
                    ? t('authorization.organizationWide')
                    : membership?.propertyName ?? t('authorization.scopeProperty')}
                </div>
                <div className={styles.muted}>
                  {t('authorization.employee')}: {row.employeeId ?? t('authorization.none')}
                </div>
              </div>
              <StatusBadge tone={membership?.isActive ? 'success' : 'neutral'}>
                {membership?.isActive ? t('authorization.active') : t('authorization.inactive')}
              </StatusBadge>
              <span>{roleNames || t('authorization.none')}</span>
              <Button variant="ghost" onClick={() => setOpenId(openId === row.id ? null : row.id)}>
                {t('authorization.memberships')}
              </Button>
              {openId === row.id && membership ? (
                <UserMembershipEditor
                  key={`${membership.id}:${(membership.departmentIds ?? []).join(',')}`}
                  user={row}
                  membershipId={membership.id}
                  roles={roles}
                  onChanged={() => void reload()}
                  onError={setError}
                />
              ) : null}
            </div>
          )
        })}
      </div>
    </div>
  )
}

function UserMembershipEditor({
  user,
  membershipId,
  roles,
  onChanged,
  onError,
}: {
  user: AuthorizationUser
  membershipId: string
  roles: AuthorizationRole[]
  onChanged: () => void
  onError: (message: string) => void
}) {
  const { t } = useTranslation()
  const membership = user.memberships.find((item) => item.id === membershipId)
  const [roleId, setRoleId] = useState(roles[0]?.id ?? '')
  const [departments, setDepartments] = useState<DepartmentRecord[]>([])
  const [scopeMode, setScopeMode] = useState<'property' | 'selected'>(
    (membership?.departmentIds.length ?? 0) > 0 ? 'selected' : 'property',
  )
  const [selectedDepartmentIds, setSelectedDepartmentIds] = useState<string[]>(
    membership?.departmentIds ?? [],
  )

  useEffect(() => {
    if (!membership?.propertyId) {
      return
    }
    let cancelled = false
    void listDepartments()
      .then((rows) => {
        if (!cancelled) {
          setDepartments(rows.filter((item) => item.propertyId === membership.propertyId && item.isActive))
        }
      })
      .catch((reason) => {
        if (!cancelled) {
          onError(t(authorizationErrorKey(reason)))
        }
      })
    return () => {
      cancelled = true
    }
  }, [membership?.propertyId, onError, t])

  if (!membership) {
    return null
  }

  function toggleDepartment(id: string) {
    setSelectedDepartmentIds((current) =>
      current.includes(id) ? current.filter((item) => item !== id) : [...current, id],
    )
  }

  return (
    <div className={styles.panel}>
      <p>
        {t('authorization.effectivePermissions')}:{' '}
        {user.effectivePermissions.map((code) => permissionLabel(t, code)).join(', ') || t('authorization.none')}
      </p>
      <div className={styles.formGrid}>
        <SelectField
          id={`assign-role-${user.id}`}
          label={t('authorization.assignRole')}
          value={roleId}
          onChange={setRoleId}
        >
          {roles.map((role) => (
            <option key={role.id} value={role.id}>
              {role.name}
            </option>
          ))}
        </SelectField>
        <Button
          onClick={() => {
            void assignRole(membershipId, roleId)
              .then(onChanged)
              .catch((reason) => onError(t(authorizationErrorKey(reason))))
          }}
        >
          {t('authorization.assignRole')}
        </Button>
        <Button
          variant="ghost"
          onClick={() => {
            void setMembershipActive(membershipId, !membership.isActive)
              .then(onChanged)
              .catch((reason) => onError(t(authorizationErrorKey(reason))))
          }}
        >
          {membership.isActive ? t('authorization.deactivate') : t('authorization.activate')}
        </Button>
      </div>
      <ul>
        {membership.roleIds.map((id) => {
          const role = roles.find((item) => item.id === id)
          return (
            <li key={id}>
              {role?.name ?? id}{' '}
              <Button
                variant="ghost"
                onClick={() => {
                  void removeRole(membershipId, id)
                    .then(onChanged)
                    .catch((reason) => onError(t(authorizationErrorKey(reason))))
                }}
              >
                {t('authorization.removeRole')}
              </Button>
            </li>
          )
        })}
      </ul>
      {membership.propertyId ? (
        <fieldset className={styles.choiceSet}>
          <legend className={styles.choiceLegend}>{t('authorization.departmentScopes')}</legend>
          <div className={styles.choiceList}>
            <label className={styles.choiceOption} htmlFor={`dept-scope-property-${membership.id}`}>
              <input
                id={`dept-scope-property-${membership.id}`}
                type="radio"
                name={`dept-scope-${membership.id}`}
                checked={scopeMode === 'property'}
                onChange={() => {
                  setScopeMode('property')
                  setSelectedDepartmentIds([])
                }}
              />
              {t('authorization.propertyWideAccess')}
            </label>
            <label className={styles.choiceOption} htmlFor={`dept-scope-selected-${membership.id}`}>
              <input
                id={`dept-scope-selected-${membership.id}`}
                type="radio"
                name={`dept-scope-${membership.id}`}
                checked={scopeMode === 'selected'}
                onChange={() => setScopeMode('selected')}
              />
              {t('authorization.selectedDepartments')}
            </label>
          </div>
          {scopeMode === 'selected' ? (
            <div className={styles.choiceList}>
              {departments.map((item) => (
                <label
                  key={item.id}
                  className={styles.choiceOption}
                  htmlFor={`dept-scope-item-${membership.id}-${item.id}`}
                >
                  <input
                    id={`dept-scope-item-${membership.id}-${item.id}`}
                    type="checkbox"
                    checked={selectedDepartmentIds.includes(item.id)}
                    onChange={() => toggleDepartment(item.id)}
                  />
                  {item.name}
                </label>
              ))}
            </div>
          ) : null}
          <div className={styles.formFooter}>
            <Button
              layout="inline"
              onClick={() => {
                const departmentIds = scopeMode === 'property' ? [] : selectedDepartmentIds
                void replaceMembershipDepartmentScopes(membershipId, departmentIds)
                  .then(onChanged)
                  .catch((reason) => onError(t(authorizationErrorKey(reason))))
              }}
            >
              {t('authorization.saveDepartmentScopes')}
            </Button>
          </div>
        </fieldset>
      ) : null}
    </div>
  )
}
