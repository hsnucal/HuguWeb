/**
 * Official HuGu HG monogram.
 *
 * Geometry matches src/assets/brand/hg-mark.svg.
 * H crossbar continues into G as the G spur; G’s left wall is H’s right stem.
 */
const glyph = {
  viewBox: '0 0 64 64',
  fill: 'currentColor',
  'aria-hidden': true as const,
  focusable: false as const,
}

export function HgMonogram({ className }: { className?: string }) {
  return (
    <svg className={className} {...glyph}>
      <rect x="6" y="6" width="10" height="52" rx="2" />
      <rect x="26" y="6" width="10" height="52" rx="2" />
      <rect x="6" y="27" width="44" height="10" rx="2" />
      <path d="M35 6h11a12 12 0 0 1 12 12v4H48V16H35z" />
      <path d="M35 48h13V42h10v4a12 12 0 0 1-12 12H35z" />
    </svg>
  )
}
