# ADR-008: Authorization Strategy

## Status

Accepted

Accepted by Product Owner and CTO on 2026-08-17 (Sprint 0.3A Architecture Freeze).

---

## Context

Hotel operations involve distinct duties (front office, housekeeping, supervisors, technical service, finance, HR, and others). The same person may wear multiple hats, especially in independent hotels. Static role names alone tend to rot (“HousekeepingPlus”, “FrontOfficeWeekend”).

Authentication (ADR-007) answers *who*. Authorization answers *what they can do*, including future property-scoped access.

This ADR does **not** create a permission matrix, roles, or implementation. MVP modules are not frozen; named departments below are **illustrative**, not an approved role list.

---

## Problem

What authorization model should HuGuWeb start with so that:

- first screens can enforce access without a giant IAM design
- duties can grow beyond a handful of static roles
- a bug or permission change in Housekeeping does not rewrite Finance
- multi-property checks can be added later without replacing the model

---

## Decision

We will use **permission-based authorization using ASP.NET Core policy-based mechanisms**.

Roles may act as **permission bundles**. Application and domain check permissions, never hard-coded role or department names.

Lean initial model:

| Layer | Meaning |
|-------|---------|
| **Permission** | A stable, checkable action such as `rooms.status.update` (examples only — not a matrix). Application and domain check permissions, never hard-coded role names. |
| **Role** | A named bundle of permissions for operational convenience (e.g. a “Housekeeping” bundle). Roles are assignment tools, not the unit of enforcement. |
| **Policy** | ASP.NET Core policies map HTTP/API endpoints to required permissions. |
| **User assignment** | A user has one or more roles (and later, assignments may be property-scoped). |

**Do not** implement only static role checks (`if (user.IsInRole("Finance"))`) as the long-term pattern.

**Do not** hard-code department names into domain logic.

**Do not** create the permission matrix in this ADR. MVP modules are not frozen; named departments below remain **illustrative**.

**Do not** build a full ABAC/ReBAC engine, workflow engine, or permission admin UI beyond what the first screens need.

---

## Alternatives Considered

| Alternative | Outcome | Reason |
|-------------|---------|--------|
| Static RBAC only (role name checks) | Rejected as the enforcement model | Simple at first; explodes as duties overlap. Couples code to org chart labels. |
| Permission-based auth via roles + policies | Accepted | Small to start; code depends on permissions; roles remain editable data. |
| Policy-based only with ad-hoc handlers | Incomplete | Policies are the *mechanism*. Without a permission catalog they become one-off code. |
| Full ABAC (attributes, environment, relationships) | Rejected for bootstrap | Needed perhaps for complex multi-property/group rules later. Not justified now. |
| External authorization service (OPA, etc.) | Rejected | Extra infrastructure for a single app. |

### Likely future needs (analysis only)

Illustrative duties — **not** a matrix to implement:

- Front Office, Housekeeping, Supervisor, Order Taker, Technical Service, Minibar, Management, HR, Finance

These suggest:

- overlapping roles for small properties
- finer permissions than department names (`folio.discount.apply` vs `folio.view`)
- later: permission + **property** scope (same user, different properties)

A pure static role model cannot absorb that without code changes. Permission checks can.

---

## Consequences

### Positive

- Application code stays stable when a hotel renames or splits duties.
- Architecture isolation: each module declares the permissions it requires.
- Policies keep API enforcement consistent.
- Property scope can wrap the same permission check later (`permission` + `propertyId`) without replacing RBAC.

### Negative

- Requires a small permission catalog discipline once the first features exist.
- Over-granular permissions can become noise; start coarse, split when a real denial case appears.

---

## Risks

| Risk | Mitigation |
|------|------------|
| Permission explosion | Add a permission only when a screen or command needs a distinct deny case. |
| Role checks sneaking into domain | Code review + optional architecture tests forbidding `IsInRole` in modules. |
| Confusing authN and authZ | Identity authenticates; permission service authorizes. |
| Premature property matrix | First pilots are single-property. Design permission APIs so a property id *can* be passed later; do not build multi-property admin now. |

---

## Revisit Conditions

- Hotel groups require central vs property-level permission inheritance.
- Evidence that relationship-based rules (e.g. “only the assigned attendant”) dominate simple permissions.
- A customer requires an external policy engine.

---

## Date

2026-08-17

---

## Related Documents

- [ADR-001 Architecture Style](ADR-001-Architecture-Style.md)
- [ADR-007 Authentication Strategy](ADR-007-Authentication-Strategy.md)
- [Hotel Problems](../../product/HOTEL_PROBLEMS.md)
- [Glossary](../../product/GLOSSARY.md)
