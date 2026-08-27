import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '../ui/Button'
import { Notice } from '../ui/Notice'
import { StatusBadge } from '../ui/StatusBadge'
import { WorkspaceDialog } from '../ui/WorkspaceDialog'
import styles from './PersonnelMasterDialog.module.css'
import { formatFileSize } from './formatFileSize'
import { PersonnelFileUploadZone } from './PersonnelFileUploadZone'
import {
  confirmHrImport,
  downloadBlob,
  downloadHrImportTemplate,
  previewHrImport,
  type PersonnelImportPreviewResult,
} from './hrPersonnelMasterApi'
import { hrErrorKey } from './hrApi'

type Props = {
  onClose: () => void
  onCompleted: () => void
}

export function PersonnelImportDialog({ onClose, onCompleted }: Props) {
  const { t } = useTranslation()
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<PersonnelImportPreviewResult | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<string | null>(null)
  const replaceInputRef = useRef<HTMLInputElement>(null)

  function resetPreview() {
    setPreview(null)
    setResult(null)
    setError(null)
  }

  function clearFile() {
    setFile(null)
    resetPreview()
  }

  async function onDownloadTemplate() {
    setError(null)
    try {
      const blob = await downloadHrImportTemplate()
      downloadBlob(blob, 'hugu-personnel-import-template.xlsx')
    } catch (reason) {
      setError(t(hrErrorKey(reason)))
    }
  }

  async function onPreview() {
    if (!file) {
      return
    }

    setBusy(true)
    setError(null)
    setResult(null)
    try {
      setPreview(await previewHrImport(file))
    } catch (reason) {
      setError(t(hrErrorKey(reason)))
      setPreview(null)
    } finally {
      setBusy(false)
    }
  }

  async function onConfirm() {
    if (!preview?.canConfirm) {
      return
    }

    setBusy(true)
    setError(null)
    try {
      const confirmed = await confirmHrImport(preview.previewToken)
      setResult(
        t('personnel.importResult', {
          created: confirmed.createdCount,
          updated: confirmed.updatedCount,
        }),
      )
      onCompleted()
    } catch (reason) {
      setError(t(hrErrorKey(reason)))
    } finally {
      setBusy(false)
    }
  }

  return (
    <WorkspaceDialog
      title={t('personnel.importTitle')}
      subtitle={preview ? undefined : t('personnel.importIntro')}
      size="compact"
      bodyOverflow="hidden"
      onRequestClose={onClose}
      footer={
        <div className={styles.footerBar}>
          <div className={styles.footerActions}>
            <Button variant="ghost" onClick={onClose}>
              {t('workforce.cancel')}
            </Button>
            {preview ? (
              <Button variant="ghost" onClick={resetPreview}>
                {t('personnel.importBack')}
              </Button>
            ) : null}
          </div>
          <div className={styles.footerActions}>
            {!preview ? (
              <Button variant="primary" layout="inline" loading={busy} disabled={!file} onClick={() => void onPreview()}>
                {t('personnel.importPreview')}
              </Button>
            ) : (
              <Button
                variant="primary"
                layout="inline"
                loading={busy}
                disabled={!preview.canConfirm}
                onClick={() => void onConfirm()}
              >
                {t('personnel.importConfirm')}
              </Button>
            )}
          </div>
        </div>
      }
    >
      <div className={`${styles.dialogStack} ${preview ? styles.dialogStackPreview : ''}`}>
        {!preview ? (
          <>
            <div className={styles.topActions}>
              <Button variant="secondary" layout="inline" onClick={() => void onDownloadTemplate()}>
                {t('personnel.importTemplate')}
              </Button>
            </div>

            {!file ? (
              <PersonnelFileUploadZone
                accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                dropLabel={t('personnel.importUploadDrop')}
                orLabel={t('personnel.importUploadOr')}
                chooseLabel={t('personnel.importChooseFile')}
                hint={t('personnel.importFileHint')}
                inputLabel={t('personnel.importFile')}
                disabled={busy}
                onFilesChange={(selected) => setFile(selected[0] ?? null)}
              />
            ) : (
              <div className={styles.selectedFile}>
                <div className={styles.selectedFileMeta}>
                  <p className={styles.selectedFileName}>{file.name}</p>
                  <p className={styles.selectedFileSize}>{formatFileSize(file.size)}</p>
                </div>
                <div className={styles.selectedFileActions}>
                  <Button
                    variant="secondary"
                    size="sm"
                    layout="inline"
                    disabled={busy}
                    onClick={() => replaceInputRef.current?.click()}
                  >
                    {t('personnel.importChangeFile')}
                  </Button>
                  <Button variant="ghost" size="sm" layout="inline" disabled={busy} onClick={clearFile}>
                    {t('personnel.importRemoveFile')}
                  </Button>
                  <input
                    ref={replaceInputRef}
                    className={styles.hiddenFileInput}
                    type="file"
                    accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    aria-label={t('personnel.importChangeFile')}
                    onChange={(event) => {
                      const next = event.target.files?.[0]
                      if (next) {
                        setFile(next)
                        resetPreview()
                      }
                      event.target.value = ''
                    }}
                  />
                </div>
              </div>
            )}
          </>
        ) : (
          <>
            <div className={styles.summaryGrid}>
              <div className={styles.summaryItem}>
                <span className={styles.summaryLabel}>{t('personnel.importSummaryTotal')}</span>
                <span className={styles.summaryValue}>{preview.totalRows}</span>
              </div>
              <div className={styles.summaryItem}>
                <span className={styles.summaryLabel}>{t('personnel.importSummaryCreate')}</span>
                <span className={styles.summaryValue}>{preview.createCount}</span>
              </div>
              <div className={styles.summaryItem}>
                <span className={styles.summaryLabel}>{t('personnel.importSummaryUpdate')}</span>
                <span className={styles.summaryValue}>{preview.updateCount}</span>
              </div>
              <div className={styles.summaryItem}>
                <span className={styles.summaryLabel}>{t('personnel.importSummaryInvalid')}</span>
                <span className={styles.summaryValue}>{preview.invalidCount}</span>
              </div>
            </div>

            <div className={styles.previewScroll}>
              <table className={styles.dataTable}>
                <thead>
                  <tr>
                    <th>{t('personnel.importColStatus')}</th>
                    <th>{t('personnel.importColRow')}</th>
                    <th>{t('personnel.importColAction')}</th>
                    <th>{t('personnel.importColPersonnelNumber')}</th>
                    <th>{t('personnel.importColGivenName')}</th>
                    <th>{t('personnel.importColFamilyName')}</th>
                    <th>{t('personnel.importColDepartment')}</th>
                    <th>{t('personnel.importColPosition')}</th>
                    <th>{t('personnel.importColStartDate')}</th>
                    <th>{t('personnel.importColError')}</th>
                  </tr>
                </thead>
                <tbody>
                  {preview.rows.map((row) => {
                    const invalid = row.errors.length > 0
                    return (
                      <tr key={row.rowNumber}>
                        <td>
                          {invalid ? (
                            <StatusBadge tone="danger" variant="outline">
                              {t('personnel.importStatusInvalid')}
                            </StatusBadge>
                          ) : row.action === 'Create' ? (
                            <StatusBadge tone="success" variant="outline">
                              {t('personnel.importStatusNew')}
                            </StatusBadge>
                          ) : (
                            <StatusBadge tone="info" variant="outline">
                              {t('personnel.importStatusUpdate')}
                            </StatusBadge>
                          )}
                        </td>
                        <td>{row.rowNumber}</td>
                        <td>
                          {row.action === 'Create'
                            ? t('personnel.importActionCreate')
                            : t('personnel.importActionUpdate')}
                        </td>
                        <td>{row.personnelNumber ?? '—'}</td>
                        <td>{row.givenName}</td>
                        <td>{row.familyName}</td>
                        <td>{row.departmentLabel}</td>
                        <td>{row.positionLabel}</td>
                        <td>{row.employmentStartDate}</td>
                        <td className={styles.errorCell}>
                          {invalid ? row.errors.map((item) => item.message).join('; ') : '—'}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </>
        )}

        {error ? <Notice tone="danger">{error}</Notice> : null}
        {result ? <Notice tone="success">{result}</Notice> : null}
      </div>
    </WorkspaceDialog>
  )
}
