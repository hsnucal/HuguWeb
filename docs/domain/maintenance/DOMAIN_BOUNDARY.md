# Domain Boundary — Teknik Servis

> **Status:** Accepted — Product Owner + CTO approved reference baseline. Sprint 0.11A.

## The question

Teknik Servis otelin tüm “oda kullanılamaz” gerçeğini mi sahiplenir, yoksa **arıza ve teknik elverişlilik** ayrı bir alan mıdır?

Product Owner workflow shows several departments acting on the same physical oda:

| Actor | Typical action |
|-------|----------------|
| Ön Büro | Misafir şikayetini alır, Teknik Servis kaydı açar / iletir; oda değişikliği; **Bloke** |
| Kat Hizmetleri | Temizlik sırasında arıza tespit eder |
| Teknik Servis | Arızayı alır, öncelik / atama, müdahale, çözüm veya çözülemedi |
| Supervisor (Kat Hizmetleri) | Tamir hazırlığı etkilediyse yeniden temizlik / denetim |
| Gelecek Rezervasyon / Stay | Oda değişikliği, doluluk; **yok** |

A housekeeping-owned status cannot own this. A Front Office-owned “blocked room” cannot own a plumbing fault. A CMMS cannot own oda hazırlık.

---

## What this domain is

Hotel **reactive technical coordination**:

- an Arıza exists
- it may or may not make the oda technically unusable
- someone is responsible for the work
- the work ends in çözüm or çözülemedi
- other domains consume those facts

It is **not** generic enterprise maintenance, asset management, purchasing, or a chat product.

---

## Ownership

| Owns (conceptual domain) | Does not own |
|--------------------------|----------------|
| `MaintenanceIssue` (Arıza) lifecycle | `Room` identity (Room Operations hosts it) |
| Category reference data for issues | `RoomReadiness` (Dirty/Clean/Inspected/Ready) |
| Priority of **this** issue | `HousekeepingWorkItem` |
| Assignment to `EmployeeId` | `Employee` / `Department` / `Position` master data |
| Blocking impact + OOO/OOS classification of a blocking issue | Ön Büro **Bloke** |
| Derived room technical serviceability | Stay, Reservation, folio, oda değişikliği |
| Resolution / unable-to-resolve + note | Sellability as a stored master status |
| Preparation-impact **declaration** (repair affected hazırlık or not) | Applying Dirty/Clean/Inspected/Ready |
| Issue business history | Generic audit infrastructure, notifications, chat |
| Repair result / history | Asset-management platform, preventive maintenance, parts / inventory |

**Kat Hizmetleri** and **Ön Büro** are **participants** (they report and consume). They do not own the Arıza aggregate.

When implementation is authorized, the module name should be **Technical Service** (`TechnicalService`), product language **Teknik Servis**. Do **not** name it Housekeeping. Do **not** name it a generic `OperationalTask` module.

---

## Rejected shapes

| Shape | Why reject |
|-------|------------|
| Fold Teknik Servis into Room Operations / `HousekeepingWorkItem` | Repair is not cleaning. Accepted Room Operations already refused this. |
| Generic `OperationalTask<T>` / workflow engine | Two implemented domains have not proved the concept identical. ADR/engineering: prefer duplication over premature abstraction. |
| Full CMMS (assets, PM, spare parts, vendors, SLA, cost) | No evidence; [MVP Candidates](../../product/MVP_CANDIDATES.md) warned about IFS-like expansion. |
| FO owns OOO/OOS because they affect availability | Availability has several independent causes. **Bloke** is FO; technical outage is TS. |
| RoomReadiness gains OutOfOrder / OutOfService | Contradicts **Accepted** Room Operations. |
| Separate `MaintenanceWorkOrder` in 0.11B | No evidence of multiple distinct executions for one issue. “İş Emri” may remain UI language around the same Arıza. |

---

## Future modules that consume or complement

| Future domain | Relationship |
|---------------|----------------|
| **Oda Operasyonları** | Consumes derived serviceability; may start a new hazırlık cycle when repair dirties the oda |
| **Ön Büro / Stay** | Consumes blocking / unable-to-resolve; owns oda değişikliği, Bloke, occupancy |
| **Inventory / Purchasing** | Later parts consumption — not referenced now |
| **Workforce** | Already exists; assignment target only |

---

## Location and assets

First slice is **oda-only** (`RoomId` required). Common areas, equipment registry, and a generic Location aggregate are **out**. See [FIRST_SLICE.md](FIRST_SLICE.md) and [ISSUE_MODEL.md](ISSUE_MODEL.md#room-vs-common-area).

This does not freeze “Teknik Servis = only rooms.” It keeps 0.11B from becoming facility management.
