# Excel provisioning (future)

> **Status:** Proposed direction. **Not implemented in AUTH-01.** See [FIRST_SLICE](FIRST_SLICE.md).

## Freeze

Excel is an **import/provisioning** channel.

Runtime authentication **must not** open, query, or cache an XLSX file. Deleting the spreadsheet after a successful import must have **no effect** on login or permissions. The database is authoritative.

Product Owner examples of “ERP reads employees from Excel or DB” mean: **load into DB**, then users authenticate against Identity.

## Future columns (illustrative)

| Column | Use |
|--------|-----|
| Email | Find/create `ApplicationUser` |
| PersonnelNumber | Resolve `Employee` in organization |
| Organization | Resolve organization |
| Property | Resolve property / membership scope |
| RoleCode | Resolve `AuthorizationRole.Code` |
| Active | Membership active flag |

## Rules

1. Validate the file.
2. Call the **same** application use cases as the admin UI (create user, membership, assign role).
3. Do not invent a parallel authorization path.
4. Do not create ERP accounts for every hired employee unless the row explicitly asks for an account.
5. Do not store plaintext passwords in the workbook. If a password column exists later, it is a one-time initial password fed to Identity `CreateAsync` / hasher — still deferred.

## Acceptance (when implemented)

Import a role-assignment row → DB rows created/updated → after login or session refresh, permissions apply → delete the Excel file → access unchanged.
