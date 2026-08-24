# Property official configuration

> **Status:** Accepted — HR-03A. Conceptual model only. **No configuration UI in HR-03A.**

## Why Property, not Employee

SGK **işyeri sicil** identifies the workplace that employs people, not the person. Duplicating it on every Employment row creates drift (one typo, 200 employees wrong) and hides the real owner.

HuGuWeb already separates:

```text
Organization  →  employer / company boundary (thin)
Property      →  physical operating hotel / tesis (thin)
```

The operating hotel is the workplace context. **Property owns SGK workplace registrations.**

Organization remains the employer identity. Do **not** collapse Organization and Property. Do **not** turn Property into a full legal/accounting master (VKN, MERSİS, trade registry, KEP, KBS passwords).

Employment does **not** own Property. See [ORGANIZATION_MODEL.md](../ORGANIZATION_MODEL.md): Employment belongs to the Organization; Assignment is property-scoped through Department.

---

## Cardinality (decided)

```text
Property
  └── SgkWorkplaceRegistrations [0..*]
```

**A Property MAY have multiple SGK workplace registrations.** Each registration is a separate business record.

**Do not** impose a “one active registration per Property” domain invariant.

Hotels can need more than one concurrent işyeri on a single tesis (restaurant or spa registered separately, seasonal outlet, historical number still referenced by existing employments). OfficialEmploymentProfile therefore **selects** the applicable registration instead of assuming one implicit workplace for every Employment at the Property.

Do **not** invent the complete registration field set in this freeze. HR-03B implements the collection and a practical identity (at least PropertyId + registration number); additional parsed/legal columns stay out until a real need is shown.

---

## Model options

| Option | Shape | Verdict |
|--------|-------|---------|
| **A. Fields directly on Property** | `Property.SgkRegistrationNumber` | Too small; cardinality > 1 is now decided |
| **B. PropertyOfficialProfile 1:1** | One owned row | Same cardinality trap as A |
| **C. `SgkWorkplaceRegistration` collection** | `Property 1 — * SgkWorkplaceRegistration` | **Decided.** 0..* business records |

**C is the model.** Zero registrations is valid (configuration not yet entered). Many concurrent registrations are valid. Soft-deactivation of a row, if used later, is configuration hygiene — not a uniqueness invariant.

---

## Recommended minimum (`SgkWorkplaceRegistration`)

Do not freeze a full legal master. The first useful record is:

| Field | Role |
|-------|------|
| Id | Technical identity |
| PropertyId | Owner |
| RegistrationNumber | SGK işyeri sicil (the number HR actually needs) |

**Allowed later, not required to invent now:** a working label so HR can tell hotel vs restaurant apart in a picker; `IsActive` as picker hygiene; ValidFrom/ValidTo if a number is replaced over time.

**03B does not include:** SGK user name/password, işyeri şifresi, KBS credentials, İŞKUR işyeri no, tehlike sınıfı, 5510 oran, vergi dairesi, VKN, NACE, MERSİS.

Parsed sicil components in the reference (21+ digits → mahiyet, işkolu, ünite, sıra, il, ilçe, CD, aracı) are a **display/parse helper**, not a second source of truth. Persist the registration number; parse when a later adapter needs parts.

---

## How Employment points at a registration

`OfficialEmploymentProfile.SgkWorkplaceRegistrationId` is optional. When set:

1. The registration must exist.
2. `registration.PropertyId` must correspond to the Property of the Employment’s **relevant organizational context**.
3. That Property is **not** stored on Employment.

**Existing Workforce path (do not redesign Employment/Assignment):**

```text
Employment
  └── Assignment (Primary; current if open, last if ended)
        └── Department.PropertyId     → Property
        └── Position.PropertyId       → Property (already property-scoped)
```

Accepted model: Employment is the work relationship with the Organization. Assignment records where/in which position the person works. Department and Position are property-scoped. Hire already requires a Department and Position, so a Primary Assignment exists for every persisted Employment.

**Validity rule:** when the profile references a registration, `SgkWorkplaceRegistration.PropertyId` must equal `Department.PropertyId` of the Employment’s current (or, if ended, last) Primary Assignment.

Position.PropertyId of that Assignment is expected to match Department.PropertyId already (both are property-scoped in the accepted model). Do **not** add `PropertyId` onto Employment to make this check possible.

First implementation is single-property, so the check is trivially true once the registration belongs to the seeded Property — the invariant is still written against the real chain.

**Unresolved (not a 03B blocker):** whether an open Employment may change SGK workplace without ending. 03B’s current-value profile can overwrite the FK; whether that is legally a new workplace period is an expert question. Effective-dated workplace history is deferred.

---

## Configuration UX (concept only — not HR-03A)

SGK workplace registrations **must not** be created by repeatedly typing a workplace number inside each Personel Card.

| Surface | Responsibility |
|---------|----------------|
| **Property / organization configuration** | Create, edit, and list `SgkWorkplaceRegistration` rows for that Property |
| **Personel Card → Resmî bilgiler → Bildirge Kodları** | **Select** an existing applicable registration |

HR-03A does **not** implement configuration UI. There is still no Organization/Property admin product. HR-03B may add a small configuration editor (permission: `workforce.manage`) so the picker is not empty. That editor is configuration, not a Personel Card workflow, and not a new `hr.official.*` permission.

Personel Card may **display** the selected registration number as read-only context after selection; it must not be an employee-typed sicil field.

---

## Sensitivity

Organization/property confidential configuration. Not operational. Not on `OperationalEmployeeReference`. Not on Room Operations / Technical Service APIs.
