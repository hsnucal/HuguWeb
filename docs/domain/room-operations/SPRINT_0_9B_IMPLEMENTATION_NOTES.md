# Sprint 0.9B — Room Operations first production slice

> Implementation note. Does not redefine Accepted domain documents.

## Module structure

```text
src/backend/modules/HuGuWeb.RoomOperations/                 Domain + application use cases
src/backend/modules/HuGuWeb.RoomOperations.Infrastructure/  EF Core, PostgreSQL mapping, seeding, DI
src/backend/HuGuWeb.Api                                     Host: endpoints, auth policies, composition
src/frontend/web/src/room-operations/                       Operations list + room detail
```

No BuildingBlocks project. No generic repository. No MediatR/CQRS framework. No broker, outbox, or worker.

## Implemented model

| Concept | Persistence | Notes |
|---------|-------------|-------|
| `Room` | `Rooms` | Property-scoped identity: `Id`, `PropertyId`, `Number`, `IsActive` |
| `RoomReadiness` | `Rooms.CurrentReadiness` | Dirty / Clean / Inspected / Ready only |
| `HousekeepingWorkItem` | `HousekeepingWorkItems` | Separate from readiness; assignment + priority + lifecycle |
| `RoomReadinessHistoryEntry` | `RoomReadinessHistory` | Business history, not application logs |
| `RoomInspection` | `RoomInspections` | Accept/reject rows are append-only |

No `RoomType`, occupancy, sellability, Blocked, OOO/OOS, DND, Minibar, or Stay columns.

## Initial readiness

Seeded development rooms start **Dirty**, with a readiness-history row (`Seeded`) and **no** work item.

A new room is not Ready. Ready requires the cleaning + inspection path. Dirty-without-work is allowed (W1).

## State / work separation

`RoomReadiness` is current preparation. `HousekeepingWorkItem` is the job to clean that room.

- Dirty can exist with no assignee until **Temizlik Gerekiyor**.
- Completing work moves Dirty → Clean and does not jump to Ready.
- Inspection is a permissioned command, not a work-item state.

## Concurrency / stale work

Each Dirty start (needs-cleaning from a non-Dirty state, or rejection) creates a new `ReadinessCycleId`.

A work item stores that cycle. Completing work requires:

1. work is Open
2. work is the current open item
3. `work.ReadinessCycleId == room.ReadinessCycleId`

Otherwise the API returns `stale-work-item` or `work-item-not-current`. Historical completed rows are not mutated.

`Rooms.ReadinessVersion` is an EF concurrency token.

## Inspection / rework

Accept is one application command that records:

1. `RoomInspection` Accepted
2. history Inspected
3. current readiness Ready
4. history Ready

Inspected remains a real persisted readiness value in history. Current state after accept is Ready.

Reject requires a reason (no photo). The completed work item stays Completed. A **new** Open rework item is created, assigned to the same employee, on a new readiness cycle. Previous inspections remain.

## Priority

`Normal` / `High` / `Urgent` on the work item only, set at creation. Rework copies the previous priority. Priority does not change readiness.

## Employee / Workforce integration

Work stores `AssignedEmployeeId` only. Employee master data stays in Workforce.

Smallest assignability rule: the employee exists in the configured organization **and** has a single current employment that is Active today.

Limitation: Workforce does not expose a safe eligibility contract beyond current employment. Position names such as Kat Görevlisi are **not** checked. Any currently employed person may be assigned. The development user is **not** automatically an Employee.

## Permissions

| Permission | Use |
|------------|-----|
| `room-operations.read` | List/detail (also granted by manage or inspect) |
| `room-operations.manage` | Needs-cleaning, complete-cleaning |
| `room-operations.inspect` | Accept/reject inspection |

Inspection is not inferred from Position. Development user is seeded all three claims. **Existing browser cookies must sign in again** after the claims are added; permission claims live in the authentication cookie.

## API

- `GET /api/room-operations/rooms`
- `GET /api/room-operations/rooms/{id}`
- `GET /api/room-operations/assignable-employees`
- `POST /api/room-operations/rooms/{id}/needs-cleaning`
- `POST /api/room-operations/work-items/{id}/complete-cleaning`
- `POST /api/room-operations/rooms/{id}/inspections`

Mutating endpoints use the existing `ValidateAntiforgeryFilter`. DTOs and Problem Details only.

## Migration

`InitialRoomOperations` (`20260823123000_InitialRoomOperations`).

Creates Room Operations tables only. No Identity or Workforce schema changes. Apply explicitly:

```bash
dotnet ef database update --project src/backend/modules/HuGuWeb.RoomOperations.Infrastructure --startup-project src/backend/HuGuWeb.Api --context RoomOperationsDbContext
```

Production does not auto-migrate.

## UX

Sidebar: **Oda Operasyonları** (not Housekeeping). List answers “what needs preparation attention now?” Detail shows current work, readiness history, and inspection history.

Manual stand-in: **Temizlik Gerekiyor** — not Checkout.

## Deferred

Reservations, Stay, occupancy, RoomType, Minibar, Technical Service, OOO/OOS, Blocked, sellability persistence, DND/No Service, Lost & Found, notifications, mobile, SGK/KBS, broker/outbox.

## Risks

| Risk | Note |
|------|------|
| Assignment is any Active employee | Documented limitation; no Position-name checks |
| Cookie claims | Sign in again after permission seed |
| Ready looks like sellable | UI shows Hazırlık, not Satışa uygun |
