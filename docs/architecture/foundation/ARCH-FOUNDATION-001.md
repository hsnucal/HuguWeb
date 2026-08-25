# ARCH-FOUNDATION-001 — Architecture Foundation

> **Status:** Accepted — ARCH-01 (2026-08-25). Product Owner + CTO approved after database and runtime verification with AUTH-01.
> AUTH-01 is committed together with this foundation.

HuGuWeb is one modular monolith, one PostgreSQL database, one schema, **shared tables**. Isolation is `OrganizationId` / `PropertyId` plus membership. This sprint adds **no new business domain**.

Rejected for this stage: microservices, broker, outbox, Redis, MediatR/CQRS framework, generic repository, generic workflow, generic soft-delete, Kubernetes, distributed cache.

## Companion documents

| Document | Topic |
|----------|--------|
| [TENANCY.md](TENANCY.md) | Organization vs Property, no pilot fallback, 100-hotel model |
| [REQUEST_CONTEXT.md](REQUEST_CONTEXT.md) | `ICurrentTenantContext`, `ActorContext` |
| [TIME_AND_TIMEZONE.md](TIME_AND_TIMEZONE.md) | TimeProvider, UTC vs business dates, Property timezone |
| [AUDIT.md](AUDIT.md) | Security admin audit vs domain history |
| [MONEY.md](MONEY.md) | Decimal + ISO 4217 convention (freeze) |
| [DOCUMENT_STORAGE.md](DOCUMENT_STORAGE.md) | Provider-neutral object storage |
| [REFERENCE_DATA.md](REFERENCE_DATA.md) | Enum vs customer ref vs external catalogue |
| [MODULE_CONTRACTS.md](MODULE_CONTRACTS.md) | In-process contracts; no cross-module DbContext |
| [LIFECYCLE.md](LIFECYCLE.md) | Domain-specific lifecycle; no universal `IsDeleted` |
| [API_CONVENTIONS.md](API_CONVENTIONS.md) | Problem Details, pagination, versioning |
| [LOCALIZATION.md](LOCALIZATION.md) | Domain `.resx` / domain `.ts` |
| [SCALING.md](SCALING.md) | Shared tables for 100+ properties |

## Implement now vs freeze only

| Subject | Decision | Why |
|---------|----------|-----|
| Tenant / actor context | **Implement now** | AUTH-01 workplace source was the production invariant |
| No pilot/default/first Property fallback | **Implement now** | Security / correctness |
| Active Property cookie + selector | **Implement now** | Required to exercise Property-scoped domains |
| Role/membership org+scope validation | **Implement now** | Cross-hotel assignment must fail |
| `UNIQUE(OrganizationId, Code)` on roles | **Already in AUTH-01** | Display name is not unique |
| Last-admin protection | **Implement now** | Admin UI exists |
| Authorization refresh (1 min stamp + write bump + session DB) | **Implement now** | Avoid per-request full graph |
| TimeProvider + Property.TimeZoneId | **Implement now** | Current clocks and operational time |
| Domain `.resx` split | **Implement now** | Product Owner domain ownership |
| Problem Details code + correlationId | **Implement now** | Support/debug |
| Architecture tests | **Implement now** | Hardest to misuse later |
| Money value object | **Freeze only** | No payroll; BES decimal fields stay in HR |
| Cloud object storage | **Freeze only** | Local filesystem remains Development |
| Page every tiny list | **Freeze only** | `ListQuery` exists; adopt on large screens |
| API `/v1` | **Freeze only** | One SPA + one backend, pre-public |
| Generic BuildingBlocks project | **Rejected** | Actor/time live in API `Context` |

## Authorization formula

```text
Authenticated User + Active Membership + Active Tenant Scope + Effective Permissions = Authorization
```

Permissions answer **WHAT**. Membership/context answers **WHERE**. Role name, Position, Department, and email are not authorization.

## Product Owner manual tests

1. **Hotel A vs Hotel B isolation** — `hr.manager@localhost` (Ankara) must not see Antalya employee `2002` / Elif Demir. `hr.antalya@localhost` must not see Ankara employees. Direct ID access → 404 `employee-not-found`.
2. **Organization-wide HR** — `hr.corporate@localhost` sees both properties’ HR directory without picking a Property.
3. **Active Property selection** — `dev@localhost` and corporate HR: selector lists Ankara Hotel and Antalya Hotel. Changing Property reloads Room Operations / Technical Service data.
4. **Menus per role** — HR manager: Personel, not Oda Operasyonları. Room attendant: Oda Operasyonları, not Personel.
5. **Role permission change** — Remove `hr.employee.manage` from HR Manager; after reload/focus, edits 403; read may remain.
6. **Membership deactivation** — Deactivate membership; within ~1 minute (or next `/api/auth/session`) permissions empty.
7. **Last-admin protection** — Cannot deactivate the last membership that still supplies `authorization.users.manage` **and** `authorization.roles.manage` in the organization (those two may be split across people). Error `last-administrator`.
8. **Localized API errors** — `property-context-required`, `last-administrator` show TR/EN/RU titles, not raw sentences parsed by the SPA.
9. **TR / EN / RU** — Language selector; Settings and Property selector strings switch.
10. **Existing HR** — Hire, transfer, Personel Card, photo still work at Ankara after selecting Ankara.
11. **Room Operations** — With Ankara selected, Ankara rooms only. Corporate HR without Property: `property-context-required`.
12. **Technical Service** — Same Property rule as Room Operations.

Development accounts (passwords from user secrets): `dev@localhost`, `hr.manager@localhost`, `hr.antalya@localhost`, `hr.corporate@localhost`, plus existing Room Ops / Maintenance personas (Ankara).
