# Brand Direction

> **Status:** Sprint 0.4 design freeze — proposed for Product Owner + CTO review. Personality and logo *direction* only. No final logo artwork. No exact palette freeze.

This is the source of truth for **brand personality, primary color direction, and logo direction**.

Visual application rules live in [Design Principles](DESIGN_PRINCIPLES.md). Token structure lives in [Design Tokens](DESIGN_TOKENS.md).

---

## Brand Personality

HuGuWeb should feel:

- warm
- hospitality-oriented
- modern
- calm
- visually pleasant during long sessions
- premium without appearing luxury-only
- professional without feeling cold or bureaucratic

It should feel like a tool a hotel team can trust at 07:00 during arrivals—not like a bank back-office, and not like a travel consumer app.

### Avoid

- generic SaaS / admin-template branding
- sterile enterprise chrome
- luxury-only hotel clichés (gold leaf, marble, serif extravagance)
- playful consumer illustration
- mascots, beds, keys, buildings, or roof/house marks as the identity

---

## Primary Color Direction

**Accepted direction:** purple.

Dark green remains a **possible future brand alternative**, not a current dual-brand system.

Do **not** hard-code product design around a single hex value. Brand color must be a token (`color.brand.primary.*`) so purple can later become dark green without rewriting components.

Purple is for identity and interaction. It is not a status system. See [Design Principles](DESIGN_PRINCIPLES.md).

---

## Candidate Purple Families

The following families are **candidates for later visual comparison only**. None is accepted. Sample hex values are illustrative, not tokens.

### Family A — Warm Plum (hospitality-leaning)

Warmer, slightly wine-adjacent purple. May feel more hospitality than “developer tool.”

| Sample role | Illustrative hex (not accepted) |
|-------------|----------------------------------|
| Primary | `#6B3D8F` |
| Hover / emphasis | `#5A3278` |
| Soft accent surface | `#F4EEF8` |

Risk: can look dated or cosmetic if overused on large surfaces.

### Family B — Cool Violet (modern software)

Slightly cooler, closer to indigo-violet. May feel more contemporary and precise.

| Sample role | Illustrative hex (not accepted) |
|-------------|----------------------------------|
| Primary | `#5B45C0` |
| Hover / emphasis | `#4A37A8` |
| Soft accent surface | `#F1EFFB` |

Risk: can feel like generic SaaS purple if saturation is too high.

### Family C — Muted Amethyst (low fatigue)

Lower saturation. Likely the calmest for long sessions.

| Sample role | Illustrative hex (not accepted) |
|-------------|----------------------------------|
| Primary | `#6A5A8A` |
| Hover / emphasis | `#574A73` |
| Soft accent surface | `#F3F1F6` |

Risk: may feel under-branded if accents are too timid.

**Comparison work stays in review.** Do not pick a family in implementation until Product Owner + CTO choose one (or a refined mix).

---

## Future Dark Green Alternative

If brand direction later moves to dark green, only `color.brand.*` tokens should need to change. Semantic colors (danger, warning, success, and similar) must not be stored as “the brand color.” Surfaces and text must not be derived only from purple.

No dark-green palette is specified in this sprint.

---

## Logo Direction

**Do not create final logo artwork in this repository.** No SVG, PNG, or favicon design is authorized here.

Desired direction:

- minimal
- memorable
- **HG monogram** as a compact-mark candidate
- **HuGuWeb wordmark** for expanded sidebar and login
- works in the sidebar (expanded wordmark; collapsed compact mark later)
- works on the login screen
- works at small sizes
- purple-first
- potentially adaptable to dark green later (mark should not depend on purple-specific illustration)

### Avoid in the mark

- detailed hotel icons
- buildings
- beds
- keys
- generic roof / house symbols
- photographic or illustrated scenes
- gradients inside the letterforms

Final logo design will be reviewed separately.

### Usage hypothesis (not production assets)

| Context | Treatment |
|---------|-----------|
| Login | Wordmark + short calm supporting line |
| Sidebar expanded | Wordmark |
| Sidebar collapsed | Compact HG mark (later) |
| Browser tab | Compact mark (later; not designed now) |

---

## Voice (Light Touch)

Brand voice in the UI should be:

- clear
- operational
- respectful of staff time

Prefer “Room 214 is not ready for a 15:00 arrival” over “Oops! Something went wrong with your dashboard widgets.”

Marketing copy, slogans, and testimonials do not belong in the product shell or login. See [Login Experience](LOGIN_EXPERIENCE.md).

---

## Related Documents

- [Design Principles](DESIGN_PRINCIPLES.md)
- [Design Tokens](DESIGN_TOKENS.md)
- [Login Experience](LOGIN_EXPERIENCE.md)
- [Product Vision](../product/PRODUCT_VISION.md)
