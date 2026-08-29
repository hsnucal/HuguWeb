import { useLayoutEffect, useRef, type ClipboardEvent, type KeyboardEvent } from 'react'
import { TextField } from '../ui/TextField'
import {
  formatMobile,
  isPayloadDigitChar,
  isUnsafePhonePaste,
  mobileCaretFromDigitCount,
  MOBILE_DIGIT_MAX,
  MOBILE_PLACEHOLDER,
  normalizeMobileDigits,
} from './personnelInput'

export function MobilePhoneField({
  id,
  label,
  value,
  onChange,
  onBlur,
  onUnsafePaste,
  error,
  disabled,
  required,
}: {
  id: string
  label: string
  value: string
  onChange: (digits: string) => void
  onBlur: () => void
  onUnsafePaste: () => void
  error?: string
  disabled?: boolean
  required?: boolean
}) {
  const inputRef = useRef<HTMLInputElement>(null)
  const caretRef = useRef<number | null>(null)
  const displayed = formatMobile(value)

  useLayoutEffect(() => {
    const node = inputRef.current
    const caret = caretRef.current
    if (!node || caret === null) {
      return
    }

    node.setSelectionRange(caret, caret)
    caretRef.current = null
  }, [displayed])

  function commit(digits: string, payloadBefore: number) {
    const next = digits.slice(0, MOBILE_DIGIT_MAX)
    caretRef.current = mobileCaretFromDigitCount(formatMobile(next), Math.min(payloadBefore, next.length))
    onChange(next)
  }

  return (
    <TextField
      id={id}
      ref={inputRef}
      label={label}
      value={displayed}
      placeholder={MOBILE_PLACEHOLDER}
      inputMode="numeric"
      autoComplete="tel"
      spellCheck={false}
      disabled={disabled}
      error={error}
      required={required}
      onBlur={onBlur}
      onChange={(next, event) => {
        const caret = event.target.selectionStart ?? next.length
        commit(normalizeMobileDigits(next), normalizeMobileDigits(next.slice(0, caret)).length)
      }}
      onKeyDown={(event: KeyboardEvent<HTMLInputElement>) => {
        const node = event.currentTarget
        const start = node.selectionStart ?? 0
        const end = node.selectionEnd ?? 0
        const hasSelection = start !== end

        if (event.key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
          if (event.key < '0' || event.key > '9') {
            event.preventDefault()
            return
          }
          if (!hasSelection && value.length >= MOBILE_DIGIT_MAX) {
            event.preventDefault()
            return
          }
        }

        if (event.key === 'Backspace' && !hasSelection) {
          const payloadBefore = normalizeMobileDigits(node.value.slice(0, start)).length
          if (!isPayloadDigitChar(node.value, start - 1)) {
            event.preventDefault()
            if (payloadBefore > 0) {
              commit(value.slice(0, payloadBefore - 1) + value.slice(payloadBefore), payloadBefore - 1)
            }
          }
          return
        }

        if (event.key === 'Delete' && !hasSelection) {
          const payloadBefore = normalizeMobileDigits(node.value.slice(0, start)).length
          if (!isPayloadDigitChar(node.value, start)) {
            event.preventDefault()
            if (payloadBefore < value.length) {
              commit(value.slice(0, payloadBefore) + value.slice(payloadBefore + 1), payloadBefore)
            }
          }
        }
      }}
      onPaste={(event: ClipboardEvent<HTMLInputElement>) => {
        event.preventDefault()
        const text = event.clipboardData.getData('text')
        if (isUnsafePhonePaste(text) || (text.trim() !== '' && normalizeMobileDigits(text).length === 0)) {
          onUnsafePaste()
          return
        }

        const node = event.currentTarget
        const start = node.selectionStart ?? 0
        const end = node.selectionEnd ?? node.value.length
        const before = normalizeMobileDigits(node.value.slice(0, start))
        const after = normalizeMobileDigits(node.value.slice(end))
        const inserted = normalizeMobileDigits(text)
        const next = (before + inserted + after).slice(0, MOBILE_DIGIT_MAX)
        commit(next, before.length + inserted.length)
      }}
    />
  )
}
