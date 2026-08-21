# Organization & Workforce — Domain Model

> **Status:** Accepted — Product Owner + CTO approved baseline. **Evidence:** E0–E1.

## Why this slice exists

The first HR-related slice answers operational questions other hotel workflows will need:

- Which organization employs this person?
- Which property / workplace context applies?
- Which departments and positions exist?
- Who is employed, in what relationship, and where are they assigned *now*?
- What history must remain valid after transfers and termination?

It does **not** answer leave, attendance, payroll, recruitment, or “complete HR.” Full HR / Payroll remains later / likely integrate ([MVP Candidates](../../product/MVP_CANDIDATES.md)).

---

## Boundary

### In

Organization, Property, Department, Position, Employee, Employment, Assignment, Personnel Number, employment lifecycle, assignment history, future government-integration readiness, localization strategy.

### Out

Leave, attendance, shift scheduling, puantaj, payroll, overtime, training, recruitment, performance, employee documents, accident records, transport, accommodation, access cards / passcards, employee mobile, organization chart, manager hierarchy, full HR, full payroll, multi-property UI, tenant infrastructure, Hotel Group.

These may **read** this foundation later. They must not expand this model now. Do not create placeholder implementation concepts for deferred scope.

---

## Core concepts

Internal names stay English and stable. Product conversation uses Turkish.

| Internal identifier | Product term (TR default) | Meaning |
|---------------------|---------------------------|---------|
| `Organization` | Organizasyon / Şirket | Employing entity. Not a hotel group. Not a tenant. |
| `Property` | Tesis | Physical operating hotel. Belongs to Organization. Not equal to Organization. |
| `Department` | Departman | Configurable structure (e.g. Kat Hizmetleri). Not an enum. |
| `Position` | Pozisyon | Configurable title. Not an enum. Not a permission. |
| `Employee` | Personel | Employed person. **Not** a user account. |
| `Employment` | İş ilişkisi | Work relationship with start/end and lifecycle. **Not** attendance. |
| `Assignment` | Görevlendirme | Where / in which position the person works for a period. |
| `PersonnelNumber` | Sicil No | Business identifier. **Not** the technical primary key. |
| `ApplicationUser` | Kullanıcı | Identity in Host. Not required. Not linked in 0.7B. |

```text
Organization 1──* Property
Property     1──* Department
Property     1──* Position

Employee 1──* Employment 1──* Assignment
Assignment *──1 Department
Assignment *──1 Position
```

Position is **not** owned by Department. Assignment independently references Department and Position.

This diagram is a **conceptual** relationship map, not an EF navigation or eager-load graph.

---

## Frozen separations

### Employee ≠ User ≠ Role ≠ Permission ≠ Position

| Concept | Answers | Owned by |
|---------|---------|----------|
| Employee | Who is employed? | Organization & Workforce |
| Employment | Are they currently employed? | Organization & Workforce |
| Position | What job title? | Organization & Workforce |
| ApplicationUser | Can they authenticate? | Host / Identity ([ADR-007](../../architecture/adr/ADR-007-Authentication-Strategy.md)) |
| Role / Permission | What may they do in HuGuWeb? | Authorization ([ADR-008](../../architecture/adr/ADR-008-Authorization-Strategy.md)) |

- An employee may have **no** login.
- Hiring must **not** create a HuGuWeb account.
- Ending employment does **not** delete Identity. Disabling a user is a separate operational action.
- Position and Department must not grant permissions.
- Sprint 0.7B must **not** implement Employee/User linking.

### Employment status ≠ daily availability

`Active` employment while the person is on leave, sick, or on a day off is valid. Leave, attendance, and shifts are future domains. Do not mix “employed” with “present today.” Government-system status must also **not** become Employment status.

### Department / Position are data, not code

Illustrative names (İnsan Kaynakları, Kat Hizmetleri, Ön Büro, Teknik Servis, Yiyecek ve İçecek) are **examples**, not constants. See [ORGANIZATION_MODEL.md](ORGANIZATION_MODEL.md).

---

## Person vs Employee

No separate `Person` aggregate. Employee holds given name, family name, and personnel number.

Rehire (later) is a new Employment on the same Employee. Unifying Person across guests, candidates, and staff would couple unrelated bounded contexts.

---

## Localization strategy

Two different categories. Do not mix them.

### A. Product terms (HuGuWeb-controlled)

Static translations in frontend i18n resources ([LOCALIZATION.md](../../product/LOCALIZATION.md)): navigation, buttons, empty states, system enum *labels*.

Internal enum *values* stay language-neutral (`Active`, `Ended`, `Primary`). Do not translate C# type names, table names, permission identifiers, or API contract identifiers.

### B. Customer-defined organizational names

Department and position names are **hotel data**, not resource-file keys.

Sprint 0.7B stores **one** customer-defined working name per department/position. Do not build multilingual customer master-data translation yet. Future customer-managed translations may be introduced if validated.

UI language remains a **user** preference (`tr` / `en` / `ru`), not a hotel setting.

---

## Aggregate boundaries

Do **not** treat Employee → Employment → Assignment as “one aggregate that loads complete lifetime history.”

A long-tenured employee may have multiple employments, transfers, temporary assignments, and later workforce records. Loading all of that because the object graph is convenient is the wrong design.

Choose aggregates from:

- transactional consistency
- invariants
- behavior
- concurrency boundaries

Sprint 0.7A documents relationships and invariants only. It does **not** freeze EF navigation graphs, eager-loading, repository pattern, or aggregate loading behavior.

Sprint 0.7B should choose the **smallest** boundaries that safely implement:

- **Hire** — Employee + Employment + Primary Assignment as one business operation
- **Transfer** — close previous Primary, open new Primary, retain history
- **End Employment** — close Employment and applicable Primary Assignment, retain records

The relational database may retain full history without loading all history into one aggregate.

Candidate persistence roots (not frozen loading strategy): Organization, Property, Department, Position, plus whatever small write models 0.7B needs for Hire / Transfer / End Employment. `ApplicationUser` is not in this domain.

Property is an operational platform concept. The first implementation may persist a thin Property here only because no Property module exists yet. Do not put Property in BuildingBlocks until a second approved module needs it ([ADR-001](../../architecture/adr/ADR-001-Architecture-Style.md)).

### Value object candidates (only if useful in 0.7B)

`PersonnelNumber`, employment/assignment periods. Do not invent value objects for purity. Do not add national-id, money, or address types.

---

## Domain events (conceptual only)

Documented so future in-process consumers — including official-notification adapters — can react to **business facts**. Do **not** introduce event sourcing, brokers, outbox, workers, or schedulers in Sprint 0.7A or 0.7B for this readiness.

| Event | When | Later consumers (not 0.7B) |
|-------|------|----------------------------|
| `EmployeeHired` | Hire committed | SGK işe giriş, applicable KBS, later operations |
| `AssignmentChanged` | Transfer (and later temporary assignment) | Operations; not government in 0.7B |
| `EmploymentEnded` | Employment closed | SGK işten ayrılış, applicable KBS, later operations |

The workforce core must not know which authority consumes an event.

---

## Official notification readiness (future)

HuGuWeb workforce lifecycle must be designed so hiring and employment termination can later integrate with official Turkish government notification systems.

Known future integration areas:

- SGK employee start notification (işe giriş)
- SGK employee termination notification (işten ayrılış)
- Police / Emniyet Kimlik Bildirim Sistemi (KBS)
- Jandarma Kimlik Bildirim Sistemi (KBS)
- other legally required workforce notifications discovered later

Sprint 0.7A documents readiness only. Sprint 0.7B must **not** implement these integrations.

### Transaction boundary

A HuGuWeb workforce transaction and an external government notification are **not** the same transaction.

```text
Hire / End Employment
    → Employee, Employment, Assignment committed in HuGuWeb
    → official notification obligations may follow
    → SGK / Police KBS / Jandarma KBS / other authority
```

External government-system downtime or failure must not corrupt or roll back valid HuGuWeb workforce history. Do **not** design a distributed transaction with government systems.

Government-system status must not become Employment status.

### Isolation

Do not couple Employee, Employment, or Assignment to SGK, Police KBS, Jandarma KBS, or other authority-specific details.

Future adapters live **outside** the workforce domain core. The core expresses facts such as `EmployeeHired` and `EmploymentEnded`.

Do **not** create placeholder interfaces (`ISgkService`, `IKbsService`, `IGovernmentIntegrationService`) until an actual integration slice is approved.

Do **not** introduce message broker, RabbitMQ, Kafka, event bus, outbox, background worker, scheduler, or retry infrastructure in 0.7A or 0.7B merely for future readiness. Those decisions must be evidence-driven when real integrations begin.

### Future tracking (not frozen, not persisted now)

Later notification records may independently track states such as Pending, Submitted, Accepted, Rejected, Failed, RequiresAction. Exact states are **not** frozen.

They may later need: notification type, target authority, related Employee and Employment, triggering business event, submission/response time, external reference, status, failure reason, retry/history, business audit.

Do **not** create those tables now. Do **not** add speculative government-specific Employee fields. Do **not** implement SGK/KBS clients, credentials, or automatic submissions.

---

## Future multi-property direction

Long-term, the same Employee should be able to work across multiple Properties of the same Organization without duplicate Employee identities.

Do **not** implement cross-property assignments, multi-property UI, or Tenant infrastructure now. Do not create speculative abstractions solely for this scenario. Persist Organization and Property as separate concepts so this remains possible.

---

## Delete, audit, privacy

- Prefer **end-date / deactivate** over generic `IsDeleted`.
- Never physically delete employment or assignment history as a normal operation.
- Collect only given name, family name, and personnel number now.
- National ID, bank, address, tax, emergency contact, and government registration numbers wait for a later justified slice.
- Historical need does not mean unrestricted access.

---

## Isolation from authentication

A defect in workforce rules must not require changes to cookie auth, Identity schema, or permission enforcement. Sprint 0.7B Hire has no Identity dependency.
