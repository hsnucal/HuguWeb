# Market Research

> **Status:** Research in progress — not a product scope document.

This document holds market research context for HuGuWeb. Detailed findings are ongoing and will be added with evidence and sources as research progresses.

---

## Target Market

HuGuWeb's first target industry is **hotels and hospitality**.

HuGuWeb is being designed as a hospitality-first ERP / PMS platform—not a generic ERP for every industry. Future expansion into other industries may be considered later, but early product decisions must reflect real hotel operational needs.

---

## Market Context

### Hospitality software landscape

Hotels typically rely on a combination of:

- Property Management Systems (PMS)
- Channel managers and OTA integrations
- Point of Sale (POS) systems
- Financial and accounting software
- Housekeeping and maintenance tools
- HR and payroll systems
- Revenue management systems

This fragmentation creates operational friction. HuGuWeb's product vision includes reducing fragmented software usage where practical, while integrating with mature external systems where building is not strategic.

### Strategic positioning hypothesis

HuGuWeb may differentiate by:

- Solving real hotel operational problems rather than maximizing feature count
- Workflow-oriented UX ("Don't show modules. Show work.")
- Connected hotel workflows across operational boundaries (Hotel Operating System hypothesis)
- Integration-friendly architecture with clear Build vs Integrate decisions

These are hypotheses requiring validation through product discovery and pilot hotel feedback—not confirmed positioning.

---

## Research Areas Under Investigation

The following domains are being researched. They are **research areas**, not approved MVP modules:

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

---

## Multi-Property and Hotel Chains

Hotel chains and multi-property management are strategically relevant to HuGuWeb's long-term direction.

Future architecture decisions should evaluate multi-property requirements early enough to avoid expensive redesign. No multi-property functionality is designed or implemented during the foundation stage.

Concepts requiring formal definition before implementation: **Tenant**, **Hotel / Property**, **Hotel Group**. These are not automatically equivalent.

---

## Open Research Questions

The following require Product Owner and CTO attention as research progresses:

- What are the highest-pain operational problems for target hotel segments?
- Which capabilities are best built vs integrated?
- What is the minimum viable scope for pilot hotel validation?
- How do hotel chains vs independent properties differ in requirements?
- What regulatory and compliance requirements apply in target markets (e.g., e-Invoice, government reporting)?

---

## Related Documents

- [Competitor Analysis](COMPETITOR_ANALYSIS.md)
- [Product Vision](../product/PRODUCT_VISION.md)
- [Product Principles](../product/PRODUCT_PRINCIPLES.md)
- [Future Scope](../product/FUTURE_SCOPE.md)
- [Roadmap](../roadmap/ROADMAP.md)
