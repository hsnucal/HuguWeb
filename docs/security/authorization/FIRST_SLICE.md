# AUTH-01 first slice / next slice

> **Status:** Accepted — AUTH-01 (2026-08-25).

## FIRST_SLICE (this sprint)

Authorization platform + localization cleanup. No new hotel business workflow.

### In

- HuGu authorization tables on the Identity context (one migration)
- Effective permissions from membership → roles → RolePermission
- Claim factory + security-stamp revalidation (`ValidationInterval = 1 minute`; admin writes bump stamps; `/api/auth/session` reloads DB snapshot)
- Development personas converted to the same model (seed only), including Ankara / Antalya / corporate HR
- Admin APIs: users, memberships, roles, permission assignment, access summary
- Small Settings UI: Kullanıcılar, Roller ve yetkiler (organization-owned roles; last-admin protection)
- Request workplace from active membership; **no** pilot Property fallback
- Explicit Property cookie + selector (ARCH-01)
- Property-scoped HR host filter; rooms/issues keep property filters
- Optional employee ↔ user link table
- Domain API `.resx` + frontend domain locale composition (tr/en/ru)
- Tests and architecture guards listed in AUTH-01
- Docs in `docs/security/authorization/`

### Out (do not expand)

- HR OfficialEmploymentProfile behavior
- Room Readiness state machine
- Technical Service lifecycle
- Payroll, SGK submission, KBS
- Email invite/SMTP
- Property switcher UI
- Generic event bus / Redis permission cache
- ABAC engine

## NEXT_SLICE

- Excel import through the same use cases ([EXCEL_PROVISIONING](EXCEL_PROVISIONING.md))
- Personel Card “ERP Kullanıcısı Oluştur” if not fully wired
- `hr.employee.sensitive.manage` and HR specialist vs manager deny split (already deferred in Personel Master)

## Product Owner manual test (Hotel X)

1. Create user `hasan@example.com` (or use seeded HR manager).
2. Membership: Hotel X (seeded development property).
3. Role: HR Manager with `workforce.read/manage`, `hr.employee.read/manage/sensitive.read`.
4. Sign in: Personel visible; Oda Operasyonları and Teknik Servis hidden; those APIs 403.
5. Admin removes `hr.employee.manage` from the role. Refresh session (reload or focus refetch): Personel still visible/readable; save/edit denied (403 / buttons hidden).
6. Admin adds `room-operations.read`. After refresh: Oda Operasyonları appears. No code change.
7. Hotel Y: same user without membership. API employee id from the other property/org → 404.

## Excel acceptance (deferred)

Documented in [EXCEL_PROVISIONING](EXCEL_PROVISIONING.md). Not run in AUTH-01.
