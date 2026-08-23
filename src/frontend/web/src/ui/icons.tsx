type IconProps = {
  className?: string
}

const svg = {
  width: 18,
  height: 18,
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.65,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
  'aria-hidden': true as const,
  focusable: false as const,
}

export function HomeIcon({ className }: IconProps) {
  return (
    <svg className={className} {...svg}>
      <path d="M4 11.5 12 5l8 6.5" />
      <path d="M6.5 10.5V19h11V10.5" />
    </svg>
  )
}

export function RoomsIcon({ className }: IconProps) {
  return (
    <svg className={className} {...svg}>
      <rect x="4" y="5" width="7" height="6" rx="1.2" />
      <rect x="13" y="5" width="7" height="6" rx="1.2" />
      <rect x="4" y="13" width="7" height="6" rx="1.2" />
      <rect x="13" y="13" width="7" height="6" rx="1.2" />
    </svg>
  )
}

export function ReservationsIcon({ className }: IconProps) {
  return (
    <svg className={className} {...svg}>
      <rect x="4" y="5" width="16" height="15" rx="1.5" />
      <path d="M8 3.5v3M16 3.5v3M4 10h16" />
    </svg>
  )
}

export function TasksIcon({ className }: IconProps) {
  return (
    <svg className={className} {...svg}>
      <path d="M9 7h11M9 12h11M9 17h11" />
      <path d="M4.5 7.2 5.8 8.5 8 6" />
      <path d="M4.5 12.2 5.8 13.5 8 11" />
      <path d="M4.5 17.2 5.8 18.5 8 16" />
    </svg>
  )
}

export function SettingsIcon({ className }: IconProps) {
  return (
    <svg className={className} {...svg}>
      <circle cx="12" cy="12" r="3" />
      <path d="M12 4.5v1.6M12 17.9v1.6M19.5 12h-1.6M6.1 12H4.5M17.3 6.7l-1.1 1.1M7.8 16.2l-1.1 1.1M17.3 17.3l-1.1-1.1M7.8 7.8 6.7 6.7" />
    </svg>
  )
}

export function WrenchIcon({ className }: IconProps) {
  return (
    <svg className={className} {...svg}>
      <path d="M14.7 6.3a4 4 0 0 1-5.4 5.4L5 16l3 3 4.3-4.3a4 4 0 0 1 5.4-5.4l-2-2Z" />
    </svg>
  )
}

export function PeopleIcon({ className }: IconProps) {
  return (
    <svg className={className} {...svg}>
      <circle cx="9" cy="8" r="2.4" />
      <path d="M4.5 18v-1.2C4.5 14.4 6.4 13 9 13s4.5 1.4 4.5 3.8V18" />
      <circle cx="16.5" cy="8.5" r="2" />
      <path d="M19.5 18v-1c0-1.7-1.2-3.1-3.2-3.6" />
    </svg>
  )
}

export function SignOutIcon({ className }: IconProps) {
  return (
    <svg className={className} {...svg}>
      <path d="M10 5H6.5A1.5 1.5 0 0 0 5 6.5v11A1.5 1.5 0 0 0 6.5 19H10" />
      <path d="M10 12h9" />
      <path d="M16 8.5 19.5 12 16 15.5" />
    </svg>
  )
}

export function ChevronRightIcon({ className }: IconProps) {
  return (
    <svg className={className} {...svg}>
      <path d="M9 6.5 14.5 12 9 17.5" />
    </svg>
  )
}

export function ChevronLeftIcon({ className }: IconProps) {
  return (
    <svg className={className} {...svg}>
      <path d="M15 6.5 9.5 12 15 17.5" />
    </svg>
  )
}
