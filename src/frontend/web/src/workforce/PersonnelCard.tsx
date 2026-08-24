import { useEffect, useMemo, useRef, useState, type RefObject } from 'react'
import { useTranslation } from 'react-i18next'
import { addDaysIso, formatDateOnly, laterIsoDate, todayIsoDate } from '../i18n/format'
import { DEFAULT_LANGUAGE, toAppLanguage, type AppLanguage } from '../i18n/languages'
import { AvatarMark } from '../ui/AvatarMark'
import { Button } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { CloseIcon, WarningIcon } from '../ui/icons'
import { Notice } from '../ui/Notice'
import { DateField, SelectField } from '../ui/SelectField'
import { TextArea, TextField } from '../ui/TextField'
import { StatusBadge } from '../ui/StatusBadge'
import { Timeline, TimelineItem } from '../ui/Timeline'
import { WorkspaceDialog } from '../ui/WorkspaceDialog'
import styles from './PersonnelCard.module.css'
import { MobilePhoneField } from './MobilePhoneField'
import {
  createHrEmployee,
  getHrEmployee,
  hrEmployeePhotoUrl,
  hrErrorKey,
  hrFieldErrorsFromProblem,
  removeHrEmployeePhoto,
  updateHrEmployee,
  uploadHrEmployeePhoto,
  type HrEmployeeCard,
} from './hrApi'
import {
  emptyPersonnelForm,
  formFromCard,
  isPersonnelFormDirty,
  snapshotOf,
  toHrWrite,
  type PersonnelForm,
} from './personnelForm'
import { restrictIdentityInput, TCKN_DIGIT_MAX, digitsOnly } from './personnelInput'
import {
  firstInvalidTarget,
  HrValidationCodes,
  revalidateKnownErrors,
  validatePersonnelField,
  validatePersonnelForm,
  validationMessageKeyFor,
  type FieldErrors,
} from './personnelValidation'
import type { DepartmentRecord, PositionRecord } from './workforceApi'
import { endEmployment, transferEmployee } from './workforceApi'
import { positionsForDepartment, retainedPositionId } from './assignmentOptions'
import { employmentStatusTone } from './workforceStatus'

type TabId = 'general' | 'identity' | 'work' | 'history'
type CardMode = { type: 'create' } | { type: 'edit'; employeeId: string }

function positionSelectPlaceholder(departmentId: string, t: (key: string) => string) {
  return departmentId === '' ? t('workforce.selectDepartmentFirst') : t('workforce.selectPosition')
}

export function PersonnelCard({
  mode,
  departments,
  positions,
  canManage,
  canManageWorkforce,
  canReadSensitive,
  onClose,
  onSaved,
}: {
  mode: CardMode
  departments: DepartmentRecord[]
  positions: PositionRecord[]
  canManage: boolean
  canManageWorkforce: boolean
  canReadSensitive: boolean
  onClose: () => void
  onSaved: (employeeId?: string) => Promise<void> | void
}) {
  const { t, i18n } = useTranslation()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const fileInput = useRef<HTMLInputElement>(null)
  const givenNameInput = useRef<HTMLInputElement>(null)
  const continueEditingRef = useRef<HTMLButtonElement>(null)
  const [tab, setTab] = useState<TabId>('general')
  const [card, setCard] = useState<HrEmployeeCard | null>(null)
  const [form, setForm] = useState<PersonnelForm>(() => emptyPersonnelForm(todayIsoDate()))
  const [snapshot, setSnapshot] = useState(() => snapshotOf(emptyPersonnelForm(todayIsoDate())))
  const [pendingPhoto, setPendingPhoto] = useState<File | null>(null)
  const [photoPreview, setPhotoPreview] = useState<string | null>(null)
  const [confirming, setConfirming] = useState(false)
  const [workMode, setWorkMode] = useState<'none' | 'transfer' | 'end'>('none')
  const [loading, setLoading] = useState(mode.type === 'edit')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saveNotice, setSaveNotice] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [focusNonce, setFocusNonce] = useState(0)
  const pendingFocus = useRef<string | null>(null)
  const saveLock = useRef(false)
  const createdId = useRef<string | null>(null)
  const [transfer, setTransfer] = useState({
    departmentId: '',
    positionId: '',
    effectiveDate: todayIsoDate(),
  })
  const [endDate, setEndDate] = useState(todayIsoDate())

  useEffect(() => {
    if (mode.type !== 'edit') {
      return
    }

    const employeeId = mode.employeeId
    let cancelled = false
    async function load() {
      try {
        const detail = await getHrEmployee(employeeId)
        if (cancelled) {
          return
        }

        const next = formFromCard(detail)
        setCard(detail)
        setForm(next)
        setSnapshot(snapshotOf(next))
        setTransfer({
          departmentId: detail.currentPrimaryAssignment?.departmentId ?? '',
          positionId: detail.currentPrimaryAssignment?.positionId ?? '',
          effectiveDate: detail.currentPrimaryAssignment
            ? laterIsoDate(todayIsoDate(), addDaysIso(detail.currentPrimaryAssignment.startDate, 1))
            : todayIsoDate(),
        })
        setFieldErrors({})
        setLoading(false)
      } catch (reason) {
        if (!cancelled) {
          setError(t(hrErrorKey(reason)))
          setLoading(false)
        }
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [mode, t])

  const validationContext = {
    createMode: mode.type === 'create',
    today: todayIsoDate(),
  }

  useEffect(() => {
    const controlId = pendingFocus.current
    if (!controlId) {
      return
    }

    pendingFocus.current = null
    const frame = requestAnimationFrame(() => {
      const node = document.getElementById(controlId)
      if (node instanceof HTMLElement) {
        node.focus()
        node.scrollIntoView({ block: 'center', inline: 'nearest' })
      }
    })
    return () => cancelAnimationFrame(frame)
  }, [focusNonce, tab])

  function fieldMessage(field: string): string | undefined {
    const code = fieldErrors[field]
    return code ? t(validationMessageKeyFor(field, code)) : undefined
  }

  function showFieldErrors(errors: FieldErrors) {
    setFieldErrors(errors)
    setError(t('personnel.errors.fixFields'))
    const target = firstInvalidTarget(errors, form, mode.type === 'create')
    if (!target) {
      return
    }

    pendingFocus.current = target.controlId
    if (tab !== target.tab) {
      setTab(target.tab)
    }
    setFocusNonce((value) => value + 1)
  }

  function blurField(field: string) {
    const code = validatePersonnelField(form, field, validationContext)
    setFieldErrors((current) => {
      const next = { ...current }
      if (code) {
        next[field] = code
      } else {
        delete next[field]
      }
      return next
    })
  }

  function patchForm(patch: Partial<PersonnelForm>) {
    const next = { ...form, ...patch }
    if (patch.departmentId !== undefined) {
      next.positionId = retainedPositionId(positions, patch.departmentId, next.positionId)
    }
    if (patch.nationalIdentityScheme !== undefined) {
      next.nationalIdentityNumber = restrictIdentityInput(
        patch.nationalIdentityScheme,
        next.nationalIdentityNumber,
      )
    }
    setForm(next)
    setSaveNotice(null)
    setFieldErrors((current) => {
      if (Object.keys(current).length === 0) {
        return current
      }

      const extra = Object.keys(patch)
      if (patch.nationalIdentityScheme !== undefined) {
        extra.push('nationalIdentityNumber')
      }
      if (patch.nationalIdentityNumber !== undefined) {
        extra.push('nationalIdentityScheme')
      }
      return revalidateKnownErrors(current, next, validationContext, extra)
    })
  }

  function patchEmergency(index: number, patch: Partial<PersonnelForm['emergencyContacts'][number]>) {
    const next = {
      ...form,
      emergencyContacts: form.emergencyContacts.map((item, itemIndex) =>
        itemIndex === index ? { ...item, ...patch } : item,
      ),
    }
    setForm(next)
    setFieldErrors((current) => {
      if (Object.keys(current).length === 0) {
        return current
      }
      const extra = Object.keys(patch).map((key) => `emergencyContacts[${index}].${key}`)
      return revalidateKnownErrors(current, next, validationContext, extra)
    })
  }

  function assignPendingPhoto(file: File | null) {
    setPendingPhoto(file)
    setPhotoPreview((current) => {
      if (current) {
        URL.revokeObjectURL(current)
      }
      return file ? URL.createObjectURL(file) : null
    })
  }

  const dirty = isPersonnelFormDirty(form, snapshot) || pendingPhoto !== null

  useEffect(() => {
    if (!dirty) {
      return
    }

    function onBeforeUnload(event: BeforeUnloadEvent) {
      event.preventDefault()
    }

    window.addEventListener('beforeunload', onBeforeUnload)
    return () => window.removeEventListener('beforeunload', onBeforeUnload)
  }, [dirty])

  const activeDepartments = useMemo(() => departments.filter((item) => item.isActive), [departments])
  const activePositions = useMemo(() => positions.filter((item) => item.isActive), [positions])
  const status = card?.currentEmployment?.status ?? card?.employments[0]?.status
  const ended = status === 'Ended'
  const displayName = `${form.givenName} ${form.familyName}`.trim() || t('personnel.cardTitleCreate')
  const photoSrc =
    photoPreview
    ?? (card?.hasPhoto && mode.type === 'edit' ? hrEmployeePhotoUrl(mode.employeeId) : null)

  function requestClose() {
    if (dirty) {
      setConfirming(true)
      return
    }

    onClose()
  }

  function continueEditing() {
    setConfirming(false)
  }

  function discardChanges() {
    setConfirming(false)
    onClose()
  }

  function markMobileInvalid() {
    setFieldErrors((current) => ({ ...current, mobilePhone: HrValidationCodes.phoneInvalid }))
  }

  async function persistPhoto(employeeId: string) {
    if (pendingPhoto) {
      await uploadHrEmployeePhoto(employeeId, pendingPhoto)
      assignPendingPhoto(null)
    }
  }

  async function onSave() {
    if (saveLock.current) {
      return
    }

    setError(null)
    setSaveNotice(null)
    const clientErrors = validatePersonnelForm(form, validationContext)
    if (Object.keys(clientErrors).length > 0) {
      showFieldErrors(clientErrors)
      return
    }

    saveLock.current = true
    setSaving(true)
    try {
      if (mode.type === 'create') {
        if (createdId.current === null) {
          const created = await createHrEmployee(toHrWrite(form, true))
          createdId.current = created.employeeId
        }

        const employeeId = createdId.current
        await persistPhoto(employeeId)
        assignPendingPhoto(null)
        setSnapshot(snapshotOf(form))
        setFieldErrors({})
        await onSaved()
        onClose()
        return
      }

      const updated = await updateHrEmployee(mode.employeeId, toHrWrite(form, false))
      await persistPhoto(mode.employeeId)
      const detail = updated.hasPhoto !== undefined ? await getHrEmployee(mode.employeeId) : updated
      const next = formFromCard(detail)
      setCard(detail)
      setForm(next)
      setSnapshot(snapshotOf(next))
      setFieldErrors({})
      setSaveNotice(t('personnel.saveSuccess'))
      await onSaved()
    } catch (reason) {
      const mapped = hrFieldErrorsFromProblem(reason)
      if (Object.keys(mapped).length > 0) {
        showFieldErrors(mapped)
      } else {
        setError(t(hrErrorKey(reason)))
      }

      if (mode.type === 'create' && createdId.current) {
        try {
          await onSaved(createdId.current)
        } catch {
          // Keep the card open; the original save error is already shown.
        }
      }
    } finally {
      saveLock.current = false
      setSaving(false)
    }
  }

  async function onRemovePhoto() {
    if (mode.type !== 'edit') {
      assignPendingPhoto(null)
      return
    }

    setError(null)
    setSaving(true)
    try {
      await removeHrEmployeePhoto(mode.employeeId)
      assignPendingPhoto(null)
      const detail = await getHrEmployee(mode.employeeId)
      setCard(detail)
      await onSaved()
    } catch (reason) {
      setError(t(hrErrorKey(reason)))
    } finally {
      setSaving(false)
    }
  }

  async function onTransfer() {
    if (mode.type !== 'edit') {
      return
    }

    setError(null)
    setSaving(true)
    try {
      await transferEmployee(mode.employeeId, transfer)
      const detail = await getHrEmployee(mode.employeeId)
      const next = formFromCard(detail)
      setCard(detail)
      setForm(next)
      setSnapshot(snapshotOf(next))
      setWorkMode('none')
      await onSaved()
    } catch (reason) {
      setError(t(hrErrorKey(reason)))
    } finally {
      setSaving(false)
    }
  }

  async function onEnd() {
    if (mode.type !== 'edit') {
      return
    }

    setError(null)
    setSaving(true)
    try {
      await endEmployment(mode.employeeId, endDate)
      const detail = await getHrEmployee(mode.employeeId)
      const next = formFromCard(detail)
      setCard(detail)
      setForm(next)
      setSnapshot(snapshotOf(next))
      setWorkMode('none')
      await onSaved()
    } catch (reason) {
      setError(t(hrErrorKey(reason)))
    } finally {
      setSaving(false)
    }
  }

  const title = mode.type === 'create' ? t('personnel.cardTitleCreate') : t('personnel.cardTitle')
  const readOnly = !canManage || (ended && mode.type === 'edit')
  const departmentName =
    card?.currentPrimaryAssignment?.departmentName
    || departments.find((item) => item.id === form.departmentId)?.name
    || '—'
  const positionName =
    card?.currentPrimaryAssignment?.positionName
    || positions.find((item) => item.id === form.positionId)?.name
    || '—'

  return (
    <>
    <WorkspaceDialog
      title={title}
      onRequestClose={requestClose}
      initialFocusRef={givenNameInput}
      inert={confirming}
      footer={
          <>
            <Button variant="ghost" onClick={requestClose}>
              {canManage && !(ended && mode.type === 'edit') ? t('personnel.cancel') : t('personnel.close')}
            </Button>
            {canManage && !(ended && mode.type === 'edit') ? (
              <Button type="button" layout="inline" loading={saving} onClick={() => void onSave()}>
                {t('personnel.save')}
              </Button>
            ) : null}
          </>
      }
    >
      {loading ? <p className={styles.meta}>{t('workforce.loading')}</p> : (
        <div className={styles.card}>
          <div className={styles.toolbar}>
            <IdentityHeader
              displayName={displayName}
              photoSrc={photoSrc}
              personnelNumber={form.personnelNumber}
              personnelNumberHint={mode.type === 'create' ? t('personnel.personnelNumberAuto') : null}
              status={status}
              canManagePhoto={canManage && !readOnly}
              fileInput={fileInput}
              onPickPhoto={assignPendingPhoto}
              onRemovePhoto={() => void onRemovePhoto()}
              saving={saving}
            />
            <Button className={styles.closeButton} variant="ghost" aria-label={t('personnel.closeCard')} onClick={requestClose}>
              <CloseIcon />
            </Button>
          </div>

          <div className={styles.facts}>
            <div className={styles.fact}>
              <span className={styles.factLabel}>{t('workforce.department')}</span>
              <span>{departmentName}</span>
            </div>
            <div className={styles.fact}>
              <span className={styles.factLabel}>{t('workforce.position')}</span>
              <span>{positionName}</span>
            </div>
            <div className={styles.fact}>
              <span className={styles.factLabel}>{t('workforce.startDate')}</span>
              <span>
                {form.employmentStartDate ? formatDateOnly(form.employmentStartDate, language) : '—'}
              </span>
            </div>
          </div>

          <div className={styles.tabs} role="tablist" aria-label={title}>
            {([
              ['general', t('personnel.tabGeneral')],
              ['identity', t('personnel.tabIdentity')],
              ['work', t('personnel.tabWork')],
              ['history', t('personnel.tabHistory')],
            ] as const).map(([id, label]) => (
              <button
                key={id}
                type="button"
                role="tab"
                aria-selected={tab === id}
                className={tab === id ? styles.tabCurrent : styles.tab}
                onClick={() => setTab(id)}
              >
                {label}
              </button>
            ))}
          </div>

          {saveNotice ? <Notice tone="success">{saveNotice}</Notice> : null}
          {error ? <Notice tone="danger">{error}</Notice> : null}
          {!canReadSensitive ? <Notice tone="info">{t('personnel.sensitiveHidden')}</Notice> : null}

          {tab === 'general' ? (
            <GeneralTab
              form={form}
              patchForm={patchForm}
              givenNameRef={givenNameInput}
              readOnly={readOnly}
              createMode={mode.type === 'create'}
              departments={activeDepartments}
              positions={activePositions}
              fieldMessage={fieldMessage}
              blurField={blurField}
              onMobileUnsafePaste={markMobileInvalid}
            />
          ) : null}

          {tab === 'identity' ? (
            <IdentityTab
              form={form}
              patchForm={patchForm}
              patchEmergency={patchEmergency}
              setForm={setForm}
              readOnly={readOnly}
              canReadSensitive={canReadSensitive}
              fieldMessage={fieldMessage}
              blurField={blurField}
              onMobileUnsafePaste={markMobileInvalid}
            />
          ) : null}

          {tab === 'work' ? (
            <WorkTab
              form={form}
              patchForm={patchForm}
              card={card}
              createMode={mode.type === 'create'}
              readOnly={readOnly}
              ended={ended}
              language={language}
              canManageWorkforce={canManageWorkforce && mode.type === 'edit' && !ended}
              departments={activeDepartments}
              positions={activePositions}
              workMode={workMode}
              setWorkMode={setWorkMode}
              transfer={transfer}
              setTransfer={setTransfer}
              endDate={endDate}
              setEndDate={setEndDate}
              saving={saving}
              onTransfer={() => void onTransfer()}
              onEnd={() => void onEnd()}
              fieldMessage={fieldMessage}
              blurField={blurField}
            />
          ) : null}

          {tab === 'history' ? (
            <HistoryTab card={card} language={language} createMode={mode.type === 'create'} />
          ) : null}
        </div>
      )}
    </WorkspaceDialog>
    {confirming ? (
      <WorkspaceDialog
        title={t('personnel.dirtyTitle')}
        size="confirm"
        stacked
        onRequestClose={continueEditing}
        initialFocusRef={continueEditingRef}
        footer={
          <>
            <Button variant="danger" onClick={discardChanges}>
              {t('personnel.dirtyDiscard')}
            </Button>
            <Button ref={continueEditingRef} variant="primary" layout="inline" onClick={continueEditing}>
              {t('personnel.dirtyContinue')}
            </Button>
          </>
        }
      >
        <div className={styles.dirtyMessage}>
          <span className={styles.dirtyIcon} aria-hidden="true">
            <WarningIcon />
          </span>
          <p className={styles.dirtyBody}>{t('personnel.dirtyBody')}</p>
        </div>
      </WorkspaceDialog>
    ) : null}
    </>
  )
}

function IdentityHeader({
  displayName,
  photoSrc,
  personnelNumber,
  personnelNumberHint,
  status,
  canManagePhoto,
  fileInput,
  onPickPhoto,
  onRemovePhoto,
  saving,
}: {
  displayName: string
  photoSrc: string | null
  personnelNumber: string
  personnelNumberHint: string | null
  status: string | undefined
  canManagePhoto: boolean
  fileInput: RefObject<HTMLInputElement | null>
  onPickPhoto: (file: File) => void
  onRemovePhoto: () => void
  saving: boolean
}) {
  const { t } = useTranslation()
  const hasPhoto = Boolean(photoSrc)
  return (
    <div className={styles.header}>
      <AvatarMark name={displayName} size="xl" src={photoSrc} alt={displayName} />
      <div className={styles.identity}>
        <div className={styles.identityRow}>
          <p className={styles.name}>{displayName}</p>
          <StatusBadge tone={employmentStatusTone(status)}>
            {status === 'Active'
              ? t('workforce.activeStatus')
              : status === 'Scheduled'
                ? t('workforce.scheduledStatus')
                : status === 'Ended'
                  ? t('workforce.endedStatus')
                  : t('personnel.cardTitleCreate')}
          </StatusBadge>
        </div>
        <p className={styles.meta}>
          {t('workforce.personnelNumber')}: {personnelNumberHint ?? (personnelNumber.trim() || '—')}
        </p>
        {canManagePhoto ? (
          <div className={styles.photoControls}>
            <input
              id="hr-employee-photo"
              ref={fileInput}
              className={styles.hiddenFile}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              tabIndex={-1}
              aria-hidden="true"
              onChange={(event) => {
                const file = event.target.files?.[0]
                if (file) {
                  onPickPhoto(file)
                }
                event.target.value = ''
              }}
            />
            <div className={styles.photoActions}>
              <Button
                variant="secondary"
                size="sm"
                layout="inline"
                disabled={saving}
                aria-controls="hr-employee-photo"
                aria-describedby="hr-photo-hint"
                onClick={() => fileInput.current?.click()}
              >
                {hasPhoto ? t('personnel.replacePhoto') : t('personnel.uploadPhoto')}
              </Button>
              {hasPhoto ? (
                <Button variant="ghost" size="sm" layout="inline" disabled={saving} onClick={onRemovePhoto}>
                  {t('personnel.removePhoto')}
                </Button>
              ) : null}
            </div>
            <p id="hr-photo-hint" className={styles.photoHint}>
              {t('personnel.photoHint')}
            </p>
          </div>
        ) : null}
      </div>
    </div>
  )
}

function GeneralTab({
  form,
  patchForm,
  givenNameRef,
  readOnly,
  createMode,
  departments,
  positions,
  fieldMessage,
  blurField,
  onMobileUnsafePaste,
}: {
  form: PersonnelForm
  patchForm: (patch: Partial<PersonnelForm>) => void
  givenNameRef: RefObject<HTMLInputElement | null>
  readOnly: boolean
  createMode: boolean
  departments: DepartmentRecord[]
  positions: PositionRecord[]
  fieldMessage: (field: string) => string | undefined
  blurField: (field: string) => void
  onMobileUnsafePaste: () => void
}) {
  const { t } = useTranslation()
  return (
    <>
      <div className={styles.grid}>
        <TextField
          id="hr-given"
          ref={givenNameRef}
          label={t('workforce.givenName')}
          value={form.givenName}
          placeholder={t('personnel.placeholders.givenName')}
          onChange={(givenName) => patchForm({ givenName })}
          onBlur={() => blurField('givenName')}
          error={fieldMessage('givenName')}
          required
          disabled={readOnly}
        />
        <TextField
          id="hr-family"
          label={t('workforce.familyName')}
          value={form.familyName}
          placeholder={t('personnel.placeholders.familyName')}
          onChange={(familyName) => patchForm({ familyName })}
          onBlur={() => blurField('familyName')}
          error={fieldMessage('familyName')}
          required
          disabled={readOnly}
        />
        <TextField
          id="hr-sicil"
          label={t('workforce.personnelNumber')}
          value={createMode ? t('personnel.personnelNumberAuto') : form.personnelNumber}
          onChange={() => undefined}
          readOnly
          hint={createMode ? undefined : t('personnel.personnelNumberReadOnly')}
        />
        <SelectField
          id="hr-education"
          label={t('personnel.educationLevel')}
          value={form.educationLevel}
          placeholder={t('personnel.placeholders.educationLevel')}
          onChange={(educationLevel) =>
            patchForm({ educationLevel: educationLevel as PersonnelForm['educationLevel'] })
          }
          disabled={readOnly}
        >
          <option value="Primary">{t('personnel.educationPrimary')}</option>
          <option value="Secondary">{t('personnel.educationSecondary')}</option>
          <option value="HighSchool">{t('personnel.educationHighSchool')}</option>
          <option value="Associate">{t('personnel.educationAssociate')}</option>
          <option value="Bachelor">{t('personnel.educationBachelor')}</option>
          <option value="Master">{t('personnel.educationMaster')}</option>
          <option value="Doctorate">{t('personnel.educationDoctorate')}</option>
        </SelectField>
        <SelectField
          id="hr-blood"
          label={t('personnel.bloodType')}
          value={form.bloodType}
          placeholder={t('personnel.placeholders.bloodType')}
          onChange={(bloodType) => patchForm({ bloodType: bloodType as PersonnelForm['bloodType'] })}
          disabled={readOnly}
        >
          <option value="APositive">A+</option>
          <option value="ANegative">A-</option>
          <option value="BPositive">B+</option>
          <option value="BNegative">B-</option>
          <option value="AbPositive">AB+</option>
          <option value="AbNegative">AB-</option>
          <option value="OPositive">O+</option>
          <option value="ONegative">O-</option>
        </SelectField>
        <MobilePhoneField
          id="hr-mobile"
          label={t('personnel.mobilePhone')}
          value={form.mobilePhone}
          onChange={(mobilePhone) => patchForm({ mobilePhone })}
          onBlur={() => blurField('mobilePhone')}
          onUnsafePaste={onMobileUnsafePaste}
          error={fieldMessage('mobilePhone')}
          disabled={readOnly}
        />
        <TextField
          id="hr-email"
          label={t('personnel.email')}
          type="email"
          value={form.email}
          placeholder={t('personnel.placeholders.email')}
          onChange={(email) => patchForm({ email })}
          onBlur={() => blurField('email')}
          error={fieldMessage('email')}
          disabled={readOnly}
        />
        {createMode ? (
          <>
            <DateField
              id="hr-start"
              label={t('workforce.startDate')}
              value={form.employmentStartDate}
              onChange={(employmentStartDate) => patchForm({ employmentStartDate })}
              onBlur={() => blurField('employmentStartDate')}
              error={fieldMessage('employmentStartDate')}
              required
            />
            <SelectField
              id="hr-department"
              label={t('workforce.department')}
              value={form.departmentId}
              placeholder={t('workforce.selectDepartment')}
              onChange={(departmentId) => patchForm({ departmentId })}
              onBlur={() => blurField('departmentId')}
              error={fieldMessage('departmentId')}
              required
            >
              {departments.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </SelectField>
            <SelectField
              id="hr-position"
              label={t('workforce.position')}
              value={form.positionId}
              placeholder={positionSelectPlaceholder(form.departmentId, t)}
              onChange={(positionId) => patchForm({ positionId })}
              onBlur={() => blurField('positionId')}
              error={fieldMessage('positionId')}
              required
              disabled={form.departmentId === ''}
              hint={
                form.departmentId !== '' && positionsForDepartment(positions, form.departmentId).length === 0
                  ? t('personnel.noPositionsForDepartment')
                  : undefined
              }
            >
              {positionsForDepartment(positions, form.departmentId).map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </SelectField>
          </>
        ) : null}
      </div>
      <TextArea
        id="hr-notes"
        label={t('personnel.notes')}
        value={form.hrNotes}
        placeholder={t('personnel.placeholders.notes')}
        onChange={(hrNotes) => patchForm({ hrNotes })}
        onBlur={() => blurField('hrNotes')}
        error={fieldMessage('hrNotes')}
        disabled={readOnly}
      />
    </>
  )
}

function IdentityTab({
  form,
  patchForm,
  patchEmergency,
  setForm,
  readOnly,
  canReadSensitive,
  fieldMessage,
  blurField,
  onMobileUnsafePaste,
}: {
  form: PersonnelForm
  patchForm: (patch: Partial<PersonnelForm>) => void
  patchEmergency: (index: number, patch: Partial<PersonnelForm['emergencyContacts'][number]>) => void
  setForm: (updater: (current: PersonnelForm) => PersonnelForm) => void
  readOnly: boolean
  canReadSensitive: boolean
  fieldMessage: (field: string) => string | undefined
  blurField: (field: string) => void
  onMobileUnsafePaste: () => void
}) {
  const { t } = useTranslation()
  return (
    <>
      <fieldset className={styles.section}>
        <legend className={styles.legend}>{t('personnel.sectionIdentity')}</legend>
        <div className={styles.grid}>
          <SelectField
            id="hr-scheme"
            label={t('personnel.identityScheme')}
            value={form.nationalIdentityScheme}
            placeholder={t('personnel.placeholders.identityScheme')}
            onChange={(nationalIdentityScheme) =>
              patchForm({ nationalIdentityScheme: nationalIdentityScheme as PersonnelForm['nationalIdentityScheme'] })
            }
            onBlur={() => blurField('nationalIdentityScheme')}
            error={fieldMessage('nationalIdentityScheme')}
            disabled={readOnly || !canReadSensitive}
          >
            <option value="Tckn">{t('personnel.schemeTckn')}</option>
            <option value="Ykn">{t('personnel.schemeYkn')}</option>
            <option value="Passport">{t('personnel.schemePassport')}</option>
            <option value="Other">{t('personnel.schemeOther')}</option>
          </SelectField>
          <TextField
            id="hr-id-number"
            label={t('personnel.identityNumber')}
            value={form.nationalIdentityNumber}
            placeholder={t('personnel.placeholders.identityNumber')}
            onChange={(nationalIdentityNumber) =>
              patchForm({
                nationalIdentityNumber: restrictIdentityInput(form.nationalIdentityScheme, nationalIdentityNumber),
              })
            }
            onBlur={() => blurField('nationalIdentityNumber')}
            error={fieldMessage('nationalIdentityNumber')}
            disabled={readOnly || !canReadSensitive}
            autoComplete="off"
            spellCheck={false}
            inputMode={
              form.nationalIdentityScheme === 'Tckn' || form.nationalIdentityScheme === 'Ykn' ? 'numeric' : undefined
            }
            pattern={
              form.nationalIdentityScheme === 'Tckn' || form.nationalIdentityScheme === 'Ykn' ? '[0-9]*' : undefined
            }
            maxLength={
              form.nationalIdentityScheme === 'Tckn' || form.nationalIdentityScheme === 'Ykn' ? 11 : undefined
            }
            onKeyDown={(event) => {
              if (form.nationalIdentityScheme !== 'Tckn' && form.nationalIdentityScheme !== 'Ykn') {
                return
              }
              if (event.key.length !== 1 || event.ctrlKey || event.metaKey || event.altKey) {
                return
              }
              if (event.key < '0' || event.key > '9') {
                event.preventDefault()
                return
              }
              const node = event.currentTarget
              const selected = (node.selectionEnd ?? 0) - (node.selectionStart ?? 0)
              if (selected === 0 && digitsOnly(form.nationalIdentityNumber).length >= TCKN_DIGIT_MAX) {
                event.preventDefault()
              }
            }}
          />
          <TextField
            id="hr-nationality"
            label={t('personnel.nationality')}
            value={form.nationality}
            placeholder={t('personnel.placeholders.nationality')}
            onChange={(nationality) => patchForm({ nationality })}
            onBlur={() => blurField('nationality')}
            error={fieldMessage('nationality')}
            disabled={readOnly}
          />
          <SelectField
            id="hr-gender"
            label={t('personnel.gender')}
            value={form.gender}
            placeholder={t('personnel.placeholders.gender')}
            onChange={(gender) => patchForm({ gender: gender as PersonnelForm['gender'] })}
            disabled={readOnly}
          >
            <option value="Female">{t('personnel.genderFemale')}</option>
            <option value="Male">{t('personnel.genderMale')}</option>
          </SelectField>
          <DateField
            id="hr-birth"
            label={t('personnel.birthDate')}
            value={form.birthDate}
            onChange={(birthDate) => patchForm({ birthDate })}
            onBlur={() => blurField('birthDate')}
            error={fieldMessage('birthDate')}
            disabled={readOnly}
          />
          <TextField
            id="hr-birthplace"
            label={t('personnel.birthPlace')}
            value={form.birthPlace}
            placeholder={t('personnel.placeholders.birthPlace')}
            onChange={(birthPlace) => patchForm({ birthPlace })}
            onBlur={() => blurField('birthPlace')}
            error={fieldMessage('birthPlace')}
            disabled={readOnly}
          />
          <SelectField
            id="hr-marital"
            label={t('personnel.maritalStatus')}
            value={form.maritalStatus}
            placeholder={t('personnel.placeholders.maritalStatus')}
            onChange={(maritalStatus) =>
              patchForm({ maritalStatus: maritalStatus as PersonnelForm['maritalStatus'] })
            }
            disabled={readOnly}
          >
            <option value="Single">{t('personnel.maritalSingle')}</option>
            <option value="Married">{t('personnel.maritalMarried')}</option>
            <option value="Divorced">{t('personnel.maritalDivorced')}</option>
            <option value="Widowed">{t('personnel.maritalWidowed')}</option>
          </SelectField>
        </div>
      </fieldset>
      <fieldset className={styles.section}>
        <legend className={styles.legend}>{t('personnel.sectionContact')}</legend>
        <div className={styles.grid}>
          <MobilePhoneField
            id="hr-mobile-2"
            label={t('personnel.mobilePhone')}
            value={form.mobilePhone}
            onChange={(mobilePhone) => patchForm({ mobilePhone })}
            onBlur={() => blurField('mobilePhone')}
            onUnsafePaste={onMobileUnsafePaste}
            error={fieldMessage('mobilePhone')}
            disabled={readOnly}
          />
          <TextField
            id="hr-home"
            label={t('personnel.homePhone')}
            value={form.homePhone}
            placeholder={t('personnel.placeholders.homePhone')}
            onChange={(homePhone) => patchForm({ homePhone })}
            onBlur={() => blurField('homePhone')}
            error={fieldMessage('homePhone')}
            disabled={readOnly}
          />
          <TextField
            id="hr-email-2"
            label={t('personnel.email')}
            type="email"
            value={form.email}
            placeholder={t('personnel.placeholders.email')}
            onChange={(email) => patchForm({ email })}
            onBlur={() => blurField('email')}
            error={fieldMessage('email')}
            disabled={readOnly}
          />
        </div>
      </fieldset>
      <fieldset className={styles.section}>
        <legend className={styles.legend}>{t('personnel.sectionAddress')}</legend>
        <div className={styles.grid}>
          <TextArea
            id="hr-address"
            label={t('personnel.residenceAddress')}
            value={form.residenceAddress}
            placeholder={t('personnel.placeholders.residenceAddress')}
            onChange={(residenceAddress) => patchForm({ residenceAddress })}
            onBlur={() => blurField('residenceAddress')}
            error={fieldMessage('residenceAddress')}
            disabled={readOnly || !canReadSensitive}
          />
          <TextField
            id="hr-city"
            label={t('personnel.city')}
            value={form.residenceCity}
            placeholder={t('personnel.placeholders.city')}
            onChange={(residenceCity) => patchForm({ residenceCity })}
            onBlur={() => blurField('residenceCity')}
            error={fieldMessage('residenceCity')}
            disabled={readOnly || !canReadSensitive}
          />
          <TextField
            id="hr-district"
            label={t('personnel.district')}
            value={form.residenceDistrict}
            placeholder={t('personnel.placeholders.district')}
            onChange={(residenceDistrict) => patchForm({ residenceDistrict })}
            onBlur={() => blurField('residenceDistrict')}
            error={fieldMessage('residenceDistrict')}
            disabled={readOnly || !canReadSensitive}
          />
          <TextArea
            id="hr-notify"
            label={t('personnel.notificationAddress')}
            value={form.notificationAddress}
            placeholder={t('personnel.placeholders.notificationAddress')}
            onChange={(notificationAddress) => patchForm({ notificationAddress })}
            onBlur={() => blurField('notificationAddress')}
            error={fieldMessage('notificationAddress')}
            disabled={readOnly || !canReadSensitive}
          />
        </div>
      </fieldset>
      <fieldset className={styles.section}>
        <legend className={styles.legend}>{t('personnel.sectionEmergency')}</legend>
        {fieldMessage('emergencyContacts') ? <Notice tone="danger">{fieldMessage('emergencyContacts')}</Notice> : null}
        {!canReadSensitive ? <p className={styles.meta}>{t('personnel.sensitiveHidden')}</p> : null}
        {canReadSensitive && form.emergencyContacts.length === 0 ? (
          <p className={styles.meta}>{t('personnel.noEmergency')}</p>
        ) : null}
        {canReadSensitive
          ? form.emergencyContacts.map((contact, index) => (
              <div className={styles.contactRow} key={contact.id ?? `new-${index}`}>
                <TextField
                  id={`hr-em-name-${index}`}
                  label={t('personnel.emergencyName')}
                  value={contact.name}
                  placeholder={t('personnel.placeholders.emergencyName')}
                  onChange={(name) => patchEmergency(index, { name })}
                  onBlur={() => blurField(`emergencyContacts[${index}].name`)}
                  error={fieldMessage(`emergencyContacts[${index}].name`)}
                  required
                  disabled={readOnly}
                />
                <TextField
                  id={`hr-em-rel-${index}`}
                  label={t('personnel.emergencyRelationship')}
                  value={contact.relationship}
                  placeholder={t('personnel.placeholders.emergencyRelationship')}
                  onChange={(relationship) => patchEmergency(index, { relationship })}
                  onBlur={() => blurField(`emergencyContacts[${index}].relationship`)}
                  error={fieldMessage(`emergencyContacts[${index}].relationship`)}
                  disabled={readOnly}
                />
                <TextField
                  id={`hr-em-phone-${index}`}
                  label={t('personnel.emergencyPhone')}
                  value={contact.phone}
                  placeholder={t('personnel.placeholders.emergencyPhone')}
                  onChange={(phone) => patchEmergency(index, { phone })}
                  onBlur={() => blurField(`emergencyContacts[${index}].phone`)}
                  error={fieldMessage(`emergencyContacts[${index}].phone`)}
                  required
                  disabled={readOnly}
                />
                <label className={styles.primary}>
                  <input
                    type="checkbox"
                    checked={contact.isPrimary}
                    disabled={readOnly}
                    onChange={(event) => {
                      const isPrimary = event.target.checked
                      setForm((current) => ({
                        ...current,
                        emergencyContacts: current.emergencyContacts.map((item, itemIndex) => ({
                          ...item,
                          isPrimary: isPrimary ? itemIndex === index : false,
                        })),
                      }))
                    }}
                  />
                  {t('personnel.emergencyPrimary')}
                </label>
                {readOnly ? null : (
                  <Button
                    variant="ghost"
                    onClick={() =>
                      setForm((current) => ({
                        ...current,
                        emergencyContacts: current.emergencyContacts.filter((_, itemIndex) => itemIndex !== index),
                      }))
                    }
                  >
                    {t('personnel.removeEmergency')}
                  </Button>
                )}
              </div>
            ))
          : null}
        {canReadSensitive && !readOnly ? (
          <Button
            variant="secondary"
            onClick={() =>
              setForm((current) => ({
                ...current,
                emergencyContacts: [
                  ...current.emergencyContacts,
                  { name: '', relationship: '', phone: '', isPrimary: current.emergencyContacts.length === 0 },
                ],
              }))
            }
          >
            {t('personnel.addEmergency')}
          </Button>
        ) : null}
      </fieldset>
    </>
  )
}

function WorkTab({
  form,
  patchForm,
  card,
  createMode,
  readOnly,
  ended,
  language,
  canManageWorkforce,
  departments,
  positions,
  workMode,
  setWorkMode,
  transfer,
  setTransfer,
  endDate,
  setEndDate,
  saving,
  onTransfer,
  onEnd,
  fieldMessage,
  blurField,
}: {
  form: PersonnelForm
  patchForm: (patch: Partial<PersonnelForm>) => void
  card: HrEmployeeCard | null
  createMode: boolean
  readOnly: boolean
  ended: boolean
  language: AppLanguage
  canManageWorkforce: boolean
  departments: DepartmentRecord[]
  positions: PositionRecord[]
  workMode: 'none' | 'transfer' | 'end'
  setWorkMode: (mode: 'none' | 'transfer' | 'end') => void
  transfer: { departmentId: string; positionId: string; effectiveDate: string }
  setTransfer: (value: { departmentId: string; positionId: string; effectiveDate: string } | ((current: { departmentId: string; positionId: string; effectiveDate: string }) => { departmentId: string; positionId: string; effectiveDate: string })) => void
  endDate: string
  setEndDate: (value: string) => void
  saving: boolean
  onTransfer: () => void
  onEnd: () => void
  fieldMessage: (field: string) => string | undefined
  blurField: (field: string) => void
}) {
  const { t } = useTranslation()
  const employment = card?.currentEmployment ?? card?.employments[0]
  return (
    <>
      <div className={styles.grid}>
        <div className={styles.fact}>
          <span className={styles.factLabel}>{t('personnel.organization')}</span>
          <span>{card?.organizationName || '—'}</span>
        </div>
        <div className={styles.fact}>
          <span className={styles.factLabel}>{t('personnel.property')}</span>
          <span>{card?.propertyName || '—'}</span>
        </div>
        <div className={styles.fact}>
          <span className={styles.factLabel}>{t('workforce.status')}</span>
          <span>
            {employment?.status === 'Active'
              ? t('workforce.activeStatus')
              : employment?.status === 'Scheduled'
                ? t('workforce.scheduledStatus')
                : employment?.status === 'Ended'
                  ? t('workforce.endedStatus')
                  : '—'}
          </span>
        </div>
        {createMode ? (
          <>
            <DateField
              id="hr-work-start"
              label={t('workforce.startDate')}
              value={form.employmentStartDate}
              onChange={(employmentStartDate) => patchForm({ employmentStartDate })}
              onBlur={() => blurField('employmentStartDate')}
              error={fieldMessage('employmentStartDate')}
              required
              disabled={readOnly}
            />
            <SelectField
              id="hr-work-department"
              label={t('workforce.department')}
              value={form.departmentId}
              placeholder={t('workforce.selectDepartment')}
              onChange={(departmentId) => patchForm({ departmentId })}
              onBlur={() => blurField('departmentId')}
              error={fieldMessage('departmentId')}
              required
              disabled={readOnly}
            >
              {departments.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </SelectField>
            <SelectField
              id="hr-work-position"
              label={t('workforce.position')}
              value={form.positionId}
              placeholder={positionSelectPlaceholder(form.departmentId, t)}
              onChange={(positionId) => patchForm({ positionId })}
              onBlur={() => blurField('positionId')}
              error={fieldMessage('positionId')}
              required
              disabled={readOnly || form.departmentId === ''}
              hint={
                form.departmentId !== '' && positionsForDepartment(positions, form.departmentId).length === 0
                  ? t('personnel.noPositionsForDepartment')
                  : undefined
              }
            >
              {positionsForDepartment(positions, form.departmentId).map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </SelectField>
          </>
        ) : (
          <>
            <div className={styles.fact}>
              <span className={styles.factLabel}>{t('workforce.startDate')}</span>
              <span>{employment ? formatDateOnly(employment.startDate, language) : '—'}</span>
            </div>
            {ended && employment?.endDate ? (
              <div className={styles.fact}>
                <span className={styles.factLabel}>{t('workforce.endDate')}</span>
                <span>{formatDateOnly(employment.endDate, language)}</span>
              </div>
            ) : null}
            <div className={styles.fact}>
              <span className={styles.factLabel}>{t('workforce.department')}</span>
              <span>{card?.currentPrimaryAssignment?.departmentName || '—'}</span>
            </div>
            <div className={styles.fact}>
              <span className={styles.factLabel}>{t('workforce.position')}</span>
              <span>{card?.currentPrimaryAssignment?.positionName || '—'}</span>
            </div>
          </>
        )}
      </div>
      {canManageWorkforce && !createMode && !ended ? (
        <div className={styles.photoActions}>
          <Button layout="inline" onClick={() => setWorkMode('transfer')}>
            {t('workforce.transfer')}
          </Button>
          <Button variant="danger" onClick={() => setWorkMode('end')}>
            {t('workforce.endEmployment')}
          </Button>
        </div>
      ) : null}
      {workMode === 'transfer' ? (
        <form
          className={styles.section}
          onSubmit={(event) => {
            event.preventDefault()
            onTransfer()
          }}
        >
          <p className={styles.meta}>{t('workforce.transferIntro')}</p>
          <div className={styles.grid}>
            <SelectField
              id="card-transfer-department"
              label={t('workforce.newDepartment')}
              value={transfer.departmentId}
              placeholder={t('workforce.selectDepartment')}
              onChange={(departmentId) =>
                setTransfer((current) => ({
                  ...current,
                  departmentId,
                  positionId: retainedPositionId(positions, departmentId, current.positionId),
                }))
              }
              required
            >
              {departments.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </SelectField>
            <SelectField
              id="card-transfer-position"
              label={t('workforce.newPosition')}
              value={transfer.positionId}
              placeholder={positionSelectPlaceholder(transfer.departmentId, t)}
              onChange={(positionId) => setTransfer((current) => ({ ...current, positionId }))}
              required
              disabled={transfer.departmentId === ''}
              hint={
                transfer.departmentId !== ''
                && positionsForDepartment(positions, transfer.departmentId).length === 0
                  ? t('personnel.noPositionsForDepartment')
                  : undefined
              }
            >
              {positionsForDepartment(positions, transfer.departmentId).map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </SelectField>
            <DateField
              id="card-transfer-date"
              label={t('workforce.effectiveDate')}
              value={transfer.effectiveDate}
              onChange={(effectiveDate) => setTransfer((current) => ({ ...current, effectiveDate }))}
              required
            />
          </div>
          <div className={styles.photoActions}>
            <Button type="submit" layout="inline" loading={saving}>
              {t('workforce.transferSubmit')}
            </Button>
            <Button variant="ghost" onClick={() => setWorkMode('none')}>
              {t('workforce.cancel')}
            </Button>
          </div>
        </form>
      ) : null}
      {workMode === 'end' ? (
        <form
          className={styles.section}
          onSubmit={(event) => {
            event.preventDefault()
            onEnd()
          }}
        >
          <Notice tone="warning">{t('workforce.confirmEnd')}</Notice>
          <DateField id="card-end-date" label={t('workforce.endDate')} value={endDate} onChange={setEndDate} required />
          <div className={styles.photoActions}>
            <Button type="submit" variant="danger" layout="inline" loading={saving}>
              {t('workforce.endEmploymentSubmit')}
            </Button>
            <Button variant="ghost" onClick={() => setWorkMode('none')}>
              {t('workforce.cancel')}
            </Button>
          </div>
        </form>
      ) : null}
    </>
  )
}

function HistoryTab({
  card,
  language,
  createMode,
}: {
  card: HrEmployeeCard | null
  language: AppLanguage
  createMode: boolean
}) {
  const { t } = useTranslation()
  if (createMode) {
    return <EmptyState compact title={t('personnel.historyEmptyCreate')} />
  }

  const items = (card?.employments ?? []).flatMap((employment) =>
    employment.primaryAssignments.map((assignment) => ({ employment, assignment })),
  )

  if (items.length === 0) {
    return <EmptyState compact title={t('workforce.noHistory')} />
  }

  return (
    <Timeline label={t('workforce.workHistory')}>
      {items.map(({ employment, assignment }) => (
        <TimelineItem
          key={assignment.id}
          time={formatDateOnly(assignment.startDate, language)}
          supporting={
            assignment.endDate ? formatDateOnly(assignment.endDate, language) : t('workforce.present')
          }
          marker={assignment.endDate || employment.status === 'Ended' ? 'neutral' : 'success'}
        >
          <span>
            {t('personnel.assignmentPeriod')}: {assignment.departmentName} · {assignment.positionName}
          </span>
          <span className={styles.meta}>
            {t('personnel.employmentPeriod')}: {formatDateOnly(employment.startDate, language)}
            {employment.endDate ? ` – ${formatDateOnly(employment.endDate, language)}` : ''}
          </span>
        </TimelineItem>
      ))}
    </Timeline>
  )
}
