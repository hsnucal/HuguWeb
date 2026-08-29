import { useEffect, useMemo, useRef, useState, type ReactNode, type RefObject } from 'react'
import { useTranslation } from 'react-i18next'
import { addDaysIso, formatDateOnly, laterIsoDate, todayIsoDate } from '../i18n/format'
import { DEFAULT_LANGUAGE, toAppLanguage, type AppLanguage } from '../i18n/languages'
import { AvatarMark } from '../ui/AvatarMark'
import { Button } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import {
  BanknoteIcon,
  BriefcaseIcon,
  BuildingIcon,
  CalendarIcon,
  CloseIcon,
  HistoryClockIcon,
  IdCardIcon,
  OfficialSealIcon,
  PersonIcon,
  RoleBadgeIcon,
  WarningIcon,
} from '../ui/icons'
import { Notice } from '../ui/Notice'
import { DateField, SelectField } from '../ui/SelectField'
import { SearchableSelect } from '../ui/SearchableSelect'
import { Skeleton } from '../ui/Skeleton'
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
  listHrSgkWorkplaces,
  listOfficialLookups,
  removeHrEmployeePhoto,
  searchOccupationCodes,
  updateHrEmployee,
  uploadHrEmployeePhoto,
  type DrivingLicenceCategory,
  type EmploymentContractType,
  type ForeignLanguageSummary,
  type HrEmployeeCard,
  type IskurStatus,
  type IskurWorkforceStatus,
  type MilitaryServiceStatus,
  type OfficialLookupItem,
  type OfficialLookups,
  type SgkWorkplaceRecord,
} from './hrApi'
import {
  getHrEmployeeErpAccount,
  getHrPersonnelHistory,
  saveHrPaymentProfile,
  type EmployeeErpAccountSummary,
  type PersonnelHistoryResponse,
} from './hrPersonnelMasterApi'
import { canManageAuthorizationUsers } from '../authorization/authorizationAccess'
import { useAuthSession } from '../auth/AuthContext'
import { Link } from 'react-router'
import { nationalityLabel } from './nationalityDisplay'
import {
  emptyPersonnelForm,
  formFromCard,
  hasPaymentInput,
  isPersonnelFormDirty,
  snapshotOf,
  toHrWrite,
  type PersonnelForm,
} from './personnelForm'
import { restrictIdentityInput, TCKN_DIGIT_MAX, digitsOnly } from './personnelInput'
import {
  firstInvalidTarget,
  invalidPersonnelTabs,
  HrValidationCodes,
  officialSectionForField,
  revalidateKnownErrors,
  validatePersonnelField,
  validatePersonnelForm,
  validationMessageKeyFor,
  type FieldErrors,
  type OfficialSectionId,
} from './personnelValidation'
import {
  resolveUniversityName,
  universitySelectOptions,
  usesUniversitySchoolField,
} from './trUniversities'
import type { DepartmentRecord, EmploymentTerminationReason, PositionRecord } from './workforceApi'
import { endEmployment, transferEmployee } from './workforceApi'
import { positionsForDepartment, retainedPositionId } from './assignmentOptions'
import { toPersistedIban } from './paymentIban'
import { TurkishIbanField } from './TurkishIbanField'
import {
  districtSelectOptions,
  provinceSelectOptions,
  retainedDistrict,
} from './trProvinces'
import { employmentStatusTone } from './workforceStatus'
import { PersonnelLeaveTab } from './PersonnelLeaveTab'

type TabId = 'general' | 'identity' | 'work' | 'official' | 'payment' | 'leave' | 'history'
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
  canReadLeave = false,
  canManageLeave = false,
  onClose,
  onSaved,
}: {
  mode: CardMode
  departments: DepartmentRecord[]
  positions: PositionRecord[]
  canManage: boolean
  canManageWorkforce: boolean
  canReadSensitive: boolean
  canReadLeave?: boolean
  canManageLeave?: boolean
  onClose: () => void
  onSaved: (employeeId?: string) => Promise<void> | void
}) {
  const { t, i18n } = useTranslation()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const fileInput = useRef<HTMLInputElement>(null)
  const givenNameInput = useRef<HTMLInputElement>(null)
  const continueEditingRef = useRef<HTMLButtonElement>(null)
  const [tab, setTab] = useState<TabId>('general')
  const [officialSection, setOfficialSection] = useState<OfficialSectionId>('declaration')
  const [dialogPhase, setDialogPhase] = useState<'open' | 'closing'>('open')
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
  const [lookups, setLookups] = useState<OfficialLookups | null>(null)
  const [workplaces, setWorkplaces] = useState<SgkWorkplaceRecord[] | null>(null)
  const pendingFocus = useRef<string | null>(null)
  const saveLock = useRef(false)
  const createdId = useRef<string | null>(null)
  const formLatest = useRef(form)
  const [transfer, setTransfer] = useState({
    departmentId: '',
    positionId: '',
    effectiveDate: todayIsoDate(),
  })
  const [endDate, setEndDate] = useState(todayIsoDate())
  const [terminationReason, setTerminationReason] = useState<EmploymentTerminationReason | ''>('')

  useEffect(() => {
    formLatest.current = form
  }, [form])

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
        formLatest.current = next
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

  useEffect(() => {
    let cancelled = false
    async function loadOfficial() {
      try {
        const [lookupRows, workplaceRows] = await Promise.all([listOfficialLookups(), listHrSgkWorkplaces()])
        if (!cancelled) {
          setLookups(lookupRows)
          setWorkplaces(workplaceRows)
        }
      } catch {
        if (!cancelled) {
          setLookups({ documentTypes: [], applicableLaws: [], insuranceBranches: [], dutyCodes: [], nationalities: [] })
          setWorkplaces([])
        }
      }
    }

    void loadOfficial()
    return () => {
      cancelled = true
    }
  }, [])

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
    const target = firstInvalidTarget(errors, formLatest.current, mode.type === 'create')
    if (!target) {
      return
    }

    pendingFocus.current = target.controlId
    if (tab !== target.tab) {
      setTab(target.tab)
    }
    if (target.tab === 'official') {
      setOfficialSection(target.officialSection ?? officialSectionForField(target.field))
    }
    setFocusNonce((value) => value + 1)
  }

  function blurField(field: string) {
    const code = validatePersonnelField(formLatest.current, field, validationContext)
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
    const next = { ...formLatest.current, ...patch }
    if (patch.departmentId !== undefined) {
      next.positionId = retainedPositionId(positions, patch.departmentId, next.positionId)
    }
    if (patch.residenceCity !== undefined) {
      next.residenceDistrict = retainedDistrict(patch.residenceCity, next.residenceDistrict)
    }
    if (patch.nationalIdentityScheme !== undefined) {
      next.nationalIdentityNumber = restrictIdentityInput(
        patch.nationalIdentityScheme,
        next.nationalIdentityNumber,
      )
    }
    formLatest.current = next
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
      if (patch.departmentId !== undefined) {
        extra.push('positionId')
      }
      if (patch.residenceCity !== undefined) {
        extra.push('residenceDistrict')
      }
      // Bank name can require IBAN immediately; IBAN keystrokes only revalidate once already errored
      // (via Object.keys(errors)) so incomplete values do not flash until blur/save.
      if (patch.paymentBankName !== undefined) {
        extra.push('paymentIban', 'paymentBankName')
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

  function beginClose() {
    if (dialogPhase === 'closing') {
      return
    }

    setDialogPhase('closing')
  }

  function requestClose() {
    if (dialogPhase === 'closing') {
      return
    }

    if (dirty) {
      setConfirming(true)
      return
    }

    beginClose()
  }

  function continueEditing() {
    setConfirming(false)
  }

  function discardChanges() {
    setConfirming(false)
    beginClose()
  }

  function markMobileInvalid() {
    setFieldErrors((current) => ({ ...current, mobilePhone: HrValidationCodes.phoneInvalid }))
  }

  function markEmergencyPhoneInvalid(index: number) {
    setFieldErrors((current) => ({
      ...current,
      [`emergencyContacts[${index}].phone`]: HrValidationCodes.phoneInvalid,
    }))
  }

  async function persistPayment(employeeId: string) {
    const paymentForm = formLatest.current
    if (!canReadSensitive || !canManage || !hasPaymentInput(paymentForm)) {
      return
    }

    await saveHrPaymentProfile(
      employeeId,
      toPersistedIban(paymentForm.paymentIban),
      paymentForm.paymentBankName.trim() === '' ? null : paymentForm.paymentBankName.trim(),
    )
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
    const draft = formLatest.current
    const clientErrors = validatePersonnelForm(draft, validationContext)
    if (Object.keys(clientErrors).length > 0) {
      showFieldErrors(clientErrors)
      return
    }

    saveLock.current = true
    setSaving(true)
    try {
      if (mode.type === 'create') {
        if (createdId.current === null) {
          const created = await createHrEmployee(toHrWrite(draft, true))
          createdId.current = created.employeeId
        }

        const employeeId = createdId.current
        await persistPhoto(employeeId)
        try {
          await persistPayment(employeeId)
        } catch (reason) {
          const mapped = hrFieldErrorsFromProblem(reason)
          if (Object.keys(mapped).length > 0) {
            showFieldErrors(mapped)
          } else {
            setError(t('personnel.paymentCreateFailed'))
          }
          setTab('payment')
          await onSaved(employeeId)
          return
        }
        assignPendingPhoto(null)
        setSnapshot(snapshotOf(draft))
        setFieldErrors({})
        await onSaved()
        beginClose()
        return
      }

      const updated = await updateHrEmployee(mode.employeeId, toHrWrite(draft, false))
      await persistPhoto(mode.employeeId)
      try {
        await persistPayment(mode.employeeId)
      } catch (reason) {
        const mapped = hrFieldErrorsFromProblem(reason)
        if (Object.keys(mapped).length > 0) {
          showFieldErrors(mapped)
        } else {
          setError(t(hrErrorKey(reason)))
        }
        setTab('payment')
        await onSaved()
        return
      }
      const detail = updated.hasPhoto !== undefined ? await getHrEmployee(mode.employeeId) : updated
      const next = formFromCard(detail)
      setCard(detail)
      formLatest.current = next
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

    if (terminationReason === '') {
      setError(t('personnel.validation.terminationReasonRequired'))
      return
    }

    setError(null)
    setSaving(true)
    try {
      await endEmployment(mode.employeeId, endDate, terminationReason)
      const detail = await getHrEmployee(mode.employeeId)
      const next = formFromCard(detail)
      setCard(detail)
      setForm(next)
      setSnapshot(snapshotOf(next))
      setWorkMode('none')
      setTerminationReason('')
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
  const invalidTabs = invalidPersonnelTabs(fieldErrors, form, mode.type === 'create')

  return (
    <>
    <WorkspaceDialog
      title={title}
      hideHeader
      bodyOverflow="hidden"
      closing={dialogPhase === 'closing'}
      onCloseAnimationComplete={onClose}
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
      {loading ? (
        <div className={styles.loading}>
          <Skeleton variant="block" label={t('workforce.loading')} />
        </div>
      ) : (
        <div className={styles.card}>
          <header className={styles.appHeader}>
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
            <div className={styles.summaries}>
              <SummaryCard icon={<BuildingIcon />} label={t('workforce.department')} value={departmentName} />
              <SummaryCard icon={<RoleBadgeIcon />} label={t('workforce.position')} value={positionName} />
              <SummaryCard
                icon={<CalendarIcon />}
                label={t('workforce.startDate')}
                value={form.employmentStartDate ? formatDateOnly(form.employmentStartDate, language) : '—'}
              />
            </div>
            <Button
              className={styles.closeButton}
              variant="ghost"
              aria-label={t('personnel.closeCard')}
              tabIndex={dialogPhase === 'closing' ? -1 : undefined}
              onClick={requestClose}
            >
              <CloseIcon />
            </Button>
          </header>

          <div className={styles.shell}>
            <nav className={styles.nav} aria-label={title} role="tablist">
              {(
                [
                  ['general', t('personnel.tabGeneral'), <PersonIcon key="general" />],
                  ['identity', t('personnel.tabIdentity'), <IdCardIcon key="identity" />],
                  ['work', t('personnel.tabWork'), <BriefcaseIcon key="work" />],
                  ['official', t('personnel.tabOfficial'), <OfficialSealIcon key="official" />],
                  ['payment', t('personnel.tabPayment'), <BanknoteIcon key="payment" />],
                  ...(mode.type === 'edit' && canReadLeave
                    ? ([['leave', t('personnel.leave.tab'), <CalendarIcon key="leave" />]] as const)
                    : []),
                  ['history', t('personnel.tabHistory'), <HistoryClockIcon key="history" />],
                ] as const
              ).map(([id, label, icon]) => {
                const tabInvalid = id !== 'leave' && invalidTabs.has(id)
                return (
                <button
                  key={id}
                  type="button"
                  role="tab"
                  aria-selected={tab === id}
                  aria-invalid={tabInvalid || undefined}
                  className={[
                    tab === id ? styles.navItemCurrent : styles.navItem,
                    tabInvalid ? styles.navItemInvalid : '',
                  ].filter(Boolean).join(' ')}
                  onClick={() => setTab(id)}
                >
                  <span className={styles.navIcon} aria-hidden="true">{icon}</span>
                  {label}
                </button>
                )
              })}
            </nav>

            <div className={styles.workspace}>
              <div className={styles.workspaceStack}>
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
                    lookups={lookups}
                    language={language}
                    fieldMessage={fieldMessage}
                    blurField={blurField}
                    onMobileUnsafePaste={markMobileInvalid}
                    onEmergencyUnsafePaste={markEmergencyPhoneInvalid}
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
                    terminationReason={terminationReason}
                    setTerminationReason={setTerminationReason}
                    saving={saving}
                    onTransfer={() => void onTransfer()}
                    onEnd={() => void onEnd()}
                    fieldMessage={fieldMessage}
                    blurField={blurField}
                  />
                ) : null}

                {tab === 'official' ? (
                  <OfficialTab
                    form={form}
                    patchForm={patchForm}
                    readOnly={readOnly}
                    lookups={lookups}
                    workplaces={workplaces}
                    fieldMessage={fieldMessage}
                    blurField={blurField}
                    officialSection={officialSection}
                    onOfficialSection={setOfficialSection}
                  />
                ) : null}

                {tab === 'payment' ? (
                  <PaymentTab
                    form={form}
                    patchForm={patchForm}
                    fieldMessage={fieldMessage}
                    blurField={blurField}
                    readOnly={readOnly || !canManage}
                    canReadSensitive={canReadSensitive}
                    employeeId={mode.type === 'edit' ? mode.employeeId : null}
                  />
                ) : null}

                {tab === 'leave' && mode.type === 'edit' && canReadLeave ? (
                  <PersonnelLeaveTab
                    employeeId={mode.employeeId}
                    canManage={canManageLeave}
                    language={language}
                  />
                ) : null}

                {tab === 'history' ? (
                  <HistoryTab
                    card={card}
                    language={language}
                    createMode={mode.type === 'create'}
                    employeeId={mode.type === 'edit' ? mode.employeeId : null}
                  />
                ) : null}
              </div>
            </div>
          </div>
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

function lookupLabel(item: OfficialLookupItem) {
  return `${item.code} — ${item.description}`
}

function SummaryCard({ icon, label, value }: { icon: ReactNode; label: string; value: string }) {
  return (
    <div className={styles.summaryCard}>
      <span className={styles.summaryIcon} aria-hidden="true">{icon}</span>
      <span className={styles.summaryLabel}>{label}</span>
      <span className={styles.summaryValue}>{value}</span>
    </div>
  )
}

function Reveal({ children }: { children: ReactNode }) {
  return <div className={styles.reveal}>{children}</div>
}

function OfficialTab({
  form,
  patchForm,
  readOnly,
  lookups,
  workplaces,
  fieldMessage,
  blurField,
  officialSection,
  onOfficialSection,
}: {
  form: PersonnelForm
  patchForm: (patch: Partial<PersonnelForm>) => void
  readOnly: boolean
  lookups: OfficialLookups | null
  workplaces: SgkWorkplaceRecord[] | null
  fieldMessage: (field: string) => string | undefined
  blurField: (field: string) => void
  officialSection: OfficialSectionId
  onOfficialSection: (section: OfficialSectionId) => void
}) {
  const { t } = useTranslation()

  if (lookups === null || workplaces === null) {
    return <Skeleton variant="block" label={t('workforce.loading')} />
  }

  const workplaceOptions = workplaces.filter(
    (item) => item.isActive || item.id === form.sgkWorkplaceRegistrationId,
  )
  const occupationOptions: OfficialLookupItem[] = form.occupationCode
    ? [{ code: form.occupationCode, description: form.occupationLabel.replace(`${form.occupationCode} — `, ''), isActive: true }]
    : []
  const licenceOptions: { value: DrivingLicenceCategory; label: string }[] = [
    { value: 'A', label: 'A' },
    { value: 'A1', label: 'A1' },
    { value: 'A2', label: 'A2' },
    { value: 'B', label: 'B' },
    { value: 'B1', label: 'B1' },
    { value: 'Be', label: 'BE' },
    { value: 'C', label: 'C' },
    { value: 'Ce', label: 'CE' },
    { value: 'D', label: 'D' },
    { value: 'De', label: 'DE' },
    { value: 'F', label: 'F' },
    { value: 'G', label: 'G' },
  ]
  const passportReuse = form.nationalIdentityScheme === 'Passport' ? form.nationalIdentityNumber : ''

  const officialChips: { id: OfficialSectionId; label: string }[] = [
    { id: 'declaration', label: t('personnel.sectionDeclarationCodes') },
    { id: 'iskur', label: t('personnel.officialChipIskur') },
    { id: 'bes', label: t('personnel.officialChipBes') },
    { id: 'social', label: t('personnel.sectionSocial') },
    { id: 'education', label: t('personnel.sectionEducation') },
  ]

  return (
    <>
      <div className={styles.chips} role="tablist" aria-label={t('personnel.tabOfficial')}>
        {officialChips.map((item) => (
          <button
            key={item.id}
            type="button"
            role="tab"
            aria-selected={officialSection === item.id}
            className={officialSection === item.id ? styles.chipCurrent : styles.chip}
            onClick={() => onOfficialSection(item.id)}
          >
            {item.label}
          </button>
        ))}
      </div>
      {officialSection === 'declaration' ? (
      <fieldset className={styles.section}>
        {workplaces.length === 0 ? (
          <Notice tone="info">{t('personnel.noSgkWorkplaceForProperty')}</Notice>
        ) : null}
        <div className={styles.grid}>
          <SelectField
            id="hr-sgk-workplace"
            label={t('personnel.sgkWorkplace')}
            value={form.sgkWorkplaceRegistrationId}
            onChange={(value) => patchForm({ sgkWorkplaceRegistrationId: value })}
            onBlur={() => blurField('sgkWorkplaceRegistrationId')}
            disabled={readOnly}
            placeholder={t('personnel.placeholders.sgkWorkplace')}
            error={fieldMessage('sgkWorkplaceRegistrationId')}
          >
            {workplaceOptions.map((item) => (
              <option key={item.id} value={item.id} disabled={!item.isActive && item.id !== form.sgkWorkplaceRegistrationId}>
                {item.pickerLabel}
              </option>
            ))}
          </SelectField>
          <SelectField
            id="hr-document-type"
            label={t('personnel.documentType')}
            value={form.documentTypeCode}
            onChange={(value) => patchForm({ documentTypeCode: value })}
            onBlur={() => blurField('documentTypeCode')}
            disabled={readOnly}
            placeholder={t('personnel.placeholders.documentType')}
            error={fieldMessage('documentTypeCode')}
          >
            {lookups.documentTypes
              .filter((item) => item.isActive || item.code === form.documentTypeCode)
              .map((item) => (
                <option key={item.code} value={item.code} disabled={!item.isActive && item.code !== form.documentTypeCode}>
                  {lookupLabel(item)}
                </option>
              ))}
          </SelectField>
          <SelectField
            id="hr-applicable-law"
            label={t('personnel.applicableLaw')}
            value={form.applicableLawCode}
            onChange={(value) => patchForm({ applicableLawCode: value })}
            onBlur={() => blurField('applicableLawCode')}
            disabled={readOnly}
            placeholder={t('personnel.placeholders.applicableLaw')}
            error={fieldMessage('applicableLawCode')}
          >
            {lookups.applicableLaws
              .filter((item) => item.isActive || item.code === form.applicableLawCode)
              .map((item) => (
                <option key={item.code} value={item.code} disabled={!item.isActive && item.code !== form.applicableLawCode}>
                  {lookupLabel(item)}
                </option>
              ))}
          </SelectField>
          <SelectField
            id="hr-insurance-branch"
            label={t('personnel.insuranceBranch')}
            value={form.insuranceBranchCode}
            onChange={(value) => patchForm({ insuranceBranchCode: value })}
            onBlur={() => blurField('insuranceBranchCode')}
            disabled={readOnly}
            placeholder={t('personnel.placeholders.insuranceBranch')}
            error={fieldMessage('insuranceBranchCode')}
          >
            {lookups.insuranceBranches
              .filter((item) => item.isActive || item.code === form.insuranceBranchCode)
              .map((item) => (
                <option key={item.code} value={item.code} disabled={!item.isActive && item.code !== form.insuranceBranchCode}>
                  {lookupLabel(item)}
                </option>
              ))}
          </SelectField>
          <SearchableSelect
            id="hr-occupation"
            label={t('personnel.occupationCode')}
            value={form.occupationCode}
            options={occupationOptions.map((item) => ({ value: item.code, label: lookupLabel(item) }))}
            onChange={(value, option) =>
              patchForm({ occupationCode: value, occupationLabel: option?.label ?? '' })
            }
            onBlur={() => blurField('occupationCode')}
            onQuery={async (query) => {
              const rows = await searchOccupationCodes(query)
              return rows.map((item) => ({ value: item.code, label: lookupLabel(item) }))
            }}
            placeholder={t('personnel.placeholders.occupationCode')}
            emptyText={t('personnel.occupationEmpty')}
            loadingText={t('personnel.occupationSearching')}
            disabled={readOnly}
            error={fieldMessage('occupationCode')}
            hint={t('personnel.occupationHint')}
            searchIcon
          />
          <SelectField
            id="hr-duty-code"
            label={t('personnel.dutyCode')}
            value={form.dutyCode}
            onChange={(value) => patchForm({ dutyCode: value })}
            onBlur={() => blurField('dutyCode')}
            disabled={readOnly}
            placeholder={t('personnel.placeholders.dutyCode')}
            error={fieldMessage('dutyCode')}
          >
            {lookups.dutyCodes
              .filter((item) => item.isActive || item.code === form.dutyCode)
              .map((item) => (
                <option key={item.code} value={item.code} disabled={!item.isActive && item.code !== form.dutyCode}>
                  {t(`personnel.dutyCodes.${item.code}`, { defaultValue: item.description })}
                </option>
              ))}
          </SelectField>
        </div>
      </fieldset>
      ) : null}

      {officialSection === 'iskur' ? (
      <fieldset className={styles.section}>
        <div className={styles.grid}>
          <SelectField
            id="hr-iskur-status"
            label={t('personnel.iskurStatus')}
            value={form.iskurStatus}
            onChange={(value) => patchForm({ iskurStatus: value as IskurStatus | '' })}
            disabled={readOnly}
            placeholder={t('personnel.placeholders.iskurStatus')}
          >
            <option value="Normal">{t('personnel.iskurNormal')}</option>
            <option value="FormerConvict">{t('personnel.iskurFormerConvict')}</option>
            <option value="TerrorVictim">{t('personnel.iskurTerrorVictim')}</option>
            <option value="TmyInjured">{t('personnel.iskurTmy')}</option>
          </SelectField>
          <DateField
            id="hr-incentive-start"
            label={t('personnel.incentiveStartDate')}
            value={form.incentiveStartDate}
            onChange={(incentiveStartDate) => patchForm({ incentiveStartDate })}
            onBlur={() => blurField('incentiveStartDate')}
            error={fieldMessage('incentiveStartDate')}
            disabled={readOnly}
          />
          <DateField
            id="hr-incentive-end"
            label={t('personnel.incentiveEndDate')}
            value={form.incentiveEndDate}
            onChange={(incentiveEndDate) => patchForm({ incentiveEndDate })}
            onBlur={() => blurField('incentiveEndDate')}
            error={fieldMessage('incentiveEndDate')}
            disabled={readOnly}
          />
          <SelectField
            id="hr-iskur-workforce"
            label={t('personnel.iskurWorkforceStatus')}
            value={form.iskurWorkforceStatus}
            onChange={(value) => patchForm({ iskurWorkforceStatus: value as IskurWorkforceStatus | '' })}
            disabled={readOnly}
            placeholder={t('personnel.placeholders.iskurWorkforceStatus')}
          >
            <option value="Indefinite">{t('personnel.iskurWfIndefinite')}</option>
            <option value="FixedTerm">{t('personnel.iskurWfFixedTerm')}</option>
            <option value="PartTime">{t('personnel.iskurWfPartTime')}</option>
            <option value="DisabledIndefinite">{t('personnel.iskurWfDisabledIndefinite')}</option>
            <option value="DisabledFixedTerm">{t('personnel.iskurWfDisabledFixedTerm')}</option>
            <option value="FormerConvict">{t('personnel.iskurWfFormerConvict')}</option>
            <option value="TerrorVictim">{t('personnel.iskurWfTerrorVictim')}</option>
          </SelectField>
        </div>
      </fieldset>
      ) : null}

      {officialSection === 'bes' ? (
      <fieldset className={styles.section}>
        <label className={styles.checkRow}>
          <input
            id="hr-bes-enabled"
            type="checkbox"
            checked={form.besDeductionEnabled}
            disabled={readOnly}
            onChange={(event) =>
              patchForm({
                besDeductionEnabled: event.target.checked,
                besRatePercent: event.target.checked ? form.besRatePercent : '',
                besExtraAmount: event.target.checked ? form.besExtraAmount : '',
              })
            }
          />
          {t('personnel.besDeduction')}
        </label>
        {form.besDeductionEnabled ? (
        <Reveal>
        <div className={styles.grid}>
          <TextField
            id="hr-bes-rate"
            label={t('personnel.besRatePercent')}
            value={form.besRatePercent}
            onChange={(besRatePercent) => patchForm({ besRatePercent })}
            onBlur={() => blurField('besRatePercent')}
            error={fieldMessage('besRatePercent')}
            inputMode="decimal"
            disabled={readOnly}
          />
          <TextField
            id="hr-bes-extra"
            label={t('personnel.besExtraAmount')}
            value={form.besExtraAmount}
            onChange={(besExtraAmount) => patchForm({ besExtraAmount })}
            onBlur={() => blurField('besExtraAmount')}
            error={fieldMessage('besExtraAmount')}
            inputMode="decimal"
            disabled={readOnly}
          />
        </div>
        </Reveal>
        ) : null}
      </fieldset>
      ) : null}

      {officialSection === 'social' ? (
      <fieldset className={styles.section}>
        <legend className={styles.legend}>{t('personnel.sectionSocial')}</legend>
        <div className={styles.socialStack}>
          <div className={styles.subSection}>
            <h3 className={styles.subTitle}>{t('personnel.sectionLicence')}</h3>
            <SelectField
              id="hr-licence"
              label={t('personnel.drivingLicence')}
              value={form.drivingLicenceCategory}
              onChange={(value) => patchForm({ drivingLicenceCategory: value as DrivingLicenceCategory | '' })}
              disabled={readOnly}
              placeholder={t('personnel.placeholders.drivingLicence')}
            >
              {licenceOptions.map((item) => (
                <option key={item.value} value={item.value}>
                  {item.label}
                </option>
              ))}
            </SelectField>
          </div>
          <div className={styles.subSection}>
            <h3 className={styles.subTitle}>{t('personnel.sectionPassport')}</h3>
            <TextField
              id="hr-passport-reuse"
              label={t('personnel.passportIdentity')}
              value={passportReuse}
              onChange={() => undefined}
              readOnly
              hint={
                form.nationalIdentityScheme === 'Passport'
                  ? t('personnel.passportReused')
                  : t('personnel.passportOnIdentityTab')
              }
            />
          </div>
          <div className={styles.subSection}>
            <h3 className={styles.subTitle}>{t('personnel.sectionMilitary')}</h3>
            <div className={styles.grid}>
              <SelectField
                id="hr-military"
                label={t('personnel.militaryStatus')}
                value={form.militaryServiceStatus}
                onChange={(value) =>
                  patchForm({
                    militaryServiceStatus: value as MilitaryServiceStatus | '',
                    militaryExemptionReason: value === 'Exempt' ? form.militaryExemptionReason : '',
                    militaryDefermentReason: value === 'Deferred' ? form.militaryDefermentReason : '',
                  })
                }
                disabled={readOnly}
                placeholder={t('personnel.placeholders.militaryStatus')}
              >
                <option value="Completed">{t('personnel.militaryCompleted')}</option>
                <option value="Exempt">{t('personnel.militaryExempt')}</option>
                <option value="Deferred">{t('personnel.militaryDeferred')}</option>
                <option value="NotCompleted">{t('personnel.militaryNotCompleted')}</option>
              </SelectField>
              {form.militaryServiceStatus === 'Exempt' ? (
                <Reveal>
                  <TextField
                    id="hr-military-exemption"
                    label={t('personnel.militaryExemptionReason')}
                    value={form.militaryExemptionReason}
                    onChange={(militaryExemptionReason) => patchForm({ militaryExemptionReason })}
                    onBlur={() => blurField('militaryExemptionReason')}
                    error={fieldMessage('militaryExemptionReason')}
                    required
                    disabled={readOnly}
                  />
                </Reveal>
              ) : null}
              {form.militaryServiceStatus === 'Deferred' ? (
                <Reveal>
                  <TextField
                    id="hr-military-deferment"
                    label={t('personnel.militaryDefermentReason')}
                    value={form.militaryDefermentReason}
                    onChange={(militaryDefermentReason) => patchForm({ militaryDefermentReason })}
                    onBlur={() => blurField('militaryDefermentReason')}
                    error={fieldMessage('militaryDefermentReason')}
                    required
                    disabled={readOnly}
                  />
                </Reveal>
              ) : null}
            </div>
          </div>
          <div className={styles.subSection}>
            <h3 className={styles.subTitle}>{t('personnel.sectionKep')}</h3>
            <TextField
              id="hr-kep"
              label={t('personnel.kepAddress')}
              value={form.kepAddress}
              onChange={(kepAddress) => patchForm({ kepAddress })}
              onBlur={() => blurField('kepAddress')}
              error={fieldMessage('kepAddress')}
              disabled={readOnly}
            />
          </div>
          <div className={styles.subSection}>
            <h3 className={styles.subTitle}>{t('personnel.sectionWorkPermit')}</h3>
            <div className={styles.grid}>
              <DateField
                id="hr-work-permit-start"
                label={t('personnel.workPermitStartDate')}
                value={form.workPermitStartDate}
                onChange={(workPermitStartDate) => patchForm({ workPermitStartDate })}
                onBlur={() => blurField('workPermitStartDate')}
                error={fieldMessage('workPermitStartDate')}
                disabled={readOnly}
              />
              <DateField
                id="hr-work-permit-end"
                label={t('personnel.workPermitEndDate')}
                value={form.workPermitEndDate}
                onChange={(workPermitEndDate) => patchForm({ workPermitEndDate })}
                onBlur={() => blurField('workPermitEndDate')}
                error={fieldMessage('workPermitEndDate')}
                disabled={readOnly}
              />
            </div>
          </div>
        </div>
      </fieldset>
      ) : null}

      {officialSection === 'education' ? (
      <fieldset className={styles.section}>
        <legend className={styles.legend}>{t('personnel.sectionEducation')}</legend>
        <div className={styles.grid}>
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
          {usesUniversitySchoolField(form.educationLevel) ? (
            <SearchableSelect
              id="hr-school"
              label={t('personnel.schoolName')}
              value={resolveUniversityName(form.schoolName)}
              options={universitySelectOptions(form.schoolName)}
              onChange={(schoolName) => patchForm({ schoolName })}
              onBlur={() => blurField('schoolName')}
              error={fieldMessage('schoolName')}
              placeholder={t('personnel.universityPlaceholder')}
              emptyText={t('personnel.universityEmpty')}
              hint={t('personnel.universityHint')}
              disabled={readOnly}
              searchIcon
            />
          ) : (
            <TextField
              id="hr-school"
              label={t('personnel.schoolName')}
              value={form.schoolName}
              onChange={(schoolName) => patchForm({ schoolName })}
              onBlur={() => blurField('schoolName')}
              error={fieldMessage('schoolName')}
              disabled={readOnly}
            />
          )}
          <TextField
            id="hr-education-description"
            label={t('personnel.educationDescription')}
            value={form.educationDescription}
            onChange={(educationDescription) => patchForm({ educationDescription })}
            onBlur={() => blurField('educationDescription')}
            error={fieldMessage('educationDescription')}
            disabled={readOnly}
          />
          <DateField
            id="hr-graduation"
            label={t('personnel.graduationDate')}
            value={form.graduationDate}
            onChange={(graduationDate) => patchForm({ graduationDate })}
            calendar
            disabled={readOnly}
          />
          <SelectField
            id="hr-foreign-language"
            label={t('personnel.foreignLanguage')}
            value={form.foreignLanguage}
            onChange={(value) => patchForm({ foreignLanguage: value as ForeignLanguageSummary | '' })}
            disabled={readOnly}
            placeholder={t('personnel.placeholders.foreignLanguage')}
          >
            <option value="English">{t('personnel.langEnglish')}</option>
            <option value="German">{t('personnel.langGerman')}</option>
            <option value="French">{t('personnel.langFrench')}</option>
            <option value="Arabic">{t('personnel.langArabic')}</option>
            <option value="Russian">{t('personnel.langRussian')}</option>
            <option value="Spanish">{t('personnel.langSpanish')}</option>
            <option value="Chinese">{t('personnel.langChinese')}</option>
            <option value="Japanese">{t('personnel.langJapanese')}</option>
            <option value="Korean">{t('personnel.langKorean')}</option>
            <option value="Other">{t('personnel.langOther')}</option>
          </SelectField>
          <TextField
            id="hr-arge-code"
            label={t('personnel.argeProjectCode')}
            value={form.argeProjectCode}
            onChange={(argeProjectCode) => patchForm({ argeProjectCode })}
            onBlur={() => blurField('argeProjectCode')}
            error={fieldMessage('argeProjectCode')}
            hint={t('personnel.argeProjectHint')}
            disabled={readOnly}
          />
        </div>
      </fieldset>
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
      <div className={styles.profilePhotoWrapper}>
        <AvatarMark
          className={styles.profilePhoto}
          name={displayName}
          size="card"
          src={photoSrc}
          alt={displayName}
        />
      </div>
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
  fieldMessage,
  blurField,
  onMobileUnsafePaste,
}: {
  form: PersonnelForm
  patchForm: (patch: Partial<PersonnelForm>) => void
  givenNameRef: RefObject<HTMLInputElement | null>
  readOnly: boolean
  createMode: boolean
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
          <DateField
            id="hr-start"
            label={t('workforce.startDate')}
            value={form.employmentStartDate}
            onChange={(employmentStartDate) => patchForm({ employmentStartDate })}
            onBlur={() => blurField('employmentStartDate')}
            error={fieldMessage('employmentStartDate')}
            required
          />
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
  lookups,
  language,
  fieldMessage,
  blurField,
  onMobileUnsafePaste,
  onEmergencyUnsafePaste,
}: {
  form: PersonnelForm
  patchForm: (patch: Partial<PersonnelForm>) => void
  patchEmergency: (index: number, patch: Partial<PersonnelForm['emergencyContacts'][number]>) => void
  setForm: (updater: (current: PersonnelForm) => PersonnelForm) => void
  readOnly: boolean
  canReadSensitive: boolean
  lookups: OfficialLookups | null
  language: AppLanguage
  fieldMessage: (field: string) => string | undefined
  blurField: (field: string) => void
  onMobileUnsafePaste: () => void
  onEmergencyUnsafePaste: (index: number) => void
}) {
  const { t } = useTranslation()
  const nationalityOptions = (lookups?.nationalities ?? []).map((code) => ({
    value: code,
    label: nationalityLabel(code, language),
  }))
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
          <SearchableSelect
            id="hr-nationality"
            label={t('personnel.nationality')}
            value={form.nationality}
            options={nationalityOptions}
            onChange={(nationality) => patchForm({ nationality })}
            onBlur={() => blurField('nationality')}
            placeholder={t('personnel.placeholders.nationality')}
            emptyText={t('personnel.nationalityEmpty')}
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
          <SearchableSelect
            id="hr-city"
            label={t('personnel.city')}
            value={form.residenceCity}
            options={provinceSelectOptions(form.residenceCity)}
            onChange={(residenceCity) => patchForm({ residenceCity })}
            onBlur={() => blurField('residenceCity')}
            placeholder={t('personnel.selectProvince')}
            emptyText={t('personnel.provinceEmpty')}
            error={fieldMessage('residenceCity')}
            disabled={readOnly || !canReadSensitive}
          />
          <SearchableSelect
            id="hr-district"
            label={t('personnel.district')}
            value={form.residenceDistrict}
            options={districtSelectOptions(form.residenceCity, form.residenceDistrict)}
            onChange={(residenceDistrict) => patchForm({ residenceDistrict })}
            onBlur={() => blurField('residenceDistrict')}
            placeholder={
              form.residenceCity === '' ? t('personnel.selectProvinceFirst') : t('personnel.selectDistrict')
            }
            emptyText={t('personnel.districtEmpty')}
            error={fieldMessage('residenceDistrict')}
            disabled={readOnly || !canReadSensitive || form.residenceCity === ''}
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
                <MobilePhoneField
                  id={`hr-em-phone-${index}`}
                  label={t('personnel.emergencyPhone')}
                  value={contact.phone}
                  onChange={(phone) => patchEmergency(index, { phone })}
                  onBlur={() => blurField(`emergencyContacts[${index}].phone`)}
                  onUnsafePaste={() => onEmergencyUnsafePaste(index)}
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
  terminationReason,
  setTerminationReason,
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
  terminationReason: EmploymentTerminationReason | ''
  setTerminationReason: (value: EmploymentTerminationReason | '') => void
  saving: boolean
  onTransfer: () => void
  onEnd: () => void
  fieldMessage: (field: string) => string | undefined
  blurField: (field: string) => void
}) {
  const { t } = useTranslation()
  const employment = card?.currentEmployment ?? card?.employments[0]
  const statusLabel =
    employment?.status === 'Active'
      ? t('workforce.activeStatus')
      : employment?.status === 'Scheduled'
        ? t('workforce.scheduledStatus')
        : employment?.status === 'Ended'
          ? t('workforce.endedStatus')
          : '—'

  return (
    <div className={styles.workStack}>
      <fieldset className={styles.section}>
        <legend className={styles.legend}>{t('personnel.sectionEmployment')}</legend>
        <div className={styles.grid}>
          <div className={styles.fact}>
            <span className={styles.factLabel}>{t('workforce.status')}</span>
            <span>{statusLabel}</span>
          </div>
          {createMode ? (
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
          ) : (
            <div className={styles.fact}>
              <span className={styles.factLabel}>{t('workforce.startDate')}</span>
              <span>{employment ? formatDateOnly(employment.startDate, language) : '—'}</span>
            </div>
          )}
          <DateField
            id="hr-seniority-start"
            label={t('personnel.seniorityStartDate')}
            value={form.seniorityStartDate}
            onChange={(seniorityStartDate) => patchForm({ seniorityStartDate })}
            onBlur={() => blurField('seniorityStartDate')}
            error={fieldMessage('seniorityStartDate')}
            hint={t('personnel.seniorityHint')}
            disabled={readOnly}
          />
          {ended && employment?.endDate ? (
            <div className={styles.fact}>
              <span className={styles.factLabel}>{t('personnel.employmentEndDate')}</span>
              <span>{formatDateOnly(employment.endDate, language)}</span>
            </div>
          ) : null}
        </div>
      </fieldset>

      <fieldset className={styles.section}>
        <legend className={styles.legend}>{t('personnel.sectionContract')}</legend>
        <div className={styles.grid}>
          <SelectField
            id="hr-contract-type"
            label={t('personnel.contractType')}
            value={form.contractType}
            onChange={(value) =>
              patchForm({
                contractType: value as EmploymentContractType | '',
                contractEndDate: value === 'FixedTerm' ? form.contractEndDate : '',
                partTimeMonthlyHours: value === 'PartTime' ? form.partTimeMonthlyHours : '',
              })
            }
            disabled={readOnly}
            placeholder={t('personnel.placeholders.contractType')}
          >
            <option value="Indefinite">{t('personnel.contractIndefinite')}</option>
            <option value="FixedTerm">{t('personnel.contractFixedTerm')}</option>
            <option value="PartTime">{t('personnel.contractPartTime')}</option>
          </SelectField>
          {form.contractType === 'FixedTerm' ? (
            <Reveal>
              <DateField
                id="hr-contract-end"
                label={t('personnel.contractEndDate')}
                value={form.contractEndDate}
                onChange={(contractEndDate) => patchForm({ contractEndDate })}
                onBlur={() => blurField('contractEndDate')}
                error={fieldMessage('contractEndDate')}
                required
                disabled={readOnly}
              />
            </Reveal>
          ) : null}
          {form.contractType === 'PartTime' ? (
            <Reveal>
              <TextField
                id="hr-part-time-hours"
                label={t('personnel.partTimeMonthlyHours')}
                value={form.partTimeMonthlyHours}
                onChange={(partTimeMonthlyHours) => patchForm({ partTimeMonthlyHours })}
                onBlur={() => blurField('partTimeMonthlyHours')}
                error={fieldMessage('partTimeMonthlyHours')}
                required
                inputMode="decimal"
                disabled={readOnly}
              />
            </Reveal>
          ) : null}
        </div>
      </fieldset>

      <fieldset className={styles.section}>
        <legend className={styles.legend}>{t('personnel.sectionOrganization')}</legend>
        <div className={styles.grid}>
          <div className={styles.fact}>
            <span className={styles.factLabel}>{t('personnel.organization')}</span>
            <span className={styles.factValue}>{card?.organizationName || '—'}</span>
          </div>
          <div className={styles.fact}>
            <span className={styles.factLabel}>{t('personnel.property')}</span>
            <span className={styles.factValue}>{card?.propertyName || '—'}</span>
          </div>
          {createMode ? (
            <>
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
                <span className={styles.factLabel}>{t('workforce.department')}</span>
                <span className={styles.factValue}>{card?.currentPrimaryAssignment?.departmentName || '—'}</span>
              </div>
              <div className={styles.fact}>
                <span className={styles.factLabel}>{t('workforce.position')}</span>
                <span className={styles.factValue}>{card?.currentPrimaryAssignment?.positionName || '—'}</span>
              </div>
            </>
          )}
        </div>
        {canManageWorkforce && !createMode && !ended ? (
          <div className={styles.photoActions}>
            <Button layout="inline" onClick={() => setWorkMode('transfer')}>
              {t('workforce.transfer')}
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
      </fieldset>

      {!createMode && (ended || canManageWorkforce) ? (
        <fieldset className={styles.section}>
          <legend className={styles.legend}>{t('personnel.sectionTermination')}</legend>
          {ended ? (
            <div className={styles.grid}>
              <div className={styles.fact}>
                <span className={styles.factLabel}>{t('personnel.employmentEndDate')}</span>
                <span>{employment?.endDate ? formatDateOnly(employment.endDate, language) : '—'}</span>
              </div>
              <div className={styles.fact}>
                <span className={styles.factLabel}>{t('personnel.terminationReason')}</span>
                <span>{terminationReasonLabel(employment?.terminationReason, t)}</span>
              </div>
            </div>
          ) : canManageWorkforce ? (
            <>
              <div className={styles.photoActions}>
                <Button
                  variant="danger"
                  onClick={() => setWorkMode(workMode === 'end' ? 'none' : 'end')}
                >
                  {t('workforce.endEmployment')}
                </Button>
              </div>
              {workMode === 'end' ? (
                <form
                  className={styles.section}
                  onSubmit={(event) => {
                    event.preventDefault()
                    onEnd()
                  }}
                >
                  <Notice tone="warning">{t('workforce.confirmEnd')}</Notice>
                  <div className={styles.grid}>
                    <DateField
                      id="card-end-date"
                      label={t('personnel.employmentEndDate')}
                      value={endDate}
                      onChange={setEndDate}
                      required
                    />
                    <SelectField
                      id="card-termination-reason"
                      label={t('personnel.terminationReason')}
                      value={terminationReason}
                      onChange={(value) => setTerminationReason(value as EmploymentTerminationReason | '')}
                      placeholder={t('personnel.placeholders.terminationReason')}
                      required
                    >
                      <option value="Resignation">{t('personnel.terminationResignation')}</option>
                      <option value="EmployerTermination">{t('personnel.terminationEmployerTermination')}</option>
                      <option value="ContractEnded">{t('personnel.terminationContractEnded')}</option>
                      <option value="Retirement">{t('personnel.terminationRetirement')}</option>
                      <option value="Other">{t('personnel.terminationOther')}</option>
                    </SelectField>
                  </div>
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
          ) : null}
        </fieldset>
      ) : null}
    </div>
  )
}

function terminationReasonLabel(
  reason: EmploymentTerminationReason | null | undefined,
  t: (key: string) => string,
) {
  switch (reason) {
    case 'Resignation':
      return t('personnel.terminationResignation')
    case 'EmployerTermination':
      return t('personnel.terminationEmployerTermination')
    case 'ContractEnded':
      return t('personnel.terminationContractEnded')
    case 'Retirement':
      return t('personnel.terminationRetirement')
    case 'Other':
      return t('personnel.terminationOther')
    default:
      return '—'
  }
}

function PaymentTab({
  form,
  patchForm,
  fieldMessage,
  blurField,
  readOnly,
  canReadSensitive,
  employeeId,
}: {
  form: PersonnelForm
  patchForm: (patch: Partial<PersonnelForm>) => void
  fieldMessage: (field: string) => string | undefined
  blurField: (field: string) => void
  readOnly: boolean
  canReadSensitive: boolean
  employeeId: string | null
}) {
  const { t } = useTranslation()
  const { user } = useAuthSession()
  const [erp, setErp] = useState<EmployeeErpAccountSummary | null>(null)

  useEffect(() => {
    if (!employeeId) {
      return
    }

    let cancelled = false
    void (async () => {
      try {
        const account = await getHrEmployeeErpAccount(employeeId)
        if (!cancelled) {
          setErp(account)
        }
      } catch {
        if (!cancelled) {
          setErp({ hasAccount: false, email: null, isLocked: null })
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [employeeId])

  return (
    <>
      {canReadSensitive ? (
        <fieldset className={styles.section}>
          <legend className={styles.legend}>{t('personnel.paymentSection')}</legend>
          <p className={styles.meta}>{t('personnel.paymentOptionalHint')}</p>
          <div className={styles.grid}>
            <TextField
              id="hr-payment-bank"
              label={t('personnel.paymentBankName')}
              value={form.paymentBankName}
              onChange={(paymentBankName) => patchForm({ paymentBankName })}
              onBlur={() => blurField('paymentBankName')}
              error={fieldMessage('paymentBankName')}
              disabled={readOnly}
            />
            <TurkishIbanField
              id="hr-payment-iban"
              label={t('personnel.paymentIban')}
              value={form.paymentIban}
              onChange={(paymentIban) => patchForm({ paymentIban })}
              onBlur={() => blurField('paymentIban')}
              error={fieldMessage('paymentIban')}
              disabled={readOnly}
            />
          </div>
        </fieldset>
      ) : (
        <p className={styles.meta}>{t('personnel.sensitiveHidden')}</p>
      )}
      {employeeId ? (
        <fieldset className={styles.section}>
          <legend className={styles.legend}>{t('personnel.erpAccess')}</legend>
          {erp?.hasAccount ? (
            <p>{t('personnel.erpActiveUser', { email: erp.email ?? '' })}</p>
          ) : (
            <p>{t('personnel.erpNoAccount')}</p>
          )}
          {canManageAuthorizationUsers(user) && !erp?.hasAccount ? (
            <Link to={`/app/users?employeeId=${employeeId}`}>{t('personnel.createErpUser')}</Link>
          ) : null}
        </fieldset>
      ) : null}
    </>
  )
}

function HistoryTab({
  card,
  language,
  createMode,
  employeeId,
}: {
  card: HrEmployeeCard | null
  language: AppLanguage
  createMode: boolean
  employeeId: string | null
}) {
  const { t } = useTranslation()
  const [history, setHistory] = useState<PersonnelHistoryResponse | null>(null)

  useEffect(() => {
    if (!employeeId) {
      return
    }

    let cancelled = false
    void (async () => {
      try {
        const rows = await getHrPersonnelHistory(employeeId)
        if (!cancelled) {
          setHistory(rows)
        }
      } catch {
        if (!cancelled) {
          setHistory({ profileChanges: [], employments: card?.employments ?? [] })
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [employeeId, card?.employments])

  if (createMode) {
    return <EmptyState compact title={t('personnel.historyEmptyCreate')} />
  }

  const assignmentItems = (history?.employments ?? card?.employments ?? []).flatMap((employment) =>
    employment.primaryAssignments.map((assignment) => ({ employment, assignment })),
  )
  const profileChanges = history?.profileChanges ?? []

  if (profileChanges.length === 0 && assignmentItems.length === 0) {
    return <EmptyState compact title={t('workforce.noHistory')} />
  }

  return (
    <>
      {profileChanges.length > 0 ? (
        <Timeline label={t('personnel.profileChangeHistory')}>
          {profileChanges.map((item) => (
            <TimelineItem key={item.id} time={formatDateOnly(item.changedAtUtc.slice(0, 10), language)} marker="neutral">
              <span>{t(`personnel.historyFields.${item.fieldCode}`, { defaultValue: item.fieldCode })}</span>
              <span className={styles.meta}>
                {item.oldValue ?? '—'} → {item.newValue ?? '—'}
              </span>
            </TimelineItem>
          ))}
        </Timeline>
      ) : null}
      {assignmentItems.length > 0 ? (
        <Timeline label={t('workforce.workHistory')}>
          {assignmentItems.map(({ employment, assignment }) => (
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
      ) : null}
    </>
  )
}
