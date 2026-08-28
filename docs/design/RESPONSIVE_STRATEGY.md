# Responsive Strategy

> **Status:** Sprint 0.4 design freeze — proposed for Product Owner + CTO review. Device behavior for the web ERP. Not a mobile technology choice. Not a substitute for the future employee app.

This is the source of truth for **viewport and product-surface behavior**.

Shell: [UX Architecture](UX_ARCHITECTURE.md).  
Future mobile capability list (not MVP): [Future Scope](../product/FUTURE_SCOPE.md).

---

## Freeze

### Main web ERP

- **desktop-first**
- **tablet-capable**

Do **not** design the desktop ERP as a mobile-responsive substitute for the future employee mobile product.

The future employee mobile experience is a **distinct product experience**.

---

## Web ERP Support

| Surface | Expectation |
|---------|-------------|
| Full desktop | Primary design target. Sidebar + work area. Dense tables and Operations Center. |
| Tablet landscape | Reasonable use: readable shell, usable tables, forms that do not require hover-only actions. |
| Tablet portrait | Degraded but usable for light tasks; density may shift toward Comfortable. |
| Small phone | May be limited. Do not spend Sprint 0.5 making the ERP a phone app. |

Recommended **minimum viewport assumptions** (not final device requirements):

| Class | Assumption |
|-------|------------|
| Desktop | ~1280px and above as the layout the product is designed in |
| Tablet landscape | ~1024px wide as a capability target |
| Below ~768px | Limited; do not promise full ERP workflows |

These numbers are planning assumptions for chrome (sidebar collapse, table overflow). They are not a procurement list of supported devices.

---

## Sidebar and Top Bar on Smaller Web Viewports

Accepted sidebar chrome:

- Desktop: expanded by default; user-controlled collapse via the HG/HuGu brand area; collapsed preference persisted locally; no hover expansion; the work area uses released space
- Narrow viewports (≤768px): labeled drawer, not the desktop collapsed icon rail; the desktop collapse preference does not force the drawer into collapsed mode
- Do not convert the ERP into a bottom-tab mobile app on the web

Tables may scroll horizontally rather than stacking every column into cards. Card-stacking every ERP table is usually worse for scanning.

---

## Mobile Product Distinction

Future HuGuWeb mobile usage may include **two different product scopes**. They may eventually live in one physical app, but they are not the same product.

### Operations mobile

Possible (future, not scope):

- housekeeping tasks
- supervisor inspection
- maintenance tasks
- minibar tasks
- operational notifications

This is floor work: short sessions, task-first, often one-handed.

### Employee self-service

Possible (future, not scope; aligns with [Future Scope](../product/FUTURE_SCOPE.md)):

- leave
- payslips
- training
- shuttle
- accommodation
- overtime

This is personal HR/services work, not room operations.

### Explicitly not decided

- Mobile technology / framework
- Whether operations and self-service share one app binary
- Whether any of the above is MVP ([MVP Candidates](../product/MVP_CANDIDATES.md) already marks employee mobile as not MVP)

Do not squeeze these flows into a responsive web sidebar.

---

## Related Documents

- [UX Architecture](UX_ARCHITECTURE.md)
- [Design Principles](DESIGN_PRINCIPLES.md)
- [Future Scope](../product/FUTURE_SCOPE.md)
- [ADR-003 Frontend Architecture](../architecture/adr/ADR-003-Frontend-Architecture.md)
