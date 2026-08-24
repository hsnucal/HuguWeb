# Employment official profile

> **Status:** Accepted — HR-03A. Conceptual model only.

## Why Employment, not Employee

Belge türü, tabi kanun, sigorta kolu, meslek kodu, and the applicable SGK workplace describe **how this work relationship is classified**, not who the person is.

- Rehire is a new Employment (Accepted). Official classification for 2019 and 2026 must not overwrite each other on Employee.
- Intern vs normal vs retired-support (SGDP) is an employment fact (often visible as belge türü `07` / `01` / `02` in the reference).
- Meslek kodu can change after promotion; that is still inside one Employment until we add history.
- Which SGK işyeri applies can differ across employments at the same Property (restaurant vs hotel registration). That choice belongs on the employment profile, not on Employee and not as a typed copy of the sicil.

Do **not** store these on Employee. Do **not** store them on Assignment. Assignment is department/position/period. **Position ≠ MeslekKodu.**

---

## Model options

| Option | Shape | Verdict |
|--------|-------|---------|
| **A. Columns on Employment** | Extra fields on the employment row | Smallest table count; mixes lifecycle with lookups; harder to grow |
| **B. 1:0..1 `OfficialEmploymentProfile` owned by Employment** | Same pattern as `EmployeeHrProfile` | **Decided for 03B** — current-value model |
| **C. Effective-dated `OfficialEmploymentClassification` records** | Many rows per Employment with ValidFrom/To | Correct later if monthly SGK hizmet needs intra-employment history. **Not 03B.** |

**B is the HR-03B model:**

```text
Employment
  └── OfficialEmploymentProfile  [0..1]
       └── SgkWorkplaceRegistrationId  (optional)
```

- Rehire-safe: a new Employment receives its own profile.
- Employee stays small.
- Option C remains possible later because codes live on a dedicated owned record, not scattered on Employee or Assignment.

**Architectural requirement:** HR-03B must not make a later migration to effective-dated statutory history unreasonably difficult. Practical implications: keep the profile a separate owned type; store stable codes (not concatenated display strings); do not denormalize official codes onto Employee, Assignment, or operational DTOs; do not treat the snapshot as if it were already a monthly as-of ledger.

Trigger to reopen Option C: legal/expert evidence that a meslek/kanun/workplace change mid-employment must remain queryable by month for SGK.

---

## Recommended `OfficialEmploymentProfile` (HR-03B)

Owned record. At most one per Employment. All business fields optional at hire and at ordinary card save.

| Field | Notes |
|-------|-------|
| EmploymentId | Owner |
| SgkWorkplaceRegistrationId | Applicable Property registration; optional; **select existing**, do not copy sicil |
| DocumentTypeCode | Belge türü — lookup `SgkDocumentType` |
| ApplicableLawCode | Tabi kanun — lookup `ApplicableLawCode` |
| InsuranceBranchCode | Sigorta kolu — lookup `InsuranceBranch` |
| OccupationCode | Meslek kodu — lookup `SgkOccupationCode`, code only |

**Do not add DutyCode** in 03B.

No SGK notified flags. No exit codes. No incentive flags. No duplicate workplace sicil string.

---

## Employment → SGK workplace validity

Employment does **not** own `PropertyId` (Accepted Workforce model).

Organizational context is:

```text
Employee
  └── Employment          (period + lifecycle; Organization employer boundary)
        └── Assignment    (Department + Position + period + kind)
              └── Department.PropertyId → Property
              └── Position.PropertyId   → Property
```

Hire always creates a Primary Assignment. At most one non-ended Primary per Employment. Transfers close the previous Primary (D−1) and open a new one (D).

**When `SgkWorkplaceRegistrationId` is present:**

1. The registration exists.
2. `SgkWorkplaceRegistration.PropertyId` equals the Property of the Employment’s relevant organizational context.
3. **Relevant context** = Department.PropertyId of the current Primary Assignment if the Employment is not ended; if ended, Department.PropertyId of the last Primary Assignment.

Do **not** redesign Employment or Assignment to carry PropertyId. Do **not** attach the registration to Assignment (temporary/joker coverage is conceptual only and must not become a second statutory workplace owner).

Picker contents: registrations of that same Property — not a free-text sicil, not registrations of other properties.

**Unresolved:** whether an open Employment may change SGK workplace. 03B may overwrite the current FK. Mid-employment workplace **history** is deferred with Option C.

---

## History / effective dating

| Event | 03B behavior |
|-------|----------------|
| Rehire | New Employment → new empty or copied-by-user profile. Old profile remains on the ended Employment |
| Edit on Resmî bilgiler | Updates the current profile snapshot for that Employment |
| Promotion / transfer | Assignment changes via existing Transfer. Meslek kodu does **not** auto-change. HR may edit OccupationCode afterward |
| Change of selected SGK workplace | Overwrites current `SgkWorkplaceRegistrationId`. No as-of history in 03B |
| Mid-employment kanun/meslek/workplace change that must be historically true | **FUTURE Option C** |

Do not blindly treat the profile as “the only codes SGK ever saw” if a later monthly adapter needs as-of-date values. 03B does not implement that adapter.

---

## Employment vs Assignment vs Position

```text
Position name          = hotel job title (customer data)
Assignment             = where/when that title applies
OccupationCode         = SGK/ISCO-style statutory occupation
DutyCode / Görev Kodu  = WebİK Bildirge label set — NOT in 03B
```

Do **not** derive OccupationCode from Position.Name. Do **not** attach authoritative SGK occupation identity to Position in HR-03B.

**FUTURE / NEEDS VALIDATION:** Position may later store a recommended `DefaultOccupationCode` to speed hire. That default is a **suggestion only**: never an automatic write, never statutory truth, never authorization.

---

## Görev Kodu (out of 03B, evidence retained)

WebİK Personel Kartı Bildirge section exposes **Görev Kodu** (`gorevKodu`) with **six Turkish labels** and **no separate numeric code** in the UI:

- İşveren veya Vekili
- İşçi
- 657 SK (4/b) Kapsamında Çalışanlar
- 657 SK (4/c) Kapsamında Çalışanlar
- Çıraklar ve Stajer Öğrenciler
- Diğerleri

**HuGuWeb does not yet model this.** Classification: **DEFERRED / NEEDS DOMAIN OR LEGAL VALIDATION**. It can be added later if validated as a stable official statutory catalogue required by the SGK model. Discovery evidence stays in [LOOKUP_CODES.md](LOOKUP_CODES.md) and [FIELD_CATALOG.md](FIELD_CATALOG.md). Do not show the field on the Personel Card in 03B.

---

## Entry / exit codes

Source Bildirge tab included:

- İşten Çıkış Kodu (SGK EK-2-style, 46 default rows)
- İşten Çıkış Kodu (İŞKUR) (5 default rows — likely incomplete)

**HuGuWeb placement:**

| Code | Owner |
|------|--------|
| SGK işten ayrılış nedeni | **B/C hybrid later:** captured on End Employment (HR-02) and/or official notification payload — **not** Employee, **not** the 03B profile |
| İŞKUR ayrılış kodu | Later İŞKUR |
| İşe giriş “neden” code | **SOURCE DOES NOT PROVIDE** a Personel Card dropdown. Do not invent. Later SGK adapter. |

No entry-code field on OfficialEmploymentProfile.

---

## Create vs edit

Profile may be absent after hire. First save of Resmî bilgiler creates the owned row. Empty codes are valid in 03B. Saving the profile does not hire, transfer, end employment, or submit to SGK.
