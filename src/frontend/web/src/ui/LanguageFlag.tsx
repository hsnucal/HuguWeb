import type { ReactElement } from 'react'
import type { AppLanguage } from '../i18n/languages'

const flag = {
  viewBox: '0 0 60 40',
  'aria-hidden': true as const,
  focusable: false as const,
}

function TurkeyFlag() {
  return (
    <svg {...flag}>
      <rect width="60" height="40" fill="#E30A17" />
      <circle cx="21" cy="20" r="10" fill="#fff" />
      <circle cx="24.4" cy="20" r="8" fill="#E30A17" />
      <polygon
        fill="#fff"
        points="33.2,20 38.7,21.8 35.2,17.1 35.2,22.9 38.7,18.2"
      />
    </svg>
  )
}

function UnitedKingdomFlag() {
  return (
    <svg {...flag}>
      <rect width="60" height="40" fill="#012169" />
      <path d="M0 0 60 40M60 0 0 40" stroke="#fff" strokeWidth="8" />
      <path d="M0 0 60 40M60 0 0 40" stroke="#C8102E" strokeWidth="4.4" />
      <path d="M30 0v40M0 20h60" stroke="#fff" strokeWidth="12" />
      <path d="M30 0v40M0 20h60" stroke="#C8102E" strokeWidth="7.2" />
    </svg>
  )
}

function RussiaFlag() {
  return (
    <svg {...flag}>
      <rect width="60" height="13.34" fill="#fff" />
      <rect y="13.33" width="60" height="13.34" fill="#0039A6" />
      <rect y="26.66" width="60" height="13.34" fill="#D52B1E" />
    </svg>
  )
}

/** Single flag-asset mapping for supported app languages. */
const LANGUAGE_FLAGS: Record<AppLanguage, () => ReactElement> = {
  tr: TurkeyFlag,
  en: UnitedKingdomFlag,
  ru: RussiaFlag,
}

export function LanguageFlag({ language }: { language: AppLanguage }) {
  const Flag = LANGUAGE_FLAGS[language]
  return <Flag />
}
