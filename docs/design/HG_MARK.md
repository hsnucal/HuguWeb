# HG Monogram

Official compact mark for HuGu.

## Mark

**HG** — a single geometric monogram. It means HuGu.

Do not put HuGu, HuGuWeb, ERP, or PMS inside the SVG.

## Construction

- The right stem of **H** is the left wall of **G**.
- The H cross-stroke continues through that shared stem and becomes the G spur.
- The G bowl opens to the right and receives that stroke.
- Stroke thickness is a 10-unit module on a 64-unit square. The H counter matches that module. Outer G corners use a 12-unit radius.

First glance: one symbol. Second glance: H + G.

## Color

| Use | Token | Hex |
|-----|--------|-----|
| Primary, on light surfaces | `--color-brand-primary` | `#5c2a6e` |
| Reverse, on dark / brand surfaces | `--color-brand-on-primary` | `#fbf7f4` |

Do not introduce other purples for the mark.

## Assets

- `src/frontend/web/src/assets/brand/hg-mark.svg`
- `src/frontend/web/src/assets/brand/hg-mark-white.svg`
- `src/frontend/web/src/assets/brand/hg-icon.svg` — reverse mark on a rounded brand square
- `src/frontend/web/public/favicon.svg` — same icon treatment, served to the browser

UI uses `BrandMark` / `HgMonogram` with `currentColor` so the live mark follows the token.

## Rules

- Do not distort, rotate as decoration, or recolor arbitrarily.
- Do not place text inside the mark.
- Keep the square aspect ratio.
- Keep clear space of about one stroke width around the mark.
- Collapsed or compact UI: mark alone. Expanded lockup: mark + the word HuGu, not a second copy of HG.

Pending Product Owner visual approval.
