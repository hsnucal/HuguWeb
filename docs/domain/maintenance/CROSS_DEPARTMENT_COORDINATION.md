# Cross-department coordination — Teknik Servis

> **Status:** Accepted — Product Owner + CTO approved reference baseline. Sprint 0.11A. Recorded domain facts, not a messaging platform.

## 1. Style

Prefer: issue status, assignment, blocking classification, resolution note, preparation-impact declaration.

Do **not** design a messenger. Do **not** implement push, SignalR, email, broker, outbox, or workers in 0.11A/0.11B.

Until a notification slice exists, the **web Operations Center / active Teknik Servis view** is the coordination surface.

---

## 2. Oda Operasyonları / Kat Hizmetleri

**TS must not set:** Dirty, Clean, Inspected, Ready.

Conceptual in-process facts (no broker):

```text
MaintenanceIssueResolved {
  propertyId, roomId, issueId,
  preparationImpact,               // None | RequiresPreparation
  occurredAt
}

RoomPreparationImpactIdentified {
  propertyId, roomId, issueId,
  occurredAt
}
```

| Declaration | Room Operations should |
|-------------|------------------------|
| `None` | No readiness change |
| `RequiresPreparation` | Ensure the oda needs hazırlık |

Who decides impact: the **resolving Technical Service operator**. Supervisor physical inspection of *technical* work is not required (Accepted Room Operations S4). Supervisor still inspects *cleaning* if a new Dirty cycle starts. Future Supervisor review/override of `PreparationImpact` may be added later if evidence requires it.

**Consume rule** when `PreparationImpact` = `RequiresPreparation`:

- If the Room is already Dirty with an appropriate active housekeeping work item → **reuse** the existing preparation flow; do **not** create duplicate cleaning work.
- If no preparation cycle/work exists → Room Operations starts the appropriate preparation / needs-cleaning behavior.

Implementation details belong to 0.11B. Use **thin in-process integration only**. Do not create broker/outbox.

Checkout does not clear open Arıza records.

Do **not** reuse `HousekeepingWorkItem` for technical work.

---

## 3. Ön Büro (future)

Front Office needs to know:

- an Arıza exists on the oda
- the oda is technically unavailable, if a blocking issue is open (OOO or OOS label)
- resolved vs çözülemedi
- oda değişikliği **may** be needed

Front Office domain is **not** implemented in 0.11B. Contract only:

```text
TechnicalIssueRaised { propertyId, roomId, issueId, blocksRoomUse, outageClassification, occurredAt }
TechnicalIssueUnableToResolve { propertyId, roomId, issueId, blocksRoomUse, note, occurredAt }
TechnicalRoomChangeMayBeNeeded { propertyId, roomId, issueId, occurredAt }
RoomServiceabilityChanged { propertyId, roomId, serviceable, outageClassification, occurredAt }
```

**Teknik Servis must not own:** guest stay, room assignment, oda değişikliği, reservation, payment Bloke.

Ön Büro / a reporting user **may** create Arıza and may initially report `BlocksRoomUse`. That is participation, not FO-module implementation. Teknik Servis may validate or change blocking during handling. Do not encode that authority with department or Position strings.

---

## 4. Future notifications (triggers only)

| Fact | Typical consumer |
|------|------------------|
| Assigned to Employee | Technician (later mobile/web) |
| Blocking issue `UnableToResolve` | Ön Büro |
| `RequiresPreparation` | Kat Hizmetleri / Supervisor |
| Serviceability restored | Ön Büro |
| Priority raised to Urgent | Assigned Employee, TS management |

No implementation in this sprint.

---

## 5. Future mobile (needs only)

Technician needs: assigned work, oda, description, priority, start, resolve, unable to resolve, preparation impact.

Web-first. Do **not** select a mobile framework. Employee mobile remains [Future Scope](../../product/FUTURE_SCOPE.md).
