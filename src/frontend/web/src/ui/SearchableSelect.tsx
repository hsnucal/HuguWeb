import { useEffect, useId, useMemo, useRef, useState, type KeyboardEvent } from 'react'
import { SearchIcon } from './icons'
import { FieldLabel } from './TextField'
import fieldStyles from './TextField.module.css'
import styles from './SearchableSelect.module.css'

export type SearchableOption = {
  value: string
  label: string
  disabled?: boolean
}

export function SearchableSelect({
  id,
  label,
  value,
  options,
  onChange,
  onQuery,
  onBlur,
  hint,
  error,
  required,
  placeholder,
  disabled,
  emptyText,
  loadingText,
  searchIcon = false,
}: {
  id: string
  label: string
  value: string
  options: SearchableOption[]
  onChange: (value: string, option?: SearchableOption) => void
  onQuery?: (query: string) => Promise<SearchableOption[]> | SearchableOption[]
  onBlur?: () => void
  hint?: string
  error?: string
  required?: boolean
  placeholder?: string
  disabled?: boolean
  emptyText?: string
  loadingText?: string
  searchIcon?: boolean
}) {
  const listId = useId()
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [highlight, setHighlight] = useState(0)
  const [remote, setRemote] = useState<SearchableOption[] | null>(null)
  const root = useRef<HTMLDivElement>(null)
  const input = useRef<HTMLInputElement>(null)
  const onQueryRef = useRef(onQuery)
  useEffect(() => {
    onQueryRef.current = onQuery
  }, [onQuery])
  const selected = options.find((item) => item.value === value) ?? remote?.find((item) => item.value === value)

  const visible = useMemo(() => {
    const source = onQuery ? (remote ?? []) : options
    const term = query.trim().toLocaleLowerCase()
    const filtered = term === '' || onQuery
      ? source
      : source.filter((item) => item.label.toLocaleLowerCase().includes(term) || item.value.toLocaleLowerCase().includes(term))
    if (selected && !filtered.some((item) => item.value === selected.value) && query.trim() === '') {
      return [selected, ...filtered]
    }

    return filtered
  }, [onQuery, options, query, remote, selected])

  useEffect(() => {
    if (!open || !onQueryRef.current) {
      return
    }

    let cancelled = false
    const load = onQueryRef.current
    const timer = window.setTimeout(() => {
      void Promise.resolve(load(query)).then((rows) => {
        if (!cancelled) {
          setRemote(rows)
          setHighlight(0)
        }
      })
    }, 180)

    return () => {
      cancelled = true
      window.clearTimeout(timer)
    }
  }, [open, query])

  useEffect(() => {
    if (!open) {
      return
    }

    function onPointer(event: MouseEvent) {
      if (root.current && !root.current.contains(event.target as Node)) {
        setOpen(false)
        setQuery('')
      }
    }

    document.addEventListener('mousedown', onPointer)
    return () => document.removeEventListener('mousedown', onPointer)
  }, [open])

  const hintId = hint ? `${id}-hint` : undefined
  const errorId = error ? `${id}-error` : undefined
  const describedBy = [hintId, errorId].filter(Boolean).join(' ') || undefined
  const activeId = open && visible[highlight] ? `${listId}-${visible[highlight].value}` : undefined

  function choose(option: SearchableOption) {
    if (option.disabled) {
      return
    }

    onChange(option.value, option)
    setOpen(false)
    setQuery('')
    input.current?.focus()
  }

  function onKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'ArrowDown') {
      event.preventDefault()
      if (!open) {
        setOpen(true)
        return
      }

      setHighlight((current) => Math.min(current + 1, Math.max(visible.length - 1, 0)))
      return
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault()
      setHighlight((current) => Math.max(current - 1, 0))
      return
    }

    if (event.key === 'Home' && open) {
      event.preventDefault()
      setHighlight(0)
      return
    }

    if (event.key === 'End' && open) {
      event.preventDefault()
      setHighlight(Math.max(visible.length - 1, 0))
      return
    }

    if (event.key === 'Enter' && open) {
      event.preventDefault()
      const option = visible[highlight]
      if (option) {
        choose(option)
      }
      return
    }

    if (event.key === 'Escape') {
      event.preventDefault()
      setOpen(false)
      setQuery('')
    }
  }

  const display = open ? query : (selected?.label ?? '')
  const searching = Boolean(open && onQuery && remote === null)

  return (
    <div className={fieldStyles.field} ref={root}>
      <FieldLabel id={id} label={label} required={required} />
      <div className={styles.control}>
        {searchIcon ? (
          <span className={styles.searchIcon} aria-hidden="true">
            <SearchIcon />
          </span>
        ) : null}
        <input
          ref={input}
          id={id}
          className={`${fieldStyles.input} ${searchIcon ? styles.inputWithIcon : ''} ${error ? fieldStyles.invalid : ''} ${!display && placeholder ? fieldStyles.placeholderValue : ''}`}
          role="combobox"
          aria-autocomplete="list"
          aria-expanded={open}
          aria-controls={listId}
          aria-activedescendant={activeId}
          aria-invalid={error ? true : undefined}
          aria-describedby={describedBy}
          aria-required={required || undefined}
          autoComplete="off"
          disabled={disabled}
          value={display}
          placeholder={placeholder}
          onFocus={() => {
            setOpen(true)
            if (onQuery) {
              setRemote(null)
            }
          }}
          onChange={(event) => {
            setQuery(event.target.value)
            setOpen(true)
            if (onQuery) {
              setRemote(null)
            }
            if (event.target.value === '' && value !== '') {
              onChange('')
            }
          }}
          onBlur={onBlur}
          onKeyDown={onKeyDown}
        />
        {open ? (
          <ul className={styles.list} id={listId} role="listbox">
            {searching ? (
              <li className={styles.empty} role="presentation">
                {loadingText}
              </li>
            ) : visible.length === 0 ? (
              <li className={styles.empty} role="presentation">
                {emptyText}
              </li>
            ) : (
              visible.map((option, index) => (
                <li
                  key={option.value}
                  id={`${listId}-${option.value}`}
                  role="option"
                  aria-selected={option.value === value}
                  aria-disabled={option.disabled || undefined}
                  className={`${styles.option} ${index === highlight ? styles.optionActive : ''} ${option.disabled ? styles.optionDisabled : ''}`}
                  onMouseEnter={() => setHighlight(index)}
                  onMouseDown={(event) => {
                    event.preventDefault()
                    choose(option)
                  }}
                >
                  {option.label}
                </li>
              ))
            )}
          </ul>
        ) : null}
      </div>
      {hint ? (
        <p className={fieldStyles.hint} id={hintId}>
          {hint}
        </p>
      ) : null}
      {error ? (
        <p className={fieldStyles.error} id={errorId} role="alert">
          {error}
        </p>
      ) : null}
    </div>
  )
}
