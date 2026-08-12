# Hotel Department Problem Discovery

> **Status:** Problem hypotheses for validation — not proven customer facts.

This document organizes potential hotel operational problems by department and work area—not by ERP module names. Problems listed here are **research hypotheses** unless explicitly supported by evidence elsewhere in the repository.

Language used throughout:

- **Problem to validate** — requires customer or pilot evidence
- **Potential pain** — plausible from industry context; not yet confirmed for HuGuWeb targets
- **Research hypothesis** — internal working theory for discovery interviews

Do **not** treat items in this document as HuGuWeb requirements.

**Generic vs hospitality-specific:** Categories such as disconnected systems, duplicate data entry, paper/Excel dependence, approval delays, and reporting delays also appear in generic ERP contexts. Listing them here does **not** mean they are confirmed hotel-priority problems. Each requires evidence that it is **materially important in hotel operations** for HuGuWeb’s target segment.

---

## How to Read This Document

Each department section includes:

1. **Responsibilities** — what the area generally must accomplish
2. **Common workflow problems to validate** — candidate pain categories
3. **Cross-department dependencies** — examples of operational handoffs

Technical workflows, APIs, and system designs are intentionally excluded.

---

## General Management

### Responsibilities

- Overall operational performance and guest satisfaction
- Cross-department coordination and escalation
- Budget oversight and cost control
- Strategic decisions on vendors, staffing, and investments
- Visibility into daily exceptions and KPIs

### Common workflow problems to validate

| Problem category | Description (hypothesis) |
|------------------|--------------------------|
| Disconnected systems | **Problem to validate:** Management may lack a single operational picture when PMS, finance, HR, and maintenance live in separate tools |
| Reporting delays | **Potential pain:** Daily or weekly reports may require manual consolidation |
| Approval bottlenecks | **Problem to validate:** Capital purchases, discounts, or exceptions may stall without clear approval paths |
| Cost visibility | **Research hypothesis:** Real-time cost drivers (payroll overtime, stock waste, maintenance backlog) may be hard to see |
| Operational handoff failures | **Problem to validate:** Issues raised in one department may not surface to management until guest impact occurs |

### Cross-department dependencies

- Approvals spanning Finance, Purchasing, and department heads
- Exception handling across Front Office, Housekeeping, and F&B during high occupancy
- Performance reporting requiring inputs from Finance, Sales, and Operations

---

## Front Office

### Responsibilities

- Guest arrival, stay, and departure handling
- Room assignment and in-stay guest requests
- Communication with housekeeping on room readiness
- Folio and charge visibility during stay
- Coordination with reservations for arrivals and changes

### Common workflow problems to validate

| Problem category | Description (hypothesis) |
|------------------|--------------------------|
| Delayed status visibility | **Problem to validate:** Front office may not reliably know which rooms are clean, inspected, or blocked |
| Manual information transfer | **Potential pain:** Housekeeping or maintenance updates may be communicated by phone, paper, or messaging apps |
| Reservation handoffs | **Problem to validate:** Changes from reservations may not reach front office in time |
| Duplicate data entry | **Research hypothesis:** Guest details may be re-entered across PMS, POS, and CRM-like tools |
| Check-in/check-out friction | **Problem to validate:** Delays when room not ready, folio disputes, or missing authorization |
| Poor cross-department communication | **Potential pain:** Guest requests may be lost between front office and other departments |

### Cross-department dependencies

```
Checkout → Housekeeping → Room readiness → Front Office (next arrival)
Reservation change → Front Office → Room assignment → Housekeeping
Guest request → Front Office → Maintenance / F&B / Housekeeping
```

---

## Reservation

### Responsibilities

- Booking creation and modification across channels
- Rate and availability management coordination
- Guest communication before arrival
- Group and special request handling
- Handoff of accurate reservation data to front office

### Common workflow problems to validate

| Problem category | Description (hypothesis) |
|------------------|--------------------------|
| Disconnected systems | **Problem to validate:** OTA, phone, email, and walk-in channels may not sync cleanly with property inventory |
| Duplicate data entry | **Potential pain:** Same booking details entered in channel tools and PMS |
| Delayed status visibility | **Research hypothesis:** Overbooking or block conflicts may surface late |
| Manual information transfer | **Problem to validate:** Special requests may not flow reliably to front office or housekeeping |
| Reporting delays | **Potential pain:** Pickup, cancellation, and channel mix reports may be delayed or manual |

### Cross-department dependencies

```
Channel / OTA → Reservation → Front Office → Housekeeping (pre-arrival prep)
Group booking → Reservation → Finance (deposits) → F&B (catering)
```

---

## Housekeeping

### Responsibilities

- Room cleaning, inspection, and readiness status
- Task assignment to room attendants and supervisors
- Coordination with front office on priority rooms
- Lost-and-found and minor maintenance issue reporting
- Linen and amenity consumption (may tie to inventory)

### Common workflow problems to validate

| Problem category | Description (hypothesis) |
|------------------|--------------------------|
| Delayed status visibility | **Problem to validate:** Room status updates may lag, affecting check-ins |
| Manual information transfer | **Potential pain:** Priority room lists may be paper-based or verbal |
| Operational handoff failures | **Problem to validate:** Maintenance issues found during cleaning may not create tracked follow-up |
| Weak mobile access | **Research hypothesis:** Staff on floors may lack practical mobile tools for status updates |
| Paper/Excel dependence | **Potential pain:** Task lists and inspections tracked outside core systems |

### Cross-department dependencies

```
Front Office (departures/arrivals) → Housekeeping → Front Office (room ready)
Housekeeping → Maintenance (defect found) → Front Office (room blocked)
Housekeeping → Inventory (linen/amenities) → Purchasing (replenishment)
```

---

## Purchasing

Purchasing problems below are **research hypotheses**. Several are generic ERP pains (approvals, duplicate entry, spreadsheet purchasing). They need evidence of **material hotel-operational impact**, and they are **not** assumed required for first-pilot value. See the Inventory/Purchasing open decision in [MVP Candidates](MVP_CANDIDATES.md).

### Responsibilities

- Supplier selection and purchase order management
- Processing requisitions from departments
- Approval workflow coordination
- Price and contract tracking
- Coordination with receiving and finance

### Common workflow problems to validate

| Problem category | Description (hypothesis) |
|------------------|--------------------------|
| Approval delays | **Problem to validate:** Requisitions may wait for manager or GM approval without visibility |
| Disconnected systems | **Potential pain:** Purchasing may operate in ERP or spreadsheets separate from property operations |
| Manual information transfer | **Research hypothesis:** Department requests via email or paper may lose context |
| Poor cross-department communication | **Problem to validate:** Urgent operational needs may not translate into prioritized purchase orders |
| Duplicate data entry | **Potential pain:** Same request re-entered into purchasing and finance |

### Cross-department dependencies

```
Department requisition → Approval → Purchasing → Receiving → Inventory → Finance
F&B menu change → Purchasing (supplier/price) → Warehouse → Kitchen
```

---

## Warehouse / Inventory

Inventory problems below are **research hypotheses**. Stock visibility and Excel counts are common in many industries. They need evidence of **material hotel-operational impact** (housekeeping, F&B, maintenance). Inventory is **not** assumed required for first-pilot value.

### Responsibilities

- Stock receipt, storage, and issuance to departments
- Stock level monitoring and reorder triggers
- Consumption tracking by department or cost center
- Coordination with purchasing on replenishment
- Periodic stock counts and variance investigation

### Common workflow problems to validate

| Problem category | Description (hypothesis) |
|------------------|--------------------------|
| Delayed status visibility | **Problem to validate:** Departments may not know if items are in stock before promising use |
| Paper/Excel dependence | **Potential pain:** Stock counts and issues tracked manually |
| Disconnected systems | **Research hypothesis:** Inventory may not connect to F&B recipes, housekeeping consumption, or maintenance parts |
| Operational handoff failures | **Problem to validate:** Received goods may not update stock promptly, causing downstream shortages |
| Reporting delays | **Potential pain:** Cost of goods and variance reports may lag operations |

### Cross-department dependencies

```
Purchasing → Receiving → Inventory → Department issue → Finance (cost)
Housekeeping consumption → Inventory → Purchasing
Maintenance parts usage → Inventory → Purchasing
```

---

## Finance / Accounting

These department responsibilities mix **different product scopes**. They are listed together because hotels often staff them together—not because HuGuWeb should build all of them.

| Layer | Examples | Not the same as |
|-------|----------|-----------------|
| Operational financial events | Charge posted, discount, deposit taken | The hotel’s books |
| Folio / guest charges | Running guest account during stay | Invoices, GL, tax returns |
| Payment handling | Capture, refund, pre-auth | Accounting software |
| Finance visibility | What is outstanding or posted today | Month-end close |
| Accounting integration | Export/post to an external ledger | Owning the ledger |
| Full statutory accounting | GL, AP/AR, tax books, statutory reports | Operational folio |

HuGuWeb must **not** automatically become an accounting software company. Problems below are **research hypotheses**. Generic finance/ERP pains (AP approval, month-end close) need evidence they are **material in hotel operations**, not only that they exist in business software.

### Responsibilities (hotel department — not HuGuWeb scope)

- Guest folio settlement and related operational events
- Handoff of operational data to accounting (where a separate system exists)
- Accounts payable and receivable (**full accounting layer** — not assumed HuGuWeb scope)
- Cost center / departmental accounting (**accounting layer**)
- Tax and regulatory reporting, market-dependent (**compliance / integrate layer**)
- Month-end close and financial reporting (**statutory accounting layer**)

### Common workflow problems to validate

| Problem category | Description (hypothesis) |
|------------------|--------------------------|
| Disconnected systems | **Problem to validate:** PMS folio data may require manual export to accounting. This is an **accounting-integration** pain, not proof that HuGuWeb should own the GL. |
| Duplicate data entry | **Potential pain (generic ERP — needs hotel evidence):** Same transaction entered in operations and finance |
| Reporting delays | **Research hypothesis:** Management may wait on manual reconciliation. Distinguish **finance visibility** (today’s operational picture) from **statutory reporting**. |
| Approval delays | **Problem to validate (generic ERP — needs hotel evidence):** Invoice approval chains may slow vendor payment |
| Regulatory reporting | **Problem to validate (market-dependent):** e-Invoice and government reporting may add integration burden — typically **integrate**, not a reason to build full accounting |

### Cross-department dependencies

```
Front Office (folio) → Finance (posting) → Management reporting
Purchasing (PO/invoice) → Finance (AP) → Cash flow reporting
HR/Payroll → Finance (payroll accrual) → Month-end close
```

---

## Human Resources

### Responsibilities

- Hiring, onboarding, and employee records
- Scheduling, leave, and overtime tracking
- Payroll data preparation (may integrate externally)
- Training and compliance tracking
- Staff services (transportation, accommodation—where applicable)

### Common workflow problems to validate

| Problem category | Description (hypothesis) |
|------------------|--------------------------|
| Disconnected systems | **Problem to validate:** HR may use separate tools from operational scheduling |
| Manual information transfer | **Potential pain:** Overtime or leave requests via informal channels |
| Paper/Excel dependence | **Research hypothesis:** Roster and attendance tracked manually |
| Weak mobile access | **Problem to validate:** Staff may lack self-service for leave, payslips, or requests (see [Future Scope](FUTURE_SCOPE.md)) |
| Poor cross-department communication | **Potential pain:** Staffing shortages may not align with housekeeping or F&B demand |

### Cross-department dependencies

```
Department manager → HR (leave/overtime) → Finance (payroll)
Housekeeping workload → HR (scheduling) → Front Office (coverage)
```

---

## Technical Service / Maintenance

### Responsibilities

- Reactive maintenance requests and work orders
- Preventive maintenance scheduling
- Asset and equipment tracking
- Vendor coordination for specialized repairs
- Room/out-of-order management coordination with front office and housekeeping

### Common workflow problems to validate

| Problem category | Description (hypothesis) |
|------------------|--------------------------|
| Operational handoff failures | **Problem to validate:** Guest-reported issues may not become tracked work orders |
| Delayed status visibility | **Potential pain:** Front office may not know repair status for blocked rooms |
| Paper/Excel dependence | **Research hypothesis:** PM schedules and asset history tracked offline |
| Disconnected systems | **Problem to validate:** Maintenance may be isolated from room status in PMS |
| Duplicate data entry | **Potential pain:** Same issue logged by housekeeping and again by maintenance |

### Cross-department dependencies

```
Guest / Front Office → Maintenance → Front Office (room block/release)
Housekeeping → Maintenance (defect) → Inventory (parts) → Purchasing
Preventive schedule → Maintenance → Asset history → Management reporting
```

---

## Food & Beverage

### Responsibilities

- Restaurant, bar, banquet, and room service operations
- Menu and recipe management (where applicable)
- POS operations and charge posting to guest folios
- Kitchen inventory and cost control
- Event and group catering coordination

### Common workflow problems to validate

| Problem category | Description (hypothesis) |
|------------------|--------------------------|
| Disconnected systems | **Problem to validate:** POS may not integrate cleanly with PMS folio or inventory |
| Duplicate data entry | **Potential pain:** Manual posting of charges or events |
| Delayed status visibility | **Research hypothesis:** Banquet/event details may not reach kitchen or front office reliably |
| Cost visibility | **Problem to validate:** Food cost and waste may be hard to tie to real-time operations |
| Integration needs | **Potential pain:** Specialized POS ecosystems may dominate F&B workflows |

### Cross-department dependencies

```
Reservation/Events → F&B → Kitchen → Inventory → Purchasing
F&B POS → Guest folio → Finance
Group booking → F&B → Front Office (billing)
```

---

## Sales / CRM (where relevant)

### Responsibilities

- Corporate accounts and contract management
- Group and event sales pipeline
- Guest relationship and repeat business tracking
- Coordination with reservations and front office on contracted rates and benefits

### Common workflow problems to validate

| Problem category | Description (hypothesis) |
|------------------|--------------------------|
| Disconnected systems | **Problem to validate:** CRM data may not connect to PMS guest profiles |
| Duplicate data entry | **Potential pain:** Account details maintained in sales tools and re-entered at booking |
| Manual information transfer | **Research hypothesis:** Sales promises may not reach operational teams consistently |
| Reporting delays | **Potential pain:** Production vs forecast reporting may be manual |

### Cross-department dependencies

```
Sales (contract) → Reservation → Front Office → Finance (billing terms)
Guest profile → Sales/CRM → Marketing (future scope; not validated)
```

---

## Major Cross-Department Problem Themes

Current research hypotheses spanning multiple areas:

1. **System fragmentation** — PMS, POS, ERP, HR, and maintenance tools may not share timely operational state ([Market Research](../research/MARKET_RESEARCH.md)).
2. **Handoff failures** — Checkout-to-housekeeping-to-arrival and requisition-to-receiving-to-finance chains may break at human communication boundaries.
3. **Status visibility gaps** — Room readiness, stock levels, maintenance progress, and approval states may be stale or invisible across departments.
4. **Manual reconciliation burden** — Finance visibility and management reporting *may* depend on exports and spreadsheets (**generic ERP pain** until hotel evidence shows material operational impact). This is not a requirement to build full accounting.
5. **Mobile access gaps** — Floor-based staff (housekeeping, maintenance) may lack practical tools; employee mobile remains future scope ([Future Scope](FUTURE_SCOPE.md)).

---

## Validation Plan (Candidate)

Problems in this document should be validated through:

- [Discovery Questions](DISCOVERY_QUESTIONS.md) during pilot/customer interviews
- [Evidence Model](EVIDENCE_MODEL.md) progression from E0 to E2/E3
- Eventual pilot observation (E4) if HuGuWeb reaches Phase 4

---

## Related Documents

- [Target Customer](TARGET_CUSTOMER.md)
- [Opportunity Matrix](OPPORTUNITY_MATRIX.md)
- [Discovery Questions](DISCOVERY_QUESTIONS.md)
- [Product Vision](PRODUCT_VISION.md)
