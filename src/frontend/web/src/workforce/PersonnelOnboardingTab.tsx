import { useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '../ui/Button'
import { EmptyState } from '../ui/EmptyState'
import { Notice } from '../ui/Notice'
import { Skeleton } from '../ui/Skeleton'
import { WorkspaceDialog } from '../ui/WorkspaceDialog'
import styles from './PersonnelCard.module.css'
import onboardingStyles from './PersonnelOnboardingTab.module.css'
import {
  canEditOnboarding,
  canExecutePersistedOnboardingTemplateActions,
  canGenerateOnboardingDraft,
  canPreviewOnboardingDraft,
  canSelectOnboardingTemplates,
  countDraftCompleted,
  countSelectedTemplates,
  draftItemsFromCatalog,
  isOnboardingDocumentDraftReady,
  onboardingProgressText,
  showOnboardingTemplateActions,
  toggleDraftItem,
  toggleSelectedTemplate,
  type OnboardingDocumentDraftFields,
  type OnboardingDraft,
} from './onboardingUi'
import {
  completeEmployeeOnboarding,
  downloadHrDocumentDocx,
  downloadHrDocumentDraftDocx,
  getEmployeeOnboardingDocuments,
  getOnboardingCatalog,
  onboardingErrorKey,
  previewHrDocumentTemplate,
  previewHrDocumentTemplateDraft,
  setEmployeeOnboardingDocument,
  type HrDocumentTemplateListItem,
  type HrDocumentTemplatePreview,
  type OnboardingChecklist,
  type OnboardingRequirementCatalogItem,
} from './onboardingApi'

type CreateProps = {
  mode: 'create'
  canManage: boolean
  draft: OnboardingDraft
  onDraftChange: (draft: OnboardingDraft) => void
  selectedTemplateIds: string[]
  onSelectedTemplateIdsChange: (ids: string[]) => void
  documentDraft: OnboardingDocumentDraftFields
}

type EditProps = {
  mode: 'edit'
  canManage: boolean
  employeeId: string
}

export type PersonnelOnboardingTabProps = CreateProps | EditProps

export function PersonnelOnboardingTab(props: PersonnelOnboardingTabProps) {
  const { t } = useTranslation()
  const mode = props.mode
  const canManage = props.canManage
  const employeeId = mode === 'edit' ? props.employeeId : null
  const createDraft = mode === 'create' ? props.draft : null
  const documentDraft = mode === 'create' ? props.documentDraft : null
  const [checklist, setChecklist] = useState<OnboardingChecklist | null>(null)
  const [requirements, setRequirements] = useState<OnboardingRequirementCatalogItem[]>([])
  const [templates, setTemplates] = useState<HrDocumentTemplateListItem[]>([])
  const [editSelectedIds, setEditSelectedIds] = useState<string[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [toggleBusy, setToggleBusy] = useState<string | null>(null)
  const [finalizing, setFinalizing] = useState(false)
  const [preview, setPreview] = useState<HrDocumentTemplatePreview | null>(null)
  const [previewLoading, setPreviewLoading] = useState(false)
  const [printDocs, setPrintDocs] = useState<HrDocumentTemplatePreview[] | null>(null)
  const printRoot = useRef<HTMLDivElement>(null)

  const canEdit = mode === 'create'
    ? canManage
    : canEditOnboarding(checklist, canManage)
  const showTemplateActions = showOnboardingTemplateActions(mode, checklist, canManage)
  const canSelectTemplates = canSelectOnboardingTemplates(mode, checklist, canManage)
  const canPreview = canPreviewOnboardingDraft(
    mode,
    checklist,
    canManage,
    documentDraft ?? { givenName: '', familyName: '', employmentStartDate: '' },
  )
  const canGenerateDraft = canGenerateOnboardingDraft(
    mode,
    checklist,
    canManage,
    documentDraft ?? { givenName: '', familyName: '', employmentStartDate: '' },
  )
  const canExecutePersisted = canExecutePersistedOnboardingTemplateActions(mode, checklist, canManage)
  const isHistorical = mode === 'edit' && checklist !== null && !checklist.canEditChecklist
  const selectedIds = mode === 'create' ? props.selectedTemplateIds : editSelectedIds

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError(null)
      try {
        if (mode === 'create') {
          const catalog = await getOnboardingCatalog()
          if (!cancelled) {
            setRequirements(catalog.requirements)
            setTemplates(catalog.documentTemplates)
            setChecklist(null)
          }
          return
        }

        if (!employeeId) {
          return
        }

        const docs = await getEmployeeOnboardingDocuments(employeeId)
        if (!cancelled) {
          setChecklist(docs)
          setTemplates(docs.documentTemplates ?? [])
        }
      } catch (reason) {
        if (!cancelled) {
          setError(t(onboardingErrorKey(reason)))
          setChecklist(null)
          setRequirements([])
          setTemplates([])
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [mode, employeeId, t])

  useEffect(() => {
    if (!printDocs || printDocs.length === 0) {
      return
    }

    const frame = requestAnimationFrame(() => {
      window.print()
      setPrintDocs(null)
    })
    return () => cancelAnimationFrame(frame)
  }, [printDocs])

  const createItems = useMemo(() => {
    if (mode !== 'create' || !createDraft) {
      return []
    }

    return draftItemsFromCatalog(requirements, createDraft)
  }, [mode, createDraft, requirements])

  const progress = useMemo(() => {
    if (mode === 'create' && createDraft) {
      return onboardingProgressText(countDraftCompleted(createDraft), requirements.length)
    }

    return checklist
      ? onboardingProgressText(checklist.completedCount, checklist.totalCount)
      : onboardingProgressText(0, 0)
  }, [checklist, mode, createDraft, requirements.length])

  const checklistItems = mode === 'create' ? createItems : checklist?.items ?? []
  const selectedCount = countSelectedTemplates(selectedIds)

  function setSelectedTemplate(templateId: string) {
    if (mode === 'create') {
      props.onSelectedTemplateIdsChange(toggleSelectedTemplate(props.selectedTemplateIds, templateId))
      return
    }

    setEditSelectedIds((current) => toggleSelectedTemplate(current, templateId))
  }

  async function toggleItem(requirementId: string, next: boolean) {
    if (!canEdit || toggleBusy) {
      return
    }

    if (mode === 'create') {
      props.onDraftChange(toggleDraftItem(props.draft, requirementId, next))
      return
    }

    if (!employeeId) {
      return
    }

    setToggleBusy(requirementId)
    setError(null)
    try {
      const updated = await setEmployeeOnboardingDocument(employeeId, requirementId, next)
      setChecklist((current) => {
        if (!current) {
          return current
        }
        const items = current.items.map((item) =>
          item.requirementId === requirementId ? updated : item,
        )
        return {
          ...current,
          items,
          completedCount: items.filter((item) => item.isCompleted).length,
          totalCount: items.length,
        }
      })
    } catch (reason) {
      setError(t(onboardingErrorKey(reason)))
    } finally {
      setToggleBusy(null)
    }
  }

  async function finalizeOnboarding() {
    if (mode !== 'edit' || !canEdit || finalizing || !employeeId) {
      return
    }

    setFinalizing(true)
    setError(null)
    try {
      const next = await completeEmployeeOnboarding(employeeId)
      setChecklist(next)
      setTemplates(next.documentTemplates ?? [])
    } catch (reason) {
      setError(t(onboardingErrorKey(reason)))
    } finally {
      setFinalizing(false)
    }
  }

  async function openPreview(templateId: string) {
    if (mode === 'create') {
      if (!canPreview || !documentDraft) {
        return
      }

      setPreviewLoading(true)
      setError(null)
      try {
        const result = await previewHrDocumentTemplateDraft(templateId, {
          givenName: documentDraft.givenName,
          familyName: documentDraft.familyName,
          employmentStartDate: documentDraft.employmentStartDate,
          departmentName: documentDraft.departmentName ?? null,
          positionName: documentDraft.positionName ?? null,
        })
        setPreview(result)
      } catch (reason) {
        setError(t(onboardingErrorKey(reason)))
      } finally {
        setPreviewLoading(false)
      }
      return
    }

    if (!canExecutePersisted || !employeeId) {
      return
    }

    setPreviewLoading(true)
    setError(null)
    try {
      const result = await previewHrDocumentTemplate(employeeId, templateId)
      setPreview(result)
    } catch (reason) {
      setError(t(onboardingErrorKey(reason)))
    } finally {
      setPreviewLoading(false)
    }
  }

  async function openDocx(templateId: string) {
    if (mode === 'create') {
      if (!canGenerateDraft || !documentDraft) {
        return
      }

      setError(null)
      try {
        await downloadHrDocumentDraftDocx(templateId, {
          givenName: documentDraft.givenName,
          familyName: documentDraft.familyName,
          employmentStartDate: documentDraft.employmentStartDate,
          departmentName: documentDraft.departmentName ?? null,
          positionName: documentDraft.positionName ?? null,
        })
      } catch (reason) {
        setError(t(onboardingErrorKey(reason)))
      }
      return
    }

    if (!canExecutePersisted || !employeeId) {
      return
    }

    setError(null)
    try {
      await downloadHrDocumentDocx(employeeId, templateId)
    } catch (reason) {
      setError(t(onboardingErrorKey(reason)))
    }
  }

  async function printSelected() {
    const printable = selectedIds.filter((id) => {
      const template = templates.find((item) => item.id === id)
      return template && !template.hasDocxAsset
    })
    if (printable.length === 0) {
      return
    }

    if (mode === 'create') {
      if (!canGenerateDraft || !documentDraft) {
        return
      }

      setError(null)
      try {
        const docs: HrDocumentTemplatePreview[] = []
        const payload = {
          givenName: documentDraft.givenName,
          familyName: documentDraft.familyName,
          employmentStartDate: documentDraft.employmentStartDate,
          departmentName: documentDraft.departmentName ?? null,
          positionName: documentDraft.positionName ?? null,
        }
        for (const id of printable) {
          docs.push(await previewHrDocumentTemplateDraft(id, payload))
        }
        setPrintDocs(docs)
      } catch (reason) {
        setError(t(onboardingErrorKey(reason)))
      }
      return
    }

    if (!canExecutePersisted || !employeeId) {
      return
    }

    setError(null)
    try {
      const docs: HrDocumentTemplatePreview[] = []
      for (const id of printable) {
        docs.push(await previewHrDocumentTemplate(employeeId, id))
      }
      setPrintDocs(docs)
    } catch (reason) {
      setError(t(onboardingErrorKey(reason)))
    }
  }

  const previewActionDisabled = mode === 'create' ? !canPreview || previewLoading : !canExecutePersisted || previewLoading
  const docxActionDisabled = mode === 'create' ? !canGenerateDraft : !canExecutePersisted
  const printActionDisabled =
    mode === 'create'
      ? !canGenerateDraft ||
        selectedIds.filter((id) => !templates.find((item) => item.id === id)?.hasDocxAsset).length === 0
      : !canExecutePersisted ||
        selectedIds.filter((id) => !templates.find((item) => item.id === id)?.hasDocxAsset).length === 0

  if (loading && (mode === 'create' ? requirements.length === 0 : checklist === null)) {
    return <Skeleton variant="list" rows={4} label={t('personnel.onboarding.loading')} />
  }

  return (
    <div className={onboardingStyles.root}>
      {error ? <Notice tone="danger">{error}</Notice> : null}
      {isHistorical ? (
        <Notice tone="info">{t('personnel.onboarding.historicalNotice')}</Notice>
      ) : null}
      {mode === 'create' && showTemplateActions && !isOnboardingDocumentDraftReady(documentDraft ?? { givenName: '', familyName: '', employmentStartDate: '' }) ? (
        <Notice tone="info">{t('personnel.onboarding.previewDraftFieldsHint')}</Notice>
      ) : null}

      <div className={onboardingStyles.columns}>
        <section className={styles.section}>
          <div className={onboardingStyles.sectionHeader}>
            <h3 className={styles.legend}>{t('personnel.onboarding.requiredTitle')}</h3>
            <span className={onboardingStyles.progress}>
              {t('personnel.onboarding.progress', { progress })}
            </span>
          </div>
          {checklistItems.length === 0 ? (
            <EmptyState
              title={t('personnel.onboarding.emptyChecklistTitle')}
              description={t('personnel.onboarding.emptyChecklistHint')}
            />
          ) : (
            <ul className={onboardingStyles.checklist}>
              {checklistItems.map((item) => (
                <li key={item.requirementId} className={onboardingStyles.checkItem}>
                  {canEdit ? (
                    <label className={onboardingStyles.checkLabel}>
                      <input
                        type="checkbox"
                        checked={item.isCompleted}
                        disabled={toggleBusy === item.requirementId}
                        onChange={(event) => void toggleItem(item.requirementId, event.target.checked)}
                      />
                      <span>
                        {item.name}
                        {item.isRequiredByDefault ? (
                          <span className={onboardingStyles.requiredMark}> *</span>
                        ) : null}
                      </span>
                    </label>
                  ) : (
                    <div className={onboardingStyles.checkLabel}>
                      <span aria-hidden="true">{item.isCompleted ? '✓' : '○'}</span>
                      <span>
                        {item.name}
                        <span className={onboardingStyles.templateMeta}>
                          {' '}
                          ·{' '}
                          {item.isCompleted
                            ? t('personnel.onboarding.received')
                            : t('personnel.onboarding.missing')}
                        </span>
                      </span>
                    </div>
                  )}
                </li>
              ))}
            </ul>
          )}
          {mode === 'edit' && canEdit ? (
            <div className={onboardingStyles.templateActions}>
              <Button
                variant="primary"
                size="sm"
                layout="inline"
                loading={finalizing}
                onClick={() => void finalizeOnboarding()}
              >
                {t('personnel.onboarding.complete')}
              </Button>
            </div>
          ) : null}
        </section>

        <section className={styles.section}>
          <div className={onboardingStyles.sectionHeader}>
            <h3 className={styles.legend}>{t('personnel.onboarding.templatesTitle')}</h3>
            {showTemplateActions ? (
              <span className={onboardingStyles.progress}>
                {t('personnel.onboarding.selectedCount', { count: selectedCount })}
              </span>
            ) : null}
          </div>
          {isHistorical ? (
            <p className={onboardingStyles.templateMeta}>
              {t('personnel.onboarding.historicalTemplatesNotice')}
            </p>
          ) : null}
          {templates.length === 0 ? (
            <EmptyState
              title={t('personnel.onboarding.emptyTemplatesTitle')}
              description={t('personnel.onboarding.emptyTemplatesHint')}
            />
          ) : (
            <>
              <ul className={onboardingStyles.templateList}>
                {templates.map((template) => (
                  <li key={template.id} className={onboardingStyles.templateRow}>
                    {showTemplateActions ? (
                      <label className={onboardingStyles.checkLabel}>
                        <input
                          type="checkbox"
                          checked={selectedIds.includes(template.id)}
                          disabled={!canSelectTemplates}
                          onChange={() => setSelectedTemplate(template.id)}
                        />
                        <span>
                          {template.name}
                          <span className={onboardingStyles.templateMeta}>
                            {' '}
                            · v{template.version}
                          </span>
                        </span>
                      </label>
                    ) : (
                      <span>
                        {template.name}
                        <span className={onboardingStyles.templateMeta}>
                          {' '}
                          · v{template.version}
                        </span>
                      </span>
                    )}
                    {showTemplateActions ? (
                      <div className={onboardingStyles.templateActions}>
                        <Button
                          variant="ghost"
                          size="sm"
                          layout="inline"
                          disabled={previewActionDisabled}
                          onClick={() => void openPreview(template.id)}
                        >
                          {t('personnel.onboarding.preview')}
                        </Button>
                        {template.hasDocxAsset ? (
                          <Button
                            variant="secondary"
                            size="sm"
                            layout="inline"
                            disabled={docxActionDisabled}
                            onClick={() => void openDocx(template.id)}
                          >
                            {t('personnel.onboarding.openWord')}
                          </Button>
                        ) : null}
                      </div>
                    ) : null}
                  </li>
                ))}
              </ul>
              {showTemplateActions ? (
                <div className={onboardingStyles.templateActions}>
                  <Button
                    variant="secondary"
                    size="sm"
                    layout="inline"
                    disabled={printActionDisabled}
                    onClick={() => void printSelected()}
                  >
                    {t('personnel.onboarding.printSelected')}
                  </Button>
                </div>
              ) : null}
            </>
          )}
        </section>
      </div>

      {preview ? (
        <WorkspaceDialog
          title={preview.name}
          size="compact"
          stacked
          onRequestClose={() => setPreview(null)}
          footer={
            <Button variant="primary" layout="inline" onClick={() => setPreview(null)}>
              {t('personnel.close')}
            </Button>
          }
        >
          <div
            className={onboardingStyles.previewSheet}
            dangerouslySetInnerHTML={{ __html: preview.renderedContent }}
          />
        </WorkspaceDialog>
      ) : null}

      {printDocs ? (
        <div ref={printRoot} className={onboardingStyles.printRoot} aria-hidden="true">
          {printDocs.map((doc) => (
            <div
              key={doc.templateId}
              className={onboardingStyles.printPage}
              dangerouslySetInnerHTML={{ __html: doc.renderedContent }}
            />
          ))}
        </div>
      ) : null}
    </div>
  )
}
