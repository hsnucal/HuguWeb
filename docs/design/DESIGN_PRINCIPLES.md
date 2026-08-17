# Design Principles

> **Status:** Sprint 0.4 design freeze — proposed for Product Owner + CTO review. Visual and interaction principles only. Not implementation authorization. Not a component library. Not MVP feature scope.

This is the source of truth for **visual and interaction principles**.

Related sources of truth:

| Topic | Document |
|-------|----------|
| Brand personality, logo direction, color families | [Brand Direction](BRAND_DIRECTION.md) |
| Token categories and naming | [Design Tokens](DESIGN_TOKENS.md) |
| Navigation, shell, roles, onboarding | [UX Architecture](UX_ARCHITECTURE.md) |
| Home / Operations Center | [Operations Center](OPERATIONS_CENTER.md) |
| Login | [Login Experience](LOGIN_EXPERIENCE.md) |
| Viewport and mobile product split | [Responsive Strategy](RESPONSIVE_STRATEGY.md) |

---

## Purpose

HuGuWeb is a hospitality-first ERP / PMS. Staff and managers may work in it for many hours per day. The interface must feel like a calm operations tool for hotels—not a generic admin template, not a consumer app, and not a decorative dashboard.

Core product principle (from [Product Principles](../product/PRODUCT_PRINCIPLES.md)):

> Don't show modules. Show work.

That principle is applied in [UX Architecture](UX_ARCHITECTURE.md). This document defines how the product should *look and behave* while people do that work.

---

## Visual Personality (Application)

Brand personality is defined in [Brand Direction](BRAND_DIRECTION.md). Applied to the UI, HuGuWeb should feel:

- warm and hospitality-oriented
- modern and calm
- visually pleasant during long sessions
- premium without appearing luxury-only
- professional without feeling cold or bureaucratic

Avoid:

- generic admin-template appearance
- sterile enterprise UI
- visually noisy dashboards
- overly playful consumer-app styling
- excessive gradients
- excessive shadows
- decorative animation
- excessive use of brand color

Purple is an accent and an interaction color. It is not a wallpaper.

---

## Color Philosophy

Primary brand direction is **purple**. Exact palette is **not frozen**. Dark green remains a possible future brand alternative. Tokenize brand color so a later change does not require rewriting components. See [Design Tokens](DESIGN_TOKENS.md) and [Brand Direction](BRAND_DIRECTION.md).

### Where purple belongs

Use primary brand color for:

- primary actions
- active navigation
- focus states
- selected states
- key brand accents (wordmark, selected indicator, important CTA)

### Where purple does not belong

Do **not** use purple as a universal status color.

Do **not** fill large surfaces, page backgrounds, or entire cards with saturated purple.

Do **not** tint every chart, badge, and table row with brand color.

### Semantic operational colors

Status and operational meaning must remain distinct from brand color:

| Intent | Role |
|--------|------|
| Danger / critical | Blocking, unsafe, or guest-impacting failure |
| Warning | Time pressure, delay, or risk that needs attention |
| Success | Completed, ready, or healthy |
| Informational | Neutral notice, context, or guidance |
| Unavailable / blocked | Out of service, permission denied, or not actionable |
| Neutral | Default operational state without urgency |

These are **intent categories**, not a frozen palette and not a mapping of Room or Housekeeping domain statuses.

### Status must never rely on color alone

Always combine color with at least one other cue:

- text label
- icon where useful
- position
- shape

Color-only status is inaccessible and operationally unsafe in a hotel.

---

## Surface and Eye Comfort

Design for low visual fatigue.

Prefer:

- warm off-white application backgrounds
- white or slightly warm surfaces (panels, cards, tables)
- dark warm-gray text instead of pure black
- subtle borders
- restrained shadows
- moderate corner radius
- calm spacing
- readable typography
- controlled information density

Avoid:

- pure white everywhere
- pure black text everywhere
- high-saturation large surfaces
- large blocks of purple
- excessive card borders
- excessive visual separators

Cards are for grouping related work, not for decorating every number. Many ERP values belong in tables, lists, or compact snapshots—not oversized metric tiles.

---

## Typography Direction

Typography principles only. **No font family is accepted.** Do not package font files or add font dependencies in application code.

The type system must support:

- dense ERP information
- long work sessions
- numeric scanning
- tables
- forms
- dashboards
- Turkish characters (including ğ, ü, ş, ı, ö, ç and capitals)
- English localization later

### Principles

- Prefer a highly readable sans-serif for UI chrome, forms, and tables.
- Use a dedicated tabular/numeric treatment for counts, times, room numbers, and money (tabular figures where the chosen family supports them).
- Keep a small type scale. Do not invent a marketing-site heading ladder for the ERP.
- Line length and line-height should favor scanning over editorial reading.
- Body text should be dark warm-gray, not pure black.

### Candidate families (not accepted)

Open, practical candidates for later visual comparison:

| Candidate | Why it may fit |
|-----------|----------------|
| Inter | Highly readable, modern, strong Turkish/Latin coverage, common in products |
| IBM Plex Sans | Neutral-professional, good numerals, suitable for dense data |
| Source Sans 3 | Neutral-warm, open, proven in long-form UI and forms |

Do not select among these in this sprint. Pairing a UI sans with a numeric-friendly style is a later decision.

---

## Density Strategy

HuGuWeb is an ERP. It must support significant information density. Do not turn every value into a huge card.

| Density | Intent |
|---------|--------|
| **Comfortable** | More spacing and larger hit targets. Useful for training, occasional users, or tablet. |
| **Standard** | Daily operations default. Readable without wasting screen. |
| **Dense** | Power-user tables and boards. More rows visible; still must remain scannable, not cramped. |

**Recommended default (candidate):** Standard.

The product should allow users to scan large amounts of information without feeling crowded. Density is a presentation concern. It is not permission to hide required operational information.

Exact density values (padding, row height) are not frozen.

---

## Tables

Do **not** select a grid library yet ([ADR-003](../architecture/adr/ADR-003-Frontend-Architecture.md) already warns against choosing a heavy grid too early).

Future tables should support:

- strong scanning (alignment, tabular numbers, restrained row height)
- sticky header where the dataset is long
- clear row selection
- sorting
- filtering
- status labels (see Status Design)
- row actions without clutter
- keyboard-friendly behavior later

Avoid:

- huge action-button clusters per row
- unnecessary vertical padding
- excessive grid borders

Prefer a primary row action plus an overflow for secondary actions. Destructive actions must be labeled and confirmed, not hidden behind an unlabeled icon-only control.

---

## Forms

Prefer:

- visible labels (placeholder is not a label)
- sensible grouping of related fields
- inline validation at the field or group that failed
- obvious required vs optional states
- safe destructive actions (explicit confirmation, reversible where practical)
- minimal modal usage

Avoid giant forms where a workflow can be split naturally (for example: identify guest → assign room → confirm charges).

Modals are appropriate for short, focused confirmations or small captures. They are not the default container for multi-step hotel workflows.

Do not create final field components in this sprint.

---

## Status Design

HuGuWeb will have many operational statuses. Status language must be consistent across cards, tables, room boards, and task lists.

A status presentation should include:

- a concise label
- semantic color from the intent categories above
- an icon where it improves scanning
- wording staff can say out loud (“not ready”, “waiting inspection”) rather than internal codes

Do **not** map actual Room or Housekeeping domain statuses into a final list here. Domain status models are product/scope decisions, not visual decisions.

Status components must remain token-driven so brand purple never becomes “the red of this product.”

---

## Data Visualization

> No chart without a decision purpose.

Charts exist only when they improve a decision. Prefer a direct number when it communicates faster.

**Good examples (illustrative, not scope):**

- room preparation time trend (are we slowing down before tonight’s arrivals?)
- unresolved issue aging (what has been open too long?)
- department workload trend (where is the bottleneck?)
- occupancy trend later, where it changes staffing or sales action

**Poor examples:**

- pie chart for every status count
- decorative donuts
- random revenue graph on every user’s Home

Do not put finance charts on the Operations Center merely to occupy space. See [Operations Center](OPERATIONS_CENTER.md).

Chart library is **not selected**.

---

## Accessibility Baseline

Pragmatic, not a compliance bureaucracy.

At minimum:

- keyboard navigation awareness for primary flows
- visible focus states (brand-tinted, not a vague browser default that disappears)
- sufficient contrast on text, borders, and status labels
- status not color-only
- accessible names for inputs, buttons, and icon-only controls
- meaningful empty, error, and loading states
- touch-target awareness for tablet

Do not claim WCAG certification in this sprint. Do not postpone basic focus, labels, and contrast until a later “accessibility project.”

---

## Motion

Animations should be subtle, short, and functional.

Allowed purposes:

- orientation (where did this panel come from?)
- state transition (selected, expanded, completed)
- feedback (saved, failed, in progress)

Avoid:

- decorative page transitions
- bouncing cards
- motion that slows task completion
- large brand-color animations

Respect reduced-motion preferences when implementation begins.

---

## Empty, Loading, and Error States

### Empty

Explain what the user can do next. “No arrivals today” is useful. “No data” is not.

### Loading

Avoid excessive full-page spinners. Use contextual loading (the region that is waiting). Prefer skeleton or inline progress only where it preserves layout enough to prevent jumpiness.

### Error

Explain:

- what failed
- whether the user needs to act
- the next safe action

Do not expose stack traces, SQL, or framework exceptions. Technical correlation identifiers may appear for support, not as the primary message. This matches the API Problem Details direction in architecture docs.

---

## Dark Mode

**Do not implement dark mode now.**

Do not create a dark theme specification in this sprint.

Design tokens should still separate color *roles* (surface, text, brand, semantic) so a later dark theme is possible without rewriting components. See [Design Tokens](DESIGN_TOKENS.md).

---

## Related Documents

- [Design README](README.md)
- [Brand Direction](BRAND_DIRECTION.md)
- [Design Tokens](DESIGN_TOKENS.md)
- [UX Architecture](UX_ARCHITECTURE.md)
- [Product Principles](../product/PRODUCT_PRINCIPLES.md)
- [ADR-003 Frontend Architecture](../architecture/adr/ADR-003-Frontend-Architecture.md)
