# Sprint 0.7B — Organization & Workforce Foundation

> Implementation note. Does not redefine Accepted domain documents.

## Module structure

Workforce is the first approved business module. It is not hosted inside `HuGuWeb.Api`.

```text
src/backend/modules/HuGuWeb.Workforce/                 Domain + application use cases
src/backend/modules/HuGuWeb.Workforce.Infrastructure/  EF Core, PostgreSQL mapping, seeding, DI
src/backend/HuGuWeb.Api                                Host: endpoints, auth policies, composition
```

One module project for domain/application keeps EF and ASP.NET out of core behavior. A second project owns persistence. Endpoints stay in the host so CSRF, cookies, and policies remain at the existing Identity boundary.

No BuildingBlocks project. No generic repository. No MediatR/CQRS framework.

## Workplace configuration

Development seeds one Organization and one Property with stable IDs, configured in `Workforce:OrganizationId` and `Workforce:PropertyId`. Runtime use cases load those records; they do not hard-code a universal Property constant through the application.

Department and Position sample names are development seed data only.

## Ownership / scope

| Concept | Scope | Not |
|---------|-------|-----|
| Organization | Employer root | Tenant, Hotel Group |
| Property | Belongs to Organization | Equal to Organization |
| Department | Belongs to Property | Owner of Position |
| Position | Belongs to Property | Owned by Department |
| Employee | Belongs to Organization | ApplicationUser |
| Employment | Belongs to Employee | Attendance |
| Assignment | Belongs to Employment; independently references Department and Position | Current-only FKs on Employee |

Assignment is the only place Department and Position are combined.

## Position uniqueness

Position identity is the technical id. No unique constraint was added on Position name (globally or per Property). Duplicate working names are allowed. There is no accepted business invariant that a title such as "Uzman" must be unique forever.

## Authorization

Permissions `workforce.read` and `workforce.manage` are enforced by ASP.NET policies. The development user is granted both as Identity claims. There is no permission-admin UI. Position and Department names are never used as roles.

Existing development sessions must sign in again after the claims are added.

**Future security/authorization consideration:** permission claims are captured in the authentication cookie. Permission changes may not take effect in an already-issued authentication session until the session is renewed. This sprint does not add Redis, a permission cache, refresh tokens, JWT, custom session infrastructure, or a dynamic permission service.

## Persistence

Workforce tables live in the same PostgreSQL database as Identity (`ConnectionStrings:IdentityDatabase`). Logical ownership is the Workforce module. Identity schema is unchanged.

Personnel number uniqueness is enforced by `IX_Employees_OrganizationId_PersonnelNumber`. Duplicate races return Problem Details `personnel-number-in-use`, not database exceptions.

Migrations are applied explicitly. `EnsureCreated()` is not used. Production does not auto-migrate.

## PostgreSQL integration tests

Deferred. Local PostgreSQL is approved for development runtime, not as an automated test database with a separate CI/test isolation strategy. Domain and use-case tests run in-memory against explicit fakes. The unique index is asserted on the EF model. SQLite and EF InMemory are not used.

## Government integration

Not implemented. No SGK/KBS clients, notification tables, broker, outbox, or worker.
