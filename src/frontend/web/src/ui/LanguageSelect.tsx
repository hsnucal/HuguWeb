import { useEffect, useLayoutEffect, useRef, useState, type KeyboardEvent } from 'react'
import { createPortal } from 'react-dom'
import { useTranslation } from 'react-i18next'
import {
  APP_LANGUAGE_OPTIONS,
  DEFAULT_LANGUAGE,
  languageNativeName,
  toAppLanguage,
  type AppLanguage,
} from '../i18n/languages'
import { ChevronDownIcon } from './icons'
import { LanguageFlag } from './LanguageFlag'
import { placeAnchoredMenu } from './placeAnchoredMenu'
import styles from './LanguageSelect.module.css'

function menuCoords(trigger: HTMLElement, menu: HTMLElement) {
  const rect = trigger.getBoundingClientRect()
  const rem = Number.parseFloat(getComputedStyle(document.documentElement).fontSize) || 16
  return placeAnchoredMenu(
    rect,
    { width: Math.max(rect.width, 10.25 * rem), height: menu.offsetHeight },
    { width: window.innerWidth, height: window.innerHeight },
  )
}

export function LanguageSelect({
  id,
  disabled,
  className,
  tone = 'default',
  compact = false,
  onChange,
}: {
  id: string
  disabled?: boolean
  className?: string
  tone?: 'default' | 'onBrand'
  compact?: boolean
  onChange: (language: AppLanguage) => void
}) {
  const { t, i18n } = useTranslation()
  const value = toAppLanguage(i18n.resolvedLanguage ?? i18n.language) ?? DEFAULT_LANGUAGE
  const nativeName = languageNativeName(value)
  const accessibleLabel = t('common.languageCurrent', { name: nativeName })
  const selectedIndex = Math.max(
    APP_LANGUAGE_OPTIONS.findIndex((option) => option.code === value),
    0,
  )
  const [open, setOpen] = useState(false)
  const [highlight, setHighlight] = useState(selectedIndex)
  const [coords, setCoords] = useState({ top: 0, left: 0, width: 0 })
  const wrapRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const menuRef = useRef<HTMLUListElement>(null)
  const listId = `${id}-list`

  useLayoutEffect(() => {
    if (!open) {
      return
    }

    const trigger = triggerRef.current
    const menu = menuRef.current
    if (!trigger || !menu) {
      return
    }
    setCoords(menuCoords(trigger, menu))
  }, [open, compact])

  useEffect(() => {
    if (!open) {
      return
    }

    function onPointer(event: MouseEvent) {
      const target = event.target as Node
      if (wrapRef.current?.contains(target) || menuRef.current?.contains(target)) {
        return
      }
      setOpen(false)
    }

    function onKey(event: globalThis.KeyboardEvent) {
      if (event.key !== 'Escape') {
        return
      }
      event.preventDefault()
      event.stopPropagation()
      setOpen(false)
      triggerRef.current?.focus()
    }

    function onReposition() {
      const trigger = triggerRef.current
      const menu = menuRef.current
      if (!trigger || !menu) {
        return
      }
      setCoords(menuCoords(trigger, menu))
    }

    document.addEventListener('mousedown', onPointer)
    document.addEventListener('keydown', onKey, true)
    window.addEventListener('resize', onReposition)
    window.addEventListener('scroll', onReposition, true)
    return () => {
      document.removeEventListener('mousedown', onPointer)
      document.removeEventListener('keydown', onKey, true)
      window.removeEventListener('resize', onReposition)
      window.removeEventListener('scroll', onReposition, true)
    }
  }, [open, compact])

  function estimateCoords() {
    const trigger = triggerRef.current
    if (!trigger) {
      return
    }
    const rect = trigger.getBoundingClientRect()
    const rem = Number.parseFloat(getComputedStyle(document.documentElement).fontSize) || 16
    setCoords({
      top: rect.bottom + 6,
      left: rect.left,
      width: Math.max(rect.width, 10.25 * rem),
    })
  }

  function choose(language: AppLanguage) {
    onChange(language)
    setOpen(false)
    triggerRef.current?.focus()
  }

  function onTriggerKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
    if (event.key === 'Tab') {
      setOpen(false)
      return
    }

    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault()
      if (!open) {
        estimateCoords()
        setOpen(true)
        setHighlight(selectedIndex)
        return
      }

      const delta = event.key === 'ArrowDown' ? 1 : -1
      setHighlight((current) => (current + delta + APP_LANGUAGE_OPTIONS.length) % APP_LANGUAGE_OPTIONS.length)
      return
    }

    if (event.key === 'Home' && open) {
      event.preventDefault()
      setHighlight(0)
      return
    }

    if (event.key === 'End' && open) {
      event.preventDefault()
      setHighlight(APP_LANGUAGE_OPTIONS.length - 1)
      return
    }

    if ((event.key === 'Enter' || event.key === ' ') && open) {
      event.preventDefault()
      const option = APP_LANGUAGE_OPTIONS[highlight]
      if (option) {
        choose(option.code)
      }
    }
  }

  const activeOption = APP_LANGUAGE_OPTIONS[highlight]
  const activeId = open && activeOption ? `${id}-option-${activeOption.code}` : undefined

  return (
    <div
      ref={wrapRef}
      className={[styles.wrap, styles.picker, compact ? styles.compact : '', className].filter(Boolean).join(' ')}
    >
      <button
        ref={triggerRef}
        type="button"
        id={id}
        className={[
          styles.trigger,
          tone === 'onBrand' ? styles.onBrandTrigger : '',
          compact ? styles.compactTrigger : '',
        ]
          .filter(Boolean)
          .join(' ')}
        disabled={disabled}
        role="combobox"
        aria-label={accessibleLabel}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={listId}
        aria-autocomplete="none"
        aria-activedescendant={activeId}
        onClick={() => {
          if (disabled) {
            return
          }
          if (!open) {
            estimateCoords()
            setHighlight(selectedIndex)
          }
          setOpen((current) => !current)
        }}
        onKeyDown={onTriggerKeyDown}
      >
        <span className={[styles.flag, tone === 'onBrand' ? styles.flagOnBrand : ''].filter(Boolean).join(' ')}>
          <LanguageFlag language={value} />
        </span>
        {compact ? null : <span className={styles.triggerLabel}>{nativeName}</span>}
        <ChevronDownIcon className={styles.chevron} />
      </button>
      {open
        ? createPortal(
            <ul
              ref={menuRef}
              id={listId}
              className={styles.menu}
              role="listbox"
              aria-labelledby={id}
              style={{ top: coords.top, left: coords.left, width: coords.width || undefined }}
            >
              {APP_LANGUAGE_OPTIONS.map((option, index) => {
                const selected = option.code === value
                return (
                  <li
                    key={option.code}
                    id={`${id}-option-${option.code}`}
                    role="option"
                    aria-selected={selected}
                    className={[
                      styles.option,
                      index === highlight ? styles.optionActive : '',
                      selected ? styles.optionSelected : '',
                    ]
                      .filter(Boolean)
                      .join(' ')}
                    onMouseEnter={() => setHighlight(index)}
                    onMouseDown={(event) => event.preventDefault()}
                    onClick={() => choose(option.code)}
                  >
                    <span className={styles.optionFlag}>
                      <LanguageFlag language={option.code} />
                    </span>
                    {option.nativeName}
                  </li>
                )
              })}
            </ul>,
            document.body,
          )
        : null}
    </div>
  )
}
