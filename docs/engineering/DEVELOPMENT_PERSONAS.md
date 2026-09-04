# Development personas

Development/test accounts only. They are **not** hotel Positions, **not** production roles, and they do not define the final authorization administration UX.

Authorization remains **permission claims**. Persona emails exist so developers can sign in to a known permission set. `Employee` is still not `ApplicationUser`. Position names are never checked. Product intent: every **active** seeded employee is identity-capable (`ApplicationUser` + `EmployeeAccountLink`). Operator accounts (`dev@localhost`, `hr.corporate@localhost`) may remain unlinked. See [Employee identity access](../domain/hr/EMPLOYEE_IDENTITY_ACCESS.md).

After permission claims change, **sign out and sign in again**, or wait for the next request: the security stamp is refreshed so the cookie is reissued. AUTH-01 sources claims from database memberships and roles. Persona emails are seed data only — see [security/authorization/DEVELOPMENT_PERSONAS.md](../security/authorization/DEVELOPMENT_PERSONAS.md).

## Credential configuration

Keys only. Do **not** put password values in git.

| Key | Purpose |
| --- | --- |
| `DevelopmentUser:Email` | Broad regression account email (default `dev@localhost`) |
| `DevelopmentUser:Password` | Password for the broad account. Also used for additional personas if the shared key is unset. |
| `DevelopmentUsers:DefaultPassword` | Optional shared password for additional development personas |

Example (local machine only):

```bash
dotnet user-secrets set "DevelopmentUsers:DefaultPassword" "<local-shared-password-meeting-Identity-rules>" --project src/backend/HuGuWeb.Api
```

If those secrets are missing, Development startup skips the affected accounts and logs the key names only.

## Persona matrix

Development emails use the existing `@localhost` convention. Do not invent a second domain.

Shared development password is configured via User Secrets (`DevelopmentUsers:DefaultPassword` / `DevelopmentUser:Password`). Password values are not documented here.

| Name | Email | Employee number | Organization | Property | Department | Position | Persona | Login | Role / scope |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| — | `dev@localhost` | none | Demo Hotel Group | org-wide | — | — | Broad regression operator | Yes | `development-superuser` (all current permissions). No EmployeeAccountLink |
| Ayşe Yılmaz | `hr.manager@localhost` | `DEMO-HR-01` | Demo Hotel Group | Ankara Hotel | İnsan Kaynakları | İK Uzmanı | Ankara HR Manager | Yes | `hr-manager` + `employee-leave-self-service` (Ankara) |
| — | `hr.corporate@localhost` | none | Demo Hotel Group | org-wide | — | — | Corporate HR operator | Yes | `hr-corporate` (organization-wide). No EmployeeAccountLink — not a workforce Employee |
| Deniz Aksoy | `hr.antalya@localhost` | `DEMO-HR-AYT-01` | Demo Hotel Group | Antalya Hotel | İnsan Kaynakları | İK Uzmanı | Antalya HR | Yes | `hr-manager` + `employee-leave-self-service` (Antalya) |
| Zeynep Demir | `roomops.attendant@localhost` | `DEMO-HK-01` | Demo Hotel Group | Ankara Hotel | Kat Hizmetleri | Kat Görevlisi | Housekeeping employee (Room Ops attendant) | Yes | `room-attendant` + `employee-leave-self-service`. Reports to Selin Arslan |
| Elif Şahin | `roomops.inspector@localhost` | `DEMO-HK-INS-01` | Demo Hotel Group | Ankara Hotel | Kat Hizmetleri | Kat Hizmetleri Sorumlusu | Housekeeping inspector | Yes | `room-inspector` + `employee-leave-self-service`. Reports to Selin Arslan |
| Selin Arslan | `roomops.manager@localhost` | `DEMO-HK-MGR-01` | Demo Hotel Group | Ankara Hotel | Kat Hizmetleri | Kat Hizmetleri Sorumlusu | Department manager (HK) | Yes | `room-operations-manager` + `department-leave-approver` + `department-scheduler` + `employee-leave-self-service`; AUTH-02 scope **HK**. Manager via `WorkforceReportingLine`, not Position title |
| Ali Tekin | `maintenance.technician@localhost` | `DEMO-TECH-01` | Demo Hotel Group | Ankara Hotel | Teknik Servis | Teknisyen | Technical employee | Yes | `maintenance-technician` + `employee-leave-self-service`. Reports to Murat Kaya |
| Murat Kaya | `maintenance.manager@localhost` | `DEMO-TECH-MGR-01` | Demo Hotel Group | Ankara Hotel | Teknik Servis | Teknisyen | Department manager (ENG) | Yes | `maintenance-manager` + `department-leave-approver` + `department-scheduler` + `employee-leave-self-service`; AUTH-02 scope **ENG**. Manager via `WorkforceReportingLine`, not Position title |
| Hasan Uçal | `frontoffice.receptionist@localhost` | `DEMO-FO-01` | Demo Hotel Group | Ankara Hotel | Ön Büro | Resepsiyon Görevlisi | Standard employee (self-service only) | Yes | `employee-leave-self-service` only (`hr.leave.request`). No HR admin, Room Ops, or Technical Service |

`hr.specialist@localhost` is **not** added. Workforce has no meaningful İK Uzmanı vs İK Müdürü permission split yet. Accepted direction (not implemented): [Personel Master privacy & permissions](../domain/hr/personnel-master/PRIVACY_AND_PERMISSIONS.md).

Least privilege: Hasan Uçal proves that login does not require HR modules. Zeynep Demir and Ali Tekin also login without HR admin; they keep operational Room Ops / Technical Service permissions for those modules.

## Navigation rules

| Menu | Required claim |
| --- | --- |
| Personel | `hr.employee.read` (directory and Personel Card). Departments / Positions remain available with `workforce.read`. |
| Oda Operasyonları | `room-operations.read` |
| Teknik Servis | `maintenance.read` |
| İzinlerim | `hr.leave.request` |

Ana Sayfa is visible to every authenticated user. Hiding a menu is not authorization; the API still returns 401 unauthenticated and 403 for authenticated users without the required claim.

## Seeding

Development environment only. Existing users are reused. Passwords are not reset on later startups. Declared development permission claims converge to the table above. Unrelated claims are left alone. No Identity schema change. Seed is idempotent: reruns must not duplicate Employees, ApplicationUsers, EmployeeAccountLinks, memberships, role assignments, or DepartmentPositionApplicabilities.
