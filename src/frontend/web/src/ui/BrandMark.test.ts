import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'

const mark = readFileSync(new URL('../../public/huguweb.svg', import.meta.url), 'utf8')
const tokens = readFileSync(new URL('../styles/tokens.css', import.meta.url), 'utf8')
const html = readFileSync(new URL('../../index.html', import.meta.url), 'utf8')
const brand = readFileSync(new URL('./BrandMark.tsx', import.meta.url), 'utf8')
const css = readFileSync(new URL('./BrandMark.module.css', import.meta.url), 'utf8')
const login = readFileSync(new URL('../auth/LoginPage.tsx', import.meta.url), 'utf8')
const ambient = readFileSync(new URL('../auth/AmbientBrandMark.tsx', import.meta.url), 'utf8')
const ambientCss = readFileSync(new URL('../auth/AmbientBrandMark.module.css', import.meta.url), 'utf8')
const sidebar = readFileSync(new URL('../app/Sidebar.tsx', import.meta.url), 'utf8')
const shell = readFileSync(new URL('../app/AppShell.tsx', import.meta.url), 'utf8')

const liveUi = `${brand}${css}${login}${ambient}${ambientCss}${sidebar}${shell}${html}`

test('checked-in HuGuWeb mark keeps authored geometry and color', () => {
  assert.match(mark, /viewBox="0 0 500 500"/)
  assert.match(mark, /fill="#862A51"/)
  assert.match(tokens, /--color-brand-primary:\s*#862A51/)
  assert.doesNotMatch(mark, /fill="#023C28"/)
  assert.doesNotMatch(mark, /C:[\\/]Users/)
})

test('login main BrandMark uses the canonical HuGuWeb SVG without inverse filter', () => {
  assert.match(brand, /HUGUWEB_MARK_SRC = '\/huguweb\.svg'/)
  assert.match(brand, /src=\{HUGUWEB_MARK_SRC\}/)
  assert.match(login, /<BrandMark size="login" label="HuGuWeb" \/>/)
  assert.doesNotMatch(login, /tone="inverse"/)
  assert.match(css, /\.login\s*\{[\s\S]*?width:\s*clamp\(5\.75rem, 36vw, 10rem\)/)
  assert.doesNotMatch(css, /\.login \.image\s*\{[^}]*filter/)
  assert.match(css, /\.inverse \.image\s*\{[\s\S]*?filter:\s*brightness\(0\) invert\(1\)/)
})

test('AmbientBrandMark collapsed state renders the canonical HG emblem only', () => {
  assert.match(login, /AmbientBrandMark/)
  assert.match(ambient, /className=\{styles\.emblem\}/)
  assert.match(ambient, /src=\{HUGUWEB_MARK_SRC\}/)
  assert.match(ambientCss, /\.emblem img/)
  assert.match(ambientCss, /object-fit:\s*contain/)
  assert.match(ambientCss, /object-position:\s*center/)
  assert.match(ambientCss, /@keyframes ambientEmblem \{[\s\S]*?0%,\s*22\.5% \{[\s\S]*?opacity: 1/)
  assert.match(ambientCss, /@keyframes ambientLetterH \{[\s\S]*?0%,\s*22\.5% \{[\s\S]*?opacity: 0/)
  assert.match(ambientCss, /@keyframes ambientLetterG \{[\s\S]*?0%,\s*22\.5% \{[\s\S]*?opacity: 0/)
  assert.match(ambientCss, /@keyframes ambientLetterU1 \{[\s\S]*?0%,\s*24\.75% \{[\s\S]*?opacity: 0/)
  assert.match(ambientCss, /@keyframes ambientLetterU2 \{[\s\S]*?0%,\s*26\.875% \{[\s\S]*?opacity: 0/)
  assert.doesNotMatch(ambient, /hg-mark|hg-icon|HgMonogram/)
  assert.doesNotMatch(ambientCss, /\.compact svg/)
  assert.doesNotMatch(ambientCss, /border-radius:\s*50%/)
})

test('AmbientBrandMark expanded state renders HuGu from H, u, G, u', () => {
  assert.match(ambient, /className=\{styles\.letterH\}>H</)
  assert.match(ambient, /className=\{styles\.letterU1\}>u</)
  assert.match(ambient, /className=\{styles\.letterG\}>G</)
  assert.match(ambient, /className=\{styles\.letterU2\}>u</)
  assert.match(ambientCss, /@keyframes ambientLetterH \{[\s\S]*?24\.75%,\s*47\.5% \{[\s\S]*?opacity: 1/)
  assert.match(ambientCss, /@keyframes ambientLetterG \{[\s\S]*?24\.75%,\s*47\.5% \{[\s\S]*?translateX\(0\)/)
  assert.match(ambientCss, /@keyframes ambientLetterU1 \{[\s\S]*?26\.875%,\s*47\.5% \{[\s\S]*?opacity: 1/)
  assert.match(ambientCss, /@keyframes ambientLetterU2 \{[\s\S]*?29%,\s*47\.5% \{[\s\S]*?opacity: 1/)
  assert.match(ambientCss, /translateX\(-0\.72em\)/)
})

test('AmbientBrandMark letters use the canonical SVG brand color', () => {
  assert.match(ambientCss, /\.ambient \{[\s\S]*?color: var\(--color-brand-primary\)/)
  assert.match(ambientCss, /\.ambient \{[\s\S]*?opacity: 0\.12/)
  assert.match(ambientCss, /\.emblem img \{[\s\S]*?filter: none/)
  assert.match(ambientCss, /\.letterH,\s*\.letterU1,\s*\.letterG,\s*\.letterU2 \{[\s\S]*?color: var\(--color-brand-primary\)/)
  assert.doesNotMatch(ambientCss, /brightness\(|invert\(|sepia\(|hue-rotate\(|saturate\(/)
  assert.match(ambientCss, /mix-blend-mode: normal/)
  assert.doesNotMatch(ambientCss, /mix-blend-mode:\s*(multiply|screen|overlay|darken|lighten|color)/)
  assert.doesNotMatch(ambientCss, /#023C28/)
  assert.doesNotMatch(ambientCss, /#[0-9A-Fa-f]{3,8}/)
  assert.doesNotMatch(ambientCss, /linear-gradient|mauve|pink|#c4|#d8a|#e8/)
})

test('AmbientBrandMark collapse reverses to the canonical HG emblem', () => {
  assert.match(ambientCss, /@keyframes ambientLetterU2 \{[\s\S]*?49\.75%,\s*100% \{[\s\S]*?opacity: 0/)
  assert.match(ambientCss, /@keyframes ambientLetterU1 \{[\s\S]*?51\.875%,\s*100% \{[\s\S]*?opacity: 0/)
  assert.match(ambientCss, /@keyframes ambientLetterG \{[\s\S]*?54%,\s*56\.25%,\s*100% \{[\s\S]*?translateX\(-0\.72em\)/)
  assert.match(ambientCss, /@keyframes ambientEmblem \{[\s\S]*?54%,\s*56\.25%,\s*100% \{[\s\S]*?opacity: 1/)
  assert.match(ambientCss, /cubic-bezier\(0\.4, 0, 0\.2, 1\)/)
  assert.doesNotMatch(ambientCss, /cubic-bezier\([^)]*[1-9]\d*\.\d+/)
})

test('AmbientBrandMark reduced-motion path snaps without a long tween', () => {
  assert.match(
    ambientCss,
    /prefers-reduced-motion: reduce[\s\S]*?animation-timing-function: step-end/,
  )
  assert.match(ambientCss, /prefers-reduced-motion: reduce[\s\S]*?\.letterH/)
})

test('favicon, sidebar, and mobile use the canonical HuGuWeb SVG', () => {
  assert.match(html, /href="\/huguweb\.svg"/)
  assert.doesNotMatch(html, /favicon\.svg/)
  assert.match(brand, /BrandMarkSize = 'login' \| 'sidebar' \| 'sidebarCollapsed' \| 'mobile'/)
  assert.match(sidebar, /size="sidebar"/)
  assert.match(sidebar, /tone="inverse"/)
  assert.match(sidebar, /railCollapsed \? 'sidebarCollapsed' : 'sidebar'/)
  assert.match(sidebar, /!railCollapsed \? <span className=\{styles\.wordmark\}>HuGu<\/span> : null/)
  assert.match(shell, /<BrandMark size="mobile" tone="inverse" \/>/)
  assert.match(shell, /<span className=\{styles\.mobileBrand\}>[\s\S]*HuGu[\s\S]*<\/span>/)
  assert.match(css, /object-fit:\s*contain/)
  assert.match(css, /transform:\s*scale\(1\.9\)/)
  assert.match(css, /\.login \.image\s*\{[\s\S]*?height:\s*auto/)
  assert.match(css, /\.sidebar\s*\{[\s\S]*?height:\s*2\.5rem/)
  assert.match(css, /\.sidebarCollapsed\s*\{[\s\S]*?height:\s*1\.75rem/)
  assert.match(css, /\.mobile\s*\{[\s\S]*?height:\s*1\.75rem/)
})

test('live UI does not keep obsolete HG geometry or desktop paths', () => {
  assert.doesNotMatch(`${liveUi}${mark}`, /C:[\\/]Users/)
  assert.doesNotMatch(liveUi, /huguweb-(small|white|collapsed)\.svg/)
  assert.doesNotMatch(liveUi, /assets\/brand\/hg-(mark|icon)/)
  assert.doesNotMatch(liveUi, /HgMonogram/)
  assert.doesNotMatch(liveUi, /fill="#023C28"/)
})
