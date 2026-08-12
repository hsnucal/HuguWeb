# Product Vision

> **Status:** Product discovery — not a frozen product decision.

---

## What HuGuWeb Is

HuGuWeb is being designed as a **hospitality-first ERP / PMS platform** for hotels and hospitality operations.

The first target industry is **hotels**. HuGuWeb is not currently intended to be a generic ERP for every industry. Future expansion into other industries (such as manufacturing) may be considered later, but the initial product must be designed around real hotel problems and workflows.

Product discovery and pilot preparation initially center on **independent mid-size hotels with meaningful cross-department operational complexity**. This is an approved discovery direction, not a claim that this is the only future market, and not customer-validated market truth. Detailed segment reasoning: [Target Customer](TARGET_CUSTOMER.md).

---

## Vision Themes

### Hospitality-first

Hotel operational workflows drive early product decisions. Generic ERP patterns are secondary to solving problems hotel staff and managers actually face.

### Operational simplicity

Complexity must provide measurable value. HuGuWeb should reduce operational friction rather than replicate the complexity of legacy systems.

### Connected hotel workflows

HuGuWeb may evolve toward a **Hotel Operating System**—a hypothesis, not a frozen decision—where hotel workflows can communicate across operational boundaries.

For example, guest checkout may eventually trigger or affect room status, housekeeping workflow, folio, invoicing, and inventory-related operations. These workflows are **not** defined or implemented yet. They represent product discovery context only.

### Reduce fragmented software usage

Where practical, HuGuWeb should help hotels reduce reliance on disconnected tools for core operational tasks. Integration with mature external systems remains an explicit design consideration (see Build vs Integrate).

### Modern UX

HuGuWeb should investigate workflow-oriented user experiences rather than exposing internal module structure directly to users.

> **Don't show modules. Show work.**

Instead of requiring a user to think in terms of a "Housekeeping Module," the product may eventually answer: *Which rooms need attention?*

Instead of a "Business Intelligence Module," management may see: *What requires my attention today?*

This is a product/UX principle. It is not permission to design UI during the foundation stage.

### Web-first

HuGuWeb will be designed as a web-first platform. A future employee-focused mobile application is documented under [Future Scope](FUTURE_SCOPE.md) but is not current MVP scope.

### Integration-friendly

HuGuWeb should not automatically build every capability internally. Product and architecture decisions must evaluate **Build vs Integrate** for capabilities such as channel managers, OTA integrations, payment providers, POS, e-Invoice/e-Archive, government reporting, payroll systems, and revenue management systems.

External systems should eventually be isolated from core business logic through appropriate integration boundaries.

### Hotel operations before generic ERP expansion

HuGuWeb should solve important hotel operational problems better—not attempt to match the largest possible feature list of existing ERP and PMS platforms.

The desired future reaction from hotel users is closer to:

> "This solves the problems we actually deal with."

—not:

> "HuGuWeb has more features."

---

## What Is Not Defined Yet

The following are intentionally **not** defined in this document:

- Final MVP scope
- Approved product modules
- Specific hotel workflows
- UI/UX designs
- Pricing or commercial model
- Technology stack

The initial discovery / pilot segment **is** recorded in [Target Customer](TARGET_CUSTOMER.md). Selecting that segment does not freeze MVP scope.

See [Roadmap](../roadmap/ROADMAP.md) for high-level project phases.

---

## Research Areas (Not Approved Scope)

The following domains are currently being **researched**. They are research inputs, not committed product scope:

- PMS
- Reservations
- Front Office
- Guests
- Rooms
- Housekeeping
- Inventory
- Purchasing
- Finance
- Accounting
- Human Resources
- Maintenance
- Reporting
- Integrations

Do not treat these as approved MVP modules unless explicitly stated in a future approved scope document.
