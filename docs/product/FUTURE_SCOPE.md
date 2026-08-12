# Future Scope

> **Important:** Everything in this document is **future product context**, not current MVP scope. Do not implement, design, or select technology for these items during the foundation stage.

---

## Employee Mobile Application

HuGuWeb may later include an employee-focused mobile application.

Potential future capabilities include:

- Leave requests
- Training
- Overtime information
- Overtime requests
- Payslip viewing
- Payslip requests
- Employee shuttle routes
- Employment start date
- Total working days
- Live GPS tracking of employee shuttle vehicles
- Staff accommodation availability
- Staff accommodation requests

Managers would manage and respond to relevant requests through the HuGuWeb web application.

### Explicitly out of scope during foundation

- Do **not** create a mobile project
- Do **not** select a mobile framework
- Do **not** design GPS infrastructure
- Do **not** create mobile APIs
- Do **not** create HR implementation

---

## Multi-Property and Hotel Chains

Hotel chains and multi-property management are strategically relevant to HuGuWeb's long-term direction.

However, during the foundation stage:

- Do **not** implement multi-property functionality
- Do **not** design database tables for multi-property
- Do **not** implement tenant infrastructure
- Do **not** create hotel group logic

Future architecture decisions should evaluate multi-property requirements early enough to avoid expensive redesign.

### Concepts requiring formal definition

The following concepts are **not** automatically equivalent and must be formally defined before implementation:

| Concept | Status |
|---------|--------|
| Tenant | Not yet defined |
| Hotel / Property | Not yet defined |
| Hotel Group | Not yet defined |

See [Glossary](GLOSSARY.md) for current terminology notes.

---

## Industry Expansion

HuGuWeb's first target industry is hotels and hospitality. Future expansion into other industries (such as manufacturing) may be considered, but the current system must not be designed around hypothetical future industries.

---

## Hotel Operating System Hypothesis

HuGuWeb may evolve toward a **Hotel Operating System** where hotel workflows communicate across operational boundaries.

For example, guest checkout may eventually trigger or affect:

- Room status
- Housekeeping workflow
- Folio
- Invoicing
- Inventory-related operations

This is a product discovery hypothesis only. Workflows are not formally defined or implemented.

---

## Integration Capabilities

Future product decisions must evaluate **Build vs Integrate** for external capabilities. Examples under future consideration:

- Channel Manager
- OTA integrations
- Payment providers
- POS
- e-Invoice / e-Archive
- Government reporting integrations
- Payroll systems
- Revenue management systems

Integration vendors are not selected during the foundation stage. External systems should eventually be isolated from core business logic through appropriate integration boundaries.

---

## Related Documents

- [Product Vision](PRODUCT_VISION.md)
- [Product Principles](PRODUCT_PRINCIPLES.md)
- [Glossary](GLOSSARY.md)
- [Architecture](../architecture/README.md)
