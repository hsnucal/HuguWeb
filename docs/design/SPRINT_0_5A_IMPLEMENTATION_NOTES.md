# Sprint 0.5A implementation notes

> Product UI implementation notes. This does **not** rewrite Sprint 0.4 design direction. Differences below are explicit Sprint 0.5A / CTO implementation choices.

## Login layout

[Login Experience](LOGIN_EXPERIENCE.md) proposed a centered one-column sign-in on a warm canvas, and advised against a split-screen lobby photo.

Sprint 0.5A authorized a **split desktop layout**: brand/atmosphere area (~55–60%) and authentication card (~40–45%), with CSS geometry rather than photography. Narrow viewports stack the brand statement above the card.

The centered one-column hypothesis remains the Sprint 0.4 design record. The implemented login follows the Sprint 0.5A product-UI brief.

Forgot password is **omitted**. Identity reset is not implemented; a control would have been non-functional.

## Brand and type

Sprint 0.4 left purple family and typeface as candidates. Sprint 0.5A authorized:

- **Muted Amethyst** as the initial brand token values
- **Inter** as the initial UI family

Hex values live only in `src/frontend/web/src/styles/tokens.css` (`--color-brand-*`). Components consume semantic tokens so a later dark-green brand change stays localized.

Inter is named first in the font stack. Font files are not in the repository, and no font CDN is loaded (availability and privacy). If Inter is not installed locally, `Segoe UI` / `system-ui` is used.

## Shell omissions

Search, notifications, and property context are omitted. They are not implemented, and placeholder controls would imply fake product behavior.

Rooms, Reservations, Tasks, and Settings appear in the sidebar as **non-interactive later items**. They are not routes.

Today summary tiles are not links. There is no destination workflow yet.

## Prototype data

Operations Center numbers and room examples come from `operationsCenterPrototype.ts`. They are visual fixtures, not domain entities, APIs, or housekeeping status models.
