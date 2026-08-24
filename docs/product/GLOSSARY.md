# Glossary

This glossary records terminology used in HuGuWeb documentation. Terms marked as **open** require formal definition before implementation.

Discovery-level definitions added in Sprint 0.2 support product documentation—not database or API design.

---

## HuGuWeb

The current project and product working name. The final commercial brand or domain may be reconsidered in the future. The GitHub repository name remains `hsnucal/HuguWeb`.

---

## PMS (Property Management System)

Industry term for software that manages hotel property operations including reservations, guest management, room inventory, front desk operations, and often housekeeping coordination.

In HuGuWeb context: PMS capabilities are part of the broader hospitality-first platform hypothesis. PMS-centric products typically excel at guest-stay operations; HuGuWeb may extend toward connected back-office workflows. No PMS functionality is implemented yet.

---

## ERP (Enterprise Resource Planning)

Industry term for integrated business management software covering finance, accounting, inventory, purchasing, HR, and other operational domains.

In HuGuWeb context: HuGuWeb is being designed as a hospitality-first ERP / PMS platform—not a generic ERP for all industries. ERP-centric products often excel at finance and enterprise controls; HuGuWeb must evaluate Build vs Integrate rather than assuming full ERP depth early.

---

## Property

**Status:** Open — exact HuGuWeb domain definition pending.

Discovery-level meaning: a single hospitality operation (e.g., one hotel building or resort operating as one logical site).

**Pending decisions:** Relationship to Tenant and Hotel Group; whether one property maps 1:1 to a tenant; multi-building/resort modeling. Do not infer database relationships from this glossary entry.

See also [Future Scope](FUTURE_SCOPE.md).

---

## Hotel Group

**Status:** Open — exact HuGuWeb domain definition pending.

Discovery-level meaning: an organization that owns or manages multiple hospitality properties, potentially with centralized procurement, finance, HR policy, or reporting.

**Pending decisions:** Relationship to Tenant and Property; group-level vs property-level permissions; cross-property analytics scope. Do not infer database relationships from this glossary entry.

---

## Tenant

**Status:** Open — exact HuGuWeb domain definition pending.

Discovery-level meaning: in multi-tenant software, often an isolated customer organization whose data and configuration are separated from others.

In HuGuWeb's hospitality context: **tenant is not automatically equivalent to hotel/property or hotel group.** A tenant might represent a group, a single property, or another isolation boundary—this remains a Product Owner + CTO decision before implementation.

Do not infer database relationships or isolation strategy from this glossary entry.

---

## Guest

Discovery-level meaning: a person (or party) who may book, stay at, or consume services at a property.

**Guest identity** (name, contact on a reservation/stay/folio) is not the same as a **Guest Profiles** capability (searchable history, preferences, repeat-guest records). Identity is implied by stay workflows; profiles remain a discovery question. HuGuWeb domain model for guest vs profile vs account is **not yet defined**.

---

## Reservation

Discovery-level meaning: a booking or hold for guest accommodation (and potentially linked services) for specific dates or conditions. Reservations connect to availability, room assignment, and front office arrival workflows.

---

## Stay

Discovery-level meaning: the actual guest occupation period at the property from arrival to departure, typically linked to a reservation and room assignment. Stay lifecycle drives folio, housekeeping, and checkout workflows.

Exact state model (reserved vs in-house vs checked-out) is **not yet defined**.

---

## Room

Discovery-level meaning: a sellable or assignable accommodation unit (or logical room inventory item) at a property.

Sprint 0.9A **Accepted** that Room is a **property-scoped identity** (`Organization` → `Property` → `Room`), first hosted by **Room Operations**. Reservations / Stay consume `RoomId` later; they do **not** own Room identity.

Cleanliness/readiness, occupancy, technical serviceability, operational block, guest service restrictions, and sellability are **separate dimensions** — not one `RoomStatus` enum mixing Dirty, Clean, Inspected, Ready, Occupied, Vacant, Blocked, OutOfOrder, OutOfService, DND, and NoService.

RoomType is **not** added in Sprint 0.9B unless implementation later proves it unavoidable. Connecting rooms and non-room inventory remain **not yet defined** as aggregates. See [Room Operations](../domain/room-operations/README.md).

---

## Room Operations / Oda Operasyonları

**Status:** Accepted — Sprint 0.9A ([ROOM-OPS-DOMAIN-001](../domain/room-operations/ROOM-OPS-DOMAIN-001.md)).

The hotel-operations domain that hosts minimal Room identity and **Room Readiness**. Kat Hizmetleri is a **primary participant**, not the owner of all room operational state. Ön Büro, Minibar, and Teknik Servis participate through distinct facts. Not a Housekeeping module and not Reservations. Sprint 0.9B implementation is a separate sprint and is not started by this acceptance.

---

## Room Readiness / Oda hazırlık

**Status:** Accepted — Sprint 0.9A.

The Dirty → Clean → Inspected → Ready (`Kirli` → `Temiz` → `Denetimli` → `Hazır`) preparation machine. **Inspected is a real domain state.** This machine is **not** occupancy, **not** Out of Order / Out of Service / Blocked, **not** Minibar, **not** DND / No Service, and **not** the same as sellable.

**Ready ≠ Sellable.** Ready means preparation completed. Sellable is composed later from independent conditions and is not stored as a master status in Sprint 0.9B.

---

## Technical Service / Teknik Servis

**Status:** Accepted — Sprint 0.11A ([MAINT-DOMAIN-001](../domain/maintenance/MAINT-DOMAIN-001.md)). Sprint 0.11B is not started.

Hotel domain that owns **Arıza** (`MaintenanceIssue`) and the facts that make an oda technically unusable. It does **not** own RoomReadiness, Bloke, Stay, or oda değişikliği. Not a CMMS and not a generic task engine. First-slice aggregate is `MaintenanceIssue`; a separate `MaintenanceWorkOrder` is deferred. Product language: Teknik Servis, Arıza, İş Emri (UI label for the same first-slice record is allowed). Technical identifiers stay English.

---

## Room Serviceability / Teknik elverişlilik

**Status:** Accepted — Sprint 0.11A. Consumed conceptually by Accepted Room Operations; not implemented in 0.9B.

Whether the oda is technically usable. **Derived** from open **blocking** technical issues — not RoomReadiness and not a stored master status on `Room`. A room may remain serviceable with a minor open Arıza.

---

## Out of Order / Out of Service (OOO / OOS)

**Status:** Accepted detailed model — Sprint 0.11A. Independence from RoomReadiness is **Accepted** (Sprint 0.9A).

Expert-supplied meaning: **Out of Order** = same-day repair expected; **Out of Service** = not expected the same day. Operator judgment, not a clock. Storage: outage classification on a **blocking** Arıza. House view is a derived label (OOS > OOO > Serviceable). Not Bloke, not Dirty/Ready.

---

## Blocked / Bloke

**Status:** Accepted as Front Office-owned and **not** RoomReadiness (Sprint 0.9A). Not persisted in 0.9B. Not part of Teknik Servis.

Operational lock (for example payment/contact). Can make a Ready room not sellable. Must not be modeled as a technical defect.

---

## Folio

Discovery-level meaning: the running account of guest charges and payments associated with a stay (or non-room guest account).

Folio is **operational guest charging**, not full accounting. These layers are not the same product scope (none decided here):

| Layer | Meaning |
|-------|---------|
| Operational financial events | Charge posted, discount, deposit |
| Folio / guest charges | Running guest account during stay |
| Payment handling | Capture, refund, pre-auth (likely integrate) |
| Finance visibility | What is outstanding / posted today |
| Accounting integration | Export or post to an external ledger |
| Full statutory accounting | GL, AP/AR, tax books — HuGuWeb must **not** automatically become an accounting product |

Folio vs invoice vs accounting posting boundaries are **not yet defined**.

---

## Housekeeping / Kat Hizmetleri

Discovery-level meaning: hotel **department** and procedures for room cleaning, inspection, and related floor work.

In HuGuWeb context: **not** an approved module name. Sprint 0.9A **Accepted** Kat Hizmetleri as a **primary participant** in **Room Operations**, which owns Oda hazırlık (Room Readiness). Current Strong MVP lean remains **room-readiness coordination with front office**, not a full housekeeping platform (linen programs, generic task engines, employee mobile).

See [Room Operations](../domain/room-operations/README.md) and [MVP Candidates](MVP_CANDIDATES.md).

---

## OTA (Online Travel Agency)

Discovery-level meaning: third-party online booking channels (e.g., major travel marketplaces) through which hotels receive reservations. OTAs typically connect via channel managers or PMS integrations rather than manual entry.

HuGuWeb integration strategy: **Integrate Candidate** — see [Build vs Integrate](BUILD_VS_INTEGRATE.md).

---

## Channel Manager

Discovery-level meaning: software that distributes rates and availability to multiple OTAs and online channels and synchronizes reservations back to the property.

HuGuWeb strategy: likely integrate rather than build initially — see [Build vs Integrate](BUILD_VS_INTEGRATE.md). No vendor selected.

---

## POS (Point of Sale)

Discovery-level meaning: system for restaurant, bar, or retail transactions, often requiring integration to guest folio or accounting.

HuGuWeb strategy: **Integrate Candidate** for F&B-heavy properties — see [Build vs Integrate](BUILD_VS_INTEGRATE.md).

---

## Multi-property

Discovery-level meaning: capability to operate or administer more than one property under related ownership or management, potentially including shared configuration, reporting, or permissions.

**Status:** Strategically relevant long-term; **not** MVP scope. Tenant/Property/Hotel Group relationships must be defined before implementation. See [Future Scope](FUTURE_SCOPE.md).

---

## Build vs Integrate

A decision framework for evaluating whether HuGuWeb should implement a capability internally or integrate with an external system.

Principle: build for strategic differentiation; integrate when external maturity or compliance burden favors partners.

See [Product Principles](PRODUCT_PRINCIPLES.md), [Build vs Integrate](BUILD_VS_INTEGRATE.md), and [Future Scope](FUTURE_SCOPE.md).

---

## Hotel Operating System

**Status:** Product hypothesis — not a frozen decision.

A conceptual direction where hotel workflows communicate across operational boundaries (e.g., checkout affecting room status, housekeeping, folio, and invoicing). Not formally defined or implemented.

---

## Modular Monolith

**Status:** Accepted in [ADR-001](../architecture/adr/ADR-001-Architecture-Style.md).

An architectural approach combining modular internal structure with a single deployable application. Final business module boundaries are **not** defined yet.

---

## ADR (Architecture Decision Record)

A documented record of a significant architecture decision including context, alternatives, consequences, and revisit conditions. See [ADR Template](../architecture/adr/ADR-TEMPLATE.md).

---

## MVP (Minimum Viable Product)

The smallest product scope that delivers validated value to target users. HuGuWeb MVP scope is **not yet defined**.

[MVP Candidates](MVP_CANDIDATES.md) lists candidates only—not approved MVP.

---

## Evidence Level (E0–E4)

Classification of how strongly a product claim is supported. See [Evidence Model](EVIDENCE_MODEL.md).

---

## Research Area

A domain under investigation (e.g., PMS, housekeeping, finance). Research areas are **not** approved product scope unless explicitly stated in an approved scope document.

---

## Sprint

A time-boxed development cycle with a defined goal, requirements, implementation, review, and approval steps. See [Development Workflow](../engineering/DEVELOPMENT_WORKFLOW.md).

---

## Employee / Personel

**Status:** Accepted identity in [HR-DOMAIN-001](../domain/hr/HR-DOMAIN-001-Organization-Workforce-Foundation.md). **Not** `ApplicationUser`. **Not** a permission. Technical id is the PK; `PersonnelNumber` (Sicil No) is the business identifier.

---

## Personel Master

**Status:** Accepted — [HR-01A](../domain/hr/personnel-master/README.md).

The HR identity/profile layer that extends Employee (contact, national identity, photo, later payment profile) without collapsing Employment, Assignment, payroll, leave, or official submissions into one Employee record. The Personel Card is a UI composition surface, not one aggregate.

---

## Official Employment Data / Bildirge Kodları

**Status:** Accepted — [HR-03A](../domain/hr/official-employment/README.md).

Statutory classification for an Employment (belge türü, tabi kanun, sigorta kolu, meslek kodu) plus Property-owned SGK workplace registrations (0..*). `OfficialEmploymentProfile` may reference the applicable registration. **Not** SGK/KBS/İŞKUR submission. **Not** Employee master columns.

---

## Related Documents

- [Target Customer](TARGET_CUSTOMER.md)
- [Hotel Problems](HOTEL_PROBLEMS.md)
- [Future Scope](FUTURE_SCOPE.md)
- [Evidence Model](EVIDENCE_MODEL.md)
- [Room Operations](../domain/room-operations/README.md) (Accepted — Sprint 0.9A)
- [Teknik Servis / Maintenance](../domain/maintenance/README.md) (Accepted — Sprint 0.11A; 0.11B not started)
- [Personel Master](../domain/hr/personnel-master/README.md) (Accepted — HR-01A / HR-01B)
- [Official Employment Data](../domain/hr/official-employment/README.md) (Accepted — HR-03A)
