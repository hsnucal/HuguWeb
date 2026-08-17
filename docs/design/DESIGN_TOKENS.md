# Design Tokens

> **Status:** Sprint 0.4 design freeze — proposed for Product Owner + CTO review. Token *categories and naming* only. Do not create production CSS variables, theme packages, or component tokens in application code in this sprint.

This is the source of truth for **token strategy**. Visual rules live in [Design Principles](DESIGN_PRINCIPLES.md). Brand meaning lives in [Brand Direction](BRAND_DIRECTION.md).

---

## Why Tokens First

HuGuWeb’s primary brand direction is purple, with dark green as a possible later alternative. Components must depend on **roles**, not hex values.

If a button’s background is `color.brand.primary.500`, rebranding is a token change. If it is `#6B3D8F` in twenty files, rebranding is a rewrite.

Dark mode is **not** being designed now, but role-based tokens keep it possible later.

---

## Design Philosophy

- Name tokens by **purpose**, not by appearance (`color.text.primary`, not `color.gray-800` as the public API).
- Separate **brand**, **neutral**, **semantic**, **chart**, and **interaction** colors.
- Keep the set small. Do not pre-create hundreds of unused steps.
- Space, radius, and type scales should be limited and repeating—not one-off magic numbers per screen.
- Semantic operational color must not alias the brand primary.

Illustrative hex values in [Brand Direction](BRAND_DIRECTION.md) are comparison samples. They are not this document’s accepted tokens.

---

## Proposed Token Categories

### Color — brand

```text
color.brand.primary.*
color.brand.on-primary
color.brand.accent.*          # rare; optional later
```

`primary` is the purple family once one is chosen. A future dark-green rebrand replaces this family.

Do not put danger/warning/success under `color.brand`.

### Color — surface

```text
color.surface.canvas          # application background (warm off-white)
color.surface.raised          # panels, cards, dialogs
color.surface.sunken          # wells, inset tables, code-like regions if any
color.surface.overlay         # modal/scrim
color.surface.selected        # selected row / nav (tint, not a purple slab)
```

### Color — text

```text
color.text.primary            # dark warm-gray, not pure black
color.text.secondary
color.text.muted
color.text.inverse            # text on brand/solid surfaces
color.text.link
```

### Color — border

```text
color.border.subtle
color.border.default
color.border.strong
color.border.focus            # typically brand-derived
```

### Color — semantic (operational)

```text
color.semantic.danger.*
color.semantic.warning.*
color.semantic.success.*
color.semantic.info.*
color.semantic.unavailable.*
color.semantic.neutral.*
```

Each semantic intent should include text, surface, and border roles so status chips work on cards and tables without ad-hoc mixing.

### Color — chart

```text
color.chart.1
color.chart.2
color.chart.3
color.chart.4
color.chart.5
color.chart.grid
color.chart.axis
```

Chart colors are a distinct set. Do not reuse brand purple for every series. Do not use the same hue for “occupied” in a chart and “selected” in a table unless that shared meaning is intentional.

### Color — interaction

```text
color.interaction.hover
color.interaction.active
color.interaction.disabled
color.interaction.focus-ring
```

Hover/active/disabled may reference brand or neutrals internally. Consumers should still ask for interaction roles, not raw brand steps.

### Space

```text
space.1
space.2
space.3
space.4
space.5
space.6
space.8
```

Use a small scale. Density modes ([Design Principles](DESIGN_PRINCIPLES.md)) may map to different scale steps later; do not invent three unrelated spacing systems.

### Radius

```text
radius.s
radius.m
radius.l
radius.full                  # avatars, pills — use sparingly
```

Prefer moderate radius. Avoid both sharp enterprise boxes and overly round consumer chips as the default.

### Shadow

```text
shadow.none
shadow.s                      # restrained elevation
shadow.m                      # modal / popover only
```

Default chrome should rely more on border and surface contrast than on shadow.

### Typography

```text
typography.family.ui          # candidate; not selected
typography.family.numeric     # optional; may equal ui
typography.size.*
typography.weight.*
typography.line-height.*
typography.letter-spacing.*
```

Do not embed a specific font family name as an accepted token in this sprint.

### Motion

```text
motion.duration.instant
motion.duration.short
motion.duration.medium
motion.easing.standard
motion.easing.emphasized
```

Durations should stay short. See motion rules in [Design Principles](DESIGN_PRINCIPLES.md).

---

## Naming Conventions

- Use dotted semantic paths in documentation: `color.surface.canvas`.
- When CSS exists later, map to custom properties such as `--color-surface-canvas`.
- Prefer `primary` / `secondary` for **text hierarchy**, not as a second brand color.
- Scale steps (`100`–`700` or `subtle` / `default` / `strong`) are an implementation choice. Do not freeze a 10-step ramp now.
- Do not name tokens after components (`color.sidebar.background`) until a token is proven to be component-specific. Start with global roles; add component tokens only when reuse is real.

---

## What This Sprint Does Not Do

- No `index.css` token file
- No Tailwind / CSS-variable theme package
- No Storybook token set
- No accepted hex palette
- No dark-theme token values
- No mapping of hotel domain statuses onto semantic colors

---

## Related Documents

- [Design Principles](DESIGN_PRINCIPLES.md)
- [Brand Direction](BRAND_DIRECTION.md)
- [Design README](README.md)
