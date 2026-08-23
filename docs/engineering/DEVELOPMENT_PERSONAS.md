# Development personas

Development/test accounts only. They are **not** hotel Positions, **not** production roles, and they do not define the final authorization administration UX.

Authorization remains **permission claims**. Persona emails exist so developers can sign in to a known permission set. `Employee` is still not `ApplicationUser`. Position names are never checked.

After permission claims change, **sign out and sign in again**. The authentication cookie captures claims.

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

| Email | Purpose | Permissions | Sidebar | Can | Cannot |
| --- | --- | --- | --- | --- | --- |
| `dev@localhost` | Broad regression | `workforce.read`, `workforce.manage`, `room-operations.read`, `room-operations.manage`, `room-operations.inspect` | Ana Sayfa, Oda Operasyonları, Personel | Current Workforce + Room Operations | — |
| `hr.manager@localhost` | İnsan Kaynakları manager | `workforce.read`, `workforce.manage` | Ana Sayfa, Personel | Workforce management | Room Operations (menu hidden; API 403) |
| `roomops.attendant@localhost` | Cleaning work | `room-operations.read`, `room-operations.manage` | Ana Sayfa, Oda Operasyonları | View rooms; needs-cleaning; complete cleaning | Inspect / approve / reject; Workforce |
| `roomops.inspector@localhost` | Inspection | `room-operations.read`, `room-operations.inspect` | Ana Sayfa, Oda Operasyonları | View rooms; accept / reject when Clean | Cleaning manage actions; Workforce |
| `roomops.manager@localhost` | Room Operations regression | `room-operations.read`, `room-operations.manage`, `room-operations.inspect` | Ana Sayfa, Oda Operasyonları | Current Room Operations | Workforce |

`hr.specialist@localhost` is **not** added. Workforce has no meaningful İK Uzmanı vs İK Müdürü permission split yet.

## Navigation rules

| Menu | Required claim |
| --- | --- |
| Personel | `workforce.read` |
| Oda Operasyonları | `room-operations.read` |

Ana Sayfa is visible to every authenticated user. Hiding a menu is not authorization; the API still returns 401 unauthenticated and 403 for authenticated users without the required claim.

## Seeding

Development environment only. Existing users are reused. Passwords are not reset on later startups. Declared development permission claims converge to the table above. Unrelated claims are left alone. No Identity schema change.
