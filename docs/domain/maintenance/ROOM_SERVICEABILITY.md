# Teknik elverişlilik — Room Serviceability

> **Status:** Accepted — Product Owner + CTO approved reference baseline. Sprint 0.11A. Product: **Teknik elverişlilik**. Technical: derived `RoomServiceability`.
>
> This **refines** Accepted Room Operations language (`RoomServiceability`: OOO vs OOS vs available). It does **not** move OOO/OOS into RoomReadiness, and it does **not** merge them with **Bloke**.

## 1. Three different questions

| Question | Owner | Machine |
|----------|-------|---------|
| Oda temizlik/denetim açısından nerede? | Oda Operasyonları | `RoomReadiness`: Dirty / Clean / Inspected / Ready |
| Oda teknik olarak kullanılabilir mi? | Teknik Servis | Derived from open **blocking** issues |
| Oda şu an satılabilir / verilebilir mi? | Derived later | Readiness + serviceability + Bloke + occupancy + other gates |

**Ready ≠ Serviceable ≠ Sellable.**

Accepted Room Operations already forbids a giant `RoomStatus` enum. This domain must not reintroduce one.

Do **not** store Serviceable as a master Room status.

---

## 2. Not every Arıza takes the oda down

| Question | Answer |
|----------|--------|
| Does every open issue make the oda technically unavailable? | **No.** |
| Can the oda stay usable with a minor issue? | **Yes.** (ör. kozmetik boya) |
| Can several issues exist while only one blocks? | **Yes.** |
| May one Room have multiple blocking issues? | **Yes.** Do not enforce a single blocker. |
| After the blocking issue is `Resolved`, what happens? | If **no** other active blocking issue remains → serviceable. Remaining non-blocking issues stay open. |
| Do OOO/OOS belong to Room or to the issue? | **To the blocking issue.** The oda **derives** a current view. |

Do **not** store a third master enum on `Room` that Teknik Servis toggles independently of an Arıza. That creates orphan OOO rooms and fights when two faults exist.

**Evidence:** EXPERT-SUPPLIED WORKFLOW (blocking vs not is an explicit operational question); REFERENCE MODEL (derive current house view).

---

## 3. Blocking

On `MaintenanceIssue`:

| Field | Rule |
|-------|------|
| `BlocksRoomUse` | Boolean. **This** is what makes an issue affect technical availability. |
| `OutageClassification` | Required **iff** `BlocksRoomUse` is true: `OutOfOrder` \| `OutOfService`. Must be null when not blocking. |

**Authority:** Ön Büro / a reporting user **may** initially report that the fault blocks room use. Teknik Servis may validate or change this during handling. For 0.11B, do not implement a separate Front Office domain. The issue owns `BlocksRoomUse`. Do not encode authority using department or Position strings. Authorization remains permission-based.

Clearing `BlocksRoomUse` without resolving is allowed when triage shows the fault does not prevent use. History is required.

`UnableToResolve` does **not** by itself clear blocking. If it blocked, it **keeps blocking** until the issue is `Resolved` or the blocking flag is explicitly changed by a valid business action. Guest movement / room change does **not** resolve the technical fault. Room Change is **not** implemented in 0.11B.

---

## 4. OOO vs OOS

OOO / OOS are classifications **on a blocking `MaintenanceIssue`**.

They are **not**:

- RoomReadiness states
- Room master statuses
- severity
- Blocked / Bloke
- sellability state

| Term | Turkish product | Meaning |
|------|-----------------|---------|
| **Out of Order** | Aynı gün giderilmesi beklenen arıza (OOO) | Same-day repair expected |
| **Out of Service** | Aynı gün giderilemeyecek arıza (OOS) | Not same-day repair expected |

This is **operator judgment**. Do **not** automatically derive it from a datetime or SLA clock.

**Accepted Room Operations:** these are **not** RoomReadiness; they are independent of **Bloke**.

If `BlocksRoomUse` = false: `OutageClassification` **must** be null.  
If `BlocksRoomUse` = true: `OutageClassification` **must** be present.

### What the oda view shows

A Room is technically unavailable when at least one issue exists where `BlocksRoomUse` = true and status is one of `Open` | `InProgress` | `UnableToResolve`.

A Room can remain technically serviceable with non-blocking open issues.

Current technical house display is **derived**:

```text
active blocking issues on this Room
  (status in Open | InProgress | UnableToResolve, BlocksRoomUse = true)

if any active blocking OOS:
    OutOfService
else if any active blocking OOO:
    OutOfOrder
else:
    Serviceable
```

Therefore: **OOS > OOO > Serviceable** for the derived display.

So for **house display**, OOO and OOS are mutually exclusive **labels** (plus Serviceable). They are **not** two independent stored room rows.

Do **not** derive OOO/OOS from an `ExpectedRepairBy` timestamp in 0.11B.

**Evidence:** EXPERT-SUPPLIED WORKFLOW (same-day vs not); ACCEPTED PRODUCT CONTEXT (independence + operator judgment); REFERENCE MODEL (classify the blocking issue, derive the room). Closed by Product Owner + CTO.

---

## 5. Bloke stays out

| Concept | Owner | Meaning |
|---------|-------|---------|
| OOO / OOS | Teknik Servis (on blocking Arıza) | Technical fault; expected duration class |
| **Blocked** / Bloke | Ön Büro later | Payment / contact / operational lock — **not** a defect |

All three can remove sellability. Only the first two are this domain. Do not pull Bloke into Teknik Servis because the rack looks similar.

---

## 6. Ready vs Serviceable vs Sellable

**Frozen:** `RoomReadiness` ≠ Technical Serviceability ≠ Sellability.

Examples (composition, not stored `Sellable`):

| Readiness | Technical | FO Bloke | Occupancy (future) | Implication |
|-----------|-----------|----------|--------------------|-------------|
| Ready | Blocking OOO/OOS | — | vacant | Hazır, **teknik olarak kullanılamaz**, not sellable |
| Dirty | Serviceable | — | vacant | Teknik olarak sorun yok, **Hazır değil**, not sellable |
| Ready | Serviceable | Blocked | vacant | Hazır ve teknik elverişli, **Bloke**, not sellable |
| Ready | Serviceable | — | occupied | Not sellable (already assigned) — Stay later |
| Inspected | Serviceable | — | vacant | Not Ready yet; technically fine |
| Ready | Serviceable | — | vacant | Candidate for sellable **plus** other hotel gates (minibar later, etc.) |

Ready + blocking technical issue → technically unavailable → not sellable.  
Dirty + serviceable → technically okay → not Ready → not sellable.  
Ready + serviceable + Bloke → not sellable.  
Ready + serviceable + vacant + not blocked → sellable **candidate only**.

Sellability is still future composition.

Repair completion ≠ Ready ≠ Sellable.

Checkout / vacated does **not** clear Teknik Servis issues (Accepted Room Operations).

---

## 7. Conceptual consume shape for Room Operations

Room Operations does not persist OOO/OOS in 0.9B and must not own them later. It **consumes** an effect, for example:

```text
RoomServiceability {
  propertyId,
  roomId,
  serviceable,                          // true | false
  outageClassification,                 // null | OutOfOrder | OutOfService
  blockingIssueId                       // optional governing issue
}
```

That is the “OOO vs OOS vs available” view already named in Accepted Room Operations — as a **derived read**, not a TS-written column on `Room`.
