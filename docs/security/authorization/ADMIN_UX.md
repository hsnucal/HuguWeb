# Administration UX

> **Status:** Accepted — AUTH-01 (2026-08-25).

## Navigation

Settings is no longer a dead “future” item for users who have administration permissions.

| Menu | Permission | Route |
|------|------------|-------|
| Kullanıcılar | `authorization.users.manage` | `/app/settings/users` |
| Roller ve yetkiler | `authorization.roles.manage` | `/app/settings/roles` |

Users with only one of the two permissions see that item. Display labels come from `authorization` locale files, not raw permission codes.

## Kullanıcılar

List: name/email, linked employee (if any), property, roles, membership status.

Detail/summary: memberships, assigned roles, **effective permissions** (union, as labels grouped by domain).

Actions (this slice): create user with an initial password (Identity hashing, never stored plaintext), create membership, assign/remove role, activate/deactivate membership.

No email invitation infrastructure (not available in product yet).

## Roller ve yetkiler

List: role name, code, scope, active.

Edit: permission **checkbox groups** by domain (İnsan Kaynakları, Oda Operasyonları, Teknik Servis, Yönetim). Codes stay in the payload; the UI shows localized labels.

Customer-created role names are not auto-translated. System template names may show a localized label **in addition to** the stored name.

Roles with assignments cannot be deleted; deactivate instead.

## Personel Card — ERP user

Backend supports: create login, link employee, membership, roles.

AUTH-01 admin Users page can pass an optional employee id. A dedicated Personel Card button “ERP Kullanıcısı Oluştur” is **NEXT_SLICE** if not wired on the card in this slice — the use case is the same.

## Privilege

Only `authorization.users.manage` / `authorization.roles.manage`. Those permissions are on the development superuser template, not on HR Manager.

Granting authorization.* to a hotel role is a deliberate admin action. AUTH-01 does not require the actor to already hold every permission they attach to a role (an IAM admin may not be an HR clerk). The protection is the administration permission itself.
