# Login Experience

> **Status:** Sprint 0.4 design freeze — proposed for Product Owner + CTO review. Auth entry experience only. Does not change the Sprint 0.3B login implementation.

This is the source of truth for the **staff sign-in experience**.

Authentication mechanics: [ADR-007](../architecture/adr/ADR-007-Authentication-Strategy.md).  
Onboarding split: [UX Architecture](UX_ARCHITECTURE.md).  
Visual rules: [Design Principles](DESIGN_PRINCIPLES.md) and [Brand Direction](BRAND_DIRECTION.md).

---

## Intent

Login should feel:

- calm
- premium-light
- hospitality-oriented
- fast
- trustworthy

It is a door into hotel operations. It is **not** a consumer landing page, a marketing site, or a product tour.

After a successful sign-in, staff land in the [Operations Center](OPERATIONS_CENTER.md) (or a permission-appropriate first screen). They do not enter a company-creation wizard.

---

## Include

- HuGu brand area (HG monogram + HuGu wordmark)
- email
- password
- primary Sign in action
- inline, human-readable error if sign-in fails
- submitting / disabled state on the button so double-submit is hard
- **Forgot password** as a **placeholder only** if it can remain visually quiet (Identity supports reset later; do not implement the flow in this design sprint)

---

## Do Not Include

- registration / “create your hotel”
- marketing carousel
- dashboard screenshots
- testimonials
- social login
- excessive animation
- onboarding wizard
- language-marketing headlines
- live occupancy widgets
- remember-me as a security-sensitive default (cookie session already persists per ADR-007; do not add a second session story here)

---

## Layout Direction

Centered, uncluttered, warm canvas. One column. No split-screen photo of a lobby.

Low-fidelity wireframe:

```text
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│                      HuGu                                   │
│              Hotel operations                               │
│                                                             │
│              ┌───────────────────────────┐                  │
│              │ Email                     │                  │
│              │ [                       ] │                  │
│              │                           │                  │
│              │ Password                  │                  │
│              │ [                       ] │                  │
│              │                           │                  │
│              │      [ Sign in ]          │                  │
│              │                           │                  │
│              │ Forgot password?          │  ← placeholder   │
│              └───────────────────────────┘                  │
│                                                             │
│         Sign-in failed. Check your details and try again.   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

Supporting line under the wordmark should stay generic and operational (“Hotel operations” is an example). It must not promise modules that are not in scope.

---

## Behavior

| State | Treatment |
|-------|-----------|
| Checking existing session | Short contextual message, not a branded spinner show |
| Already authenticated | Go to Home; do not show login |
| Validation | Browser/native required fields are acceptable; labels stay visible |
| Failure | Explain that sign-in failed; do not reveal whether the email exists |
| Success | Enter the authenticated shell |
| Forgot password | Placeholder control only; if shown before the flow exists, it should not pretend to send mail |

Staff access assumes an already configured hotel. See employee vs administrator onboarding in [UX Architecture](UX_ARCHITECTURE.md).

---

## Visual Notes (Not Implementation)

- Warm off-white canvas, raised sign-in surface, restrained border
- Primary button uses brand color; it is the only large purple element
- No full-bleed purple panel
- No decorative illustration
- Turkish and English labels must fit without clipping

The current bootstrap login is a functional placeholder. Sprint 0.5 should restyle it; it should not replace the cookie session flow. See [Design README](README.md#current-bootstrap-ui).

---

## Related Documents

- [UX Architecture](UX_ARCHITECTURE.md)
- [Brand Direction](BRAND_DIRECTION.md)
- [ADR-007 Authentication Strategy](../architecture/adr/ADR-007-Authentication-Strategy.md)
