# Glossary

This glossary records terminology used in HuGuWeb documentation. Terms marked as **open** require formal definition before implementation.

---

## HuGuWeb

The current project and product working name. The final commercial brand or domain may be reconsidered in the future. The GitHub repository name remains `hsnucal/HuguWeb`.

---

## PMS (Property Management System)

Industry term for software that manages hotel property operations including reservations, guest management, room inventory, and front desk operations. HuGuWeb is being designed with PMS capabilities as part of a broader hospitality-first platform. No PMS functionality is implemented yet.

---

## ERP (Enterprise Resource Planning)

Industry term for integrated business management software covering finance, inventory, purchasing, HR, and other operational domains. HuGuWeb is being designed as a hospitality-first ERP / PMS platform—not a generic ERP for all industries.

---

## Hotel Operating System

**Status:** Product hypothesis — not a frozen decision.

A conceptual direction where hotel workflows communicate across operational boundaries (e.g., checkout affecting room status, housekeeping, folio, and invoicing). Not formally defined or implemented.

---

## Build vs Integrate

A decision framework for evaluating whether HuGuWeb should implement a capability internally or integrate with an external system. See [Product Principles](PRODUCT_PRINCIPLES.md) and [Future Scope](FUTURE_SCOPE.md).

---

## Tenant

**Status:** Open — not yet formally defined.

In software architecture, "tenant" often refers to an isolated customer or organization within a multi-tenant system. In HuGuWeb's hospitality context, **tenant is not automatically equivalent to hotel/property or hotel group**. Formal definition is required before implementation.

---

## Hotel / Property

**Status:** Open — not yet formally defined.

Refers to a single hospitality operation (e.g., one hotel). Must be formally defined in relation to tenant and hotel group concepts before multi-property architecture is implemented.

---

## Hotel Group

**Status:** Open — not yet formally defined.

Refers to an organization managing multiple hotels or properties. Must be formally defined in relation to tenant and hotel/property concepts before multi-property architecture is implemented.

---

## Modular Monolith

**Status:** Architectural candidate — not an approved decision.

An architectural approach combining modular internal structure with a single deployable application. Currently considered a strong candidate for HuGuWeb but not yet approved via ADR.

---

## ADR (Architecture Decision Record)

A documented record of a significant architecture decision including context, alternatives, consequences, and revisit conditions. See [ADR Template](../architecture/adr/ADR-TEMPLATE.md).

---

## MVP (Minimum Viable Product)

The smallest product scope that delivers validated value to target users. HuGuWeb MVP scope is **not yet defined**.

---

## Research Area

A domain under investigation (e.g., PMS, housekeeping, finance). Research areas are **not** approved product scope unless explicitly stated in an approved scope document.

---

## Sprint

A time-boxed development cycle with a defined goal, requirements, implementation, review, and approval steps. See [Development Workflow](../engineering/DEVELOPMENT_WORKFLOW.md).
