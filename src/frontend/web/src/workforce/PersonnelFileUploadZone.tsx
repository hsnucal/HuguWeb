import { useId, useRef, useState, type DragEvent, type ReactNode } from 'react'
import { Button } from '../ui/Button'
import styles from './PersonnelMasterDialog.module.css'

type Props = {
  accept: string
  multiple?: boolean
  disabled?: boolean
  dropLabel: string
  orLabel: string
  chooseLabel: string
  hint: ReactNode
  inputLabel: string
  onFilesChange: (files: File[]) => void
}

export function PersonnelFileUploadZone({
  accept,
  multiple = false,
  disabled = false,
  dropLabel,
  orLabel,
  chooseLabel,
  hint,
  inputLabel,
  onFilesChange,
}: Props) {
  const inputId = useId()
  const inputRef = useRef<HTMLInputElement>(null)
  const [dragActive, setDragActive] = useState(false)

  function applySelection(selected: FileList | null) {
    if (!selected || selected.length === 0) {
      return
    }

    onFilesChange(multiple ? Array.from(selected) : [selected[0]!])
  }

  function onDragOver(event: DragEvent) {
    event.preventDefault()
    event.stopPropagation()
    if (!disabled) {
      setDragActive(true)
    }
  }

  function onDragLeave(event: DragEvent) {
    event.preventDefault()
    event.stopPropagation()
    setDragActive(false)
  }

  function onDrop(event: DragEvent) {
    event.preventDefault()
    event.stopPropagation()
    setDragActive(false)
    if (disabled) {
      return
    }

    applySelection(event.dataTransfer.files)
  }

  return (
    <div className={styles.uploadBlock}>
      <div
        className={`${styles.uploadZone} ${dragActive ? styles.uploadZoneActive : ''}`}
        onDragOver={onDragOver}
        onDragLeave={onDragLeave}
        onDrop={onDrop}
        onClick={() => {
          if (!disabled) {
            inputRef.current?.click()
          }
        }}
      >
        <p className={styles.uploadDropLabel}>{dropLabel}</p>
        <p className={styles.uploadOr}>{orLabel}</p>
        <Button
          type="button"
          variant="secondary"
          layout="inline"
          disabled={disabled}
          aria-controls={inputId}
          onClick={(event) => {
            event.stopPropagation()
            inputRef.current?.click()
          }}
        >
          {chooseLabel}
        </Button>
        <p className={styles.uploadHint}>{hint}</p>
        <input
          id={inputId}
          ref={inputRef}
          className={styles.hiddenFileInput}
          type="file"
          accept={accept}
          multiple={multiple}
          disabled={disabled}
          aria-label={inputLabel}
          onChange={(event) => {
            applySelection(event.target.files)
            event.target.value = ''
          }}
        />
      </div>
    </div>
  )
}
