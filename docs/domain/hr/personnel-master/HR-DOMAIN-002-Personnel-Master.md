# HR-DOMAIN-002: Personel Master

## Status

**Accepted**

Product Owner + CTO approved reference baseline (2026-08-24). Sprint HR-01A — Personel Master Data Discovery & Freeze. Documentation / domain design only. This record does **not** authorize HR-01B implementation.

**HR-DOMAIN-001 remains Accepted and is not superseded.** This record extends Organization & Workforce Foundation. It does not reopen Employee ≠ User, Position ≠ Permission, PersonnelNumber ≠ PK, Employment ≠ Attendance, or assignment-history rules.

Approved product/domain direction is **not** a validated universal hotel truth. Reference-product observations are labeled as such.

---

## Context

Sprint 0.7B/0.8 delivered a working workforce identity: Organization, Property, Department, Position, Employee, Employment, Assignment, Personnel Number, hire / transfer / end employment, Personel list and detail, TR/EN/RU, permission-driven navigation.

That foundation is intentionally thin: given name, family name, personnel number. Later HR slices (official notifications, documents, leave, compensation, payroll, education, performance, career, discipline, assets, recruitment, employee portal) need a stable **Personel Master** without collapsing those domains into Employee.

A Product Owner–provided WebİK frontend snapshot was used as a **capability reference** (fields, card UX, list/bulk patterns). HuGuWeb does not copy its source, branding, CSS, architecture, or assume that frontend validation is server-side truth.

Current `workforce.read` / `workforce.manage` is too coarse once TCKN, address, bank, and emergency contacts exist. Technical Service and Room Operations already consume Employee references and must not receive an expanded HR profile.

---

## Boundary

**In:** Personel identity/profile foundation; Personel Card information architecture; Personel List behavior; data classification; permission direction; photo model; import/export *rules*; official-data *prerequisites*; narrow profile-history direction.

**Out of this record’s implementation (and out of HR-01B):** payroll, leave, shifts, puantaj, SGK/KBS/İŞKUR adapters, documents/storage, recruitment, performance, DISC, employee portal, DB-managed roles, personas as runtime roles, Technical Service / Room Operations changes.

---

## Decision

1. **Personel Master extends Employee. It does not replace Workforce with an HR monolith.** Keep `Employee → Employment → Assignment`. Do not collapse salary, leave, SGK, documents, or attendance onto Employee.
2. **The Personel Card is a composition surface**, not an aggregate. One large modal may show many tabs. Backend ownership stays split.
3. **Employee core stays small:** `EmployeeId`, `OrganizationId`, `PersonnelNumber`, `GivenName`, `FamilyName`. Additional profile data lives in owned profile records, not as dozens of columns pretending to be “the Employee.”
4. **No second Person aggregate.** Guest, candidate, and staff remain separate bounded contexts. Rehire remains a later new Employment on the same Employee.
5. **National identity is sensitive PII**, never PK, never PersonnelNumber, never login username, never a cross-domain public identifier. Optional where legally/operationally appropriate. Conceptual uniqueness when present: **Organization + Scheme + normalized identifier**. `NationalIdentityScheme` is `Tckn` | `Ykn` | `Passport` | `Other`. Do not build a generic identity platform.
6. **Emergency contacts are a small collection**, not `EmergencyContact1Name` fields.
7. **Grade is deferred.** Do not add it in HR-01B. Position remains sufficient. Career-level structure waits for Career / Compensation. Grade never grants permissions.
8. **Working Group is not a Personel Master concept.** Do not add it. Reference values (Normal / Retired / Disabled / Foreign / Intern) look like employment/legal classifications. Revisit as `EmploymentClassification` in HR-02 / Official / Compensation. Do not equate it with Shift.
9. **No wage in HR-01B.** Current/base wage, net/gross, wage period, and salary history belong to HR-09. Do not create compensation stubs because the reference card shows salary. IBAN + optional BankName is `EmployeePaymentProfile` in **HR-01C**. Branch/account stay deferred.
10. **Photo is metadata + storage object**, not base64 on Employee. Bulk photo later matches **PersonnelNumber** first.
11. **Employee is never physically deleted** because employment ended. Personel Card must not expose normal “Personeli Sil.”
12. **Documents are deferred to HR-04.** Employee does not need attachment metadata now.
13. **Official lifecycle state does not live on Employee.** HR-01B prepares identity/profile only. Parent names, disability, and official codes belong to **HR-03**. SGK/KBS/İŞKUR adapters stay later.
14. **Permissions split:** `workforce.read` remains the operational reference; `hr.employee.*` owns Personel Card; `hr.employee.sensitive.read` owns highly sensitive fields. `hr.employee.sensitive.manage` is later. No DB-managed roles. No personas implemented.
15. **Operational modules keep a minimal `OperationalEmployeeReference`.** Technical Service and Room Operations must not consume HR profile DTOs.
16. **Unsaved-changes guard is an accepted UX invariant** (not implemented in HR-01A).
17. **Excel import/export and bulk photo are accepted capabilities** with authorization and preview rules; they are **HR-01C**, not HR-01B.
18. **Column picker is allowed** but cannot reveal highly sensitive fields without the matching permission. MVP preference storage is **local UI**, not a server preference service.

---

## Key decisions

| Topic | Choice |
|-------|--------|
| Relationship to Workforce | Extend; do not replace |
| Person aggregate | Still no |
| Employee core | Id, OrganizationId, PersonnelNumber, GivenName, FamilyName |
| Profile shape | Owned entities on Employee write model; not four new aggregate roots |
| National identity | Scheme + number; unique when present as Organization + Scheme + normalized id; optional |
| TCKN as PK / sicil / username | Forbidden |
| Grade | Deferred. Not HR-01B. Never grants permissions |
| Working Group | Not added. Revisit as EmploymentClassification later. Not Shift |
| Education level | Optional Personel Master summary in HR-01B; detailed education later |
| Disability | HR-03 official/legal; highly sensitive |
| Parent names | HR-03 Official/Government |
| Blood type | Optional HR-01B; sensitive; not authorization, not default list, not operational APIs |
| Employment dates | Start/End on Employment. OriginalCompanyStartDate / SeniorityStartDate wait for HR-02 |
| SGK dates / flags | Official slice; not Employment status |
| Current wage | Employment compensation terms; not HR-01B |
| Bank / IBAN | Payment profile; highly sensitive; HR-01C to persist |
| Photo | Storage abstraction + metadata; PersonnelNumber match |
| Documents | HR-04 |
| Physical delete | Forbidden as termination |
| Operational DTO | Minimal reference only |
| Import/export / bulk photo | HR-01C |
| First production slice | [FIRST_SLICE.md](FIRST_SLICE.md) HR-01B |

---

## Rejected alternatives

| Alternative | Why |
|-------------|-----|
| Giant Employee record | Mixes lifecycles; explodes every consumer DTO |
| Replace Workforce with HR module | Breaks accepted TS/Room Ops integration and invariants |
| Employee = ApplicationUser | Already rejected; portal password-from-TCKN is a reference anti-pattern |
| TCKN required for every hire | Hotels employ foreign staff; reference product over-constrains |
| TCKN as list default column | Highly sensitive; permission-gated |
| WebİK Kademe as authorization/manager flag | Position/Department must not grant permissions |
| Working Group = Shift | Different meaning in the reference product |
| Cumulative tax / AGİ / BES on Personel Master | Payroll law; not identity |
| Leave balance / FM balance on Employee | Leave / attendance domains |
| Attachment metadata on Employee now | HR-04 |
| Operational APIs returning full Personel Card | Data minimization |
| Generic bulk-update engine | Unsafe; Toplu Zam belongs to compensation |
| Server-side column-preference service in HR-01B | Too much platform for MVP |
| DISC on Personel Card | Out of HuGuWeb scope |

---

## Risks

| Risk | Mitigation |
|------|------------|
| Official specs later require extra identity-card fields | Keep a small official-identity extension point; do not store obsolete booklet fields unless KBS evidence appears |
| HR-01B becomes a second ERP | Cap 01B to card + identity/contact + photo + list + permissions |
| Sensitive data leaks through `workforce.read` | Separate DTOs; freeze operational reference shape |
| Grade/Working Group smuggled onto Employee | Grade deferred; WorkingGroup not added; EmploymentClassification is later |
| Physical delete returns as “cleanup” | Invariant + no delete control on the card |

---

## Date

2026-08-24

---

## Related Documents

- [README.md](README.md)
- [HR-DOMAIN-001](../HR-DOMAIN-001-Organization-Workforce-Foundation.md)
- [FIELD_CATALOG.md](FIELD_CATALOG.md)
- [FIRST_SLICE.md](FIRST_SLICE.md)
