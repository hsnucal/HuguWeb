# Workflow — Arıza coordination

> **Status:** Accepted — Product Owner + CTO approved reference baseline. Sprint 0.11A. HuGuWeb records operational facts. **Not** chat, WhatsApp, or radio replacement infrastructure.

## 1. Report

Misafir often tells **Ön Büro**. Kat Hizmetleri may find the fault while cleaning. Teknik Servis may record what they already see.

Someone with permission creates `MaintenanceIssue`:

- Property + Room
- Category + description
- Priority
- Optional reporter Employee + origin note
- Whether it **blocks room use**; if yes, OOO or OOS

Ön Büro / a reporting user **may** initially report that the fault blocks room use. Teknik Servis may validate or change this during handling. For 0.11B, do not implement a separate Front Office domain.

Creating an Arıza is the system record that today is a phone call plus (sometimes) a notebook.

## 2. Assignment

Determine:

- responsible Employee (not Position name)
- priority (changeable, auditable)
- blocking / OOO vs OOS (changeable, auditable)

Assigned is **data**, not a state. The assignee must have Active employment. First slice may assign any Active Employee because safe technician eligibility does not yet exist.

Much of this is phone today. 0.11B records it. Do **not** build a messenger.

Unassigned `Open` issues are valid. The active work view must show them.

## 3. Intervention

Start work: `Open` → `InProgress`. Assignee required.

No timer/SLA engine. No spare-parts consumption.

## 4. Çözüm

Technician (resolver) sets `Resolved`, required note, required `PreparationImpact`.

- `None` → Room Operations unchanged; RoomReadiness unchanged
- `RequiresPreparation` → Room Operations must ensure room preparation is required; TS does not write Dirty/Clean/Inspected/Ready

If the issue was blocking and no other blocker remains → oda becomes technically serviceable. It does **not** become Ready by this fact alone.

## 5. Çözülememe

`UnableToResolve` + required note.

Teknik Servis informs Ön Büro — in HuGuWeb this is the **status + note**, later a notification trigger, not a chat thread.

If blocking: oda stays technically unavailable. Guest movement / room change does **not** resolve the technical fault. Reservation/Stay/Room Change **does not exist**. Do not implement it. Emit a conceptual fact only (see [CROSS_DEPARTMENT_COORDINATION.md](CROSS_DEPARTMENT_COORDINATION.md)).

Same Arıza later resumes `UnableToResolve` → `InProgress` (for example, parts arrived). Recurrence after `Resolved` is a new Arıza.

## 6. What we do not automate

- Phone/radio as a product
- Guest-facing maintenance portal
- Auto-assign by trade/Position
- Auto-OOO at night because the clock passed “same day”
