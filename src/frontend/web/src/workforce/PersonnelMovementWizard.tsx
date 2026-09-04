import { useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { formatDateOnly, todayIsoDate } from '../i18n/format'
import { toAppLanguage } from '../i18n/languages'
import { Button } from '../ui/Button'
import { Notice } from '../ui/Notice'
import { DateField, SelectField } from '../ui/SelectField'
import { TextArea, TextField } from '../ui/TextField'
import { WorkspaceDialog } from '../ui/WorkspaceDialog'
import { toIsoDate } from '../ui/dateEntry'
import { positionsForDepartment, promotionTargetPositions, isEligiblePromotionTarget } from './assignmentOptions'
import {
  MOVEMENT_NOTE_MAX,
  MOVEMENT_REASON_MAX,
  createHrMovement,
  getHrEmployee,
  getHrMovementStructure,
  hrMovementErrorMessage,
  hrMovementErrorStep,
  listHrEmployees,
  listHrManagerCandidates,
  type CreatableMovementType,
  type HrEmployeeCard,
  type HrEmployeeListItem,
  type ManagerCandidate,
  type MovementStructure,
  type PersonnelMovementDetail,
} from './hrMovementsWizardDeps'
import {
  adjacentWizardStep,
  assignmentMovementDateTooEarly,
  authorizedDestinationProperties,
  buildCreateMovementRequest,
  departmentChangeNeedsTargetPosition,
  earliestAssignmentMovementDate,
  emptyMovementWizardDraft,
  isMovementWizardDirty,
  matchesEmployeeSearch,
  MOVEMENT_WIZARD_STEPS,
  movementTypeLabelKey,
  movementWizardShowsPicker,
  movementWizardStepStatus,
  selectableCreatableMovementTypes,
  reconcileMovementWizardDraft,
  sourceAssignmentAsOf,
  sourceOrganizationalLevel,
  type MovementWizardDraft,
  type MovementWizardStep,
} from './movementDisplay'
import styles from './PersonnelMovementsPage.module.css'
import type { AccessibleProperty } from '../shared/types'
import type { DepartmentRecord, PositionRecord } from './workforceApi'

type Step = MovementWizardStep
const STEPS = MOVEMENT_WIZARD_STEPS

export function PersonnelMovementWizard({
  accessibleProperties,
  onClose,
  onCreated,
}: {
  accessibleProperties: AccessibleProperty[]
  onClose: () => void
  onCreated: (created: PersonnelMovementDetail) => void
}) {
  const { t, i18n } = useTranslation()
  const language = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? 'tr'
  const [step, setStep] = useState<Step>('personnel')
  const [draft, setDraft] = useState<MovementWizardDraft>(emptyMovementWizardDraft)
  const [employees, setEmployees] = useState<HrEmployeeListItem[]>([])
  const [managerCandidates, setManagerCandidates] = useState<ManagerCandidate[]>([])
  const [employeeQuery, setEmployeeQuery] = useState('')
  const [managerQuery, setManagerQuery] = useState('')
  const [card, setCard] = useState<HrEmployeeCard | null>(null)
  const [structure, setStructure] = useState<MovementStructure | null>(null)
  const [destStructure, setDestStructure] = useState<MovementStructure | null>(null)
  const [error, setError] = useState<{ message: string; code?: string } | null>(null)
  const [busy, setBusy] = useState(false)
  const [replacingEmployee, setReplacingEmployee] = useState(false)
  const [replacingManager, setReplacingManager] = useState(false)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const continueEditingRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    let cancelled = false
    void listHrEmployees()
      .then((rows) => {
        if (!cancelled) {
          setEmployees(rows.filter((item) => item.employmentStatus !== 'Ended'))
        }
      })
      .catch((reason: unknown) => {
        if (!cancelled) {
          setError({ message: hrMovementErrorMessage(reason, t) })
        }
      })
    return () => {
      cancelled = true
    }
  }, [t])

  const filteredEmployees = useMemo(
    () => employees.filter((item) => matchesEmployeeSearch(item, employeeQuery)).slice(0, 20),
    [employeeQuery, employees],
  )
  const filteredManagers = useMemo(
    () => managerCandidates.filter((item) => matchesEmployeeSearch(item, managerQuery)).slice(0, 20),
    [managerCandidates, managerQuery],
  )

  const currentDepartmentId = card?.currentPrimaryAssignment?.departmentId ?? ''
  const currentPositionId = card?.currentPrimaryAssignment?.positionId ?? ''
  const effectiveIso = toIsoDate(draft.effectiveDate)
  const sourceAssignment = sourceAssignmentAsOf(card, draft.employmentId, effectiveIso)
  const sourceDepartmentId = sourceAssignment?.departmentId ?? currentDepartmentId
  const sourcePositionId = sourceAssignment?.positionId ?? currentPositionId
  const assignmentStart = sourceAssignment?.startDate ?? card?.currentPrimaryAssignment?.startDate ?? null
  const earliestIso = earliestAssignmentMovementDate(assignmentStart)
  const dateTooEarly = assignmentMovementDateTooEarly(effectiveIso, assignmentStart, draft.type)
  const earliestDateLabel = earliestIso ? formatDateOnly(earliestIso, language) : ''
  const departments = useMemo(
    () => structure?.departments.filter((item) => item.isActive) ?? [],
    [structure],
  )
  const positions = useMemo(
    () => structure?.positions.filter((item) => item.isActive) ?? [],
    [structure],
  )
  const destDepartments = useMemo(
    () =>
      draft.targetPropertyId === ''
        ? []
        : destStructure?.departments.filter((item) => item.isActive) ?? [],
    [destStructure, draft.targetPropertyId],
  )
  const destPositions = useMemo(
    () =>
      draft.targetPropertyId === ''
        ? []
        : destStructure?.positions.filter((item) => item.isActive) ?? [],
    [destStructure, draft.targetPropertyId],
  )
  const sourceLevel = sourceOrganizationalLevel(positions, sourcePositionId)
  const promotionTargets = promotionTargetPositions(
    positions,
    sourceDepartmentId,
    sourcePositionId,
    sourceLevel,
  )
  const promotionTargetReady = isEligiblePromotionTarget(
    positions,
    sourceDepartmentId,
    sourcePositionId,
    sourceLevel,
    draft.targetPositionId,
  )
  const needsDeptPosition = departmentChangeNeedsTargetPosition(
    positions,
    draft.targetDepartmentId,
    sourcePositionId,
  )
  const sourcePropertyId = structure?.propertyId ?? ''
  const destinationProperties = useMemo(
    () => authorizedDestinationProperties(accessibleProperties, sourcePropertyId),
    [accessibleProperties, sourcePropertyId],
  )
  const selectableTypes = useMemo(
    () => selectableCreatableMovementTypes(accessibleProperties, sourcePropertyId),
    [accessibleProperties, sourcePropertyId],
  )
  const reconciled = reconcileMovementWizardDraft(draft, {
    selectableTypes,
    destinationProperties,
    positions,
    sourceDepartmentId,
    sourcePositionId,
    sourceOrganizationalLevel: sourceLevel,
  })
  if (reconciled !== draft) {
    setDraft(reconciled)
  }

  useEffect(() => {
    if (draft.type !== 'ManagerChange' || draft.employmentId === '' || effectiveIso === null) {
      return
    }

    let cancelled = false
    void listHrManagerCandidates(draft.employmentId, effectiveIso)
      .then((rows) => {
        if (!cancelled) {
          setManagerCandidates(rows)
        }
      })
      .catch((reason: unknown) => {
        if (!cancelled) {
          setManagerCandidates([])
          setError({ message: hrMovementErrorMessage(reason, t) })
        }
      })
    return () => {
      cancelled = true
    }
  }, [draft.employmentId, draft.type, effectiveIso, t])

  const patch = (next: Partial<MovementWizardDraft>) => {
    setError(null)
    if (
      ('type' in next && next.type !== 'ManagerChange')
      || 'employmentId' in next
      || 'effectiveDate' in next
    ) {
      setManagerCandidates([])
    }
    setDraft((current) => {
      const merged = { ...current, ...next }
      if ('effectiveDate' in next && next.effectiveDate !== current.effectiveDate) {
        merged.targetManagerEmploymentId = ''
      }
      return merged
    })
  }

  const selectEmployee = async (item: HrEmployeeListItem) => {
    setError(null)
    try {
      const detail = await getHrEmployee(item.employeeId)
      const employmentId = detail.currentEmployment?.id
      if (!employmentId || detail.currentEmployment?.status === 'Ended') {
        setCard(null)
        patch({ employeeId: '', employmentId: '' })
        setError({ message: t('workforce.errors.noCurrentEmployment') })
        return
      }
      setCard(detail)
      setReplacingEmployee(false)
      patch({
        employeeId: item.employeeId,
        employmentId,
        targetDepartmentId: '',
        targetPositionId: '',
        targetPropertyId: '',
        targetManagerEmploymentId: '',
      })
      const propertyId = await resolvePropertyId(
        detail.currentPrimaryAssignment?.departmentId ?? '',
        accessibleProperties,
        setStructure,
      )
      if (propertyId) {
        const loaded = await getHrMovementStructure(propertyId)
        setStructure(loaded)
      }
    } catch (reason) {
      setError({ message: hrMovementErrorMessage(reason, t) })
    }
  }

  const selectManager = (item: ManagerCandidate) => {
    if (item.employmentId === draft.employmentId) {
      setError({ message: t('movements.errors.selfManager') })
      return
    }
    setError(null)
    patch({ targetManagerEmploymentId: item.employmentId })
    setManagerQuery(`${item.givenName} ${item.familyName}`)
    setReplacingManager(false)
  }

  const onTargetProperty = async (propertyId: string) => {
    patch({ targetPropertyId: propertyId, targetDepartmentId: '', targetPositionId: '' })
    setDestStructure(null)
    if (!propertyId) {
      return
    }
    try {
      setDestStructure(await getHrMovementStructure(propertyId))
    } catch (reason) {
      setError({ message: hrMovementErrorMessage(reason, t) })
    }
  }

  const canContinue = (): boolean => {
    if (step === 'personnel') {
      return draft.employmentId !== ''
    }
    if (step === 'type') {
      return draft.type !== ''
    }
    if (step === 'date') {
      return effectiveIso !== null && !dateTooEarly
    }
    if (step === 'target') {
      return targetReady(draft, sourceDepartmentId, sourcePositionId, needsDeptPosition, promotionTargetReady)
    }
    if (step === 'reason') {
      const reason = draft.reason.trim()
      return reason !== '' && reason.length <= MOVEMENT_REASON_MAX && draft.note.length <= MOVEMENT_NOTE_MAX
    }
    if (step === 'review') {
      const reason = draft.reason.trim()
      return (
        effectiveIso !== null &&
        !dateTooEarly &&
        targetReady(draft, sourceDepartmentId, sourcePositionId, needsDeptPosition, promotionTargetReady) &&
        reason !== '' &&
        reason.length <= MOVEMENT_REASON_MAX &&
        draft.note.length <= MOVEMENT_NOTE_MAX
      )
    }
    return false
  }

  const goNext = () => {
    const next = adjacentWizardStep(step, 1)
    if (next) {
      setStep(next)
    }
  }

  const goBack = () => {
    const previous = adjacentWizardStep(step, -1)
    if (previous) {
      setStep(previous)
    }
  }

  const submit = async () => {
    const payload = buildCreateMovementRequest(
      draft,
      {
        departmentId: sourceDepartmentId,
        positionId: sourcePositionId,
      },
      { positions, sourceOrganizationalLevel: sourceLevel },
    )
    if ('error' in payload) {
      setError({ message: t(`movements.wizard.invalid.${payload.error}`) })
      return
    }
    setBusy(true)
    setError(null)
    try {
      const created = await createHrMovement(payload)
      onCreated(created)
    } catch (reason) {
      const code =
        typeof reason === 'object' && reason !== null && 'problem' in reason
          ? (reason as { problem?: { code?: string } }).problem?.code
          : undefined
      setError({
        message: hrMovementErrorMessage(reason, t, {
          earliestEffectiveDateLabel: earliestDateLabel || undefined,
        }),
        code,
      })
    } finally {
      setBusy(false)
    }
  }

  const future = (() => {
    const iso = toIsoDate(draft.effectiveDate)
    return iso !== null && iso > todayIsoDate()
  })()

  const showEmployeePicker = movementWizardShowsPicker(draft.employeeId !== '' && card !== null, replacingEmployee)
  const showManagerPicker = movementWizardShowsPicker(draft.targetManagerEmploymentId !== '', replacingManager)
  const dirty = isMovementWizardDirty(draft, `${employeeQuery} ${managerQuery}`)

  const requestClose = () => {
    if (dirty) {
      setConfirmingClose(true)
      return
    }
    onClose()
  }

  const errorStep = hrMovementErrorStep(error?.code)

  return (
    <>
    <WorkspaceDialog
      title={t('movements.new')}
      size="dialog"
      showClose
      closeLabel={t('personnel.close')}
      inert={confirmingClose}
      onRequestClose={requestClose}
      footer={
        <div className={styles.wizardFooter}>
          <Button variant="ghost" layout="inline" onClick={step === 'personnel' ? requestClose : goBack}>
            {step === 'personnel' ? t('personnel.cancel') : t('movements.wizard.back')}
          </Button>
          {step === 'review' ? (
            <Button layout="inline" loading={busy} disabled={!canContinue()} onClick={() => void submit()}>
              {t('movements.wizard.save')}
            </Button>
          ) : (
            <Button layout="inline" disabled={!canContinue()} onClick={goNext}>
              {t('movements.wizard.next')}
            </Button>
          )}
        </div>
      }
    >
      <div className={styles.wizard} data-movement-wizard="compact">
      <ol className={styles.steps} aria-label={t('movements.new')}>
        {STEPS.map((id) => {
          const status = movementWizardStepStatus(id, step)
          const complete = status === 'complete'
          return (
            <li key={id}>
              <button
                type="button"
                className={
                  status === 'current' ? styles.stepCurrent : complete ? styles.stepComplete : styles.step
                }
                aria-current={status === 'current' ? 'step' : undefined}
                disabled={!complete}
                onClick={() => {
                  if (complete) {
                    setStep(id)
                  }
                }}
              >
                {t(`movements.wizard.steps.${id}`)}
              </button>
            </li>
          )
        })}
      </ol>
      {error ? (
        <div className={styles.wizardError} role="alert">
          <p>{error.message}</p>
          {errorStep === 'date' ? (
            <Button
              variant="ghost"
              layout="inline"
              size="sm"
              onClick={() => {
                setStep('date')
              }}
            >
              {t('movements.errors.editEffectiveDate')}
            </Button>
          ) : null}
        </div>
      ) : null}

      {step === 'personnel' ? (
        <>
          {card && !showEmployeePicker ? (
            <SelectedPersonSummary
              name={`${card.givenName} ${card.familyName}`}
              personnelNumber={card.personnelNumber}
              departmentName={card.currentPrimaryAssignment?.departmentName}
              positionName={card.currentPrimaryAssignment?.positionName}
              onChange={() => setReplacingEmployee(true)}
              changeLabel={t('movements.wizard.changeSelection')}
            />
          ) : (
            <>
              <TextField
                id="movement-employee-search"
                label={t('movements.filters.employee')}
                value={employeeQuery}
                onChange={setEmployeeQuery}
                placeholder={t('movements.wizard.employeeSearchHint')}
              />
              <div className={styles.pickerList} role="listbox" data-wizard-employee-picker>
                {filteredEmployees.map((item) => {
                  const selected = item.employeeId === draft.employeeId
                  return (
                    <button
                      key={item.employeeId}
                      type="button"
                      className={selected ? styles.pickerItemCurrent : styles.pickerItem}
                      aria-selected={selected}
                      onClick={() => void selectEmployee(item)}
                    >
                      <span className={styles.pickerName}>
                        {item.givenName} {item.familyName}
                      </span>
                      <span className={styles.pickerMeta}>
                        {item.personnelNumber}
                        {item.departmentName ? ` · ${item.departmentName}` : ''}
                        {item.positionName ? ` · ${item.positionName}` : ''}
                      </span>
                    </button>
                  )
                })}
              </div>
            </>
          )}
        </>
      ) : null}

      {step === 'type' ? (
        <SelectField
          id="movement-type"
          label={t('movements.columns.type')}
          value={draft.type}
          placeholder={t('movements.wizard.selectType')}
          onChange={(value) => patch({ type: value as CreatableMovementType | '', targetDepartmentId: '', targetPositionId: '', targetPropertyId: '', targetManagerEmploymentId: '' })}
          required
        >
          {selectableTypes.map((type) => (
            <option key={type} value={type}>
              {t(movementTypeLabelKey(type))}
            </option>
          ))}
        </SelectField>
      ) : null}

      {step === 'date' ? (
        <DateField
          id="movement-effective-date"
          label={t('movements.columns.effectiveDate')}
          value={draft.effectiveDate}
          onChange={(effectiveDate) => patch({ effectiveDate })}
          hint={t('movements.wizard.dateHint')}
          error={
            dateTooEarly && earliestDateLabel
              ? t('movements.errors.dateConflictWithBound', { date: earliestDateLabel })
              : undefined
          }
          required
        />
      ) : null}

      {step === 'target' && draft.type === 'DepartmentChange' ? (
        <div className={styles.wizardGrid}>
          <SelectField
            id="movement-target-department"
            label={t('movements.wizard.targetDepartment')}
            value={draft.targetDepartmentId}
            placeholder={t('workforce.selectDepartment')}
            onChange={(targetDepartmentId) =>
              patch({
                targetDepartmentId,
                targetPositionId: departmentChangeNeedsTargetPosition(positions, targetDepartmentId, sourcePositionId)
                  ? ''
                  : sourcePositionId,
              })
            }
            required
          >
            {departments
              .filter((item) => item.id !== sourceDepartmentId)
              .map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
          </SelectField>
          {needsDeptPosition ? (
            <SelectField
              id="movement-target-position"
              label={t('movements.wizard.targetPosition')}
              value={draft.targetPositionId}
              placeholder={t('workforce.selectPosition')}
              hint={t('movements.wizard.positionRequiredForDepartment')}
              onChange={(targetPositionId) => patch({ targetPositionId })}
              required
              disabled={draft.targetDepartmentId === ''}
            >
              {positionsForDepartment(positions, draft.targetDepartmentId).map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </SelectField>
          ) : (
            <p className={styles.intro}>{t('movements.wizard.positionKept')}</p>
          )}
        </div>
      ) : null}

      {step === 'target' && (draft.type === 'PositionChange' || draft.type === 'Promotion') ? (
        <div className={styles.wizardGrid}>
          <SelectField
            id="movement-current-department"
            label={t('workforce.department')}
            value={sourceDepartmentId}
            disabled
            onChange={() => undefined}
          >
            {departments.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </SelectField>
          <SelectField
            id="movement-target-position-same-dept"
            label={t('movements.wizard.targetPosition')}
            value={draft.targetPositionId}
            placeholder={t('workforce.selectPosition')}
            hint={draft.type === 'Promotion' ? t('movements.wizard.promotionHint') : undefined}
            onChange={(targetPositionId) => patch({ targetPositionId })}
            required={draft.type !== 'Promotion' || promotionTargets.length > 0}
            disabled={draft.type === 'Promotion' && promotionTargets.length === 0}
          >
            {(draft.type === 'Promotion'
              ? promotionTargets
              : positionsForDepartment(positions, sourceDepartmentId).filter((item) => item.id !== sourcePositionId)
            ).map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </SelectField>
          {draft.type === 'Promotion' && promotionTargets.length === 0 ? (
            <Notice tone="info">{t('movements.wizard.noPromotionTargets')}</Notice>
          ) : null}
        </div>
      ) : null}

      {step === 'target' && draft.type === 'PropertyTransfer' ? (
        <div className={styles.wizardGrid}>
          <SelectField
            id="movement-target-property"
            label={t('movements.wizard.targetProperty')}
            value={draft.targetPropertyId}
            placeholder={t('common.selectProperty')}
            onChange={(value) => void onTargetProperty(value)}
            required
          >
            {destinationProperties.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </SelectField>
          <SelectField
            id="movement-dest-department"
            label={t('movements.wizard.targetDepartment')}
            value={draft.targetDepartmentId}
            placeholder={t('workforce.selectDepartment')}
            onChange={(targetDepartmentId) => patch({ targetDepartmentId, targetPositionId: '' })}
            required
            disabled={draft.targetPropertyId === ''}
          >
            {destDepartments.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </SelectField>
          <SelectField
            id="movement-dest-position"
            label={t('movements.wizard.targetPosition')}
            value={draft.targetPositionId}
            placeholder={t('workforce.selectPosition')}
            onChange={(targetPositionId) => patch({ targetPositionId })}
            required
            disabled={draft.targetDepartmentId === ''}
          >
            {positionsForDepartment(destPositions, draft.targetDepartmentId).map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </SelectField>
        </div>
      ) : null}

      {step === 'target' && draft.type === 'ManagerChange' ? (
        <>
          {draft.targetManagerEmploymentId !== '' && !showManagerPicker ? (
            <SelectedPersonSummary
              name={managerQuery}
              personnelNumber=""
              onChange={() => {
                setReplacingManager(true)
                setManagerQuery('')
              }}
              changeLabel={t('movements.wizard.changeSelection')}
            />
          ) : (
            <>
              <TextField
                id="movement-manager-search"
                label={t('movements.wizard.targetManager')}
                value={managerQuery}
                onChange={setManagerQuery}
                placeholder={t('movements.wizard.employeeSearchHint')}
                hint={t('movements.wizard.managerLevelHint')}
              />
              <div className={styles.pickerList} data-wizard-manager-picker>
                {managerCandidates.length === 0 ? (
                  <Notice tone="info">{t('movements.wizard.noManagerCandidates')}</Notice>
                ) : (
                  filteredManagers.map((item) => (
                    <button
                      key={item.employmentId}
                      type="button"
                      className={styles.pickerItem}
                      onClick={() => selectManager(item)}
                    >
                      <span className={styles.pickerName}>
                        {item.givenName} {item.familyName}
                      </span>
                      <span className={styles.pickerMeta}>
                        {item.personnelNumber}
                        {item.departmentName ? ` · ${item.departmentName}` : ''}
                        {item.positionName ? ` · ${item.positionName}` : ''}
                      </span>
                    </button>
                  ))
                )}
              </div>
            </>
          )}
        </>
      ) : null}

      {step === 'reason' ? (
        <>
          <TextArea
            id="movement-reason"
            label={t('movements.columns.reason')}
            value={draft.reason}
            onChange={(reason) => patch({ reason: reason.slice(0, MOVEMENT_REASON_MAX) })}
            required
            maxLength={MOVEMENT_REASON_MAX}
          />
          <p className={styles.counter}>
            {draft.reason.length}/{MOVEMENT_REASON_MAX}
          </p>
          <TextArea
            id="movement-note"
            label={t('personnel.notes')}
            value={draft.note}
            onChange={(note) => patch({ note: note.slice(0, MOVEMENT_NOTE_MAX) })}
            maxLength={MOVEMENT_NOTE_MAX}
          />
          <p className={styles.counter}>
            {draft.note.length}/{MOVEMENT_NOTE_MAX}
          </p>
        </>
      ) : null}

      {step === 'review' ? (
        <div>
          {future ? <Notice tone="info">{t('movements.wizard.scheduledNotice')}</Notice> : null}
          <ReviewLine label={t('movements.columns.personnel')} value={`${card?.givenName ?? ''} ${card?.familyName ?? ''} · ${card?.personnelNumber ?? ''}`} />
          <ReviewLine
            label={t('movements.columns.type')}
            value={draft.type ? t(movementTypeLabelKey(draft.type)) : '—'}
          />
          <ReviewLine
            label={t('movements.columns.effectiveDate')}
            value={toIsoDate(draft.effectiveDate) ? formatDateOnly(toIsoDate(draft.effectiveDate)!, language) : draft.effectiveDate}
          />
          <ReviewLine
            label={t('movements.detail.previous')}
            value={
              draft.type === 'ManagerChange'
                ? t('movements.wizard.currentManagerUnknown')
                : `${sourceAssignment?.departmentName ?? card?.currentPrimaryAssignment?.departmentName ?? '—'} · ${sourceAssignment?.positionName ?? card?.currentPrimaryAssignment?.positionName ?? '—'}`
            }
          />
          <ReviewLine label={t('movements.detail.next')} value={reviewTarget(draft, departments, positions, destDepartments, destPositions, accessibleProperties, t)} />
          <ReviewLine label={t('movements.columns.reason')} value={draft.reason.trim()} />
          {draft.note.trim() ? <ReviewLine label={t('personnel.notes')} value={draft.note.trim()} /> : null}
        </div>
      ) : null}
      </div>
    </WorkspaceDialog>
    {confirmingClose ? (
      <WorkspaceDialog
        title={t('personnel.dirtyTitle')}
        size="confirm"
        stacked
        onRequestClose={() => setConfirmingClose(false)}
        initialFocusRef={continueEditingRef}
        footer={
          <>
            <Button variant="danger" onClick={onClose}>
              {t('personnel.dirtyDiscard')}
            </Button>
            <Button ref={continueEditingRef} variant="primary" layout="inline" onClick={() => setConfirmingClose(false)}>
              {t('personnel.dirtyContinue')}
            </Button>
          </>
        }
      >
        <p className={styles.dirtyBody}>{t('personnel.dirtyBody')}</p>
      </WorkspaceDialog>
    ) : null}
    </>
  )
}

function SelectedPersonSummary({
  name,
  personnelNumber,
  departmentName,
  positionName,
  onChange,
  changeLabel,
}: {
  name: string
  personnelNumber: string
  departmentName?: string
  positionName?: string
  onChange: () => void
  changeLabel: string
}) {
  const meta = [departmentName, positionName].filter(Boolean).join(' · ')
  return (
    <div className={styles.selectedPerson} data-wizard-selected-employee>
      <div className={styles.selectedPersonCopy}>
        <span className={styles.pickerName}>{name}</span>
        {personnelNumber ? <span className={styles.pickerMeta}>{personnelNumber}</span> : null}
        {meta ? <span className={styles.pickerMeta}>{meta}</span> : null}
      </div>
      <Button variant="ghost" layout="inline" size="sm" onClick={onChange}>
        {changeLabel}
      </Button>
    </div>
  )
}

function ReviewLine({ label, value }: { label: string; value: string }) {
  return (
    <div className={styles.reviewLine}>
      <span className={styles.sectionLabel}>{label}</span>
      <span>{value}</span>
    </div>
  )
}

function targetReady(
  draft: MovementWizardDraft,
  currentDepartmentId: string,
  currentPositionId: string,
  needsDeptPosition: boolean,
  promotionReady: boolean,
): boolean {
  if (draft.type === 'DepartmentChange') {
    if (draft.targetDepartmentId === '' || draft.targetDepartmentId === currentDepartmentId) {
      return false
    }
    return !needsDeptPosition || (draft.targetPositionId !== '' && draft.targetPositionId !== currentPositionId)
  }
  if (draft.type === 'Promotion') {
    return promotionReady
  }
  if (draft.type === 'PositionChange') {
    return draft.targetPositionId !== '' && draft.targetPositionId !== currentPositionId
  }
  if (draft.type === 'PropertyTransfer') {
    return draft.targetPropertyId !== '' && draft.targetDepartmentId !== '' && draft.targetPositionId !== ''
  }
  if (draft.type === 'ManagerChange') {
    return draft.targetManagerEmploymentId !== '' && draft.targetManagerEmploymentId !== draft.employmentId
  }
  return false
}

async function resolvePropertyId(
  departmentId: string,
  properties: AccessibleProperty[],
  setStructure: (value: MovementStructure | null) => void,
): Promise<string | null> {
  setStructure(null)
  for (const property of properties) {
    const loaded = await getHrMovementStructure(property.id)
    if (departmentId === '' || loaded.departments.some((item) => item.id === departmentId)) {
      setStructure(loaded)
      return property.id
    }
  }
  return properties[0]?.id ?? null
}

function reviewTarget(
  draft: MovementWizardDraft,
  departments: DepartmentRecord[],
  positions: PositionRecord[],
  destDepartments: DepartmentRecord[],
  destPositions: PositionRecord[],
  properties: AccessibleProperty[],
  t: (key: string) => string,
): string {
  if (draft.type === 'ManagerChange') {
    return t('movements.wizard.managerSelected')
  }
  if (draft.type === 'PropertyTransfer') {
    const property = properties.find((item) => item.id === draft.targetPropertyId)?.name ?? '—'
    const department = destDepartments.find((item) => item.id === draft.targetDepartmentId)?.name ?? '—'
    const position = destPositions.find((item) => item.id === draft.targetPositionId)?.name ?? '—'
    return `${property} · ${department} · ${position}`
  }
  const department = departments.find((item) => item.id === draft.targetDepartmentId)?.name
  const position = positions.find((item) => item.id === draft.targetPositionId)?.name
  return [department, position].filter(Boolean).join(' · ') || '—'
}
