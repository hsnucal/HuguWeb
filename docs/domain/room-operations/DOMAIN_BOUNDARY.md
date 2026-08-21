# Domain Boundary — Oda Operasyonları

> **Status:** Accepted — Product Owner + CTO approved baseline. **Option B** (with a thin Room identity hosted here until a fuller Rooms inventory domain exists).

## The question

Kat Hizmetleri her oda operasyon gerçeğini mi sahiplenir, yoksa **Oda hazırlık (Room Readiness)** daha geniş bir Oda Operasyonları alanı mıdır ve Kat Hizmetleri bir katılımcı mıdır?

Product Owner workflow shows multiple departments acting on the same physical oda:

| Actor | Typical action |
|-------|----------------|
| Ön Büro | Checkout sonrası görünürlük, acil öncelik, Bloke, oda değişikliği |
| Kat Hizmetleri | Temizlik, kat dağıtımı, öncelik uygulaması |
| Supervisor | Fiziksel denetim, kabul/red, red gerekçesi |
| Order Taker | Bugün koordinasyon/Kayıp Eşya teslim (mevcut süreç); HuGuWeb’te sohbet değil kayıtlı iş |
| Minibar | Checkout minibar kontrolü (tesise göre checkout bekletebilir veya bekletmez) |
| Teknik Servis | Arıza, OOO/OOS, tamir sonrası bilgi |
| Gelecek Rezervasyon/Stay | Checkout gerçeği, geliş saati, hazırlık notları |

A single Housekeeping-owned “room status” cannot honestly represent this without absorbing Ön Büro, Minibar, and Teknik Servis.

---

## Option A — Kat Hizmetleri owns Room Readiness and all room operational state

**Shape:** A Housekeeping module owns Dirty/Clean/Inspected/Ready **and** occupancy, block, OOO/OOS, DND, sellability.

| Criterion | Assessment |
|-----------|------------|
| Ownership clarity | Poor. Ön Büro Bloke and Teknik Servis OOO/OOS are not housekeeping facts. |
| Coupling to Reservations | High risk of HK owning stay occupancy. |
| Coupling to Front Office | FO becomes a client of HK for facts FO actually decides (block, sell). |
| Coupling to Maintenance | HK would store technical serviceability. |
| Minibar | Forced inside HK status or ignored. |
| Future mobile | HK app would need to show FO/TS state it does not own. |
| Auditability | Mixed actors written into one HK aggregate. |
| Large/chain later | A giant HK module is hard to split. |
| MVP complexity | Looks simple, becomes a god-module. |
| Giant-module risk | **High.** |

**Reject.** Contradicts “don’t show modules, show work” *and* ADR-001 change isolation. Also contradicts MVP candidate language: Strong HK is **readiness coordination**, not a full platform that owns the hotel.

---

## Option B — Room Operations owns Room Readiness; Kat Hizmetleri participates

**Shape:** A **Room Operations** domain owns:

- minimal **Room** identity (first host)
- current **readiness** lifecycle
- housekeeping **work items** that change readiness
- composition of **sellability** from readiness + consumed facts (documented now; not stored as master status in 0.9B)

Kat Hizmetleri is the **primary participant**, not the owner of all room operational state. Supervisor, Ön Büro, later Minibar and Teknik Servis **participate** through recorded domain actions. They do not each own a copy of “the room status enum.”

| Criterion | Assessment |
|-----------|------------|
| Ownership clarity | Clear: readiness vs work vs consumed FO/TS facts. |
| Coupling to Reservations | Checkout is an incoming fact/contract, not Stay ownership. |
| Coupling to Front Office | FO consumes readiness; FO owns Block later; FO may set business priority. |
| Coupling to Maintenance | TS owns work orders later; Room Operations consumes **serviceability**. |
| Minibar | Independent operational dependency, not a readiness enum value. |
| Future mobile | Each role sees assigned work + room facts; same domain. |
| Auditability | Transitions and inspections are first-class. |
| Large/chain later | Room identity can later move to a richer Rooms inventory without rewriting HK tasks. |
| MVP complexity | First slice can be small (readiness + assignment + inspection). |
| Giant-module risk | **Medium if undisciplined.** Mitigate by keeping Minibar, TS, L&F, generic tasks **out** of the first module internals. |

**Accepted.** Matches hotel workflow, product vision (“which rooms need attention?”), and Workforce pattern (foundation domain, not a department-named module).

---

## Option C — Room core/reference fully separated from Housekeeping operations now

**Shape:** Two domains immediately: **Rooms** (identity, types, inventory) and **Housekeeping** (tasks only). Readiness might live in either.

| Criterion | Assessment |
|-----------|------------|
| Ownership clarity | Clean in a mature PMS. Premature now: we have no Reservations or inventory product yet. |
| Coupling | Lowest long-term coupling **if** both modules exist. |
| MVP complexity | Two modules/projects before any operational proof. ADR-001: modules when approved functionality exists. |
| Giant-module risk | Low later; **process risk now** (empty Rooms module + empty HK module). |

**Reject for Sprint 0.9B.** Keep the *conceptual* split (identity vs work) inside one Room Operations documentation/module **until** a real Rooms/Reservation inventory need appears. Do not create two empty projects.

---

## Option D — Front Office owns room operational state

**Shape:** Ön Büro board is the source of truth; HK only reports.

**Reject.** Physical Clean/Inspected is not an Ön Büro observation. Supervisor inspection is a Kat Hizmetleri management act. FO needs **visibility and some gates** (block, priority conflict, later sell), not ownership of cleanliness.

---

## Accepted ownership

**Option B.**

| Owns now | Does not own |
|----------|----------------|
| Room identity (minimal, first host) | Reservation, Stay, Folio |
| Room readiness lifecycle | Technical work-order lifecycle |
| Housekeeping work items for room cleaning/inspection | Generic hotel task engine |
| Inspection history | Minibar inventory and charging |
| Priority of **this** cleaning work | Attendance, shift, passcards, floor roster |
| Derived sellability rules (documented) | Commercial room inventory / RoomType catalog (later); stored Sellable flag |

**Kat Hizmetleri** is a **primary participant department** (Workforce `Department` data, e.g. name “Kat Hizmetleri”) and a **set of work procedures**. It is **not** the name of the HuGuWeb module.

When implementation is authorized, the module name should be **Room Operations** (`RoomOperations`), not `Housekeeping`.

Future modules/domains may own:

- Stay / Reservations
- Front Office-specific block/sellability decisions
- Technical Service work orders
- Minibar
- generic operational tasks
- Lost & Found

UX may still say **Odalar** / “Hangi odalar dikkat gerektiriyor?” — that is navigation, not module ownership ([UX Architecture](../../design/UX_ARCHITECTURE.md)).
