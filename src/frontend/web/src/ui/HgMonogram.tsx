/**
 * HuGuWeb compact monogram.
 *
 * One geometric system: H’s crossbar continues into G, and G wraps the
 * shared right stem. Keep this outline in sync with public/favicon.svg.
 */
const glyph = {
  viewBox: '0 0 32 32',
  fill: 'currentColor',
  'aria-hidden': true as const,
  focusable: false as const,
}

export function HgMonogram({ className }: { className?: string }) {
  return (
    <svg className={className} {...glyph}>
      <HgMonogramShapes />
    </svg>
  )
}

function HgMonogramShapes() {
  return (
    <>
      {/* H left stem */}
      <rect x="2.25" y="4" width="6.25" height="24" rx="1.15" />
      {/* H right stem — shared spine that G wraps */}
      <rect x="12.75" y="4" width="6.25" height="24" rx="1.15" />
      {/* H crossbar continuing into G as the tongue */}
      <rect x="2.25" y="12.875" width="19.5" height="6.25" rx="1.15" />
      {/* G bowl: architectural C that receives the H stroke */}
      <path d="M16 4h8.15A6.25 6.25 0 0 1 30.4 10.25v11.5A6.25 6.25 0 0 1 24.15 28H16v-6.25h8.15V10.25H16V4z" />
    </>
  )
}
