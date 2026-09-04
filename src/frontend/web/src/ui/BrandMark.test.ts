import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'

const mark = readFileSync(new URL('../../public/huguweb.svg', import.meta.url), 'utf8')
const html = readFileSync(new URL('../../index.html', import.meta.url), 'utf8')
const brand = readFileSync(new URL('./BrandMark.tsx', import.meta.url), 'utf8')
const css = readFileSync(new URL('./BrandMark.module.css', import.meta.url), 'utf8')
const login = readFileSync(new URL('../auth/LoginPage.tsx', import.meta.url), 'utf8')
const ambient = readFileSync(new URL('../auth/AmbientBrandMark.tsx', import.meta.url), 'utf8')
const sidebar = readFileSync(new URL('../app/Sidebar.tsx', import.meta.url), 'utf8')
const shell = readFileSync(new URL('../app/AppShell.tsx', import.meta.url), 'utf8')

test('checked-in HuGuWeb mark keeps authored geometry and color', () => {
  assert.match(mark, /viewBox="0 0 500 500"/)
  assert.match(mark, /fill="#023C28"/)
  assert.doesNotMatch(mark, /C:[\\/]Users/)
})

test('favicon, login, and sidebar use the canonical HuGuWeb SVG', () => {
  assert.match(html, /href="\/huguweb\.svg"/)
  assert.doesNotMatch(html, /favicon\.svg/)
  assert.match(brand, /HUGUWEB_MARK_SRC = '\/huguweb\.svg'/)
  assert.match(brand, /BrandMarkSize = 'login' \| 'sidebar' \| 'sidebarCollapsed' \| 'mobile'/)
  assert.match(login, /<BrandMark size="login" label="HuGuWeb" \/>/)
  assert.match(login, /AmbientBrandMark/)
  assert.match(ambient, /HUGUWEB_MARK_SRC/)
  assert.match(sidebar, /size="sidebar"/)
  assert.match(sidebar, /railCollapsed \? 'sidebarCollapsed' : 'sidebar'/)
  assert.match(sidebar, /!railCollapsed \? <span className=\{styles\.wordmark\}>HuGu<\/span> : null/)
  assert.match(shell, /<BrandMark size="mobile" tone="inverse" \/>/)
  assert.match(shell, /<span className=\{styles\.mobileBrand\}>[\s\S]*HuGu[\s\S]*<\/span>/)
  assert.match(css, /object-fit:\s*contain/)
  assert.match(css, /transform:\s*scale\(1\.9\)/)
  assert.match(css, /\.login\s*\{[\s\S]*?width:\s*clamp\(5\.75rem, 36vw, 10rem\)/)
  assert.match(css, /\.login \.image\s*\{[\s\S]*?height:\s*auto/)
  assert.match(css, /\.sidebar\s*\{[\s\S]*?height:\s*2\.5rem/)
  assert.match(css, /\.sidebarCollapsed\s*\{[\s\S]*?height:\s*1\.75rem/)
  assert.match(css, /\.mobile\s*\{[\s\S]*?height:\s*1\.75rem/)
  assert.doesNotMatch(`${brand}${login}${sidebar}${shell}${html}${ambient}${mark}`, /C:[\\/]Users/)
  assert.doesNotMatch(`${brand}${login}${sidebar}${shell}${html}${ambient}`, /huguweb-(small|white|collapsed)\.svg/)
})
