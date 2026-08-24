# Visual Foundation 2026

Sprint 0.10B structural visual redesign of HuGuWeb’s product language.

This is presentation only. Domain rules, permissions, Identity, and data stay unchanged.

## Accepted state

The Product Owner accepted this implementation as the HuGuWeb visual foundation baseline.

- Future screens should reuse these tokens and primitives rather than inventing a parallel look.
- Visual evolution is allowed later; it must not change product workflows, authorization, or domain rules.
- The official HG monogram is in `src/frontend/web/src/assets/brand/` and `BrandMark`. See [HG Mark](HG_MARK.md). Product Owner visual approval remaining.

---

## Design philosophy

**Warm Hospitality + Modern Operations + Premium Calm**

HuGuWeb should feel like a high-end hospitality operations workspace people can use all day: warm linen and sand, distinctive deep plum, architectural geometry, and work-first composition.

Purple is identity and interaction. Inspected readiness may use a related amethyst accent. Purple is not a generic status system.

Avoid: generic admin templates, KPI-card walls, cold gray, identical white cards, oversized enterprise tables, gold luxury clichés, glassmorphism everywhere, and motion for its own sake.

---

## Why 0.10A was insufficient

Sprint 0.10A refined tokens, badges, and spacing on the existing composition. Login remained a split-screen. The shell remained a conventional admin sidebar. Pages remained stacked white cards. The Product Owner could not see a new product.

0.10B changes composition, page framing, navigation, hierarchy, data presentation, and brand presence.

---

## Color roles

Tokens live in `src/frontend/web/src/styles/tokens.css`.

| Category | Role |
|----------|------|
| Brand | Deep plum for primary action, active nav, focus, selected tint |
| Accent | Amethyst for Inspected / transition states |
| Surface | Page sand, sidebar stone, workspace linen, elevated, inset |
| Text | Warm charcoal primary / secondary / muted |
| Border | Subtle, default, strong, focus |
| Readiness | Dirty warm warning, Clean cool info, Inspected amethyst, Ready restrained success |
| Semantic | Success, warning, danger, info (text + soft + border) |

A later dark-green rebrand should change `--color-brand-*` (and optionally `--color-accent-*`). Dark mode is not implemented; token roles remain theme-compatible.

---

## Typography

Privacy-safe stack: Inter if installed locally, otherwise Segoe UI / system-ui. No font package or CDN.

Roles: kicker, page title, section title, body, supporting, label, table header, numeric, badge, button.

Page titles have presence. Section titles are editorial (sentence case), not uppercase admin labels. Kickers remain small uppercase. Data-heavy areas stay compact. Tabular numbers for rooms, counts, and times.

---

## Surfaces / page canvas

Layered hierarchy:

1. Ambient page (warm sand, quiet washes)
2. Workspace canvas (inset rounded linen surface)
3. Primary working surface (elevated lists, forms, heroes)
4. Secondary / inset wells (history, snapshot, current work)

Do not wrap every section in an identical card.

---

## Navigation

Slim rail with compact brand, icon wells + labels, filled active icon, quieter account block, and a rounded workspace to the right. Language stays in the rail (and the mobile bar). Top area is a workspace header: kicker, title, supporting line.

---

## Lists / grids

Deterministic columns remain. Visual language: readiness rails, row-state washes, identity marks, hover depth, labeled collapse on narrower viewports.

Do not add a generic DataGrid library.

---

## Visual data language

CSS/SVG primitives only. No chart library.

Allowed on real data already shown by the page: segmented distribution bars, relative progress tracks, compact meters, timeline graphics.

No fabricated occupancy, revenue, or satisfaction metrics.

---

## Timeline

Reusable `Timeline` / `TimelineItem` for room readiness history, inspection history, workforce assignment history, and Operations Center attention.

Markers, connectors, time typography. Historical records are not altered.

---

## Forms

Visible labels. Grouped sections with editorial legends. Shared focus ring. Helper and error text on the field. Action footer separated by a hairline.

---

## Motion

| Token | Use |
|-------|-----|
| `--motion-duration-press` | Button press ~110ms |
| `--motion-duration-fast` | Hover / focus |
| `--motion-duration-standard` | State change |
| `--motion-duration-panel` | Panel / timeline / readiness flash ~180–220ms |
| `--motion-duration-enter` | Login entrance |
| `--motion-duration-ambient` | Slow login atmosphere; disabled when reduced-motion |

`prefers-reduced-motion` zeroes durations and disables ambient/entrance animation and skeleton shimmer.

No bouncing, pulsing badges, or looping attention animation.

---

## Icons

Handcrafted 18px stroke SVGs, one stroke weight. No icon-library dependency: the set is small, already consistent, and a package would add bundle cost without solving a consistency gap.

---

## Accessibility

Keyboard paths, `:focus-visible`, contrast on warm surfaces, labeled controls, non-color status (labels + rails + meters), reduced motion, and meaningful loading/empty states.

---

## Anti-patterns

- Recoloring the old split login and calling it a redesign
- Purple cards, titles, and badges everywhere
- KPI walls and decorative donuts
- Heavy drop shadows on every panel
- Each domain inventing badge or table CSS
- Mobile-first ERP chrome
- New UI frameworks (MUI, Ant, Tailwind, shadcn, Bootstrap)
